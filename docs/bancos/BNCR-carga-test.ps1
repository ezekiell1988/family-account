#!/usr/bin/env pwsh
# ============================================================================
#  Carga bancaria BNCR — prueba completa CRC + USD
#
#  Archivos:
#    CRC ─ bancos/BNCR-CR86015100020019688637-202503.csv  → IdBankAccount=7  (BNCR-AHO-CRC-001)
#    USD ─ bancos/BNCR-CR06015107220020012339-202603.csv  → IdBankAccount=8  (BNCR-AHO-USD-001)
#
#  Plantilla seed:
#    IdBankStatementTemplate=3 → BNCR-CSV-V1 (ambas cuentas)
#
#  Transacciones esperadas:
#    CRC: ~38 transacciones (muchos PAGO TARJETA BAC → clasificados)
#    USD:   2 transacciones (1 PAGO SERVICIO PROFESIONAL → clasificada)
#
#  Uso:
#    pwsh docs/bancos/BNCR-carga-test.ps1
#
#  Requisitos:
#    - sqlcmd instalado
#    - API corriendo en $HOST_URL
#    - credentials/db.txt en la raíz del proyecto
# ============================================================================

$SCRIPT_DIR = $PSScriptRoot
$REPO_ROOT  = (Resolve-Path (Join-Path $SCRIPT_DIR "../..")).Path
$CREDS_FILE = Join-Path $REPO_ROOT "credentials/db.txt"
$HOST_URL   = "https://localhost:8000/api/v1"
$EMAIL      = "ezekiell1988@hotmail.com"

$ID_TEMPLATE_BNCR = 3

# Cargas a ejecutar
$CARGAS = @(
    @{ Id = 7; Temp = $ID_TEMPLATE_BNCR; File = "BNCR-CR86015100020019688637-202503.csv"; Label = "BNCR CRC" },
    @{ Id = 8; Temp = $ID_TEMPLATE_BNCR; File = "BNCR-CR06015107220020012339-202603.csv"; Label = "BNCR USD" }
)

$script:HTTP_STATUS   = 0
$script:LAST_RESPONSE = $null

# ── Helpers ───────────────────────────────────────────────────────────────────
function Write-Step($msg) {
    Write-Host ""
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "▶  $msg" -ForegroundColor Cyan
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
}
function Write-Ok($msg)   { Write-Host "  OK  $msg" -ForegroundColor Green }
function Write-Info($msg) { Write-Host "  ·   $msg" }
function Write-Warn($msg) { Write-Host "  ⚠   $msg" -ForegroundColor Yellow }

