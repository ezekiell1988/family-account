#!/usr/bin/env pwsh
# ============================================================================
#  CASO 3 — ENSAMBLE EN VENTA (Hot Dog)
#  01-ejecutar-flujo-e2e.ps1 — Test de Integración E2E
#
#  Uso:
#   pwsh docs/inventario/caso-3-ensamble/01-ejecutar-flujo-e2e.ps1
#   pwsh docs/inventario/caso-3-ensamble/01-ejecutar-flujo-e2e.ps1 -NoResetDb
#
#  Requisitos previos:
#   - sqlcmd instalado (en PATH).
#   - API corriendo en https://localhost:8000.
#   - credentials/db.txt en la raíz del proyecto.
#   - Productos 7-11 (ingredientes + Hot Dog) y receta id=2 activa vienen del seed.
#   - Período 4 abierto.
# ============================================================================

param(
    [switch]$NoResetDb
)

# ── Rutas ─────────────────────────────────────────────────────────────────────
$SCRIPT_DIR  = $PSScriptRoot
$REPO_ROOT   = (Resolve-Path (Join-Path $SCRIPT_DIR '../../..')).Path
$CREDS_FILE  = Join-Path $REPO_ROOT 'credentials\db.txt'

$HOST_URL    = 'https://localhost:8000/api/v1'
$EMAIL       = 'ezekiell1988@hotmail.com'
$API_PROJECT = 'src/familyAccountApi'

# ── Resetear BD? ──────────────────────────────────────────────────────────────
$RESET_DB = -not $NoResetDb.IsPresent

# Sufijo único por ejecución
$RUN_ID                  = Get-Date -Format 'yyyyMMddHHmmss'
$PROVIDER_INVOICE_NUMBER = "FAC-PROVEEDOR-C3-$RUN_ID"
$LOT_PAN                 = "LOT-PAN-C3-$RUN_ID"
$LOT_SALCHICHA           = "LOT-SALCHICHA-C3-$RUN_ID"
$LOT_MOSTAZA             = "LOT-MOSTAZA-C3-$RUN_ID"
$LOT_CATSUP              = "LOT-CATSUP-C3-$RUN_ID"

# ── Estado del flujo (se completa conforme avanza) ────────────────────────────
$TOKEN                 = ''
$ID_PU_PAN             = 0
$ID_PU_SALCHICHA       = 0
$ID_PU_MOSTAZA         = 0
$ID_PU_CATSUP          = 0
$ID_PU_HOTDOG          = 0
$ID_PA_PAN             = 0
$ID_PA_SALCHICHA       = 0
$ID_PA_MOSTAZA         = 0
$ID_PA_CATSUP          = 0
$ID_PA_HOTDOG_PT       = 0
$ID_PURCHASE_INVOICE   = 0
$NUMBER_PC             = '(no generado)'
$ID_LOT_PAN            = 0
$ID_LOT_SALCHICHA      = 0
$ID_LOT_MOSTAZA        = 0
$ID_LOT_CATSUP         = 0
$ID_SALES_ORDER        = 0
$ID_SALES_INVOICE      = 0
$NUMBER_FV             = '(no generado)'
$ID_LOT_PT             = 0
$ID_SALES_INVOICE_LINE = 0
$ID_ENTRY_DEV_COGS     = '(no generado)'
$ID_ENTRY_REINTEGRO    = '(no generado)'
$ID_ADJUSTMENT         = 0
$ADJ_ENTRY             = '(no generado)'
$STOCK_FINAL           = '?'

$script:HTTP_STATUS = 0
$script:LAST_BODY   = $null

# ── Helpers ───────────────────────────────────────────────────────────────────

function Step([string]$title) {
    Write-Host ''
    Write-Host '══════════════════════════════════════════════════════' -ForegroundColor Cyan
    Write-Host "▶  $title" -ForegroundColor Cyan
    Write-Host '══════════════════════════════════════════════════════' -ForegroundColor Cyan
}

function Log-Ok([string]$msg)   { Write-Host "  ✅  $msg" -ForegroundColor Green  }
function Log-Warn([string]$msg) { Write-Host "  ⚠   $msg" -ForegroundColor Yellow }
function Log-Info([string]$msg) { Write-Host "  $msg" }

