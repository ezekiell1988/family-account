#!/usr/bin/env pwsh
# ============================================================================
#  BAC Financiamientos (Tasa Cero) — prueba del endpoint sync-bac-financiamientos
#
#  Sube los XLS de "Consulta de Financiamientos" de BAC Credomatic al endpoint
#  POST /financial-obligations/sync-bac-financiamientos y espera a que el job
#  de Hangfire complete el upsert de obligaciones y cuotas.
#
#  Flujo:
#    1.  Verificar tablas en BD
#    2.  Verificar que los archivos XLS existen
#    3.  Login
#    4.  Subir archivos XLS al endpoint (multipart/form-data)
#    5.  Esperar job de Hangfire (poll BD, máx 30 s)
#    6.  Verificar obligaciones y cuotas en BD
#
#  Uso:
#    pwsh docs/operaciones-bancarias/BAC-TASA-CERO-sync-test.ps1
#
#  Requisitos:
#    - sqlcmd instalado
#    - API corriendo en $HOST_URL
#    - credentials/db.txt en la raíz del proyecto
#    - Los dos XLS en la carpeta bancos/
#
#  Cuentas contables del seed (AccountConfiguration.cs):
#    143 = 2.1.01.08  BAC TC 5466-8608 Financiamientos Tasa Cero (₡)
#    144 = 2.1.01.09  BAC TC 5466-8608 Financiamientos Tasa Cero ($)
#    145 = 2.1.01.10  BAC TC 5491-6515 Financiamientos Tasa Cero (₡)
# ============================================================================

$SCRIPT_DIR = $PSScriptRoot
$REPO_ROOT  = (Resolve-Path (Join-Path $SCRIPT_DIR "../..")).Path
$CREDS_FILE = Join-Path $REPO_ROOT "credentials/db.txt"
$HOST_URL   = "https://localhost:8000/api/v1"
$EMAIL      = "ezekiell1988@hotmail.com"
$PIN        = "12345"

