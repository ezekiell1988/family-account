#!/usr/bin/env pwsh
# ============================================================================
#  Carga bancaria BAC Credomatic — prueba completa CRC + USD
#
#  Archivos:
#    CRC ─ bancos/BAC-CR64010202312918989651-202603-CRC.txt  → IdBankAccount=3  (AMEX CRC)
#    CRC ─ bancos/BAC-CR69010202510369031047-202603-CRC.txt  → IdBankAccount=4  (MC-6515 CRC)
#    CRC ─ bancos/BAC-CR48010202514509181545-202603-CRC.txt  → IdBankAccount=5  (MC-8608 CRC)
#    USD ─ bancos/BAC-CR13010202321157328803-202603-USD.txt  → IdBankAccount=12 (AMEX USD)
#    USD ─ bancos/BAC-CR17010202526537778556-202603-USD.txt  → IdBankAccount=13 (MC-6515 USD)
#    USD ─ bancos/BAC-CR18010202522447454214-202603-USD.txt  → IdBankAccount=14 (MC-8608 USD)
#
#  Plantillas seed:
#    IdBankStatementTemplate=4 → BAC-TXT-CRC-V1
#    IdBankStatementTemplate=5 → BAC-TXT-USD-V1
#
#  Uso:
#    pwsh docs/bancos/BAC-carga-test.ps1
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

$ID_TEMPLATE_CRC = 4
$ID_TEMPLATE_USD = 5

# Cargas a ejecutar: @(idBankAccount, idTemplate, nombreArchivo, etiqueta)
$CARGAS = @(
    @{ Id = 3;  Temp = $ID_TEMPLATE_CRC; File = "BAC-CR64010202312918989651-202603-CRC.txt"; Label = "AMEX   CRC" },
    @{ Id = 4;  Temp = $ID_TEMPLATE_CRC; File = "BAC-CR69010202510369031047-202603-CRC.txt"; Label = "MC6515 CRC" },
    @{ Id = 5;  Temp = $ID_TEMPLATE_CRC; File = "BAC-CR48010202514509181545-202603-CRC.txt"; Label = "MC8608 CRC" },
    @{ Id = 12; Temp = $ID_TEMPLATE_USD; File = "BAC-CR13010202321157328803-202603-USD.txt"; Label = "AMEX   USD" },
    @{ Id = 13; Temp = $ID_TEMPLATE_USD; File = "BAC-CR17010202526537778556-202603-USD.txt"; Label = "MC6515 USD" },
    @{ Id = 14; Temp = $ID_TEMPLATE_USD; File = "BAC-CR18010202522447454214-202603-USD.txt"; Label = "MC8608 USD" }
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
Write-Host "║   BAC Credomatic — Carga bancaria CRC + USD         ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Info "API   : $HOST_URL"
Write-Info "BD    : ${DB_HOST}:${DB_PORT} / dbfa"
Write-Info "Cargas: $($CARGAS.Count) archivos (3 CRC + 3 USD)"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 1 — Verificar plantillas en BD"

$sqlTemplates = @"
SET NOCOUNT ON;
SELECT idBankStatementTemplate, codeTemplate, nameTemplate
FROM dbo.bankStatementTemplate
WHERE idBankStatementTemplate IN (4, 5);
"@
$tmplResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlTemplates 2>&1
Write-Ok "Plantillas encontradas:"
$tmplResult | Where-Object { $_ -match '\d' } | ForEach-Object { Write-Info "  $_" }

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 2 — Verificar cuentas bancarias BAC en BD"

$sqlAccounts = @"
SET NOCOUNT ON;
SELECT ba.idBankAccount, ba.codeBankAccount, ba.accountNumber,
       c.codeCurrency
FROM dbo.bankAccount ba
JOIN dbo.currency c ON c.idCurrency = ba.idCurrency
WHERE ba.idBankAccount IN (3,4,5,12,13,14)
ORDER BY ba.idBankAccount;
"@
$accResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlAccounts 2>&1
Write-Ok "Cuentas BAC:"
$accResult | Where-Object { $_ -match '\d' } | ForEach-Object { Write-Info "  $_" }

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 3 — Insertar PIN y hacer login"

$PIN      = "12345"
$sqlPin   = "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser=1 AND pin='$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');"
& sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlPin *>$null
Write-Ok "PIN '$PIN' insertado"

$resp = Invoke-Api POST "/auth/login" "{`"emailUser`":`"$EMAIL`",`"pin`":`"$PIN`"}"
Assert-Status 200 "login"
$TOKEN = $resp.accessToken
if (-not $TOKEN -or $TOKEN -eq "null") { Invoke-Fail "No se obtuvo accessToken" }
Write-Ok "Token obtenido: $($TOKEN.Substring(0, [Math]::Min(30,$TOKEN.Length)))..."

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 4 — Cargar los 6 archivos BAC"

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
        $script:HTTP_STATUS = [int]$_.Exception.Response.StatusCode
        Write-Warn "[$($carga.Label)] HTTP $($script:HTTP_STATUS) — $($_.ErrorDetails.Message)"
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
    while ($finalStatus -notin @("Completado","Error") -and $attempt -lt $maxAttempts) {
        Start-Sleep -Milliseconds 2000
        $pollResp    = Invoke-Api GET "/bank-statement-imports/$importId.json" "" $TOKEN
        $finalStatus = $pollResp.status
        $attempt++
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
    COUNT(bst.idBankStatementTransaction)          AS totalTx,
    SUM(CASE WHEN bst.idBankMovementType IS NOT NULL THEN 1 ELSE 0 END) AS clasificadas,
    SUM(CASE WHEN bst.idBankMovementType IS NULL     THEN 1 ELSE 0 END) AS sinClasificar,
    SUM(ISNULL(bst.debitAmount,0))                 AS totalDebitos,
    SUM(ISNULL(bst.creditAmount,0))                AS totalCreditos
FROM dbo.bankStatementImport bsi
JOIN dbo.bankAccount ba ON ba.idBankAccount = bsi.idBankAccount
JOIN dbo.currency c     ON c.idCurrency     = ba.idCurrency
LEFT JOIN dbo.bankStatementTransaction bst ON bst.idBankStatementImport = bsi.idBankStatementImport
WHERE bsi.idBankStatementImport IN ($importIds)
GROUP BY bsi.idBankStatementImport, ba.codeBankAccount, c.codeCurrency
ORDER BY bsi.idBankStatementImport;
"@
    $txResult = & sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlTx 2>&1
    $txResult | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Warn "No hay importaciones con ID para consultar en BD."
}

# ══════════════════════════════════════════════════════════════════════════════
$errores = @($resultados | Where-Object { $_.Status -ne "Completado" })

Write-Host ""
if ($errores.Count -eq 0) {
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║   TODAS LAS CARGAS BAC COMPLETADAS EXITOSAMENTE     ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Green
} else {
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Yellow
    Write-Host "║   CARGA TERMINADA CON $($errores.Count) ERROR(ES)                     ║" -ForegroundColor Yellow
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Yellow
    $errores | Format-Table -AutoSize
}