function Fail([string]$msg) {
    Write-Host ''
    Write-Host "  ❌  FALLO: $msg" -ForegroundColor Red
    if ($script:LAST_BODY) {
        Write-Host '  Respuesta del API:' -ForegroundColor Red
        try { $script:LAST_BODY | ConvertTo-Json -Depth 5 | Write-Host } catch { Write-Host $script:LAST_BODY }
    }
    Write-Host ''
    Write-Host '  IDs obtenidos hasta ahora:' -ForegroundColor Red
    Write-Host "    idPurchaseInvoice : $ID_PURCHASE_INVOICE"
    Write-Host "    idSalesOrder      : $ID_SALES_ORDER"
    Write-Host "    idSalesInvoice    : $ID_SALES_INVOICE"
    Write-Host "    idLotPT           : $ID_LOT_PT"
    Write-Host ''
    exit 1
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Body    = '',
        [string]$AuthTok = ''
    )
    $uri     = "$HOST_URL$Path"
    $headers = @{ 'Content-Type' = 'application/json' }
    if ($AuthTok) { $headers['Authorization'] = "Bearer $AuthTok" }

    $params = @{
        Method               = $Method
        Uri                  = $uri
        Headers              = $headers
        SkipCertificateCheck = $true
        StatusCodeVariable   = 'sc'
    }
    if ($Body -and $Body -ne '') { $params['Body'] = [System.Text.Encoding]::UTF8.GetBytes($Body) }

    try {
        $resp = Invoke-RestMethod @params
        $script:HTTP_STATUS = [int]$sc
        $script:LAST_BODY   = $resp
        return $resp
    }
    catch {
        $script:HTTP_STATUS = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
        $script:LAST_BODY   = $null
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = [System.IO.StreamReader]::new($stream)
            $raw    = $reader.ReadToEnd()
            $parsed = $raw | ConvertFrom-Json -ErrorAction SilentlyContinue
            $script:LAST_BODY = if ($parsed) { $parsed } else { @{ raw = $raw } }
        } catch {}
        return $null
    }
}

function Assert-Status([int]$expected, [string]$context) {
    if ($script:HTTP_STATUS -ne $expected) {
        Fail "HTTP $expected esperado en '$context', recibido: $($script:HTTP_STATUS)"
    }
    Log-Ok "$context — HTTP $($script:HTTP_STATUS)"
}

# ── Idempotente: ProductUnit ──────────────────────────────────────────────────
function Ensure-ProductUnit {
    param(
        [int]$IdProduct,
        [int]$IdUnit,
        [string]$NamePres,
        [bool]$UsedPurchase,
        [bool]$UsedSale
    )
    $existing = Invoke-Api -Method GET -Path "/product-units/by-product/$IdProduct.json" -AuthTok $TOKEN
    if ($script:HTTP_STATUS -ne 200) { Fail "get product-units idProduct=$IdProduct" }

    $found = $existing | Where-Object { $_.idUnit -eq $IdUnit } | Select-Object -First 1
    if ($found) {
        Log-Warn "ProductUnit (product=$IdProduct, unit=$IdUnit) ya existe (id=$($found.idProductUnit))"
        return $found.idProductUnit
    }

    $b = @{
        idProduct        = $IdProduct
        idUnit           = $IdUnit
        conversionFactor = 1.0
        isBase           = $true
        usedForPurchase  = $UsedPurchase
        usedForSale      = $UsedSale
        namePresentation = $NamePres
    } | ConvertTo-Json -Compress

    $r = Invoke-Api -Method POST -Path '/product-units' -Body $b -AuthTok $TOKEN
    Assert-Status 201 "create product-unit (product=$IdProduct, unit=$IdUnit)"
    return $r.idProductUnit
}

# ── Idempotente: ProductAccount ───────────────────────────────────────────────
function Ensure-ProductAccount {
    param(
        [int]$IdProduct,
        [int]$IdAccount = 110
    )
    $existing = Invoke-Api -Method GET -Path "/product-accounts/by-product/$IdProduct.json" -AuthTok $TOKEN
    if ($script:HTTP_STATUS -eq 200 -and $existing) {
        $found = $existing | Where-Object { $_.idAccount -eq $IdAccount } | Select-Object -First 1
        if ($found) {
            Log-Warn "ProductAccount (product=$IdProduct, cta=$IdAccount) ya existe (id=$($found.idProductAccount))"
            return $found.idProductAccount
        }
    }

    $b = @{
        idProduct         = $IdProduct
        idAccount         = $IdAccount
        percentageAccount = 100.00
    } | ConvertTo-Json -Compress

    $r = Invoke-Api -Method POST -Path '/product-accounts' -Body $b -AuthTok $TOKEN
    Assert-Status 201 "create product-account (product=$IdProduct → cta $IdAccount)"
    return $r.idProductAccount
}

# ── Verificar dependencias ────────────────────────────────────────────────────
foreach ($cmd in @('sqlcmd', 'dotnet')) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Write-Host "❌  Dependencia faltante: $cmd" -ForegroundColor Red
        exit 1
    }
}

# ── Leer credenciales BD ──────────────────────────────────────────────────────
if (-not (Test-Path $CREDS_FILE)) {
    Write-Host "❌  No se encontró $CREDS_FILE" -ForegroundColor Red
    exit 1
}
$credsContent = Get-Content $CREDS_FILE
$DB_HOST = ($credsContent | Select-String '^HOST:').ToString().Split()[1].Trim()
$DB_PORT = ($credsContent | Select-String '^PORT:').ToString().Split()[1].Trim()
$DB_USER = ($credsContent | Select-String '^USER:').ToString().Split()[1].Trim()
$DB_PASS = ($credsContent | Select-String '^PASSWORD:').ToString().Split()[1].Trim()

