#!/usr/bin/env pwsh
# ============================================================================
#  Carga bancaria BCR — prueba rápida
#  Archivo  : bancos/BCR-CR07015202001294229652-202603.xls
#  Cuenta   : idBankAccount=1  → BCR-AHO-001 (seed)
#  Plantilla: idBankStatementTemplate=1 → BCR-HTML-XLS-V1 (seed)
#
#  Uso:
#   pwsh docs/bancos/BCR-carga-test.ps1
#
#  Requisitos:
#   - sqlcmd instalado
#   - API corriendo en $HOST_URL
#   - credentials/db.txt en la raíz del proyecto
# ============================================================================

$SCRIPT_DIR = $PSScriptRoot
$REPO_ROOT  = (Resolve-Path (Join-Path $SCRIPT_DIR "../.." )).Path
$CREDS_FILE = Join-Path $REPO_ROOT "credentials\db.txt"
$HOST_URL   = "https://localhost:8000/api/v1"
$EMAIL      = "ezekiell1988@hotmail.com"
$FILE_PATH  = Join-Path $REPO_ROOT "bancos\BCR-CR07015202001294229652-202603.xls"

# IDs seed
$ID_BANK_ACCOUNT = 1
$ID_TEMPLATE     = 1

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
function Write-Info($msg) { Write-Host "  $msg" }

function Invoke-Fail($msg) {
    Write-Host "" 
    Write-Host "  ERROR  FALLO: $msg" -ForegroundColor Red
    if ($script:LAST_RESPONSE) {
        Write-Host "  Respuesta del API:" -ForegroundColor Red
        Write-Host ($script:LAST_RESPONSE | ConvertTo-Json -Depth 10 -ErrorAction SilentlyContinue)
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
        StatusCodeVariable   = "sc"
    }
    if ($Body) { $params["Body"] = [System.Text.Encoding]::UTF8.GetBytes($Body) }

    try {
        $result               = Invoke-RestMethod @params
        $script:HTTP_STATUS   = [int]$sc
        $script:LAST_RESPONSE = $result
        return $result
    } catch {
        $script:HTTP_STATUS = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
        $script:LAST_RESPONSE = $null
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = [System.IO.StreamReader]::new($stream)
            $raw    = $reader.ReadToEnd()
            $parsed = $raw | ConvertFrom-Json -ErrorAction SilentlyContinue
            $script:LAST_RESPONSE = if ($parsed) { $parsed } else { @{ raw = $raw } }
        } catch {}
        return $null
    }
}

function Assert-Status($expected, $context) {
    if ($script:HTTP_STATUS -ne $expected) {
        Invoke-Fail "HTTP $expected esperado en '$context', recibido: $($script:HTTP_STATUS)"
    }
    Write-Ok "$context — HTTP $($script:HTTP_STATUS)"
}

# ── Leer credenciales de BD ───────────────────────────────────────────────────
if (-not (Test-Path $CREDS_FILE)) {
    Write-Host "ERROR  No se encontró $CREDS_FILE" -ForegroundColor Red; exit 1
}
$creds   = Get-Content $CREDS_FILE
$DB_HOST = (($creds | Where-Object { $_ -match '^HOST:' })     -split '\s+')[1]
$DB_PORT = (($creds | Where-Object { $_ -match '^PORT:' })     -split '\s+')[1]
$DB_USER = (($creds | Where-Object { $_ -match '^USER:' })     -split '\s+')[1]
$DB_PASS = (($creds | Where-Object { $_ -match '^PASSWORD:' }) -split '\s+')[1]