function Invoke-Fail($msg) {
    Write-Host ""
    Write-Host "  ✗   FALLO: $msg" -ForegroundColor Red
    if ($script:LAST_RESPONSE) {
        Write-Host "  Respuesta:" -ForegroundColor Red
        $script:LAST_RESPONSE | ConvertTo-Json -Depth 5 | Write-Host
    }
    exit 1
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Body  = "",
        [string]$Token = ""
    )
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
    catch [System.Net.Http.HttpRequestException] {
        $script:HTTP_STATUS   = 0
        $script:LAST_RESPONSE = $null
        Invoke-Fail "Error de conexión: $($_.Exception.Message)"
    }
    catch {
        $errResp = $_.Exception.Response
        if ($errResp) {
            $stream  = $errResp.GetResponseStream()
            $reader  = New-Object System.IO.StreamReader($stream)
            $content = $reader.ReadToEnd()
            $script:HTTP_STATUS   = [int]$errResp.StatusCode
            $script:LAST_RESPONSE = $content | ConvertFrom-Json -ErrorAction SilentlyContinue
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

# ── Leer credenciales de BD ───────────────────────────────────────────────────
if (-not (Test-Path $CREDS_FILE)) { Invoke-Fail "No se encontró $CREDS_FILE" }
$creds   = Get-Content $CREDS_FILE
$DB_HOST = (($creds | Where-Object { $_ -match '^HOST:' })     -split '\s+')[1]
$DB_PORT = (($creds | Where-Object { $_ -match '^PORT:' })     -split '\s+')[1]
$DB_USER = (($creds | Where-Object { $_ -match '^USER:' })     -split '\s+')[1]
$DB_PASS = (($creds | Where-Object { $_ -match '^PASSWORD:' }) -split '\s+')[1]

# ── Verificar archivos ────────────────────────────────────────────────────────
foreach ($c in $CARGAS) {
    $fp = Join-Path $REPO_ROOT "bancos/$($c.File)"
    if (-not (Test-Path $fp)) { Invoke-Fail "No se encontró el archivo: $fp" }
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Banco Nacional de Costa Rica — Carga CRC + USD    ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Info "API   : $HOST_URL"
Write-Info "BD    : ${DB_HOST}:${DB_PORT} / dbfa"
Write-Info "Cargas: $($CARGAS.Count) archivos (1 CRC + 1 USD)"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 1 — Verificar plantilla BNCR-CSV-V1 en BD"

$sqlTemplate = @"
SET NOCOUNT ON;
SELECT idBankStatementTemplate, codeTemplate, nameTemplate, isActive
FROM dbo.bankStatementTemplate
WHERE idBankStatementTemplate = $ID_TEMPLATE_BNCR;
"@
$tmplResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlTemplate 2>&1
$tmplResult | Where-Object { $_ -match '\S' } | ForEach-Object { Write-Info $_ }
if (-not ($tmplResult | Where-Object { $_ -match 'BNCR' })) {
    Invoke-Fail "Plantilla BNCR-CSV-V1 no encontrada en BD (idBankStatementTemplate=$ID_TEMPLATE_BNCR)"
}
Write-Ok "Plantilla encontrada"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 2 — Verificar cuentas bancarias BNCR en BD"

$sqlAccounts = @"
SET NOCOUNT ON;
SELECT ba.idBankAccount, ba.codeBankAccount, ba.accountNumber, c.codeCurrency, ba.isActive
FROM dbo.bankAccount ba
JOIN dbo.currency c ON c.idCurrency = ba.idCurrency
WHERE ba.idBankAccount IN (7, 8)
ORDER BY ba.idBankAccount;
"@
$accResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlAccounts 2>&1
Write-Ok "Cuentas BNCR:"
$accResult | Where-Object { $_ -match '\d' } | ForEach-Object { Write-Info "  $_" }

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 3 — Insertar PIN y hacer login"

$PIN    = "12345"
$sqlPin = "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser=1 AND pin='$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');"
& sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlPin *>$null
Write-Ok "PIN '$PIN' insertado"

$resp = Invoke-Api POST "/auth/login" "{`"emailUser`":`"$EMAIL`",`"pin`":`"$PIN`"}"
Assert-Status 200 "login"
$TOKEN = $resp.accessToken
if (-not $TOKEN -or $TOKEN -eq "null") { Invoke-Fail "No se obtuvo accessToken" }
Write-Ok "Token obtenido: $($TOKEN.Substring(0, [Math]::Min(30,$TOKEN.Length)))..."

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 4 — Cargar los 2 archivos BNCR"

$resultados = @()

foreach ($carga in $CARGAS) {
    $filePath = Join-Path $REPO_ROOT "bancos/$($carga.File)"
    Write-Info "Subiendo [$($carga.Label)] → idBankAccount=$($carga.Id) plantilla=$($carga.Temp)"

    try {
        $uploadResp = Invoke-RestMethod `
            -Method Post `
            -Uri "$HOST_URL/bank-statement-imports/upload/$($carga.Id)/$($carga.Temp)" `
            -Headers @{ Authorization = "Bearer $TOKEN" } `
            -Form @{ file = Get-Item $filePath } `
            -SkipCertificateCheck
        $script:HTTP_STATUS = 201
    } catch {
        $script:HTTP_STATUS = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
        $raw = $_.ErrorDetails.Message
        Write-Warn "[$($carga.Label)] HTTP $($script:HTTP_STATUS) — $raw"
        $resultados += [PSCustomObject]@{
            Label     = $carga.Label
            ImportId  = "-"
            Status    = "ERROR HTTP $($script:HTTP_STATUS)"
            Total     = 0
            Processed = 0
        }
        continue
    }

    if (-not $uploadResp) {
        Write-Warn "[$($carga.Label)] Sin respuesta del servidor"
        $resultados += [PSCustomObject]@{
            Label     = $carga.Label
            ImportId  = "-"
            Status    = "SIN RESPUESTA"
            Total     = 0
            Processed = 0
        }
        continue
    }

    $importId    = $uploadResp.idBankStatementImport
    $finalStatus = $uploadResp.status
    $pollResp    = $uploadResp

    # ── Polling hasta Completado/Error (máx 30 s) ────────────────────────────
    $maxAttempts = 15
    $attempt     = 0
    while ($finalStatus -notin @("Completado", "Error") -and $attempt -lt $maxAttempts) {
        Start-Sleep -Milliseconds 2000
        $pollResp    = Invoke-Api GET "/bank-statement-imports/$importId.json" "" $TOKEN
        $finalStatus = $pollResp.status
        $attempt++
        Write-Info "  [$attempt] status=$finalStatus  procesadas=$($pollResp.processedTransactions)/$($pollResp.totalTransactions)"
    }

    $resultados += [PSCustomObject]@{
        Label     = $carga.Label
        ImportId  = $importId
        Status    = $finalStatus
        Total     = $pollResp.totalTransactions
        Processed = $pollResp.processedTransactions
    }

    if ($finalStatus -eq "Completado") {
        Write-Ok "[$($carga.Label)] importId=$importId  total=$($pollResp.totalTransactions)  procesadas=$($pollResp.processedTransactions)"
    } else {
        Write-Warn "[$($carga.Label)] importId=$importId  status=$finalStatus  error=$($pollResp.errorMessage)"
    }
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 5 — Resumen de cargas"

$resultados | Format-Table -AutoSize

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 6 — Verificar transacciones en BD"

$importIds = ($resultados | Where-Object { $_.ImportId -ne "-" } | ForEach-Object { $_.ImportId }) -join ","

if ($importIds) {
    $sqlTx = @"
SET NOCOUNT ON;
SELECT
    bsi.idBankStatementImport,
    ba.codeBankAccount,
    c.codeCurrency,
    COUNT(bst.idBankStatementTransaction)                               AS totalTx,
    SUM(CASE WHEN bst.idBankMovementType IS NOT NULL THEN 1 ELSE 0 END) AS clasificadas,
    SUM(CASE WHEN bst.idBankMovementType IS NULL     THEN 1 ELSE 0 END) AS sinClasificar,
    CAST(SUM(ISNULL(bst.debitAmount,  0)) AS DECIMAL(18,2))            AS totalDebitos,
    CAST(SUM(ISNULL(bst.creditAmount, 0)) AS DECIMAL(18,2))            AS totalCreditos
FROM dbo.bankStatementImport bsi
JOIN dbo.bankAccount ba ON ba.idBankAccount = bsi.idBankAccount
JOIN dbo.currency c     ON c.idCurrency     = ba.idCurrency
LEFT JOIN dbo.bankStatementTransaction bst
       ON bst.idBankStatementImport = bsi.idBankStatementImport
WHERE bsi.idBankStatementImport IN ($importIds)
GROUP BY bsi.idBankStatementImport, ba.codeBankAccount, c.codeCurrency
ORDER BY bsi.idBankStatementImport;
"@
    $txResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlTx 2>&1
    $txResult | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Warn "No hay importaciones con ID para consultar en BD."
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 7 — Detalle de transacciones (TOP 20 por import)"

$importIds2 = ($resultados | Where-Object { $_.ImportId -ne "-" } | ForEach-Object { $_.ImportId }) -join ","

if ($importIds2) {
    $sqlDet = @"
SET NOCOUNT ON;
SELECT TOP 20
    bst.idBankStatementTransaction,
    ba.codeBankAccount,
    CONVERT(varchar(10), bst.transactionDate, 103) AS fecha,
    LEFT(bst.description, 40)                      AS descripcion,
    CAST(ISNULL(bst.debitAmount,  0) AS DECIMAL(18,2)) AS debito,
    CAST(ISNULL(bst.creditAmount, 0) AS DECIMAL(18,2)) AS credito,
    ISNULL(bmt.codeBankMovementType, '(sin clasificar)') AS tipo
FROM dbo.bankStatementTransaction bst
JOIN dbo.bankStatementImport bsi  ON bsi.idBankStatementImport = bst.idBankStatementImport
JOIN dbo.bankAccount ba           ON ba.idBankAccount           = bsi.idBankAccount
LEFT JOIN dbo.bankMovementType bmt ON bmt.idBankMovementType    = bst.idBankMovementType
WHERE bst.idBankStatementImport IN ($importIds2)
ORDER BY bst.idBankStatementImport, bst.transactionDate;
"@
    $detResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlDet 2>&1
    $detResult | ForEach-Object { Write-Host "  $_" }
}

# ══════════════════════════════════════════════════════════════════════════════
$errores = @($resultados | Where-Object { $_.Status -ne "Completado" })

Write-Host ""
if ($errores.Count -eq 0) {
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║   TODAS LAS CARGAS BNCR COMPLETADAS EXITOSAMENTE    ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Green
} else {
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║   ALGUNAS CARGAS BNCR FALLARON                      ║" -ForegroundColor Red
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Red
    $errores | ForEach-Object { Write-Warn "  $($_.Label): $($_.Status)" }
    exit 1
}
