#!/usr/bin/env pwsh
# ============================================================================
#  Obligaciones Financieras — prueba completa
#
#  Flujo:
#    1. Verificar tablas en BD (financialObligation*)
#    2. Verificar cuentas contables requeridas para COOPEALIANZA
#    3. Login
#    4. Crear obligación COOPEALIANZA (si no existe)
#    5. Sincronizar Excel → upsert cuotas + pagos automáticos + asientos + reclasificación
#    6. Consultar summary del préstamo
#    7. Verificar auxiliar en BD (cuotas, pagos, asientos)
#    8. Verificar cuentas contables afectadas (saldos de asientos en Borrador)
#
#  Uso:
#    pwsh docs/operaciones-bancarias/COOPEAL-sync-test.ps1
#
#  Requisitos:
#    - sqlcmd instalado
#    - API corriendo en $HOST_URL
#    - credentials/db.txt en la raíz del proyecto
#    - Archivo bancos/COOPEALIANZA-Tabla-Pagos-202603-CRC.xlsx
# ============================================================================

$SCRIPT_DIR  = $PSScriptRoot
$REPO_ROOT   = (Resolve-Path (Join-Path $SCRIPT_DIR "../..")).Path
$CREDS_FILE  = Join-Path $REPO_ROOT "credentials/db.txt"
$EXCEL_FILE  = Join-Path $REPO_ROOT "bancos/COOPEALIANZA-Tabla-Pagos-202603-CRC.xlsx"
$HOST_URL    = "https://localhost:8000/api/v1"
$EMAIL       = "ezekiell1988@hotmail.com"
$PIN         = "12345"

# IDs del plan de cuentas (seed — verificados en BD)
# 2.2.01.01    → Pasivo No Corriente COOPEALIANZA préstamo          id 42
# 2.1.02.01    → Pasivo Corriente porción corriente (nueva)         id 134
# 5.5.05       → Intereses Coopealianza (nuevo)                     id 137
# 5.5.06       → Mora Coopealianza (nuevo)                          id 138
# 1.1.02.01    → BAC Cta. CR73 (cuenta contable id 27)
# bankAccount  → BAC-AHO-001 CR73010200009497305680                 id 2
$ID_ACCOUNT_LONG_TERM  = 42    # 2.2.01.01
$ID_ACCOUNT_SHORT_TERM = 134   # 2.1.02.01 — Porción Corriente Coopealianza
$ID_ACCOUNT_INTEREST   = 137   # 5.5.05 — Intereses Coopealianza
$ID_ACCOUNT_LATE_FEE   = 138   # 5.5.06 — Mora Coopealianza
$ID_BANK_ACCOUNT_BAC   = 2     # BAC-AHO-001 CR73010200009497305680

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
            } catch {
                $script:LAST_RESPONSE = $null
            }
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

if (-not (Test-Path $EXCEL_FILE)) { Invoke-Fail "No se encontró $EXCEL_FILE" }

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   COOPEALIANZA — Sync Excel obligaciones financieras ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Info "API   : $HOST_URL"
Write-Info "BD    : ${DB_HOST}:${DB_PORT} / dbfa"
Write-Info "Excel : $EXCEL_FILE"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 1 — Verificar tablas en BD"

$sqlTables = @"
SET NOCOUNT ON;
SELECT
    t.name                                          AS tabla,
    SUM(p.rows)                                     AS filas
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1)
WHERE t.name IN ('financialObligation','financialObligationInstallment','financialObligationPayment')
GROUP BY t.name
ORDER BY t.name;
"@
$tablesResult = Invoke-Sql $sqlTables
Write-Ok "Tablas encontradas:"
$tablesResult | Where-Object { $_ -match '\w' -and $_ -notmatch '^-' -and $_ -notmatch 'filas' } | ForEach-Object { Write-Info "  $_" }

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 2 — Verificar cuentas contables requeridas"

$sqlAccounts = @"
SET NOCOUNT ON;
SELECT idAccount, codeAccount, nameAccount, typeAccount, allowsMovements, isActive
FROM dbo.account
WHERE idAccount IN ($ID_ACCOUNT_LONG_TERM, $ID_ACCOUNT_SHORT_TERM, $ID_ACCOUNT_INTEREST)
ORDER BY codeAccount;
"@
$accResult = Invoke-Sql $sqlAccounts
Write-Ok "Cuentas contables:"
$accResult | Where-Object { $_ -match '\d' -and $_ -notmatch '^-' } | ForEach-Object { Write-Info "  $_" }

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 3 — Verificar cuenta bancaria BAC para pago"

