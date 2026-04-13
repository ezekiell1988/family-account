#!/usr/bin/env pwsh
# ============================================================================
#  BAC Financiamientos (Tasa Cero) — carga y verificación
#
#  Lee los XLS de "Consulta de Financiamientos" de BAC Credomatic,
#  reconstruye la tabla de amortización para cada plan y la upsertea
#  directamente en BD (SQL MERGE). Crea la FinancialObligation vía API
#  si todavía no existe.
#
#  Flujo:
#    1.  Verificar tablas en BD
#    2.  Parsear los dos XLS con Python/xlrd (produce JSON)
#    3.  Buscar cuentas bancarias de las tarjetas en BD (idBankAccount)
#    4.  Login
#    5.  Para cada plan de financiamiento:
#          a. Buscar obligación existente (por matchKeyword)
#          b. Crear si no existe (POST /financial-obligations)
#          c. SQL MERGE de cuotas reconstruidas
#    6.  Resumen: cuotas por obligación
#
#  Uso:
#    pwsh docs/operaciones-bancarias/BAC-TASA-CERO-sync-test.ps1
#
#  Requisitos:
#    - python3 + xlrd instalado   (pip install xlrd)
#    - sqlcmd instalado
#    - API corriendo en $HOST_URL
#    - credentials/db.txt en la raíz del proyecto
#    - Los dos XLS en la carpeta bancos/
#
#  ⚠ PENDIENTE ANTES DE EJECUTAR:
#    1. Crear cuentas contables por tarjeta (migración AddBacCreditCardObligationAccounts)
#       y actualizar $ID_ACCOUNT_* abajo según los IDs asignados.
#    2. Verificar que las tarjetas existan como bankAccount en BD;
#       si no, crearlas primero desde el módulo de cuentas bancarias.
#    3. Relajar validación InterestRate min=0 en CreateFinancialObligationRequest.cs
#       (actualmente min=0.01 — el script usa 0.01 como workaround temporal).
# ============================================================================

$SCRIPT_DIR = $PSScriptRoot
$REPO_ROOT  = (Resolve-Path (Join-Path $SCRIPT_DIR "../..")).Path
$CREDS_FILE = Join-Path $REPO_ROOT "credentials/db.txt"
$HOST_URL   = "https://localhost:8000/api/v1"
$EMAIL      = "ezekiell1988@hotmail.com"
$PIN        = "12345"

# Archivos XLS por tarjeta
$XLS_FILES = @(
    @{ Path = (Join-Path $REPO_ROOT "bancos/BAC-5466-37XX-XXXX-8608-202603-Financiamientos.xls"); CardSuffix = "8608" }
    @{ Path = (Join-Path $REPO_ROOT "bancos/BAC-5491-94XX-XXXX-6515-202603-Financiamientos.xls"); CardSuffix = "6515" }
)

# ─── IDs de cuentas contables ────────────────────────────────────────────────
# TODO: actualizar con los IDs reales una vez creada la migración
# AddBacCreditCardObligationAccounts.
#
# Mientras tanto el script intenta detectarlos desde BD (busca cuentas hijas de
# 2.1.01 que contengan el sufijo de la tarjeta).  Si no los encuentra, solicita
# confirmación al usuario antes de continuar.
#
# 28 = 2.1.01  BAC Credomatic – Tarjetas (agrupador)   ← fallback si no hay específica
# InterestRate = 0.01 workaround (Tasa cero = 0% pero API requiere > 0)
$INTEREST_RATE_PLACEHOLDER = "0.01"   # ← cambiar a 0 cuando se relaje la validación

$script:HTTP_STATUS   = 0
$script:LAST_RESPONSE = $null

# ── Helpers ───────────────────────────────────────────────────────────────────
function Write-Step($msg) {
    Write-Host ""
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "▶  $msg" -ForegroundColor Cyan
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
}
function Write-Ok($msg)   { Write-Host "  ✔  $msg" -ForegroundColor Green }
function Write-Info($msg) { Write-Host "  ·  $msg" }
function Write-Warn($msg) { Write-Host "  ⚠  $msg" -ForegroundColor Yellow }