# Archivos XLS por tarjeta
$XLS_FILES = @(
    (Join-Path $REPO_ROOT "bancos/BAC-5466-37XX-XXXX-8608-202603-Financiamientos.xls")
    (Join-Path $REPO_ROOT "bancos/BAC-5491-94XX-XXXX-6515-202603-Financiamientos.xls")
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
Write-Host "║   BAC Financiamientos (Tasa Cero) — Sync vía API    ║" -ForegroundColor Cyan
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
Write-Step "PASO 2 — Verificar archivos XLS"

foreach ($xlsPath in $XLS_FILES) {
    if (-not (Test-Path $xlsPath)) {
        Invoke-Fail "No se encontró el archivo XLS: $xlsPath"
    }
    Write-Ok "Archivo XLS encontrado: $(Split-Path $xlsPath -Leaf)"
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 3 — Login"

$sqlPin = "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser=1 AND pin='$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');"
Invoke-Sql $sqlPin *>$null
Write-Ok "PIN '$PIN' insertado"

$resp = Invoke-Api POST "/auth/login" "{`"emailUser`":`"$EMAIL`",`"pin`":`"$PIN`"}"
Assert-Status 200 "login"
$TOKEN = $resp.accessToken
if (-not $TOKEN -or $TOKEN -eq "null") { Invoke-Fail "No se obtuvo accessToken" }
Write-Ok "Token obtenido"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 4 — Subir archivos XLS al endpoint sync-bac-financiamientos"

# curl es el más confiable para multipart con múltiples archivos en macOS/Linux
$curlArgs = @(
    "-k", "-s",
    "-o", "/tmp/bactc_sync_response.json",
    "-w", "%{http_code}",
    "-X", "POST",
    "-H", "Authorization: Bearer $TOKEN"
)
foreach ($xlsPath in $XLS_FILES) {
    Write-Info "  Archivo adjuntado: $(Split-Path $xlsPath -Leaf)"
    $curlArgs += @("-F", "files=@$xlsPath")
}
$curlArgs += "$HOST_URL/financial-obligations/sync-bac-financiamientos"

$httpCode = (& curl @curlArgs 2>&1).Trim()
$rawBody  = Get-Content "/tmp/bactc_sync_response.json" -ErrorAction SilentlyContinue

if ($httpCode -ne "202") {
    Write-Host "  HTTP $httpCode : $rawBody" -ForegroundColor Red
    Invoke-Fail "El endpoint devolvió HTTP $httpCode (esperado 202)"
}

$uploadResp = $rawBody | ConvertFrom-Json -ErrorAction SilentlyContinue
Write-Ok "Job encolado correctamente"
Write-Info "  SyncId            : $($uploadResp.syncId)"
Write-Info "  JobId             : $($uploadResp.jobId)"
Write-Info "  Archivos recibidos: $($uploadResp.filesSubmitted)"

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 5 — Esperar a que el job de Hangfire complete"

# Poll: verificar en BD que las obligaciones ya tienen cuotas (máx 30 s).
$maxWaitSec  = 30
$intervalSec = 2
$waited      = 0
$jobDone     = $false

$sqlPoll = @"
SET NOCOUNT ON;
SELECT COUNT(*) FROM dbo.financialObligationInstallment i
JOIN dbo.financialObligation o ON o.idFinancialObligation = i.idFinancialObligation
WHERE o.matchKeyword LIKE '%-8608-%' OR o.matchKeyword LIKE '%-6515-%';
"@

while ($waited -lt $maxWaitSec) {
    Start-Sleep -Seconds $intervalSec
    $waited += $intervalSec
    $pollResult  = Invoke-Sql $sqlPoll
    $cuotasCount = $pollResult | Where-Object { $_ -match '^\s*\d+\s*$' } | Select-Object -First 1
    $n           = if ($cuotasCount) { [int]($cuotasCount.Trim()) } else { 0 }
    Write-Info "  ${waited}s — cuotas en BD: $n"
    if ($n -gt 0) {
        $jobDone = $true
        break
    }
}

if (-not $jobDone) {
    Write-Warn "Tiempo de espera agotado (${maxWaitSec}s). Verificar Hangfire Dashboard en /hangfire"
} else {
    Write-Ok "Job completado — cuotas detectadas en BD"
}

# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 6 — Verificar obligaciones y cuotas en BD"

$sqlResumen = @"
SET NOCOUNT ON;
SELECT
    o.idFinancialObligation     AS id,
    o.nameObligation            AS nombre,
    o.originalAmount            AS montoOriginal,
    cu.codeCurrency             AS moneda,
    o.termMonths                AS plazo,
    a.codeAccount               AS cuenta,
    o.statusObligation          AS estado,
    COUNT(i.idFinancialObligationInstallment)                             AS totalCuotas,
    SUM(CASE WHEN i.statusInstallment='Pagada'    THEN 1 ELSE 0 END)     AS pagadas,
    SUM(CASE WHEN i.statusInstallment='Vigente'   THEN 1 ELSE 0 END)     AS vigentes,
    SUM(CASE WHEN i.statusInstallment='Pendiente' THEN 1 ELSE 0 END)     AS pendientes,
    MIN(CASE WHEN i.statusInstallment IN ('Vigente','Pendiente') THEN CAST(i.dueDate AS varchar) END) AS proximaFecha,
    MIN(CASE WHEN i.statusInstallment IN ('Vigente','Pendiente') THEN i.amountTotal END)              AS proximaCuota
FROM dbo.financialObligation o
LEFT JOIN dbo.financialObligationInstallment i
    ON i.idFinancialObligation = o.idFinancialObligation
JOIN dbo.currency cu ON cu.idCurrency = o.idCurrency
JOIN dbo.account   a ON a.idAccount   = o.idAccountLongTerm
WHERE o.matchKeyword LIKE '%-8608-%' OR o.matchKeyword LIKE '%-6515-%'
GROUP BY o.idFinancialObligation, o.nameObligation, o.originalAmount,
         cu.codeCurrency, o.termMonths, a.codeAccount, o.statusObligation
ORDER BY o.idFinancialObligation;
"@

Write-Ok "Obligaciones Tasa Cero en BD:"
Invoke-Sql $sqlResumen | Where-Object { $_ -match '\w' -and $_ -notmatch '^-' } |
    ForEach-Object { Write-Info "  $_" }

# Estado de cuotas por obligación
$sqlCuotasEstado = @"
SET NOCOUNT ON;
SELECT o.matchKeyword, i.statusInstallment, COUNT(*) AS qty
FROM dbo.financialObligation o
JOIN dbo.financialObligationInstallment i
    ON i.idFinancialObligation = o.idFinancialObligation
WHERE o.matchKeyword LIKE '%-8608-%' OR o.matchKeyword LIKE '%-6515-%'
GROUP BY o.matchKeyword, i.statusInstallment
ORDER BY o.matchKeyword, i.statusInstallment;
"@

Write-Host ""
Write-Ok "Estado de cuotas por obligación:"
Invoke-Sql $sqlCuotasEstado | Where-Object { $_ -match '\w' -and $_ -notmatch '^-' } |
    ForEach-Object { Write-Info "  $_" }

Write-Host ""
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Host "  FIN — Sincronización BAC Tasa Cero completada" -ForegroundColor Green
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Green
Write-Warn "Pendientes para el ciclo completo:"
Write-Warn "  · Actualizar matchKeyword en 01a-keywords.md (Templates 4 y 5)"