$sqlBankAcc = @"
SET NOCOUNT ON;
SELECT ba.idBankAccount, ba.codeBankAccount, ba.accountNumber,
       a.codeAccount, a.nameAccount, c.codeCurrency
FROM dbo.bankAccount ba
JOIN dbo.account  a ON a.idAccount  = ba.idAccount
JOIN dbo.currency c ON c.idCurrency = ba.idCurrency
WHERE ba.idBankAccount = $ID_BANK_ACCOUNT_BAC;
"@
$bankAccResult = Invoke-Sql $sqlBankAcc
Write-Ok "Cuenta BAC:"
$bankAccResult | Where-Object { $_ -match '\w' -and $_ -notmatch '^-' -and $_ -notmatch 'codeAccount' } | ForEach-Object { Write-Info "  $_" }

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
Write-Step "PASO 5 — Obtener o crear la obligación COOPEALIANZA"

$obligations = Invoke-Api GET "/financial-obligations/data.json" "" $TOKEN
Assert-Status 200 "GET financial-obligations"

$obligation = $obligations | Where-Object { $_.matchKeyword -eq "COOPEALIANZA" } | Select-Object -First 1

if ($null -eq $obligation) {
    Write-Info "No existe — creando..."

    $createBody = @{
        nameObligation       = "Préstamo COOPEALIANZA CRC — CR05081302810003488995"
        idCurrency           = 1
        originalAmount       = 8599416.00
        interestRate         = 18.50
        startDate            = "2024-03-05"
        termMonths           = 36
        idBankAccountPayment = $ID_BANK_ACCOUNT_BAC
        idAccountLongTerm    = $ID_ACCOUNT_LONG_TERM
        idAccountShortTerm   = $ID_ACCOUNT_SHORT_TERM
        idAccountInterest    = $ID_ACCOUNT_INTEREST
        idAccountLateFee     = $ID_ACCOUNT_LATE_FEE
        matchKeyword         = "COOPEALIANZA"
        notes                = "Importado desde Excel mensual COOPEALIANZA-Tabla-Pagos-*.xlsx"
    } | ConvertTo-Json

    $obligation = Invoke-Api POST "/financial-obligations" $createBody $TOKEN
    Assert-Status 201 "POST financial-obligations"
    Write-Ok "Obligación creada  id=$($obligation.idFinancialObligation)"
} else {
    Write-Ok "Obligación existente  id=$($obligation.idFinancialObligation)  estado=$($obligation.statusObligation)"
}

$OBL_ID = $obligation.idFinancialObligation

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 6 — Sincronizar Excel → cuotas + pagos + asientos"

Write-Info "Subiendo $EXCEL_FILE → /financial-obligations/$OBL_ID/sync-excel"

