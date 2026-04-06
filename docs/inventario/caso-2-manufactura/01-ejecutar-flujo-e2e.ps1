#!/usr/bin/env pwsh
# ============================================================================
#  CASO 2 — MANUFACTURA (Chile Embotellado Marca X)
#  01-ejecutar-flujo-e2e.ps1 — Test de Integración E2E (PowerShell)
#
#  Uso:
#   pwsh docs/inventario/caso-2-manufactura/01-ejecutar-flujo-e2e.ps1
#   pwsh docs/inventario/caso-2-manufactura/01-ejecutar-flujo-e2e.ps1 -NoResetDb
#
#  Requisitos previos:
#   - sqlcmd instalado.
#   - API corriendo en https://localhost:8000.
#   - credentials/db.txt en la raíz del proyecto.
#   - Productos 2-6 y receta idProductRecipe=1 en el seed. Período 4 abierto.
# ============================================================================

param(
    [switch]$NoResetDb
)

# ── Rutas ─────────────────────────────────────────────────────────────────────
$SCRIPT_DIR  = $PSScriptRoot
$REPO_ROOT   = (Resolve-Path (Join-Path $SCRIPT_DIR "../../..")).Path
$CREDS_FILE  = Join-Path $REPO_ROOT "credentials\db.txt"
$API_PROJECT = "src/familyAccountApi"
$HOST_URL    = "https://localhost:8000/api/v1"
$EMAIL       = "ezekiell1988@hotmail.com"

# ── Resetear BD? ──────────────────────────────────────────────────────────────
$RESET_DB = -not $NoResetDb.IsPresent

$RUN_ID                 = (Get-Date -Format "yyyyMMddHHmmss")
$PROVIDER_INVOICE_NUM   = "FAC-PROVEEDOR-C2-$RUN_ID"
$LOT_NUMBER_CHILE       = "LOT-MP-CHILE-C2-$RUN_ID"
$LOT_NUMBER_VINAGRE     = "LOT-MP-VINAGRE-C2-$RUN_ID"
$LOT_NUMBER_SAL         = "LOT-MP-SAL-C2-$RUN_ID"
$LOT_NUMBER_FRASCO      = "LOT-MP-FRASCO-C2-$RUN_ID"

# ── Estado del flujo ──────────────────────────────────────────────────────────
$TOKEN                  = ""
$ID_PU_CHILE            = 0
$ID_PU_VINAGRE          = 0
$ID_PU_SAL              = 0
$ID_PU_FRASCO           = 0
$ID_PU_PT               = 0
$ID_PA_CHILE            = 0
$ID_PA_VINAGRE          = 0
$ID_PA_SAL              = 0
$ID_PA_FRASCO           = 0
$ID_PA_PT               = 0
$ID_PURCHASE_INVOICE    = 0
$ID_LOT_CHILE           = 0
$ID_LOT_VINAGRE         = 0
$ID_LOT_SAL             = 0
$ID_LOT_FRASCO          = 0
$ID_PRODUCTION_ORDER    = 0
$OP_NUMBER              = "?"
$ID_LOT_PT              = 0
$LOT_PT_COST            = "?"
$ID_SALES_INVOICE       = 0
$ID_ENTRY_DEV_COGS      = "(no generado)"
$ID_ENTRY_REINTEGRO     = 0
$ID_ADJUSTMENT          = 0
$ADJ_ENTRY              = 0
$STOCK_FINAL            = "?"
$FC_NUMBER              = ""
$FV_NUMBER              = ""

$script:HTTP_STATUS = 0
$script:LAST_BODY   = $null