function Invoke-Fail($msg) {
    Write-Host ""
    Write-Host "  ✗  FALLO: $msg" -ForegroundColor Red
    if ($script:LAST_RESPONSE) {
        $script:LAST_RESPONSE | ConvertTo-Json -Depth 5 | Write-Host
    }
    exit 1
}

function Invoke-Api {
    param([string]$Method, [string]$Path, [string]$Body = "", [string]$Token = "")
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    $params = @{
        Method               = $Method
        Uri                  = "$HOST_URL$Path"
        Headers              = $headers
        SkipCertificateCheck = $true
    }
    if ($Body) { $params["Body"] = [System.Text.Encoding]::UTF8.GetBytes($Body) }
    try {
        $response = Invoke-WebRequest @params -ErrorAction Stop
        $script:HTTP_STATUS   = [int]$response.StatusCode
        $script:LAST_RESPONSE = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
        return $script:LAST_RESPONSE
    }
    catch {
        $errResp = $_.Exception.Response
        if ($errResp) {
            $script:HTTP_STATUS = [int]$errResp.StatusCode
            try {
                $stream  = $errResp.GetResponseStream()
                $reader  = New-Object System.IO.StreamReader($stream)
                $content = $reader.ReadToEnd()
                $script:LAST_RESPONSE = $content | ConvertFrom-Json -ErrorAction SilentlyContinue
            } catch { $script:LAST_RESPONSE = $null }
        } else {
            $script:HTTP_STATUS   = 0
            $script:LAST_RESPONSE = $null
        }
        return $null
    }
}

function Assert-Status($expected, $context) {
    if ($script:HTTP_STATUS -ne $expected) {
        Invoke-Fail "$context devolvió HTTP $($script:HTTP_STATUS) (esperado $expected)"
    }
}

function Invoke-Sql($query) {
    $result = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $query 2>&1
    return $result
}

# ── Leer credenciales ─────────────────────────────────────────────────────────
if (-not (Test-Path $CREDS_FILE)) { Invoke-Fail "No se encontró $CREDS_FILE" }
$rawCreds = Get-Content $CREDS_FILE
$DB_HOST  = (($rawCreds | Where-Object { $_ -match '^HOST:' })     -split '\s+')[1]
$DB_PORT  = (($rawCreds | Where-Object { $_ -match '^PORT:' })     -split '\s+')[1]
$DB_USER  = (($rawCreds | Where-Object { $_ -match '^USER:' })     -split '\s+')[1]
$DB_PASS  = (($rawCreds | Where-Object { $_ -match '^PASSWORD:' }) -split '\s+')[1]

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   BAC Financiamientos (Tasa Cero) — Sync y carga    ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Info "API   : $HOST_URL"
Write-Info "BD    : ${DB_HOST}:${DB_PORT} / dbfa"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 1 — Verificar tablas en BD"

$sqlTables = @"
SET NOCOUNT ON;
SELECT t.name AS tabla, SUM(p.rows) AS filas
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1)
WHERE t.name IN ('financialObligation','financialObligationInstallment','financialObligationPayment')
GROUP BY t.name ORDER BY t.name;
"@
$tablesResult = Invoke-Sql $sqlTables
Write-Ok "Tablas encontradas:"
$tablesResult | Where-Object { $_ -match '\w' -and $_ -notmatch '^-' -and $_ -notmatch 'filas' } |
    ForEach-Object { Write-Info "  $_" }

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 2 — Parsear XLS con Python"

# Verificar xlrd disponible
$xlrdCheck = python3 -c "import xlrd; print('ok')" 2>&1
if ($xlrdCheck -ne 'ok') {
    Write-Warn "xlrd no instalado. Instalando..."
    pip install xlrd --quiet
}

