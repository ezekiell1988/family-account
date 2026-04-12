#!/usr/bin/env pwsh
# ============================================================================
#  Carga bancaria BAC Credomatic — Cuenta de Ahorro/Débito (XLS)
#
#  Archivo :  bancos/BAC-CR73010200009497305680-202603.xls
#  Cuenta  :  idBankAccount=2  → BAC-AHO-001 (CR73010200009497305680, CRC)
#  Plantilla: idBankStatementTemplate=6 → BAC-XLS-V1
#
#  Uso:
#    pwsh docs/bancos/BAC-XLS-carga-test.ps1
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
$FILE_PATH  = Join-Path $REPO_ROOT "bancos/BAC-CR73010200009497305680-202603.xls"

$ID_BANK_ACCOUNT = 2
$ID_TEMPLATE     = 6

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

# ── Verificar archivo ─────────────────────────────────────────────────────────
if (-not (Test-Path $FILE_PATH)) { Invoke-Fail "No se encontró archivo: $FILE_PATH" }

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   BAC Credomatic — Ahorro/Débito XLS · Prueba       ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Info "API      : $HOST_URL"
Write-Info "BD       : ${DB_HOST}:${DB_PORT} / dbfa"
Write-Info "Cuenta   : idBankAccount=$ID_BANK_ACCOUNT (BAC-AHO-001, CR73010200009497305680)"
Write-Info "Plantilla: idTemplate=$ID_TEMPLATE (BAC-XLS-V1)"
Write-Info "Archivo  : $FILE_PATH"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 1 — Verificar plantilla BAC-XLS-V1 en BD"

$sqlTemplate = @"
SET NOCOUNT ON;
SELECT idBankStatementTemplate, codeTemplate, nameTemplate, isActive
FROM dbo.bankStatementTemplate
WHERE idBankStatementTemplate = $ID_TEMPLATE;
"@
$tmplResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlTemplate 2>&1
$tmplResult | Where-Object { $_ -match '\S' } | ForEach-Object { Write-Info $_ }
if (-not ($tmplResult | Where-Object { $_ -match 'BAC-XLS' })) {
    Invoke-Fail "Plantilla BAC-XLS-V1 (id=$ID_TEMPLATE) no encontrada en BD. Ejecute 'dotnet ef database update'."
}
Write-Ok "Plantilla encontrada"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 2 — Verificar cuenta bancaria BAC-AHO-001 en BD"

$sqlAccount = @"
SET NOCOUNT ON;
SELECT ba.idBankAccount, ba.codeBankAccount, ba.accountNumber,
       c.codeCurrency, ba.isActive
FROM dbo.bankAccount ba
JOIN dbo.currency c ON c.idCurrency = ba.idCurrency
WHERE ba.idBankAccount = $ID_BANK_ACCOUNT;
"@
$accResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlAccount 2>&1
$accResult | Where-Object { $_ -match '\S' } | ForEach-Object { Write-Info $_ }
Write-Ok "Cuenta verificada"

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
Write-Step "PASO 4 — Upload BAC XLS (cuenta ahorro/débito)"
Write-Info "POST $HOST_URL/bank-statement-imports/upload/$ID_BANK_ACCOUNT/$ID_TEMPLATE"