try {
    $syncResp = Invoke-RestMethod `
        -Method Post `
        -Uri "$HOST_URL/financial-obligations/$OBL_ID/sync-excel" `
        -Headers @{ Authorization = "Bearer $TOKEN" } `
        -Form @{ file = Get-Item $EXCEL_FILE } `
        -SkipCertificateCheck
    $script:HTTP_STATUS = 200
} catch {
    $script:HTTP_STATUS = [int]$_.Exception.Response.StatusCode
    Invoke-Fail "sync-excel devolvió HTTP $($script:HTTP_STATUS): $($_.ErrorDetails.Message)"
}

Write-Ok "Sync completado:"
Write-Info "  InstallmentsUpserted      : $($syncResp.installmentsUpserted)"
Write-Info "  PaymentsCreated           : $($syncResp.paymentsCreated)"
Write-Info "  PaymentsSkipped           : $($syncResp.paymentsSkipped)"
Write-Info "  ReclassificationEntryId   : $($syncResp.reclassificationEntryId)"
Write-Info "  PreviousShortTermPortion  : $($syncResp.previousShortTermPortion)"
Write-Info "  NewShortTermPortion       : $($syncResp.newShortTermPortion)"

if ($syncResp.warnings -and $syncResp.warnings.Count -gt 0) {
    Write-Warn "Warnings ($($syncResp.warnings.Count)):"
    $syncResp.warnings | ForEach-Object { Write-Warn "  · $_" }
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 7 — Summary del préstamo (via API)"

$summary = Invoke-Api GET "/financial-obligations/$OBL_ID/summary.json" "" $TOKEN
Assert-Status 200 "GET summary"

Write-Ok "Resumen del préstamo:"
Write-Info "  Saldo vigente            : $($summary.currentBalance)"
Write-Info "  Total capital pagado     : $($summary.totalCapitalPaid)"
Write-Info "  Total interés pagado     : $($summary.totalInterestPaid)"
Write-Info "  Porción corriente 12m    : $($summary.portionCurrentYear)"
Write-Info "  Cuotas pagadas           : $($summary.installmentsPaid)"
Write-Info "  Cuotas pendientes        : $($summary.installmentsPending)"
Write-Info "  Cuota actual #           : $($summary.currentInstallmentNumber)  vence: $($summary.currentInstallmentDue)  total: $($summary.currentInstallmentTotal)"
Write-Info "  Próxima cuota #          : $($summary.nextInstallmentNumber)  vence: $($summary.nextInstallmentDue)"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 8 — Auxiliar de cuotas en BD"

$sqlInstallments = @"
SET NOCOUNT ON;
SELECT
    i.numberInstallment     AS '#',
    i.dueDate               AS vencimiento,
    i.balanceAfter          AS saldo,
    i.amountCapital         AS capital,
    i.amountInterest        AS interes,
    i.amountLateFee         AS mora,
    i.amountTotal           AS total,
    i.statusInstallment     AS estado,
    CASE WHEN p.idFinancialObligationPayment IS NOT NULL THEN 'SI' ELSE '-' END AS pago,
    p.idBankMovement        AS idMovBAC,
    p.idAccountingEntry     AS idAsiento,
    p.isAutoProcessed       AS automatico
FROM dbo.financialObligationInstallment i
LEFT JOIN dbo.financialObligationPayment p
    ON p.idFinancialObligationInstallment = i.idFinancialObligationInstallment
WHERE i.idFinancialObligation = $OBL_ID
ORDER BY i.numberInstallment;
"@
Write-Ok "Cuotas del préstamo #${OBL_ID}:"
Invoke-Sql $sqlInstallments | ForEach-Object { Write-Info "  $_" }

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 9 — Asientos contables generados"

$sqlEntries = @"
SET NOCOUNT ON;
SELECT
    ae.idAccountingEntry,
    ae.numberEntry,
    ae.dateEntry,
    ae.descriptionEntry,
    ae.statusEntry,
    ae.originModule,
    COUNT(l.idAccountingEntryLine)  AS lineas,
    SUM(l.debitAmount)              AS totalDebe,
    SUM(l.creditAmount)             AS totalHaber
FROM dbo.accountingEntry ae
JOIN dbo.accountingEntryLine l ON l.idAccountingEntry = ae.idAccountingEntry
WHERE ae.originModule IN ('FinancialObligation','FinancialObligationReclassify')
  AND ae.idOriginRecord = $OBL_ID
GROUP BY ae.idAccountingEntry, ae.numberEntry, ae.dateEntry,
         ae.descriptionEntry, ae.statusEntry, ae.originModule
ORDER BY ae.idAccountingEntry;
"@
Write-Ok "Asientos generados para obligación #${OBL_ID}:"
Invoke-Sql $sqlEntries | ForEach-Object { Write-Info "  $_" }

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 10 — Saldo acumulado por cuenta contable (asientos en Borrador)"

$sqlAccBalances = @"
SET NOCOUNT ON;
SELECT
    a.codeAccount,
    a.nameAccount,
    a.typeAccount,
    SUM(l.debitAmount)   AS totalDebe,
    SUM(l.creditAmount)  AS totalHaber,
    SUM(l.debitAmount) - SUM(l.creditAmount) AS saldoNeto
FROM dbo.accountingEntryLine l
JOIN dbo.accountingEntry ae ON ae.idAccountingEntry = l.idAccountingEntry
JOIN dbo.account         a  ON a.idAccount          = l.idAccount
WHERE ae.originModule IN ('FinancialObligation','FinancialObligationReclassify')
  AND ae.idOriginRecord = $OBL_ID
GROUP BY a.codeAccount, a.nameAccount, a.typeAccount
ORDER BY a.codeAccount;
"@
Write-Ok "Saldos contables afectados:"
Invoke-Sql $sqlAccBalances | ForEach-Object { Write-Info "  $_" }

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 11 — Movimientos BAC vinculados"

$sqlBacLinks = @"
SET NOCOUNT ON;
SELECT
    p.idFinancialObligationPayment,
    i.numberInstallment,
    p.datePayment,
    p.amountPaid,
    bm.numberMovement,
    bm.dateMovement,
    bm.descriptionMovement,
    bm.amount               AS montoBac,
    bm.statusMovement
FROM dbo.financialObligationPayment p
JOIN dbo.financialObligationInstallment i
    ON i.idFinancialObligationInstallment = p.idFinancialObligationInstallment
LEFT JOIN dbo.bankMovement bm ON bm.idBankMovement = p.idBankMovement
WHERE i.idFinancialObligation = $OBL_ID
ORDER BY i.numberInstallment;
"@
Write-Ok "Pagos vinculados a movimientos BAC:"
Invoke-Sql $sqlBacLinks | ForEach-Object { Write-Info "  $_" }

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 12 — Confirmar asiento de reclasificación (Borrador → Publicado)"

# Usar el id del sync si está disponible; de lo contrario buscar cualquier Borrador RCLS en BD
$RCLS_ID = $null
if ($syncResp.reclassificationEntryId -and $syncResp.reclassificationEntryId -ne 0) {
    $RCLS_ID = $syncResp.reclassificationEntryId
    Write-Info "  Asiento RCLS recién creado por el sync: id=$RCLS_ID"
} else {
    # Buscar via BD si existe algún asiento de reclasificación Borrador para esta obligación
    $sqlFindRcls = @"
SET NOCOUNT ON;
SELECT TOP 1 idAccountingEntry
FROM dbo.accountingEntry
WHERE originModule = 'FinancialObligationReclassify'
  AND idOriginRecord = $OBL_ID
  AND statusEntry = 'Borrador'
ORDER BY idAccountingEntry DESC;
"@
    $rclsRow = Invoke-Sql $sqlFindRcls | Where-Object { $_ -match '^\s*\d+\s*$' } | Select-Object -First 1
    if ($rclsRow) {
        $RCLS_ID = [int]($rclsRow.Trim())
        Write-Info "  Asiento RCLS existente en Borrador encontrado en BD: id=$RCLS_ID"
    }
}

if ($null -eq $RCLS_ID) {
    Write-Warn "No hay asiento de reclasificación en Borrador para confirmar"
} else {

    # 1. Obtener el asiento completo para reutilizar sus datos en el PUT
    $entry = Invoke-Api GET "/accounting-entries/${RCLS_ID}.json" "" $TOKEN
    Assert-Status 200 "GET accounting-entry/${RCLS_ID}"

    Write-Info "  Asiento: $($entry.numberEntry)  Estado actual: $($entry.statusEntry)"
    Write-Info "  Líneas  : $($entry.lines.Count)"
    $entry.lines | ForEach-Object {
        Write-Info "    · idAccount=$($_.idAccount)  DR=$($_.debitAmount)  CR=$($_.creditAmount)"
    }

    if ($entry.statusEntry -eq "Publicado") {
        Write-Ok "El asiento ya estaba Publicado — nada que hacer"
    } else {
        # 2. Reconstruir el body con las mismas líneas y statusEntry = "Publicado"
        $confirmLines = $entry.lines | ForEach-Object {
            @{
                idAccount       = $_.idAccount
                debitAmount     = $_.debitAmount
                creditAmount    = $_.creditAmount
                descriptionLine = $_.descriptionLine
            }
        }

        $confirmBody = @{
            idFiscalPeriod   = $entry.idFiscalPeriod
            idCurrency       = $entry.idCurrency
            numberEntry      = $entry.numberEntry
            dateEntry        = if ($entry.dateEntry -is [datetime]) { $entry.dateEntry.ToString("yyyy-MM-dd") } else { ([string]$entry.dateEntry).Substring(0, [Math]::Min(10, ([string]$entry.dateEntry).Length)) }
            descriptionEntry = $entry.descriptionEntry
            statusEntry      = "Publicado"
            referenceEntry   = $entry.referenceEntry
            exchangeRateValue = $entry.exchangeRateValue
            lines            = $confirmLines
        } | ConvertTo-Json -Depth 5

        $confirmed = Invoke-Api PUT "/accounting-entries/${RCLS_ID}" $confirmBody $TOKEN
        Assert-Status 200 "PUT accounting-entries/${RCLS_ID}"

        Write-Ok "Asiento confirmado  →  Estado: $($confirmed.statusEntry)"
        Write-Info "  Número  : $($confirmed.numberEntry)"
        Write-Info "  Fecha   : $($confirmed.dateEntry)"
        Write-Info "  Período : $($confirmed.nameFiscalPeriod)"

        # 3. Verificar en BD que el estado cambió
        $sqlConfirm = @"
SET NOCOUNT ON;
SELECT numberEntry, dateEntry, statusEntry, originModule
FROM dbo.accountingEntry
WHERE idAccountingEntry = $RCLS_ID;
"@
        Write-Ok "Verificación en BD:"
        Invoke-Sql $sqlConfirm | Where-Object { $_ -match '\w' -and $_ -notmatch '^-' -and $_ -notmatch 'numberEntry' } | ForEach-Object { Write-Info "  $_" }
    }
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║   SYNC COMPLETADO EXITOSAMENTE                      ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Green