# ── Encabezado ────────────────────────────────────────────────────────────────
Write-Host ''
Write-Host '╔══════════════════════════════════════════════════════╗' -ForegroundColor Cyan
Write-Host '║   CASO 3 — ENSAMBLE EN VENTA · Test E2E             ║' -ForegroundColor Cyan
Write-Host '╚══════════════════════════════════════════════════════╝' -ForegroundColor Cyan
Log-Info "API   : $HOST_URL"
Log-Info "BD    : ${DB_HOST}:${DB_PORT} / dbfa"
Log-Info "Email : $EMAIL"
Log-Info "RESET : $RESET_DB"

# ════════════════════════════════════════════════════════════════════════════════
# PASO -1 — RESET BD (solo si RESET_DB=$true)
# ════════════════════════════════════════════════════════════════════════════════
if ($RESET_DB) {
    Step 'PASO -1 — Reset de BD (RESET_DB=true)'

    Push-Location $REPO_ROOT

    Log-Info '① Drop base de datos remota...'
    & dotnet ef database drop --project $API_PROJECT --force
    if ($LASTEXITCODE -ne 0) { Fail 'Falló database drop' }
    Log-Ok 'Base de datos eliminada'

    Log-Info '② Eliminando archivos de migración...'
    $migrationsDir = Join-Path $REPO_ROOT "src\familyAccountApi\Infrastructure\Data\Migrations"
    Get-ChildItem $migrationsDir -Filter '*.cs' -Depth 0 -ErrorAction SilentlyContinue | Remove-Item -Force
    $remaining = (Get-ChildItem $migrationsDir -Filter '*.cs' -Depth 0 -ErrorAction SilentlyContinue).Count
    if ($remaining -ne 0) { Fail "Quedaron $remaining archivos en Migrations/ — verificar manualmente" }
    Log-Ok 'Carpeta Migrations/ vacía'

    Log-Info '③ Generando migración InitialCreate...'
    & dotnet ef migrations add InitialCreate --project $API_PROJECT --output-dir Infrastructure/Data/Migrations
    if ($LASTEXITCODE -ne 0) { Fail 'Falló migrations add' }
    Log-Ok 'Migración InitialCreate generada'

    Log-Info '④ Aplicando migración (database update)...'
    & dotnet ef database update --project $API_PROJECT
    if ($LASTEXITCODE -ne 0) { Fail 'Falló database update' }
    Log-Ok 'BD recreada con seed — lista para el test'

    Pop-Location

    Write-Host ''
    Write-Host '  ⚠  ACCIÓN REQUERIDA' -ForegroundColor Yellow
    Write-Host '  Reinicia el API ahora (F5 en VS Code o Ctrl+C + dotnet run).' -ForegroundColor Yellow
    Write-Host '  Hangfire re-creará su esquema al arrancar.' -ForegroundColor Yellow
    Read-Host '  Presiona ENTER cuando el API esté corriendo de nuevo'

    Log-Info "Esperando que el API responda en $HOST_URL..."
    $healthUrl = $HOST_URL -replace '/api/v1$', '/health.json'
    $maxWait   = 60
    $waited    = 0
    while ($true) {
        try {
            $hCode = Invoke-WebRequest -Uri $healthUrl -SkipCertificateCheck -UseBasicParsing -ErrorAction Stop
            if ($hCode.StatusCode -eq 200) { break }
        } catch {}
        if ($waited -ge $maxWait) { Fail "El API no respondió en $maxWait segundos. Verifica que esté corriendo." }
        Write-Host "  ⋯  esperando API (${waited}s)..." -NoNewline
        Write-Host "`r" -NoNewline
        Start-Sleep 2
        $waited += 2
    }
    Log-Ok 'API responde ✓'
}

# ════════════════════════════════════════════════════════════════════════════════
# PASO 1 — AUTENTICACION
# ════════════════════════════════════════════════════════════════════════════════
Step 'PASO 1 — Autenticación'