# Construir la lista de archivos como JSON para pasar al script Python
$fileList = ($XLS_FILES | ForEach-Object {
    $p = $_.Path -replace '\\','\\'
    $c = $_.CardSuffix
    "`"$p|$c`""
}) -join ","

$pyScript = @"
import json, xlrd, sys

entries = [$fileList]

obligations = []
for entry in entries:
    path, card_suffix = entry.split('|', 1)
    try:
        wb = xlrd.open_workbook(path)
    except Exception as e:
        print(json.dumps({'error': str(e), 'file': path}), file=sys.stderr)
        continue

    sh = wb.sheets()[0]
    # Filas 0-6: cabeceras/metadatos; datos desde fila 7
    for r in range(7, sh.nrows):
        row = [sh.cell(r, c).value for c in range(7)]
        fecha_val  = str(row[1]).strip()
        concepto   = str(row[2]).strip()
        cuotas_str = str(row[3]).strip()   # "009/012"
        monto_str  = str(row[4]).strip()   # "29,472.00 CRC"
        saldo_ini  = str(row[5]).strip()   # "353,664.79 CRC"
        saldo_fal  = str(row[6]).strip()   # "88,416.79 CRC"

        # Ignorar filas de totales o vacías
        if not cuotas_str or '/' not in cuotas_str: continue
        if concepto.lower() == 'total': continue
        if not fecha_val or fecha_val == '': continue

        # Parsear cuotas: "009/012" → (9, 12)
        parts = cuotas_str.split('/')
        cuota_actual = int(parts[0])
        cuota_total  = int(parts[1])

        # Parsear monto + moneda: "29,472.00 CRC" → (29472.00, 'CRC')
        def parse_amount(s):
            moneda = 'CRC' if 'CRC' in s else 'USD'
            num_str = s.replace('CRC','').replace('USD','').replace('\xa0','').replace(' ','').replace(',','')
            return float(num_str), moneda

        monto_cuota,   moneda = parse_amount(monto_str)
        saldo_inicial, _      = parse_amount(saldo_ini)
        saldo_faltante, _     = parse_amount(saldo_fal)

        # Parsear fecha: puede ser string "15/07/2025" o número serial
        if '/' in fecha_val:
            d, m, y = fecha_val.split('/')
            start_date = f"{y}-{m.zfill(2)}-{d.zfill(2)}"
        else:
            try:
                import datetime
                dt = datetime.datetime(1899, 12, 30) + datetime.timedelta(days=float(fecha_val))
                start_date = dt.strftime('%Y-%m-%d')
            except:
                start_date = fecha_val

        obligations.append({
            "cardSuffix":    card_suffix,
            "concepto":      concepto,
            "startDate":     start_date,
            "termMonths":    cuota_total,
            "cuotaActual":   cuota_actual,
            "montoCuota":    round(monto_cuota,  2),
            "originalAmount":round(saldo_inicial, 2),
            "saldoFaltante": round(saldo_faltante,2),
            "moneda":        moneda,
        })

print(json.dumps(obligations))
"@

$pyOutput = python3 -c $pyScript 2>&1
if ($LASTEXITCODE -ne 0) { Invoke-Fail "Error al parsear XLS: $pyOutput" }

try {
    $financiamientos = $pyOutput | ConvertFrom-Json
} catch {
    Invoke-Fail "No se pudo deserializar el JSON del parser: $pyOutput"
}

Write-Ok "Planes de financiamiento encontrados: $($financiamientos.Count)"
$financiamientos | ForEach-Object {
    Write-Info "  [$($_.cardSuffix)]  $($_.concepto)  $($_.cuotaActual)/$($_.termMonths)  $($_.moneda) $($_.montoCuota)/cuota  Saldo: $($_.saldoFaltante)"
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 3 — Buscar cuentas bancarias (bankAccount) de las tarjetas"

# Sufijos de tarjeta para query
$cardSuffixes = ($XLS_FILES | ForEach-Object { "'%$($_.CardSuffix)%'" }) -join ","

# Búsqueda por codeBankAccount (contiene el sufijo de tarjeta, ej. 'BAC-CC-MC-8608-CRC')
# Usa delimitador | para evitar problemas de parsing con columnas angostas de sqlcmd
$sqlBankAccounts = @"
SET NOCOUNT ON;
SELECT
    CAST(ba.idBankAccount AS varchar) + '|' +
    ba.codeBankAccount              + '|' +
    CAST(ba.idAccount    AS varchar) + '|' +
    c.codeCurrency                  + '|' +
    CAST(ba.idCurrency   AS varchar)
FROM dbo.bankAccount ba
JOIN dbo.currency c ON c.idCurrency = ba.idCurrency
WHERE ba.codeBankAccount LIKE '%8608%'
   OR ba.codeBankAccount LIKE '%6515%'
ORDER BY ba.idBankAccount;
"@
$bankAccResults = Invoke-Sql $sqlBankAccounts

# Construir mapa  "suffix-MONEDA" → { idBankAccount, idAccount, idCurrency }
# Ej: '8608-CRC' → @{ idBankAccount=5; idAccount=31; idCurrency=1 }
$cardAccountMap = @{}
$bankAccResults | Where-Object { $_ -match '\d\|' } |
    ForEach-Object {
        $cols = ($_.Trim() -split '\|')
        if ($cols.Count -eq 5) {
            $code   = $cols[1]   # ej. 'BAC-CC-MC-8608-CRC'
            $suffix = if ($code -match '(\d{4})-(CRC|USD)') { "$($matches[1])-$($matches[2])" } else { '' }
            if ($suffix) {
                $cardAccountMap[$suffix] = @{
                    idBankAccount = [int]$cols[0]
                    idAccount     = [int]$cols[2]
                    idCurrency    = [int]$cols[4]
                }
            }
        }
    }

if ($cardAccountMap.Count -gt 0) {
    Write-Ok "Cuentas bancarias encontradas:"
    $cardAccountMap.GetEnumerator() | ForEach-Object {
        Write-Info "  TC-$($_.Key) → idBankAccount=$($_.Value.idBankAccount)  idAccount=$($_.Value.idAccount)"
    }
} else {
    Write-Warn "No se encontraron bankAccount para las tarjetas 8608 / 6515 en BD."
    Write-Warn "Crea las cuentas bancarias de las TC en el módulo antes de ejecutar este script."
    Write-Warn "Continuando sin IdBankAccountPayment (se podrá actualizar luego)."
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 4 — Login"

$sqlPin = "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser=1 AND pin='$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');"
Invoke-Sql $sqlPin *>$null
Write-Ok "PIN '$PIN' insertado"

$resp = Invoke-Api POST "/auth/login" "{`"emailUser`":`"$EMAIL`",`"pin`":`"$PIN`"}"
Assert-Status 200 "login"
$TOKEN = $resp.accessToken
if (-not $TOKEN -or $TOKEN -eq "null") { Invoke-Fail "No se obtuvo accessToken" }
Write-Ok "Token obtenido"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 5 — Buscar cuentas contables por tarjeta (cuentas hijas de 2.1.01)"

# Intenta encontrar cuentas específicas por tarjeta (ej. "2.1.01.01" con sufijo en nombre)
# Si no existen, usa cuenta agrupadora 28 (2.1.01) como fallback con advertencia.
$sqlAccTC = @"
SET NOCOUNT ON;
SELECT idAccount, codeAccount, nameAccount, allowsMovements, isActive
FROM dbo.account
WHERE (nameAccount LIKE '%8608%' OR nameAccount LIKE '%6515%'
       OR nameAccount LIKE '%Tasa Cero%' OR nameAccount LIKE '%BAC TC%')
  AND isActive = 1
ORDER BY codeAccount;
"@
$accTcResult = Invoke-Sql $sqlAccTC
$specificAccounts = $accTcResult | Where-Object { $_ -match '^\s*\d+' }

if ($specificAccounts) {
    Write-Ok "Cuentas contables específicas para TC encontradas:"
    $specificAccounts | ForEach-Object { Write-Info "  $_" }
} else {
    Write-Warn "No se encontraron cuentas contables específicas para BAC TC."
    Write-Warn "Se usará la cuenta agrupadora 28 (2.1.01) como IdAccountLongTerm."
    Write-Warn "Crea la migración AddBacCreditCardObligationAccounts para separar por tarjeta."
}

# Función helper: resolver idAccount usando el mapa suffix+moneda ya construido
# $suffix = '8608' | '6515',  $moneda = 'CRC' | 'USD'
function Get-AccountForCard($suffix, $moneda) {
    $key = "$suffix-$moneda"
    if ($cardAccountMap.ContainsKey($key)) {
        return $cardAccountMap[$key].idAccount
    }
    # Fallback: buscar en BD por codeBankAccount
    $sqlFind = @"
SET NOCOUNT ON;
SELECT TOP 1 ba.idAccount
FROM dbo.bankAccount ba
WHERE ba.codeBankAccount LIKE '%-$suffix-$moneda'
  AND ba.isActive = 1;
"@
    $row = Invoke-Sql $sqlFind | Where-Object { $_ -match '^\s*\d+\s*$' } | Select-Object -First 1
    if ($row) { return [int]($row.Trim()) }
    # Último fallback: agrupadora 28
    Write-Warn "No se encontró cuenta específica para $key — usando agrupadora 28"
    return 28
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 6 — Crear/verificar cada obligación y cargar cuotas"

$CURRENCY_CRC = 1
$CURRENCY_USD = 2

$obligacionesCreadas    = 0
$obligacionesExistentes = 0
$cuotasTotalesUpserted  = 0

# Obtener lista actual de obligaciones
$existingObligations = Invoke-Api GET "/financial-obligations/data.json" "" $TOKEN
Assert-Status 200 "GET financial-obligations"

foreach ($plan in $financiamientos) {
    $suffix     = $plan.cardSuffix
    $concepto   = $plan.concepto

    # matchKeyword único: concepto + sufijo tarjeta + fecha inicio (evita colisión de dos
    # "TRASLADO SALDO REVOLUTIVO" en la misma tarjeta)
    $keyword = "$($concepto.Substring(0, [Math]::Min(20,$concepto.Length)).Trim())-$suffix-$($plan.startDate.Substring(0,7))"

    Write-Host ""
    Write-Host "  ── Plan: $concepto  [$suffix]  $($plan.cuotaActual)/$($plan.termMonths)  $($plan.moneda) $($plan.montoCuota)" -ForegroundColor White

    # Resolución de IDs
    $idCurrency    = if ($plan.moneda -eq 'USD') { $CURRENCY_USD } else { $CURRENCY_CRC }
    $idAccLong     = Get-AccountForCard $suffix $plan.moneda
    $idAccShort    = $idAccLong   # simplificado — refinar cuando se separe corriente/no corriente
    $idAccInterest = $idAccLong   # Tasa cero: placeholder, nunca genera asiento

    $bankAccData   = $cardAccountMap["$suffix-$($plan.moneda)"]
    $idBankAccPay  = if ($bankAccData) { $bankAccData.idBankAccount } else { $null }

    # ── a) Buscar obligación existente ────────────────────────────────────────
    $obligation = $existingObligations |
        Where-Object { $_.matchKeyword -eq $keyword } |
        Select-Object -First 1

    if ($null -eq $obligation) {
        Write-Info "  No existe — creando..."

        $nameObligation = "BAC TC $suffix — $($concepto.Substring(0,[Math]::Min(30,$concepto.Length)).Trim())"

        $createBody = @{
            nameObligation       = $nameObligation
            idCurrency           = $idCurrency
            originalAmount       = $plan.originalAmount
            interestRate         = [double]$INTEREST_RATE_PLACEHOLDER
            startDate            = $plan.startDate
            termMonths           = $plan.termMonths
            idAccountLongTerm    = $idAccLong
            idAccountShortTerm   = $idAccShort
            idAccountInterest    = $idAccInterest
            idAccountLateFee     = $null
            idAccountOther       = $null
            matchKeyword         = $keyword
            notes                = "Tasa cero BAC. Cuotas reconstituidas desde XLS de Financiamientos. InterestRate=0.01 workaround."
        }
        if ($idBankAccPay) { $createBody["idBankAccountPayment"] = $idBankAccPay }

        $createJson = $createBody | ConvertTo-Json -Compress

        $obligation = Invoke-Api POST "/financial-obligations" $createJson $TOKEN
        if ($script:HTTP_STATUS -eq 201) {
            Write-Ok "Obligación creada  id=$($obligation.idFinancialObligation)  ($nameObligation)"
            $obligacionesCreadas++
            # Refrescar lista para siguientes iteraciones
            $existingObligations = Invoke-Api GET "/financial-obligations/data.json" "" $TOKEN
        } else {
            Write-Warn "No se pudo crear la obligación (HTTP $($script:HTTP_STATUS)) — saltando cuotas."
            if ($script:LAST_RESPONSE) { $script:LAST_RESPONSE | ConvertTo-Json -Depth 3 | Write-Host }
            continue
        }
    } else {
        Write-Ok "Obligación existente  id=$($obligation.idFinancialObligation)"
        $obligacionesExistentes++
    }

    $OBL_ID      = $obligation.idFinancialObligation
    $termMonths  = $plan.termMonths
    $cuotaActual = $plan.cuotaActual
    $montoCuota  = $plan.montoCuota
    $origAmount  = $plan.originalAmount
    $startDate   = [datetime]::ParseExact($plan.startDate, "yyyy-MM-dd", $null)

    # ── b) Reconstruir tabla de cuotas e insertar con SQL MERGE ───────────────
    Write-Info "  Generando tabla de $termMonths cuotas (SQL MERGE)..."

    # Generar VALUES para el MERGE
    $valuesRows = @()
    for ($n = 1; $n -le $termMonths; $n++) {
        $dueDate    = $startDate.AddMonths($n).ToString("yyyy-MM-dd")
        $balance    = [Math]::Max(0, [Math]::Round($origAmount - $n * $montoCuota, 2))
        $status     = if ($n -lt $cuotaActual)  { "Pagada" }
                      elseif ($n -eq $cuotaActual) { "Vigente" }
                      else                         { "Pendiente" }

        $valuesRows += "($n, '$dueDate', $balance, $montoCuota, 0, 0, 0, $montoCuota, '$status', GETUTCDATE())"
    }
    $valuesBlock = $valuesRows -join ",`n"

    $sqlMerge = @"
SET NOCOUNT ON;
MERGE dbo.financialObligationInstallment AS tgt
USING (
    VALUES $valuesBlock
) AS src (
    numberInstallment, dueDate, balanceAfter,
    amountCapital, amountInterest, amountLateFee, amountOther, amountTotal,
    statusInstallment, syncedAt
)
ON  tgt.idFinancialObligation = $OBL_ID
AND tgt.numberInstallment     = src.numberInstallment
WHEN MATCHED THEN UPDATE SET
    dueDate           = src.dueDate,
    balanceAfter      = src.balanceAfter,
    amountCapital     = src.amountCapital,
    amountInterest    = src.amountInterest,
    amountLateFee     = src.amountLateFee,
    amountOther       = src.amountOther,
    amountTotal       = src.amountTotal,
    statusInstallment = src.statusInstallment,
    syncedAt          = src.syncedAt
WHEN NOT MATCHED THEN INSERT (
    idFinancialObligation, numberInstallment, dueDate, balanceAfter,
    amountCapital, amountInterest, amountLateFee, amountOther, amountTotal,
    statusInstallment, syncedAt
) VALUES (
    $OBL_ID, src.numberInstallment, src.dueDate, src.balanceAfter,
    src.amountCapital, src.amountInterest, src.amountLateFee, src.amountOther, src.amountTotal,
    src.statusInstallment, src.syncedAt
);
SELECT @@ROWCOUNT AS cuotasAfectadas;
"@
    $mergeResult = Invoke-Sql $sqlMerge
    $rowsAffected = $mergeResult | Where-Object { $_ -match '^\s*\d+\s*$' } | Select-Object -First 1
    if ($rowsAffected) {
        $cuotasTotalesUpserted += [int]($rowsAffected.Trim())
        Write-Ok "MERGE completado  cuotas afectadas=$($rowsAffected.Trim())"
    } else {
        Write-Warn "MERGE ejecutado, no se pudo leer rowcount."
        Write-Info ($mergeResult -join "`n")
    }
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 7 — Resumen de obligaciones cargadas"

$sqlResumen = @"
SET NOCOUNT ON;
SELECT
    o.idFinancialObligation     AS id,
    o.nameObligation            AS nombre,
    o.originalAmount            AS montoOriginal,
    cu.codeCurrency             AS moneda,
    o.termMonths                AS plazo,
    o.statusObligation          AS estado,
    COUNT(i.idFinancialObligationInstallment)    AS totalCuotas,
    SUM(CASE WHEN i.statusInstallment='Pagada'   THEN 1 ELSE 0 END) AS pagadas,
    SUM(CASE WHEN i.statusInstallment='Vigente'  THEN 1 ELSE 0 END) AS vigentes,
    SUM(CASE WHEN i.statusInstallment='Pendiente'THEN 1 ELSE 0 END) AS pendientes,
    MIN(CASE WHEN i.statusInstallment IN ('Vigente','Pendiente') THEN i.dueDate END) AS proximaFecha,
    MIN(CASE WHEN i.statusInstallment IN ('Vigente','Pendiente') THEN i.amountTotal END) AS proximaCuota
FROM dbo.financialObligation o
LEFT JOIN dbo.financialObligationInstallment i
    ON i.idFinancialObligation = o.idFinancialObligation
JOIN dbo.currency cu ON cu.idCurrency = o.idCurrency
WHERE o.matchKeyword LIKE '%-8608-%' OR o.matchKeyword LIKE '%-6515-%'
GROUP BY o.idFinancialObligation, o.nameObligation, o.originalAmount,
         cu.codeCurrency, o.termMonths, o.statusObligation
ORDER BY o.idFinancialObligation;
"@

Write-Ok "Obligaciones Tasa Cero en BD:"
Invoke-Sql $sqlResumen | ForEach-Object { Write-Info "  $_" }

Write-Host ""
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  RESUMEN FINAL" -ForegroundColor Green
Write-Host "    Planes parseados            : $($financiamientos.Count)" -ForegroundColor Green
Write-Host "    Obligaciones creadas        : $obligacionesCreadas"       -ForegroundColor Green
Write-Host "    Obligaciones ya existentes  : $obligacionesExistentes"    -ForegroundColor Green
Write-Host "    Cuotas upserted (BD)        : $cuotasTotalesUpserted"     -ForegroundColor Green
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Warn "Pendientes para el ciclo completo:"
Write-Warn "  · Crear migración AddBacCreditCardObligationAccounts (cuentas 2.1.01.XX)"
Write-Warn "  · Actualizar matchKeyword en 01a-keywords.md (Templates 4 y 5)"
Write-Warn "  · Relajar InterestRate min=0 en CreateFinancialObligationRequest.cs"
Write-Warn "  · Crear FinancialObligationBacFinanciamientosParser.cs para endpoint sync-bac-financiamientos"