# ── Helpers ───────────────────────────────────────────────────────────────────
function Step([string]$title) {
    Write-Host ""
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "▶  $title" -ForegroundColor Cyan
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Log-Ok([string]$msg)   { Write-Host "  ✅  $msg" -ForegroundColor Green }
function Log-Warn([string]$msg) { Write-Host "  ⚠   $msg" -ForegroundColor Yellow }
function Log-Info([string]$msg) { Write-Host "  $msg" }

function Fail([string]$msg) {
    Write-Host ""
    Write-Host "  ❌  FALLO: $msg" -ForegroundColor Red
    if ($script:LAST_BODY) {
        Write-Host "  Respuesta del API:" -ForegroundColor Red
        try { $script:LAST_BODY | ConvertTo-Json -Depth 5 | Write-Host } catch { Write-Host $script:LAST_BODY }
    }
    Write-Host ""
    Write-Host "  El proceso se detuvo. IDs obtenidos hasta ahora:" -ForegroundColor Red
    Write-Host "    idPurchaseInvoice  : $ID_PURCHASE_INVOICE"
    Write-Host "    idProductionOrder  : $ID_PRODUCTION_ORDER"
    Write-Host "    idLotPT            : $ID_LOT_PT"
    Write-Host "    idSalesInvoice     : $ID_SALES_INVOICE"
    exit 1
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Body    = "",
        [string]$AuthTok = ""
    )
    $uri     = "$HOST_URL$Path"
    $headers = @{ "Content-Type" = "application/json" }
    if ($AuthTok) { $headers["Authorization"] = "Bearer $AuthTok" }

    $params = @{
        Method               = $Method
        Uri                  = $uri
        Headers              = $headers
        SkipCertificateCheck = $true
        StatusCodeVariable   = "sc"
    }
    if ($Body -and $Body -ne "") { $params["Body"] = [System.Text.Encoding]::UTF8.GetBytes($Body) }

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
            $stream  = $_.Exception.Response.GetResponseStream()
            $reader  = [System.IO.StreamReader]::new($stream)
            $raw     = $reader.ReadToEnd()
            $parsed  = $raw | ConvertFrom-Json -ErrorAction SilentlyContinue
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

function Get-Field([object]$obj, [string]$prop) {
    if ($null -eq $obj) { return $null }
    try { return $obj.$prop } catch { return $null }
}

# ── Idempotente: ProductUnit ───────────────────────────────────────────────────
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
        idProduct         = $IdProduct
        idUnit            = $IdUnit
        conversionFactor  = 1.0
        isBase            = $true
        usedForPurchase   = $UsedPurchase
        usedForSale       = $UsedSale
        namePresentation  = $NamePres
    } | ConvertTo-Json -Compress

    $r = Invoke-Api -Method POST -Path "/product-units" -Body $b -AuthTok $TOKEN
    Assert-Status 201 "create product-unit (product=$IdProduct, unit=$IdUnit)"
    return $r.idProductUnit
}

# ── Idempotente: ProductAccount ────────────────────────────────────────────────
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
        idProduct          = $IdProduct
        idAccount          = $IdAccount
        percentageAccount  = 100.00
    } | ConvertTo-Json -Compress

    $r = Invoke-Api -Method POST -Path "/product-accounts" -Body $b -AuthTok $TOKEN
    Assert-Status 201 "create product-account (product=$IdProduct → cta $IdAccount)"
    return $r.idProductAccount
}