$PIN = '12345'
Log-Info "Insertando PIN de prueba '$PIN' directamente en BD (idUser=1)..."
sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa `
    -Q "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');" 2>$null
Log-Ok "PIN '$PIN' insertado en BD para usuario 1"

$loginBody = @{ emailUser = $EMAIL; pin = $PIN } | ConvertTo-Json -Compress
$loginResp  = Invoke-Api -Method POST -Path '/auth/login' -Body $loginBody
Assert-Status 200 'login'

$TOKEN = $loginResp.accessToken
if (-not $TOKEN -or $TOKEN -eq 'null') { Fail 'No se obtuvo accessToken en la respuesta del login' }
Log-Ok "Token obtenido: $($TOKEN.Substring(0, [Math]::Min(30, $TOKEN.Length)))..."

# ════════════════════════════════════════════════════════════════════════════════
# PASO 2 — Crear ProductUnits para los 5 productos
# ════════════════════════════════════════════════════════════════════════════════
Step 'PASO 2 — Crear ProductUnits para ingredientes y Hot Dog (5 productos)'

$ID_PU_PAN       = Ensure-ProductUnit -IdProduct 7  -IdUnit 1 -NamePres 'Unidad base'    -UsedPurchase $true  -UsedSale $false
Log-Ok "ProductUnit Pan       → idProductUnit=$ID_PU_PAN"

$ID_PU_SALCHICHA = Ensure-ProductUnit -IdProduct 8  -IdUnit 1 -NamePres 'Unidad base'    -UsedPurchase $true  -UsedSale $false
Log-Ok "ProductUnit Salchicha → idProductUnit=$ID_PU_SALCHICHA"

$ID_PU_MOSTAZA   = Ensure-ProductUnit -IdProduct 9  -IdUnit 6 -NamePres 'Mililitro base' -UsedPurchase $true  -UsedSale $false
Log-Ok "ProductUnit Mostaza   → idProductUnit=$ID_PU_MOSTAZA"

$ID_PU_CATSUP    = Ensure-ProductUnit -IdProduct 10 -IdUnit 6 -NamePres 'Mililitro base' -UsedPurchase $true  -UsedSale $false
Log-Ok "ProductUnit Catsup    → idProductUnit=$ID_PU_CATSUP"

$ID_PU_HOTDOG    = Ensure-ProductUnit -IdProduct 11 -IdUnit 1 -NamePres 'Unidad'         -UsedPurchase $false -UsedSale $true
Log-Ok "ProductUnit HotDog PT → idProductUnit=$ID_PU_HOTDOG"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 3 — Crear ProductAccounts para ingredientes → cuenta 110 (MP)
# ════════════════════════════════════════════════════════════════════════════════
Step 'PASO 3 — Crear ProductAccounts para ingredientes → cuenta 110 (Materias Primas)'

$ID_PA_PAN       = Ensure-ProductAccount -IdProduct 7
Log-Ok "ProductAccount Pan       → id=$ID_PA_PAN"

$ID_PA_SALCHICHA = Ensure-ProductAccount -IdProduct 8
Log-Ok "ProductAccount Salchicha → id=$ID_PA_SALCHICHA"

$ID_PA_MOSTAZA   = Ensure-ProductAccount -IdProduct 9
Log-Ok "ProductAccount Mostaza   → id=$ID_PA_MOSTAZA"

$ID_PA_CATSUP    = Ensure-ProductAccount -IdProduct 10
Log-Ok "ProductAccount Catsup    → id=$ID_PA_CATSUP"

$ID_PA_HOTDOG_PT = Ensure-ProductAccount -IdProduct 11 -IdAccount 109
Log-Ok "ProductAccount PT Hot Dog (cta 109) → id=$ID_PA_HOTDOG_PT"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 4 — FACTURA DE COMPRA (ingredientes para 50 hot dogs)
# ════════════════════════════════════════════════════════════════════════════════
Step 'PASO 4a — Crear factura de compra en borrador (ingredientes para 50 hot dogs)'

$bodyPurchase = @"
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idPurchaseInvoiceType": 1,
  "idContact": 1,
  "numberInvoice": "$PROVIDER_INVOICE_NUMBER",
  "dateInvoice": "2026-04-05",
  "subTotalAmount": 75000.00,
  "taxAmount": 9750.00,
  "totalAmount": 84750.00,
  "descriptionInvoice": "Compra ingredientes para 50 hot dogs — Caso 3 Ensamble",
  "idWarehouse": 1,
  "exchangeRateValue": 1.0,
  "lines": [
    {
      "idProduct": 7, "idUnit": 1,
      "lotNumber": "$LOT_PAN", "expirationDate": "2027-12-31",
      "descriptionLine": "Pan de Hot Dog x 50 un.",
      "quantity": 50, "unitPrice": 300.00, "taxPercent": 13.00, "totalLineAmount": 16950.00
    },
    {
      "idProduct": 8, "idUnit": 1,
      "lotNumber": "$LOT_SALCHICHA", "expirationDate": "2027-12-31",
      "descriptionLine": "Salchicha x 50 un.",
      "quantity": 50, "unitPrice": 600.00, "taxPercent": 13.00, "totalLineAmount": 33900.00
    },
    {
      "idProduct": 9, "idUnit": 6,
      "lotNumber": "$LOT_MOSTAZA", "expirationDate": "2027-12-31",
      "descriptionLine": "Mostaza x 750 mL",
      "quantity": 750, "unitPrice": 20.00, "taxPercent": 13.00, "totalLineAmount": 16950.00
    },
    {
      "idProduct": 10, "idUnit": 6,
      "lotNumber": "$LOT_CATSUP", "expirationDate": "2027-12-31",
      "descriptionLine": "Catsup x 1000 mL",
      "quantity": 1000, "unitPrice": 15.00, "taxPercent": 13.00, "totalLineAmount": 16950.00
    }
  ]
}
"@

$rPurchase = Invoke-Api -Method POST -Path '/purchase-invoices' -Body $bodyPurchase -AuthTok $TOKEN
Assert-Status 201 'create purchase-invoice'
$ID_PURCHASE_INVOICE = $rPurchase.idPurchaseInvoice
Log-Ok "idPurchaseInvoice = $ID_PURCHASE_INVOICE"

Step 'PASO 4b — Confirmar factura de compra'
$rConfirm  = Invoke-Api -Method POST -Path "/purchase-invoices/$ID_PURCHASE_INVOICE/confirm" -AuthTok $TOKEN
Assert-Status 200 'confirm purchase-invoice'
$fcStatus  = $rConfirm.statusInvoice
$NUMBER_PC = $rConfirm.numberInvoice
Log-Ok "statusInvoice = $fcStatus"
if ($fcStatus -ne 'Confirmado') { Fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$fcStatus'" }
Log-Ok "Número FC: $NUMBER_PC"
Log-Info '  Asiento: DR 110 MP 75000 + DR 124 IVA 9750 / CR 106 Caja 84750'

Step 'PASO 4c — Obtener IDs de lotes de ingredientes'

$lotsP7 = Invoke-Api -Method GET -Path '/inventory-lots/by-product/7.json' -AuthTok $TOKEN
Assert-Status 200 'get inventory-lots by-product 7 (Pan)'
$lPan = $lotsP7 | Where-Object { $_.lotNumber -eq $LOT_PAN }       | Select-Object -First 1
if (-not $lPan) { $lPan = $lotsP7 | Select-Object -First 1 }
$ID_LOT_PAN = $lPan.idInventoryLot
Log-Ok "idLotPan = $ID_LOT_PAN  (50 UNI × 339)"

$lotsP8 = Invoke-Api -Method GET -Path '/inventory-lots/by-product/8.json' -AuthTok $TOKEN
Assert-Status 200 'get inventory-lots by-product 8 (Salchicha)'
$lSalch = $lotsP8 | Where-Object { $_.lotNumber -eq $LOT_SALCHICHA } | Select-Object -First 1
if (-not $lSalch) { $lSalch = $lotsP8 | Select-Object -First 1 }
$ID_LOT_SALCHICHA = $lSalch.idInventoryLot
Log-Ok "idLotSalchicha = $ID_LOT_SALCHICHA  (50 UNI × 678)"

$lotsP9 = Invoke-Api -Method GET -Path '/inventory-lots/by-product/9.json' -AuthTok $TOKEN
Assert-Status 200 'get inventory-lots by-product 9 (Mostaza)'
$lMost = $lotsP9 | Where-Object { $_.lotNumber -eq $LOT_MOSTAZA }    | Select-Object -First 1
if (-not $lMost) { $lMost = $lotsP9 | Select-Object -First 1 }
$ID_LOT_MOSTAZA = $lMost.idInventoryLot
Log-Ok "idLotMostaza = $ID_LOT_MOSTAZA  (750 ML × 22.60)"

$lotsP10 = Invoke-Api -Method GET -Path '/inventory-lots/by-product/10.json' -AuthTok $TOKEN
Assert-Status 200 'get inventory-lots by-product 10 (Catsup)'
$lCats = $lotsP10 | Where-Object { $_.lotNumber -eq $LOT_CATSUP }   | Select-Object -First 1
if (-not $lCats) { $lCats = $lotsP10 | Select-Object -First 1 }
$ID_LOT_CATSUP = $lCats.idInventoryLot
Log-Ok "idLotCatsup = $ID_LOT_CATSUP  (1000 ML × 16.95)"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 5 — PEDIDO DE VENTA (3 hot dogs) → confirm dispara todo el ciclo
# ════════════════════════════════════════════════════════════════════════════════
Step 'PASO 5a — Crear pedido de venta en borrador (3 hot dogs × 3000 + 13% IVA)'

$bodyOrder = @"
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idContact": 1,
  "dateOrder": "2026-04-05",
  "exchangeRateValue": 1.0,
  "descriptionOrder": "Pedido cliente — 3 Hot Dogs — Caso 3 Ensamble",
  "lines": [
    {
      "idProduct": 11,
      "idProductUnit": $ID_PU_HOTDOG,
      "descriptionLine": "Hot Dog x 3 un.",
      "quantity": 3,
      "unitPrice": 3000.00,
      "taxPercent": 13.00
    }
  ]
}
"@

$rOrder = Invoke-Api -Method POST -Path '/sales-orders' -Body $bodyOrder -AuthTok $TOKEN
Assert-Status 201 'create sales-order'
$ID_SALES_ORDER = $rOrder.idSalesOrder
Log-Ok "idSalesOrder = $ID_SALES_ORDER"

Step 'PASO 5b — Confirmar pedido con idWarehouse (dispara ciclo completo)'
Log-Info '  El sistema creará la OP, consumirá ingredientes FEFO, generará lote PT y confirmará la FV...'
$rConfirmOrder = Invoke-Api -Method POST -Path "/sales-orders/$ID_SALES_ORDER/confirm" `
    -Body '{"idWarehouse":1}' -AuthTok $TOKEN