# ── Verificar archivo ─────────────────────────────────────────────────────────
if (-not (Test-Path $FILE_PATH)) {
    Write-Host "ERROR  Archivo no encontrado: $FILE_PATH" -ForegroundColor Red; exit 1
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   BCR — Carga bancaria · Prueba rápida              ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Info "API   : $HOST_URL"
Write-Info "BD    : ${DB_HOST}:${DB_PORT} / dbfa"
Write-Info "Archivo: $FILE_PATH"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 1 — Insertar PIN en BD"

$PIN      = "12345"
$sqlQuery = "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');"
& sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlQuery *>$null
Write-Ok "PIN '$PIN' insertado en BD para usuario 1"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 2 — Login"

$resp = Invoke-Api POST "/auth/login" "{`"emailUser`":`"$EMAIL`",`"pin`":`"$PIN`"}"
Assert-Status 200 "login"

$TOKEN = $resp.accessToken
if (-not $TOKEN -or $TOKEN -eq "null") { Invoke-Fail "No se obtuvo accessToken" }
Write-Ok "Token obtenido: $($TOKEN.Substring(0, [Math]::Min(30, $TOKEN.Length)))..."

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 3 — Upload BCR XLS"
Write-Info "Endpoint: POST $HOST_URL/bank-statement-imports/upload/$ID_BANK_ACCOUNT/$ID_TEMPLATE"

try {
    $uploadResp = Invoke-RestMethod -Method POST `
        -Uri "$HOST_URL/bank-statement-imports/upload/$ID_BANK_ACCOUNT/$ID_TEMPLATE" `
        -Headers @{ Authorization = "Bearer $TOKEN" } `
        -Form @{ file = (Get-Item $FILE_PATH) } `
        -SkipCertificateCheck `
        -StatusCodeVariable "uploadSc"
    $script:HTTP_STATUS   = [int]$uploadSc
    $script:LAST_RESPONSE = $uploadResp
} catch {
    $script:HTTP_STATUS = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
    $raw = $_.ErrorDetails.Message
    Write-Host "  Exception : $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Body      : $raw" -ForegroundColor Red
    $parsed = $raw | ConvertFrom-Json -ErrorAction SilentlyContinue
    $script:LAST_RESPONSE = if ($parsed) { $parsed } else { @{ raw = $raw } }
    Invoke-Fail "Upload falló con HTTP $($script:HTTP_STATUS)"
}
Assert-Status 201 "upload bank-statement"

$IMPORT_ID = $uploadResp.idBankStatementImport
Write-Ok "Importación creada — idBankStatementImport=$IMPORT_ID  status=$($uploadResp.status)"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 4 — Polling hasta Completado/Error (máx 30 s)"

$maxAttempts = 15
$attempt     = 0
$finalStatus = $uploadResp.status
$pollResp    = $uploadResp

while ($finalStatus -notin @("Completado", "Error") -and $attempt -lt $maxAttempts) {
    Start-Sleep -Seconds 2
    $attempt++
    $pollResp = Invoke-Api GET "/bank-statement-imports/$IMPORT_ID.json" "" $TOKEN
    if ($script:HTTP_STATUS -ne 200) { Invoke-Fail "Polling falló con HTTP $($script:HTTP_STATUS)" }
    $finalStatus = $pollResp.status
    Write-Info "[$attempt] status=$finalStatus  procesadas=$($pollResp.processedTransactions)/$($pollResp.totalTransactions)"
}

if ($finalStatus -eq "Completado") {
    Write-Ok "Job completado — total=$($pollResp.totalTransactions)  procesadas=$($pollResp.processedTransactions)"
} elseif ($finalStatus -eq "Error") {
    Invoke-Fail "Job terminó con error: $($pollResp.errorMessage)"
} else {
    Write-Host "`n  WARN  Timeout: el job aún no terminó (status=$finalStatus)" -ForegroundColor Yellow
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 5 — Transacciones importadas"

$txResp = Invoke-Api GET "/bank-statement-transactions/import/$IMPORT_ID.json" "" $TOKEN
Assert-Status 200 "get transactions"

Write-Ok "$(@($txResp).Count) transacciones recibidas"
@($txResp) | Select-Object transactionDate, description, debitAmount, creditAmount, movementType | Format-Table -AutoSize

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║   CARGA BCR COMPLETADA EXITOSAMENTE                 ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ("  {0,-25} {1}" -f "idBankStatementImport:", $IMPORT_ID)
Write-Host ("  {0,-25} {1}" -f "Total transacciones:",   $pollResp.totalTransactions)
Write-Host ""