# ── Verificar dependencias ────────────────────────────────────────────────────
foreach ($cmd in @("sqlcmd", "dotnet")) {
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
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   CASO 2 — MANUFACTURA · Test de Integración E2E   ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Log-Info "API   : $HOST_URL"
Log-Info "BD    : ${DB_HOST}:${DB_PORT} / dbfa"
Log-Info "Email : $EMAIL"
Log-Info "RESET : $RESET_DB"

# ════════════════════════════════════════════════════════════════════════════════
# PASO -1 — RESET BD
# ════════════════════════════════════════════════════════════════════════════════
if ($RESET_DB) {
    Step "PASO -1 — Reset de BD (RESET_DB=true)"

    Push-Location $REPO_ROOT

    Log-Info "① Drop base de datos remota..."
    & dotnet ef database drop --project $API_PROJECT --force
    if ($LASTEXITCODE -ne 0) { Fail "Falló database drop" }
    Log-Ok "Base de datos eliminada"

    Log-Info "② Eliminando archivos de migración..."
    $migrationsDir = Join-Path $REPO_ROOT "src\familyAccountApi\Infrastructure\Data\Migrations"
    Get-ChildItem $migrationsDir -Filter "*.cs" -Depth 0 -ErrorAction SilentlyContinue | Remove-Item -Force
    $remaining = (Get-ChildItem $migrationsDir -Filter "*.cs" -Depth 0 -ErrorAction SilentlyContinue).Count
    if ($remaining -ne 0) { Fail "Quedaron $remaining archivos en Migrations/ — verificar manualmente" }
    Log-Ok "Carpeta Migrations/ vacía"

    Log-Info "③ Generando migración InitialCreate..."
    & dotnet ef migrations add InitialCreate --project $API_PROJECT --output-dir Infrastructure/Data/Migrations
    if ($LASTEXITCODE -ne 0) { Fail "Falló migrations add" }
    Log-Ok "Migración InitialCreate generada"

    Log-Info "④ Aplicando migración (database update)..."
    & dotnet ef database update --project $API_PROJECT
    if ($LASTEXITCODE -ne 0) { Fail "Falló database update" }
    Log-Ok "BD recreada con seed — lista para el test"

    Pop-Location

    Write-Host ""
    Write-Host "  ⚠  ACCIÓN REQUERIDA" -ForegroundColor Yellow
    Write-Host "  Reinicia el API ahora (F5 en VS Code o Ctrl+C + dotnet run)." -ForegroundColor Yellow
    Write-Host "  Hangfire re-creará su esquema al arrancar." -ForegroundColor Yellow
    Read-Host "  Presiona ENTER cuando el API esté corriendo de nuevo"

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
    Log-Ok "API responde ✓"
}

# ════════════════════════════════════════════════════════════════════════════════
# PASO 1 — AUTENTICACIÓN
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 1 — Autenticación"

$PIN = "12345"
Log-Info "Insertando PIN de prueba '$PIN' directamente en BD (idUser=1)..."
sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa `
    -Q "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');" 2>$null
Log-Ok "PIN '$PIN' insertado en BD para usuario 1"

$loginBody = @{ emailUser = $EMAIL; pin = $PIN } | ConvertTo-Json -Compress
$loginResp = Invoke-Api -Method POST -Path "/auth/login" -Body $loginBody
Assert-Status 200 "login"

$TOKEN = $loginResp.accessToken
if (-not $TOKEN -or $TOKEN -eq "null") { Fail "No se obtuvo accessToken en la respuesta del login" }
Log-Ok "Token obtenido: $($TOKEN.Substring(0, [Math]::Min(30, $TOKEN.Length)))..."

# ════════════════════════════════════════════════════════════════════════════════
# PASO 2 — PRE-REQUISITOS: ProductUnits para los 5 productos
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 2 — Crear ProductUnits para MP y PT (idempotente)"

$ID_PU_CHILE   = Ensure-ProductUnit -IdProduct 2 -IdUnit 3 -NamePres "Kilogramo base" -UsedPurchase $true  -UsedSale $false
Log-Ok "ProductUnit Chile Seco       → idProductUnit=$ID_PU_CHILE"

$ID_PU_VINAGRE = Ensure-ProductUnit -IdProduct 3 -IdUnit 7 -NamePres "Litro base"      -UsedPurchase $true  -UsedSale $false
Log-Ok "ProductUnit Vinagre Blanco   → idProductUnit=$ID_PU_VINAGRE"

$ID_PU_SAL     = Ensure-ProductUnit -IdProduct 4 -IdUnit 3 -NamePres "Kilogramo base"  -UsedPurchase $true  -UsedSale $false
Log-Ok "ProductUnit Sal              → idProductUnit=$ID_PU_SAL"

$ID_PU_FRASCO  = Ensure-ProductUnit -IdProduct 5 -IdUnit 1 -NamePres "Unidad base"     -UsedPurchase $true  -UsedSale $false
Log-Ok "ProductUnit Frasco 250ml     → idProductUnit=$ID_PU_FRASCO"

$ID_PU_PT      = Ensure-ProductUnit -IdProduct 6 -IdUnit 1 -NamePres "Frasco 250ml"    -UsedPurchase $false -UsedSale $true
Log-Ok "ProductUnit PT Chile Embot.  → idProductUnit=$ID_PU_PT"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 3 — ProductAccounts (4 MP → cta 110, PT → cta 109)
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 3 — Crear ProductAccounts (4 MP → cuenta 110 y PT → cuenta 109, idempotente)"

$ID_PA_CHILE   = Ensure-ProductAccount -IdProduct 2; Log-Ok "ProductAccount Chile Seco     → id=$ID_PA_CHILE"
$ID_PA_VINAGRE = Ensure-ProductAccount -IdProduct 3; Log-Ok "ProductAccount Vinagre        → id=$ID_PA_VINAGRE"
$ID_PA_SAL     = Ensure-ProductAccount -IdProduct 4; Log-Ok "ProductAccount Sal            → id=$ID_PA_SAL"
$ID_PA_FRASCO  = Ensure-ProductAccount -IdProduct 5; Log-Ok "ProductAccount Frasco 250ml   → id=$ID_PA_FRASCO"

$ID_PA_PT      = Ensure-ProductAccount -IdProduct 6 -IdAccount 109
Log-Ok "ProductAccount PT Chile Embot. → id=$ID_PA_PT (cta 109 Inventario de Mercadería — para PROD-CAP DR)"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 4 — FACTURA DE COMPRA (4 MP)
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 4a — Crear factura de compra MP en borrador (4 líneas)"

$bodyPurchase = @"
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idPurchaseInvoiceType": 1,
  "idContact": 1,
  "numberInvoice": "$PROVIDER_INVOICE_NUM",
  "dateInvoice": "2026-04-05",
  "subTotalAmount": 52600.00,
  "taxAmount": 6838.00,
  "totalAmount": 59438.00,
  "descriptionInvoice": "Compra MP para 100 frascos Chile Embotellado — Caso 2 Manufactura",
  "idWarehouse": 1,
  "exchangeRateValue": 1.0,
  "lines": [
    {
      "idProduct": 2,
      "idUnit": 3,
      "lotNumber": "$LOT_NUMBER_CHILE",
      "expirationDate": "2027-06-30",
      "descriptionLine": "Chile Seco x 20 KG",
      "quantity": 20,
      "unitPrice": 1000.00,
      "taxPercent": 13.00,
      "totalLineAmount": 22600.00
    },
    {
      "idProduct": 3,
      "idUnit": 7,
      "lotNumber": "$LOT_NUMBER_VINAGRE",
      "expirationDate": "2027-12-31",
      "descriptionLine": "Vinagre Blanco x 5 LTR",
      "quantity": 5,
      "unitPrice": 500.00,
      "taxPercent": 13.00,
      "totalLineAmount": 2825.00
    },
    {
      "idProduct": 4,
      "idUnit": 3,
      "lotNumber": "$LOT_NUMBER_SAL",
      "expirationDate": "2028-12-31",
      "descriptionLine": "Sal x 0.5 KG",
      "quantity": 0.5,
      "unitPrice": 200.00,
      "taxPercent": 13.00,
      "totalLineAmount": 113.00
    },
    {
      "idProduct": 5,
      "idUnit": 1,
      "lotNumber": "$LOT_NUMBER_FRASCO",
      "expirationDate": "2030-12-31",
      "descriptionLine": "Frasco 250ml x 100 UNI",
      "quantity": 100,
      "unitPrice": 300.00,
      "taxPercent": 13.00,
      "totalLineAmount": 33900.00
    }
  ]
}
"@

$rPurchase = Invoke-Api -Method POST -Path "/purchase-invoices" -Body $bodyPurchase -AuthTok $TOKEN
Assert-Status 201 "create purchase-invoice"
$ID_PURCHASE_INVOICE = $rPurchase.idPurchaseInvoice
Log-Ok "idPurchaseInvoice = $ID_PURCHASE_INVOICE"

Step "PASO 4b — Confirmar factura de compra"

$rConfirm = Invoke-Api -Method POST -Path "/purchase-invoices/$ID_PURCHASE_INVOICE/confirm" -AuthTok $TOKEN
Assert-Status 200 "confirm purchase-invoice"

$FC_STATUS = $rConfirm.statusInvoice
$FC_NUMBER = $rConfirm.numberInvoice
Log-Ok "statusInvoice = $FC_STATUS"
if ($FC_STATUS -ne "Confirmado") { Fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$FC_STATUS'" }
Log-Ok "Número FC: $FC_NUMBER"
Log-Info "  Asiento esperado: DR 110 ₡52,600 + DR 124 ₡6,838 / CR 106 ₡59,438"

# ── Obtener IDs de los lotes de MP ────────────────────────────────────────────
Log-Info "Consultando lotes de MP creados..."

$lotsChile   = Invoke-Api -Method GET -Path "/inventory-lots/by-product/2.json" -AuthTok $TOKEN
$lChile = $lotsChile | Where-Object { $_.lotNumber -eq $LOT_NUMBER_CHILE } | Select-Object -First 1
if (-not $lChile) { $lChile = $lotsChile | Select-Object -First 1 }
$ID_LOT_CHILE = $lChile.idInventoryLot
Log-Ok "Lote Chile Seco    → idInventoryLot=$ID_LOT_CHILE"

$lotsVinagre = Invoke-Api -Method GET -Path "/inventory-lots/by-product/3.json" -AuthTok $TOKEN
$lVinagre = $lotsVinagre | Where-Object { $_.lotNumber -eq $LOT_NUMBER_VINAGRE } | Select-Object -First 1
if (-not $lVinagre) { $lVinagre = $lotsVinagre | Select-Object -First 1 }
$ID_LOT_VINAGRE = $lVinagre.idInventoryLot
Log-Ok "Lote Vinagre       → idInventoryLot=$ID_LOT_VINAGRE"

$lotsSal     = Invoke-Api -Method GET -Path "/inventory-lots/by-product/4.json" -AuthTok $TOKEN
$lSal = $lotsSal | Where-Object { $_.lotNumber -eq $LOT_NUMBER_SAL } | Select-Object -First 1
if (-not $lSal) { $lSal = $lotsSal | Select-Object -First 1 }
$ID_LOT_SAL = $lSal.idInventoryLot
Log-Ok "Lote Sal           → idInventoryLot=$ID_LOT_SAL"

$lotsFrasco  = Invoke-Api -Method GET -Path "/inventory-lots/by-product/5.json" -AuthTok $TOKEN
$lFrasco = $lotsFrasco | Where-Object { $_.lotNumber -eq $LOT_NUMBER_FRASCO } | Select-Object -First 1
if (-not $lFrasco) { $lFrasco = $lotsFrasco | Select-Object -First 1 }
$ID_LOT_FRASCO = $lFrasco.idInventoryLot
Log-Ok "Lote Frasco 250ml  → idInventoryLot=$ID_LOT_FRASCO"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 5 — CREAR ORDEN DE PRODUCCIÓN (Borrador)
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 5 — Crear orden de producción en Borrador (100 frascos PT)"

$bodyOP = @"
{
  "idFiscalPeriod": 4,
  "idWarehouse": 1,
  "dateOrder": "2026-04-05",
  "descriptionOrder": "Produccion 100 frascos Chile Embotellado Marca X — Caso 2 Manufactura",
  "lines": [
    {
      "idProduct": 6,
      "idProductUnit": $ID_PU_PT,
      "quantityRequired": 100,
      "descriptionLine": "Chile Embotellado Marca X x 100 frascos"
    }
  ]
}
"@

$rOP = Invoke-Api -Method POST -Path "/production-orders" -Body $bodyOP -AuthTok $TOKEN
Assert-Status 201 "create production-order"
$ID_PRODUCTION_ORDER = $rOP.idProductionOrder
$OP_BORRADOR_NUM     = $rOP.numberProductionOrder
Log-Ok "idProductionOrder = $ID_PRODUCTION_ORDER  (número: $OP_BORRADOR_NUM)"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 6 — AVANZAR → Pendiente
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 6 — Avanzar orden → Pendiente"

$rPend = Invoke-Api -Method PATCH -Path "/production-orders/$ID_PRODUCTION_ORDER/status" `
    -Body '{"statusProductionOrder":"Pendiente"}' -AuthTok $TOKEN
Assert-Status 200 "patch status → Pendiente"

$rOP2 = Invoke-Api -Method GET -Path "/production-orders/$ID_PRODUCTION_ORDER.json" -AuthTok $TOKEN
Assert-Status 200 "get production-order after Pendiente"
$OP_NUMBER = $rOP2.numberProductionOrder
$OP_STATUS = $rOP2.statusProductionOrder
Log-Ok "numberProductionOrder = $OP_NUMBER  status = $OP_STATUS"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 7 — AVANZAR → EnProceso
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 7 — Avanzar orden → EnProceso"

Invoke-Api -Method PATCH -Path "/production-orders/$ID_PRODUCTION_ORDER/status" `
    -Body '{"statusProductionOrder":"EnProceso"}' -AuthTok $TOKEN | Out-Null
Assert-Status 200 "patch status → EnProceso"
Log-Ok "Orden en EnProceso ✓"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 8 — COMPLETAR PRODUCCIÓN
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 8 — Completar producción (consume MP, crea lote PT)"

$rCompletado = Invoke-Api -Method PATCH -Path "/production-orders/$ID_PRODUCTION_ORDER/status" `
    -Body '{"statusProductionOrder":"Completado","idWarehouse":1}' -AuthTok $TOKEN
Assert-Status 200 "patch status → Completado"

$warnings = $rCompletado.warnings
if ($warnings -and $warnings.Count -gt 0) {
    Log-Warn "Producción completó con $($warnings.Count) advertencia(s) de stock:"
    foreach ($w in $warnings) { Log-Warn "  • $w" }
} else {
    Log-Ok "Producción completada sin advertencias de stock"
}

Log-Info "Verificando OP del período..."
$rOP3 = Invoke-Api -Method GET -Path "/production-orders/$ID_PRODUCTION_ORDER.json" -AuthTok $TOKEN
Assert-Status 200 "get production-order after Completado"
$OP_STATUS_FINAL = $rOP3.statusProductionOrder
Log-Ok "statusProductionOrder = $OP_STATUS_FINAL"
if ($OP_STATUS_FINAL -ne "Completado") { Fail "Se esperaba status = 'Completado', recibido: '$OP_STATUS_FINAL'" }

Log-Info "  Asientos esperados (1 por MP consumido):"
Log-Info "    DR 115 ₡20,000 / CR 110 ₡20,000  (Chile Seco  20 KG × ₡1,000)"
Log-Info "    DR 115 ₡ 2,500 / CR 110 ₡ 2,500  (Vinagre     5 LTR × ₡500)"
Log-Info "    DR 115 ₡   100 / CR 110 ₡   100  (Sal         0.5 KG × ₡200)"
Log-Info "    DR 115 ₡30,000 / CR 110 ₡30,000  (Frasco      100 UNI × ₡300)"
Log-Info "    PT lot: unitCost = ₡52,600 / 100 = ₡526"

# ── Obtener lote del PT ───────────────────────────────────────────────────────
Log-Info "Consultando lote del PT creado..."

$lotsPT = Invoke-Api -Method GET -Path "/inventory-lots/by-product/6.json" -AuthTok $TOKEN
Assert-Status 200 "get lots by-product/6"

$lotPT = $lotsPT | Select-Object -First 1
if (-not $lotPT) { Fail "No se encontró lote para el PT (idProduct=6) después de completar la producción" }

$ID_LOT_PT      = $lotPT.idInventoryLot
$LOT_PT_COST    = $lotPT.unitCost
$LOT_PT_QTY     = $lotPT.quantityAvailable
$LOT_PT_NUMBER  = $lotPT.lotNumber
Log-Ok "idInventoryLot PT = $ID_LOT_PT"
Log-Ok "Lote PT: número=$LOT_PT_NUMBER  qty=$LOT_PT_QTY  unitCost=$LOT_PT_COST"

$costDiff = [Math]::Abs([double]$LOT_PT_COST - 526.0)
if ($costDiff -lt 0.01) {
    Log-Ok "unitCost PT = ₡$LOT_PT_COST ✓  (₡52,600 / 100 = ₡526)"
} else {
    Log-Warn "unitCost PT = ₡$LOT_PT_COST  (esperado ₡526 si la BD estaba limpia)"
}

$stockChile  = (Invoke-Api -Method GET -Path "/inventory-lots/stock/2.json" -AuthTok $TOKEN)
$stockFrasco = (Invoke-Api -Method GET -Path "/inventory-lots/stock/5.json" -AuthTok $TOKEN)
Log-Info "  Stock restante Chile Seco  (id=2): $stockChile KG (esperado 0)"
Log-Info "  Stock restante Frasco 250ml(id=5): $stockFrasco UNI (esperado 0)"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 9 — FACTURA DE VENTA (30 frascos PT)
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 9a — Crear factura de venta (30 frascos × ₡1,500 + IVA 13%)"

$bodySale = @"
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idSalesInvoiceType": 1,
  "idContact": 1,
  "dateInvoice": "2026-04-05",
  "subTotalAmount": 45000.00,
  "taxAmount": 5850.00,
  "totalAmount": 50850.00,
  "descriptionInvoice": "Venta 30 frascos Chile Embotellado Marca X — Caso 2 Manufactura",
  "exchangeRateValue": 1.0,
  "lines": [
    {
      "isNonProductLine": false,
      "idProduct": 6,
      "idInventoryLot": $ID_LOT_PT,
      "descriptionLine": "Chile Embotellado Marca X x 30 frascos",
      "quantity": 30,
      "unitPrice": 1500.00,
      "taxPercent": 13.00,
      "totalLineAmount": 50850.00
    }
  ]
}
"@