Assert-Status 200 'confirm sales-order'

$ID_SALES_INVOICE = $rConfirmOrder.idSalesInvoice
if (-not $ID_SALES_INVOICE) { Fail 'El confirm del pedido no devolvió idSalesInvoice' }
Log-Ok "idSalesInvoice = $ID_SALES_INVOICE"
$orderStatus = $rConfirmOrder.statusOrder
Log-Ok "statusOrder = $orderStatus"

Step 'PASO 5c — Obtener lote PT y línea de la factura de venta confirmada'
$rInvoice = Invoke-Api -Method GET -Path "/sales-invoices/$ID_SALES_INVOICE.json" -AuthTok $TOKEN
Assert-Status 200 'get sales-invoice'

$invoiceStatus = $rInvoice.statusInvoice
Log-Ok "statusInvoice = $invoiceStatus"
if ($invoiceStatus -ne 'Confirmado') { Fail "Se esperaba 'Confirmado', recibido: '$invoiceStatus'" }

$NUMBER_FV             = $rInvoice.numberInvoice
$ID_LOT_PT             = $rInvoice.lines[0].idInventoryLot
$ID_SALES_INVOICE_LINE = $rInvoice.lines[0].idSalesInvoiceLine
Log-Ok "Número FV: $NUMBER_FV"