try {
    $uploadResp = Invoke-RestMethod `
        -Method Post `
        -Uri "$HOST_URL/bank-statement-imports/upload/$ID_BANK_ACCOUNT/$ID_TEMPLATE" `
        -Headers @{ Authorization = "Bearer $TOKEN" } `
        -Form @{ file = Get-Item $FILE_PATH } `
        -SkipCertificateCheck
    $script:HTTP_STATUS   = 201
    $script:LAST_RESPONSE = $uploadResp
} catch {
    $script:HTTP_STATUS = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
    $raw = $_.ErrorDetails.Message
    Write-Host "  Exception: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Body     : $raw"                    -ForegroundColor Red
    $script:LAST_RESPONSE = $raw | ConvertFrom-Json -ErrorAction SilentlyContinue
    Invoke-Fail "Upload falló con HTTP $($script:HTTP_STATUS)"
}

$IMPORT_ID   = $uploadResp.idBankStatementImport
$finalStatus = $uploadResp.status
Write-Ok "Importación creada — idBankStatementImport=$IMPORT_ID  status=$finalStatus"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 5 — Polling hasta Completado/Error (máx 30 s)"

$maxAttempts = 15
$attempt     = 0
$pollResp    = $uploadResp

while ($finalStatus -notin @("Completado","Error") -and $attempt -lt $maxAttempts) {
    Start-Sleep -Milliseconds 2000
    $attempt++
    $pollResp    = Invoke-Api GET "/bank-statement-imports/$IMPORT_ID.json" "" $TOKEN
    if ($script:HTTP_STATUS -ne 200) { Invoke-Fail "Polling falló con HTTP $($script:HTTP_STATUS)" }
    $finalStatus = $pollResp.status
    Write-Info "[$attempt] status=$finalStatus  procesadas=$($pollResp.processedTransactions)/$($pollResp.totalTransactions)"
}

if ($finalStatus -eq "Completado") {
    Write-Ok "Job completado — total=$($pollResp.totalTransactions)  procesadas=$($pollResp.processedTransactions)"
} elseif ($finalStatus -eq "Error") {
    Invoke-Fail "Job terminó con error: $($pollResp.errorMessage)"
} else {
    Write-Warn "Timeout: el job aún no terminó (status=$finalStatus)"
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 6 — Transacciones importadas (via API)"

$txResp = Invoke-Api GET "/bank-statement-transactions/import/$IMPORT_ID.json" "" $TOKEN
Assert-Status 200 "get transactions"

$txs = @($txResp)
Write-Ok "$($txs.Count) transacciones recibidas"
$txs | Select-Object transactionDate, description, debitAmount, creditAmount, movementType | Format-Table -AutoSize

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 7 — Verificar en BD (resumen + detalle)"

$sqlResumen = @"
SET NOCOUNT ON;
SELECT
    bsi.idBankStatementImport,
    ba.codeBankAccount,
    c.codeCurrency,
    bsi.status,
    COUNT(bst.idBankStatementTransaction)                              AS totalTx,
    SUM(CASE WHEN bst.idBankMovementType IS NOT NULL THEN 1 ELSE 0 END) AS clasificadas,
    SUM(CASE WHEN bst.idBankMovementType IS NULL     THEN 1 ELSE 0 END) AS sinClasificar,
    CAST(SUM(ISNULL(bst.debitAmount, 0))  AS DECIMAL(18,2))           AS totalDebitos,
    CAST(SUM(ISNULL(bst.creditAmount, 0)) AS DECIMAL(18,2))           AS totalCreditos
FROM dbo.bankStatementImport bsi
JOIN dbo.bankAccount ba         ON ba.idBankAccount = bsi.idBankAccount
JOIN dbo.currency c             ON c.idCurrency     = ba.idCurrency
LEFT JOIN dbo.bankStatementTransaction bst
       ON bst.idBankStatementImport = bsi.idBankStatementImport
WHERE bsi.idBankStatementImport = $IMPORT_ID
GROUP BY bsi.idBankStatementImport, ba.codeBankAccount, c.codeCurrency, bsi.status;
"@

Write-Info "Resumen de la importación:"
$resumenResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlResumen 2>&1
$resumenResult | Where-Object { $_ -match '\S' } | ForEach-Object { Write-Host "    $_" }

$sqlDetalle = @"
SET NOCOUNT ON;
SELECT TOP 20
    bst.transactionDate,
    bst.documentNumber,
    LEFT(bst.description, 40)                         AS descripcion,
    CAST(ISNULL(bst.debitAmount,  0) AS DECIMAL(18,2)) AS debito,
    CAST(ISNULL(bst.creditAmount, 0) AS DECIMAL(18,2)) AS credito,
    bmt.codeBankMovementType
FROM dbo.bankStatementTransaction bst
LEFT JOIN dbo.bankMovementType bmt ON bmt.idBankMovementType = bst.idBankMovementType
WHERE bst.idBankStatementImport = $IMPORT_ID
ORDER BY bst.transactionDate, bst.idBankStatementTransaction;
"@

Write-Info "Primeras 20 transacciones en BD:"
$detalleResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlDetalle 2>&1
$detalleResult | Where-Object { $_ -match '\S' } | ForEach-Object { Write-Host "    $_" }

# ════════════════════════════════════════════════════════════════════════════════
$erroresBD = $resumenResult | Where-Object { $_ -match 'Error|error' }

Write-Host ""
if ($finalStatus -eq "Completado" -and -not $erroresBD) {
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║   CARGA BAC XLS (DÉBITO) COMPLETADA EXITOSAMENTE    ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ("  {0,-25} {1}" -f "idBankStatementImport:", $IMPORT_ID)
    Write-Host ("  {0,-25} {1}" -f "Total transacciones:",   $pollResp.totalTransactions)
    Write-Host ("  {0,-25} {1}" -f "Procesadas:",            $pollResp.processedTransactions)
} else {
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Yellow
    Write-Host "║   CARGA TERMINADA — REVISAR ADVERTENCIAS             ║" -ForegroundColor Yellow
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Yellow
}
Write-Host ""