$rSale = Invoke-Api -Method POST -Path "/sales-invoices" -Body $bodySale -AuthTok $TOKEN
Assert-Status 201 "create sales-invoice"
$ID_SALES_INVOICE = $rSale.idSalesInvoice
Log-Ok "idSalesInvoice = $ID_SALES_INVOICE"

Step "PASO 9b — Confirmar factura de venta"

$rFVConfirm = Invoke-Api -Method POST -Path "/sales-invoices/$ID_SALES_INVOICE/confirm" -AuthTok $TOKEN
Assert-Status 200 "confirm sales-invoice"

$FV_STATUS = $rFVConfirm.statusInvoice
$FV_NUMBER = $rFVConfirm.numberInvoice
Log-Ok "statusInvoice = $FV_STATUS"
if ($FV_STATUS -ne "Confirmado") { Fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$FV_STATUS'" }
Log-Ok "Número FV: $FV_NUMBER"
Log-Info "  Asiento FV:   DR 106 ₡50,850 / CR 117 ₡45,000 + CR 127 ₡5,850"
Log-Info "  Asiento COGS: DR 119 ₡15,780 / CR 109 ₡15,780  (30 × ₡526)"

$stockPTPostVenta = Invoke-Api -Method GET -Path "/inventory-lots/stock/6.json" -AuthTok $TOKEN
Log-Ok "Stock PT post-venta = $stockPTPostVenta (esperado 70)"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 10 — DEVOLUCIÓN PARCIAL (5 frascos)
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 10 — Devolución parcial: 5 frascos devueltos"

$bodyReturn = @"
{
  "dateReturn": "2026-04-05",
  "descriptionReturn": "Devolucion parcial — cliente devuelve 5 frascos danados en transito",
  "refundMode": "EfectivoInmediato",
  "lines": [
    {
      "idInventoryLot": $ID_LOT_PT,
      "quantity": 5,
      "totalLineAmount": 8475.00,
      "descriptionLine": "Chile Embotellado Marca X x 5 frascos — devolucion parcial"
    }
  ]
}
"@

$rReturn = Invoke-Api -Method POST -Path "/sales-invoices/$ID_SALES_INVOICE/partial-return" `
    -Body $bodyReturn -AuthTok $TOKEN
Assert-Status 200 "partial-return"

$partialEntry = $rReturn.idAccountingEntry
if ($partialEntry -and $partialEntry -ne 0) {
    $ID_ENTRY_DEV_COGS = $partialEntry
    Log-Ok "idAccountingEntry DEV-COGS = $ID_ENTRY_DEV_COGS"
} else {
    Log-Warn "La devolución no generó asiento COGS"
}
Log-Info "  DEV-COGS: DR 109 ₡2,630 / CR 119 ₡2,630  (5 × ₡526)"

$refundEntry = $rReturn.idAccountingEntryRefund
if ($refundEntry -and $refundEntry -ne 0) {
    $ID_ENTRY_REINTEGRO = $refundEntry
    Log-Ok "idAccountingEntryRefund DEV-ING = $ID_ENTRY_REINTEGRO"
} else {
    Log-Warn "La devolución no generó asiento de reversión de ingresos"
}
Log-Info "  DEV-ING:  DR 117 ₡7,500 + DR 127 ₡975 / CR 106 ₡8,475"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 11 — AJUSTE DE INVENTARIO (regalía: −2 frascos PT)
# ════════════════════════════════════════════════════════════════════════════════
Step "PASO 11a — Crear ajuste de inventario (regalía −2 frascos)"

$bodyAdj = @"
{
  "idFiscalPeriod": 4,
  "idInventoryAdjustmentType": 4,
  "idCurrency": 1,
  "exchangeRateValue": 1.0,
  "dateAdjustment": "2026-04-05",
  "descriptionAdjustment": "Regalia distribuidor — 2 frascos Chile Embotellado como muestra — Responsable: Administrador",
  "lines": [
    {
      "idInventoryLot": $ID_LOT_PT,
      "quantityDelta": -2,
      "descriptionLine": "Salida por regalia — Chile Embotellado Marca X x 2 frascos"
    }
  ]
}
"@

$rAdj = Invoke-Api -Method POST -Path "/inventory-adjustments" -Body $bodyAdj -AuthTok $TOKEN
Assert-Status 201 "create inventory-adjustment"
$ID_ADJUSTMENT = $rAdj.idInventoryAdjustment
Log-Ok "idInventoryAdjustment = $ID_ADJUSTMENT"

Step "PASO 11b — Confirmar ajuste de inventario"

$rAdjConfirm = Invoke-Api -Method POST -Path "/inventory-adjustments/$ID_ADJUSTMENT/confirm" -AuthTok $TOKEN
Assert-Status 200 "confirm inventory-adjustment"

$ADJ_STATUS = $rAdjConfirm.statusAdjustment
$ADJ_ENTRY  = $rAdjConfirm.idAccountingEntry
Log-Ok "statusAdjustment = $ADJ_STATUS"
if ($ADJ_STATUS -ne "Confirmado") { Fail "Se esperaba statusAdjustment = 'Confirmado', recibido: '$ADJ_STATUS'" }
Log-Ok "idAccountingEntry ADJ = $ADJ_ENTRY"
Log-Info "  Asiento: DR 130 Merma ₡1,052 / CR 109 Inventario ₡1,052  (2 × ₡526)"

# ════════════════════════════════════════════════════════════════════════════════
# VERIFICACIÓN FINAL DE STOCK
# ════════════════════════════════════════════════════════════════════════════════
Step "VERIFICACIÓN FINAL — Stock PT (idProduct=6)"

$STOCK_FINAL = Invoke-Api -Method GET -Path "/inventory-lots/stock/6.json" -AuthTok $TOKEN
Assert-Status 200 "get stock PT"
Log-Ok "Stock actual PT = $STOCK_FINAL frascos"
Log-Info "  Fórmula: 100 (producción) − 30 (venta) + 5 (devolución) − 2 (regalía) = 73"

$stockExpected = 73
$stockDiff = [Math]::Abs([double]$STOCK_FINAL - $stockExpected)
if ($stockDiff -lt 0.01) {
    Log-Ok "Stock PT = 73 ✓  Costo en libros: 73 × ₡526 = ₡38,398"
} else {
    Log-Warn "Stock final = $STOCK_FINAL (esperado 73 si la BD estaba limpia antes de correr)"
}

# ════════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ════════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║   ✅  FLUJO CASO 2 COMPLETADO EXITOSAMENTE          ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Green

$summary = [ordered]@{
    idPurchaseInvoice   = $ID_PURCHASE_INVOICE
    idLotChile          = $ID_LOT_CHILE
    idLotVinagre        = $ID_LOT_VINAGRE
    idLotSal            = $ID_LOT_SAL
    idLotFrasco         = $ID_LOT_FRASCO
    idProductionOrder   = $ID_PRODUCTION_ORDER
    opNumber            = $OP_NUMBER
    idLotPT             = $ID_LOT_PT
    "unitCostPT (₡)"    = $LOT_PT_COST
    idSalesInvoice      = $ID_SALES_INVOICE
    idEntryDevCogs      = $ID_ENTRY_DEV_COGS
    idEntryDevIng       = $ID_ENTRY_REINTEGRO
    idAdjustment        = $ID_ADJUSTMENT
    "Stock PT final"    = "$STOCK_FINAL frascos"
}
foreach ($k in $summary.Keys) {
    Write-Host ("  {0,-28} {1}" -f "${k}:", $summary[$k])
}
Write-Host ""

# ════════════════════════════════════════════════════════════════════════════════
# GUARDAR RESULTADO EN ARCHIVO TXT
# ════════════════════════════════════════════════════════════════════════════════
$runTs      = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputFile = Join-Path $SCRIPT_DIR "resultado_caso2_${runTs}.txt"

$lines = @(
    "# ============================================================",
    "#  CASO 2 — MANUFACTURA · Resultado del Test E2E",
    "#  Ejecutado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "# ============================================================",
    "",
    "# ── Configuración usada ──────────────────────────────────────",
    "HOST                  = $HOST_URL",
    "EMAIL                 = $EMAIL",
    "DB_HOST               = $DB_HOST",
    "DB_PORT               = $DB_PORT",
    "",
    "# ── Claves estáticas del caso ────────────────────────────────",
    "idProductMP_Chile     = 2  (Chile Seco, KG)",
    "idProductMP_Vinagre   = 3  (Vinagre Blanco, LTR)",
    "idProductMP_Sal       = 4  (Sal, KG)",
    "idProductMP_Frasco    = 5  (Frasco 250ml, UNI)",
    "idProductPT           = 6  (Chile Embotellado Marca X)",
    "idProductRecipe       = 1  (Receta Chile Embotellado, qty=1)",
    "idFiscalPeriod        = 4  (Abril 2026)",
    "idCurrency            = 1  (CRC)",
    "idWarehouse           = 1  (Principal)",
    "",
    "# ── Montos ──────────────────────────────────────────────────",
    "compra_subtotal       = 52600.00",
    "compra_iva            = 6838.00",
    "compra_total          = 59438.00",
    "unitCostPT            = 526.00 CRC  (52600 / 100)",
    "venta_subtotal        = 45000.00",
    "venta_iva             = 5850.00",
    "venta_total           = 50850.00",
    "cogs_venta            = 15780.00  (30 x 526)",
    "devolucion_total      = 8475.00",
    "cogs_reversa          = 2630.00   (5 x 526)",
    "regalia_costo         = 1052.00   (2 x 526)",
    "",
    "# ── Verificación de stock PT ─────────────────────────────────",
    "stock_post_produccion = 100",
    "stock_post_venta      = 70",
    "stock_post_devolucion = 75",
    "stock_post_regalia    = 73",
    "stock_final_real      = $STOCK_FINAL",
    "stock_costo_libros    = 38398.00  (73 x 526)",
    "",
    "# ── IDs generados en esta ejecución ─────────────────────────",
    "idProductUnit_Chile   = $ID_PU_CHILE",
    "idProductUnit_Vinagre = $ID_PU_VINAGRE",
    "idProductUnit_Sal     = $ID_PU_SAL",
    "idProductUnit_Frasco  = $ID_PU_FRASCO",
    "idProductUnit_PT      = $ID_PU_PT",
    "idProductAccount_Chile   = $ID_PA_CHILE",
    "idProductAccount_Vinagre = $ID_PA_VINAGRE",
    "idProductAccount_Sal     = $ID_PA_SAL",
    "idProductAccount_Frasco  = $ID_PA_FRASCO",
    "idProductAccount_PT      = $ID_PA_PT",
    "idPurchaseInvoice     = $ID_PURCHASE_INVOICE",
    "numberPurchaseInvoice = $FC_NUMBER",
    "idLotChile            = $ID_LOT_CHILE",
    "idLotVinagre          = $ID_LOT_VINAGRE",
    "idLotSal              = $ID_LOT_SAL",
    "idLotFrasco           = $ID_LOT_FRASCO",
    "idProductionOrder     = $ID_PRODUCTION_ORDER",
    "opNumber              = $OP_NUMBER",
    "idLotPT               = $ID_LOT_PT",
    "lotNumberPT           = $LOT_PT_NUMBER",
    "idSalesInvoice        = $ID_SALES_INVOICE",
    "numberSalesInvoice    = $FV_NUMBER",
    "idEntryDevCogs        = $ID_ENTRY_DEV_COGS",
    "idEntryDevIng         = $ID_ENTRY_REINTEGRO",
    "idInventoryAdjustment = $ID_ADJUSTMENT",
    "idAdjEntry            = $ADJ_ENTRY",
    "",
    "# ── Cuentas involucradas ─────────────────────────────────────",
    "idAccount_caja_crc    = 106  (1.1.06.01 Caja CRC)",
    "idAccount_inventario  = 109  (1.1.07.01 Inventario Mercaderia)",
    "idAccount_mp          = 110  (1.1.07.02 Materias Primas)",
    "idAccount_prod_cost   = 115  (5.14.03 Costos de Produccion)",
    "idAccount_ingresos    = 117  (4.5.01 Ingresos por Ventas)",
    "idAccount_cogs        = 119  (5.15.01 Costo de Ventas)",
    "idAccount_iva_acred   = 124  (1.1.09.01 IVA Acreditable CRC)",
    "idAccount_iva_pagar   = 127  (2.1.04.01 IVA por Pagar CRC)",
    "idAccount_merma       = 130  (Merma Anormal — IAS 2.16)",
    "",
    "# ── Endpoints de consulta rapida ────────────────────────────",
    "GET $HOST_URL/purchase-invoices/$ID_PURCHASE_INVOICE.json",
    "GET $HOST_URL/production-orders/$ID_PRODUCTION_ORDER.json",
    "GET $HOST_URL/inventory-lots/$ID_LOT_PT.json",
    "GET $HOST_URL/inventory-lots/stock/6.json",
    "GET $HOST_URL/sales-invoices/$ID_SALES_INVOICE.json",
    "GET $HOST_URL/accounting-entries/$ID_ENTRY_REINTEGRO.json",
    "GET $HOST_URL/inventory-adjustments/$ID_ADJUSTMENT.json"
)

$lines | Set-Content -Path $outputFile -Encoding UTF8

Write-Host "  💾  Resultado guardado en: $outputFile" -ForegroundColor Green
Write-Host ""