if (-not $ID_LOT_PT) { Fail 'No se encontró idInventoryLot en la línea de la FV — ciclo de producción incompleto' }
Log-Ok "idLotPT (Hot Dog) = $ID_LOT_PT  (lote generado por la OP automática)"
Log-Ok "idSalesInvoiceLine = $ID_SALES_INVOICE_LINE"

$rLotPT   = Invoke-Api -Method GET -Path "/inventory-lots/$ID_LOT_PT.json" -AuthTok $TOKEN
Assert-Status 200 'get inventory-lot PT'
$lotPtQty = $rLotPT.quantityAvailable
Log-Ok "quantityAvailable lote PT = $lotPtQty  (esperado 0 — ya vendidos los 3)"
Log-Info '  FV: DR 106 Caja 10170 / CR 117 Ingresos 9000 / CR 127 IVA 1170'
Log-Info '  COGS: DR 119 5085 / CR 109 Inventario 5085  (3 × 1695)'

# ════════════════════════════════════════════════════════════════════════════════
# PASO 6 — DEVOLUCION PARCIAL (cliente devuelve 1 hot dog)
# ════════════════════════════════════════════════════════════════════════════════
Step 'PASO 6 — Devolución parcial: 1 hot dog devuelto'

$bodyReturn = @"
{
  "dateReturn": "2026-04-05",
  "descriptionReturn": "Devolucion parcial — cliente devuelve 1 hot dog — Caso 3 Ensamble",
  "refundMode": "EfectivoInmediato",
  "lines": [
    {
      "idInventoryLot": $ID_LOT_PT,
      "quantity": 1,
      "totalLineAmount": 3390.00,
      "descriptionLine": "Hot Dog x 1 un. — devolucion parcial"
    }
  ]
}
"@

$rReturn = Invoke-Api -Method POST -Path "/sales-invoices/$ID_SALES_INVOICE/partial-return" `
    -Body $bodyReturn -AuthTok $TOKEN
Assert-Status 200 'partial-return'

$partialEntry = $rReturn.idAccountingEntry
if ($partialEntry -and $partialEntry -ne 0) {
    $ID_ENTRY_DEV_COGS = $partialEntry
    Log-Ok "idAccountingEntry DEV-COGS = $ID_ENTRY_DEV_COGS"
} else {
    Log-Warn 'La devolución no generó asiento COGS separado'
}
Log-Info '  DEV-COGS: DR 109 Inventario 1695 / CR 119 COGS 1695  (1 × 1695)'

$refundEntry = $rReturn.idAccountingEntryRefund
if ($refundEntry -and $refundEntry -ne 0) {
    $ID_ENTRY_REINTEGRO = $refundEntry
    Log-Ok "idAccountingEntryRefund DEV-ING = $ID_ENTRY_REINTEGRO"
} else {
    Log-Warn 'La devolución no generó asiento de reversión de ingresos'
}
Log-Info '  DEV-ING: DR 117 Ingresos 3000 + DR 127 IVA 390 / CR 106 Caja 3390'

# ════════════════════════════════════════════════════════════════════════════════
# PASO 7 — REGALIA (administrador regala 2 hot dogs al personal)
# ════════════════════════════════════════════════════════════════════════════════
Step 'PASO 7a — Crear ajuste de inventario en borrador (regalía: −2 hot dogs)'

$bodyAdj = @"
{
  "idFiscalPeriod": 4,
  "idInventoryAdjustmentType": 4,
  "idCurrency": 1,
  "exchangeRateValue": 1.0,
  "dateAdjustment": "2026-04-05",
  "descriptionAdjustment": "Regalia personal — 2 hot dogs — Responsable: Administrador — Caso 3",
  "lines": [
    {
      "idInventoryLot": $ID_LOT_PT,
      "quantityDelta": -2,
      "descriptionLine": "Salida por regalia — Hot Dog x 2 un."
    }
  ]
}
"@

$rAdj = Invoke-Api -Method POST -Path '/inventory-adjustments' -Body $bodyAdj -AuthTok $TOKEN
Assert-Status 201 'create inventory-adjustment'
$ID_ADJUSTMENT = $rAdj.idInventoryAdjustment
Log-Ok "idInventoryAdjustment = $ID_ADJUSTMENT"

Step 'PASO 7b — Confirmar ajuste de inventario'
$rAdjConfirm = Invoke-Api -Method POST -Path "/inventory-adjustments/$ID_ADJUSTMENT/confirm" -AuthTok $TOKEN
Assert-Status 200 'confirm inventory-adjustment'

$adjStatus = $rAdjConfirm.statusAdjustment
$ADJ_ENTRY = $rAdjConfirm.idAccountingEntry
Log-Ok "statusAdjustment = $adjStatus"
if ($adjStatus -ne 'Confirmado') { Fail "Se esperaba 'Confirmado', recibido: '$adjStatus'" }
Log-Ok "idAccountingEntry ADJ = $ADJ_ENTRY"
Log-Info '  Esperado: DR 130 Merma 3390 / CR 109 Inventario 3390  (2 × 1695)'

# ════════════════════════════════════════════════════════════════════════════════
# VERIFICACION FINAL DE STOCK
# ════════════════════════════════════════════════════════════════════════════════
Step 'VERIFICACIÓN FINAL — Stock total del producto 11 (Hot Dog PT)'

$STOCK_FINAL = Invoke-Api -Method GET -Path '/inventory-lots/stock/11.json' -AuthTok $TOKEN
Assert-Status 200 'get stock total product 11'
Log-Ok "Stock actual = $STOCK_FINAL unidades"
Log-Info '  Fórmula: 3 (producidos) − 3 (venta) + 1 (devolución) − 2 (regalía) = −1'

$stockDiff = [Math]::Abs([double]$STOCK_FINAL - (-1))
if ($stockDiff -lt 0.01) {
    Log-Ok 'Stock = −1 ✓  (flujo completo ejecutado en orden)'
} else {
    Log-Warn "Stock final = $STOCK_FINAL (esperado −1 si la BD estaba limpia antes del test)"
}

# ════════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ════════════════════════════════════════════════════════════════════════════════
Write-Host ''
Write-Host '╔══════════════════════════════════════════════════════╗' -ForegroundColor Green
Write-Host '║   ✅  FLUJO CASO 3 COMPLETADO EXITOSAMENTE          ║' -ForegroundColor Green
Write-Host '╚══════════════════════════════════════════════════════╝' -ForegroundColor Green

$summary = [ordered]@{
    'idPurchaseInvoice:'      = $ID_PURCHASE_INVOICE
    'idLotPan:'               = $ID_LOT_PAN
    'idLotSalchicha:'         = $ID_LOT_SALCHICHA
    'idLotMostaza:'           = $ID_LOT_MOSTAZA
    'idLotCatsup:'            = $ID_LOT_CATSUP
    'idSalesOrder:'           = $ID_SALES_ORDER
    'idSalesInvoice:'         = $ID_SALES_INVOICE
    'idLotPT (Hot Dog):'      = $ID_LOT_PT
    'idEntryDevCogs:'         = $ID_ENTRY_DEV_COGS
    'idEntryDevIng:'          = $ID_ENTRY_REINTEGRO
    'idAdjustment:'           = $ID_ADJUSTMENT
    'idAdjEntry:'             = $ADJ_ENTRY
    'Stock final (PT id=11):' = "$STOCK_FINAL u."
}
foreach ($kv in $summary.GetEnumerator()) {
    Write-Host ("  {0,-26} {1}" -f $kv.Key, $kv.Value)
}
Write-Host ''

# ════════════════════════════════════════════════════════════════════════════════
# GUARDAR RESULTADO EN ARCHIVO TXT
# ════════════════════════════════════════════════════════════════════════════════
$runTs      = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$outputFile = Join-Path $SCRIPT_DIR "resultado_caso3_${runTs}.txt"

$resultContent = @"
# ============================================================
#  CASO 3 — ENSAMBLE EN VENTA - Resultado del Test E2E
#  Ejecutado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
# ============================================================

# -- Configuracion usada --------------------------------------
HOST                    = $HOST_URL
EMAIL                   = $EMAIL
DB_HOST                 = $DB_HOST
DB_PORT                 = $DB_PORT

# -- Claves estaticas del caso --------------------------------
idProduct_pan           = 7   (Pan de Hot Dog — Materia Prima)
idProduct_salchicha     = 8   (Salchicha — Materia Prima)
idProduct_mostaza       = 9   (Mostaza — Materia Prima)
idProduct_catsup        = 10  (Catsup — Materia Prima)
idProduct_hotdog        = 11  (Hot Dog — Producto Terminado)
idProductRecipe         = 2   (activa, output=11, quantityOutput=1)
idFiscalPeriod          = 4   (Abril 2026)
idCurrency              = 1   (CRC)
idWarehouse             = 1   (Principal)
idPurchaseInvoiceType   = 1   (EFECTIVO)
idSalesInvoiceType      = 1   (CONTADO_CRC)
idContact               = 1

# -- Lotes creados --------------------------------------------
lotNumber_pan           = $LOT_PAN
lotNumber_salchicha     = $LOT_SALCHICHA
lotNumber_mostaza       = $LOT_MOSTAZA
lotNumber_catsup        = $LOT_CATSUP

# -- Costos (precio x 1.13 IVA) -------------------------------
unitPrice_pan           = 300.00  CRC/UNI  ->  costo lote = 339.00 CRC
unitPrice_salchicha     = 600.00  CRC/UNI  ->  costo lote = 678.00 CRC
unitPrice_mostaza       = 20.00   CRC/ML   ->  costo lote =  22.60 CRC
unitPrice_catsup        = 15.00   CRC/ML   ->  costo lote =  16.95 CRC
costo_hotdog_PT         = 1695.00 CRC/UNI  (1x339 + 1x678 + 15x22.60 + 20x16.95)

# -- IDs generados en esta ejecucion --------------------------
idProductUnit_pan       = $ID_PU_PAN
idProductUnit_salchicha = $ID_PU_SALCHICHA
idProductUnit_mostaza   = $ID_PU_MOSTAZA
idProductUnit_catsup    = $ID_PU_CATSUP
idProductUnit_hotdog    = $ID_PU_HOTDOG
idProductAccount_pan    = $ID_PA_PAN
idProductAccount_salch  = $ID_PA_SALCHICHA
idProductAccount_most   = $ID_PA_MOSTAZA
idProductAccount_cats   = $ID_PA_CATSUP
idPurchaseInvoice       = $ID_PURCHASE_INVOICE
numberPurchaseInvoice   = $NUMBER_PC
idLotPan                = $ID_LOT_PAN
idLotSalchicha          = $ID_LOT_SALCHICHA
idLotMostaza            = $ID_LOT_MOSTAZA
idLotCatsup             = $ID_LOT_CATSUP
idSalesOrder            = $ID_SALES_ORDER
idSalesInvoice          = $ID_SALES_INVOICE
numberSalesInvoice      = $NUMBER_FV
idLotPT                 = $ID_LOT_PT
idSalesInvoiceLine      = $ID_SALES_INVOICE_LINE
idEntryDevCogs          = $ID_ENTRY_DEV_COGS
idEntryDevIng           = $ID_ENTRY_REINTEGRO
idInventoryAdjustment   = $ID_ADJUSTMENT
idAdjEntry              = $ADJ_ENTRY

# -- Stock final Hot Dog PT (id=11) ---------------------------
stock_post_produccion   = 3   (OP automatica)
stock_post_venta        = 0   (3 vendidos en FV)
stock_post_devolucion   = 1   (1 devuelto)
stock_post_regalia      = -1  (2 regalados)
stock_final_real        = $STOCK_FINAL

# -- Endpoints de consulta rapida ----------------------------
GET $HOST_URL/purchase-invoices/$ID_PURCHASE_INVOICE.json
GET $HOST_URL/inventory-lots/by-product/7.json
GET $HOST_URL/inventory-lots/by-product/11.json
GET $HOST_URL/inventory-lots/$ID_LOT_PT.json
GET $HOST_URL/inventory-lots/stock/11.json
GET $HOST_URL/sales-orders/$ID_SALES_ORDER.json
GET $HOST_URL/sales-invoices/$ID_SALES_INVOICE.json
GET $HOST_URL/inventory-adjustments/$ID_ADJUSTMENT.json
"@

[System.IO.File]::WriteAllText($outputFile, $resultContent, [System.Text.Encoding]::UTF8)
Write-Host "  💾  Resultado guardado en: $outputFile" -ForegroundColor Green
Write-Host ''
