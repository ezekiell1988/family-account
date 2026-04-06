#!/usr/bin/env pwsh
# ============================================================================
#  CASO 3 — ENSAMBLE EN VENTA (Hot Dog)
#  02-verificar-documentos.ps1 — Verificacion de Inventarios y Documentos
#
#  Propósito:
#   Descubre automaticamente los IDs de los documentos del Caso 3 consultando
#   los asientos contables y la API, sin depender de resultado_caso3_*.txt.
#   Verifica que todos los inventarios sean correctos tras el flujo completo.
#   Genera verificacion_docs_caso3_*.txt con el reporte completo.
#
#  Inventario esperado al final del flujo:
#   Pan de Hot Dog  (id=7)  : 50 - 3  =  47 UNI
#   Salchicha       (id=8)  : 50 - 3  =  47 UNI
#   Mostaza         (id=9)  : 750 - 45 = 705 ML
#   Catsup          (id=10) : 1000 - 60 = 940 ML
#   Hot Dog PT      (id=11) : 3 - 3 + 1 - 2 = -1 UNI
#
#  Uso:
#   pwsh docs/inventario/caso-3-ensamble/02-verificar-documentos.ps1
# ============================================================================

# ── Rutas ─────────────────────────────────────────────────────────────────────
$SCRIPT_DIR  = $PSScriptRoot
$HOST_URL    = 'https://localhost:8000/api/v1'
$EMAIL       = 'ezekiell1988@hotmail.com'
$CREDS_FILE  = Join-Path $SCRIPT_DIR '../../../credentials/db.txt'

# ── Contadores ────────────────────────────────────────────────────────────────
$script:CHECKS_OK   = 0
$script:CHECKS_FAIL = 0
$script:HTTP_STATUS = 0

# ── Archivo de reporte ────────────────────────────────────────────────────────
$RUN_TS      = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$OUTPUT_FILE = Join-Path $SCRIPT_DIR "verificacion_docs_caso3_${RUN_TS}.txt"

# ── Helpers ───────────────────────────────────────────────────────────────────

function Write-Section([string]$title) {
    Write-Host ''
    Write-Host "-- $title" -ForegroundColor Cyan
    Add-Content $OUTPUT_FILE ''
    Add-Content $OUTPUT_FILE "-- $title"
}

function Write-Ok([string]$msg) {
    Write-Host "  ✅  $msg" -ForegroundColor Green
    $script:CHECKS_OK++
    Add-Content $OUTPUT_FILE "  [OK]   $msg"
}

function Write-Fail([string]$msg) {
    Write-Host "  ❌  $msg" -ForegroundColor Red
    $script:CHECKS_FAIL++
    Add-Content $OUTPUT_FILE "  [FAIL] $msg"
}

function Write-Info([string]$msg) {
    Write-Host "  $msg" -ForegroundColor DarkGray
    Add-Content $OUTPUT_FILE "         $msg"
}

function Write-Warn([string]$msg) {
    Write-Host "  ⚠   $msg" -ForegroundColor Yellow
    Add-Content $OUTPUT_FILE "  [WARN] $msg"
}

function Invoke-ApiGet([string]$Path) {
    $params = @{
        Method               = 'GET'
        Uri                  = "$HOST_URL$Path"
        Headers              = @{ 'Authorization' = "Bearer $TOKEN" }
        SkipCertificateCheck = $true
        StatusCodeVariable   = 'sc'
    }
    try {
        $resp = Invoke-RestMethod @params
        $script:HTTP_STATUS = [int]$sc
        return $resp
    }
    catch {
        $script:HTTP_STATUS = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
        return $null
    }
}

function Assert-Http200([string]$label) {
    if ($script:HTTP_STATUS -eq 200) {
        Write-Ok "$label — HTTP 200"
    } else {
        Write-Fail "$label — HTTP $($script:HTTP_STATUS) (esperado 200)"
    }
}

function Assert-Equal([string]$label, [string]$expected, $actual) {
    $actualStr = "$actual"
    if ($actualStr -eq $expected) {
        Write-Ok "$label = $actualStr"
    } else {
        Write-Fail "${label}: esperado '$expected', recibido '$actualStr'"
    }
}

function Assert-Gte([string]$label, [string]$expected, $actual) {
    try {
        if ([double]"$actual" -ge [double]$expected) {
            Write-Ok "$label >= $expected  (actual=$actual)"
        } else {
            Write-Fail "${label}: esperado >= $expected, recibido '$actual'"
        }
    } catch {
        Write-Fail "${label}: error al comparar numeros (expected=$expected, actual=$actual)"
    }
}

function Assert-FloatEqual([string]$label, [string]$expected, $actual) {
    try {
        $diff = [Math]::Abs([double]"$actual" - [double]$expected)
        if ($diff -le 0.01) {
            Write-Ok "$label = $actual"
        } else {
            Write-Fail "${label}: esperado aprox $expected, recibido '$actual'"
        }
    } catch {
        Write-Fail "${label}: error al comparar numeros (expected=$expected, actual=$actual)"
    }
}

# ── Verificar dependencias ────────────────────────────────────────────────────
if (-not (Get-Command 'sqlcmd' -ErrorAction SilentlyContinue)) {
    Write-Host '❌  Dependencia faltante: sqlcmd' -ForegroundColor Red; exit 1
}

# ── Leer credenciales BD ──────────────────────────────────────────────────────
$credsContent = Get-Content $CREDS_FILE
$DB_HOST = ($credsContent | Select-String '^HOST:').ToString().Split()[1].Trim()
$DB_PORT = ($credsContent | Select-String '^PORT:').ToString().Split()[1].Trim()
$DB_USER = ($credsContent | Select-String '^USER:').ToString().Split()[1].Trim()
$DB_PASS = ($credsContent | Select-String '^PASSWORD:').ToString().Split()[1].Trim()

# ── Autenticacion ─────────────────────────────────────────────────────────────
$PIN = '12345'
sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa `
    -Q "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');" 2>$null

$loginBody = @{ emailUser = $EMAIL; pin = $PIN } | ConvertTo-Json -Compress
$loginResp  = Invoke-RestMethod -Method POST -Uri "$HOST_URL/auth/login" `
    -Body ([System.Text.Encoding]::UTF8.GetBytes($loginBody)) `
    -ContentType 'application/json' -SkipCertificateCheck -StatusCodeVariable 'loginSc'
$TOKEN = $loginResp.accessToken
if (-not $TOKEN -or $TOKEN -eq 'null') {
    Write-Host '❌  No se pudo obtener token' -ForegroundColor Red; exit 1
}

# ── Constantes del caso ───────────────────────────────────────────────────────
$ID_WAREHOUSE        = 1
$EXP_COST_PAN        = '300'
$EXP_COST_SALCHICHA  = '600'
$EXP_COST_MOSTAZA    = '20'
$EXP_COST_CATSUP     = '15'
$EXP_COST_PT         = '1500'
$EXP_STOCK_PAN       = '47'
$EXP_STOCK_SALCHICHA = '47'
$EXP_STOCK_MOSTAZA   = '705'
$EXP_STOCK_CATSUP    = '940'
$EXP_STOCK_PT        = '-1'

# ── Descubrir IDs desde asientos contables ────────────────────────────────────
$entries = Invoke-ApiGet '/accounting-entries/data.json'
$ID_PURCHASE_INVOICE = ($entries | Where-Object { $_.numberEntry -like 'FC-*' } | Sort-Object idAccountingEntry | Select-Object -Last 1).idOriginRecord
$ID_SALES_INVOICE    = ($entries | Where-Object { $_.numberEntry -like 'FV-*' } | Sort-Object idAccountingEntry | Select-Object -Last 1).idOriginRecord
$ID_ADJUSTMENT       = ($entries | Where-Object { $_.numberEntry -like 'AJ-*' } | Sort-Object idAccountingEntry | Select-Object -Last 1).idOriginRecord

# ── Descubrir lotes ───────────────────────────────────────────────────────────
$lotsP7  = @(Invoke-ApiGet "/inventory-lots/by-product/7.json?idWarehouse=$ID_WAREHOUSE")
$lPan    = $lotsP7 | Where-Object { $_.idPurchaseInvoice -eq $ID_PURCHASE_INVOICE } | Sort-Object idInventoryLot | Select-Object -Last 1
if (-not $lPan) { $lPan = $lotsP7 | Select-Object -First 1 }
$ID_LOT_PAN = $lPan.idInventoryLot

$lotsP8   = @(Invoke-ApiGet "/inventory-lots/by-product/8.json?idWarehouse=$ID_WAREHOUSE")
$lSalch   = $lotsP8 | Where-Object { $_.idPurchaseInvoice -eq $ID_PURCHASE_INVOICE } | Sort-Object idInventoryLot | Select-Object -Last 1
if (-not $lSalch) { $lSalch = $lotsP8 | Select-Object -First 1 }
$ID_LOT_SALCHICHA = $lSalch.idInventoryLot

$lotsP9  = @(Invoke-ApiGet "/inventory-lots/by-product/9.json?idWarehouse=$ID_WAREHOUSE")
$lMost   = $lotsP9 | Where-Object { $_.idPurchaseInvoice -eq $ID_PURCHASE_INVOICE } | Sort-Object idInventoryLot | Select-Object -Last 1
if (-not $lMost) { $lMost = $lotsP9 | Select-Object -First 1 }
$ID_LOT_MOSTAZA = $lMost.idInventoryLot

$lotsP10 = @(Invoke-ApiGet "/inventory-lots/by-product/10.json?idWarehouse=$ID_WAREHOUSE")
$lCats   = $lotsP10 | Where-Object { $_.idPurchaseInvoice -eq $ID_PURCHASE_INVOICE } | Sort-Object idInventoryLot | Select-Object -Last 1
if (-not $lCats) { $lCats = $lotsP10 | Select-Object -First 1 }
$ID_LOT_CATSUP = $lCats.idInventoryLot

$lotsP11   = @(Invoke-ApiGet "/inventory-lots/by-product/11.json?idWarehouse=$ID_WAREHOUSE")
$ID_LOT_PT = ($lotsP11 | Sort-Object idInventoryLot | Select-Object -Last 1).idInventoryLot

$fvResp        = Invoke-ApiGet "/sales-invoices/$ID_SALES_INVOICE.json"
$ID_SALES_ORDER = $fvResp.idSalesOrder

# ── Encabezado ────────────────────────────────────────────────────────────────
Write-Host ''
Write-Host '╔══════════════════════════════════════════════════════╗' -ForegroundColor Cyan
Write-Host '║   CASO 3 — ENSAMBLE - Verificacion de Inventarios   ║' -ForegroundColor Cyan
Write-Host '╚══════════════════════════════════════════════════════╝' -ForegroundColor Cyan

$headerLines = @(
    '# =================================================================='
    "# CASO 3 — ENSAMBLE EN VENTA - Verificacion de Inventarios"
    "# Ejecutado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    '# =================================================================='
)
$headerLines | Set-Content $OUTPUT_FILE -Encoding UTF8

Write-Host ''
Write-Host '  IDs descubiertos:' -ForegroundColor DarkGray
Write-Host "  PC=$ID_PURCHASE_INVOICE  FV=$ID_SALES_INVOICE  SO=$( $ID_SALES_ORDER ? $ID_SALES_ORDER : '(no encontrado)' )  ADJ=$ID_ADJUSTMENT" -ForegroundColor DarkGray
Write-Host "  LotPan=$ID_LOT_PAN  LotSalch=$ID_LOT_SALCHICHA  LotMost=$ID_LOT_MOSTAZA  LotCats=$ID_LOT_CATSUP  LotPT=$ID_LOT_PT" -ForegroundColor DarkGray

# ════════════════════════════════════════════════════════════════════════════
# SECCION 1 — CATALOGO (seed)
# ════════════════════════════════════════════════════════════════════════════
Write-Section 'SECCION 1 — Catalogo (configuracion seed)'

foreach ($idProd in @(7, 8, 9, 10)) {
    $p = Invoke-ApiGet "/products/$idProd.json"
    Assert-Http200 "GET /products/$idProd.json"
    Assert-Equal "product[$idProd].idProductType = 1 (Materia Prima)" '1' $p.idProductType
}

$p11 = Invoke-ApiGet '/products/11.json'
Assert-Http200 'GET /products/11.json'
Assert-Equal 'product[11].idProductType = 3 (Producto Terminado)' '3' $p11.idProductType

$recipes       = @(Invoke-ApiGet '/product-recipes/by-output/11.json')
Assert-Http200 'GET /product-recipes/by-output/11.json'
$activeRecipes = @($recipes | Where-Object { $_.isActive -eq $true })
Assert-Gte 'receta activa para producto 11 existe' '1' $activeRecipes.Count
$firstRecipe   = $activeRecipes | Select-Object -First 1
Write-Info "  Receta id=$($firstRecipe.idProductRecipe)  quantityOutput=$($firstRecipe.quantityOutput)"

$pcTypes = @(Invoke-ApiGet '/purchase-invoice-types/data.json')
Assert-Http200 'GET /purchase-invoice-types/data.json'
Assert-Equal 'purchase-invoice-type id=1 existe' '1' (@($pcTypes | Where-Object { $_.idPurchaseInvoiceType -eq 1 }).Count)

$svTypes = @(Invoke-ApiGet '/sales-invoice-types/data.json')
Assert-Http200 'GET /sales-invoice-types/data.json'
Assert-Equal 'sales-invoice-type id=1 existe' '1' (@($svTypes | Where-Object { $_.idSalesInvoiceType -eq 1 }).Count)

$adjTypes = @(Invoke-ApiGet '/inventory-adjustment-types/data.json')
Assert-Http200 'GET /inventory-adjustment-types/data.json'
Assert-Equal 'inventory-adjustment-type id=1 existe' '1' (@($adjTypes | Where-Object { $_.idInventoryAdjustmentType -eq 1 }).Count)

foreach ($idProd in @(7, 8, 9, 10)) {
    $accs = @(Invoke-ApiGet "/product-accounts/by-product/$idProd.json")
    Assert-Http200 "GET /product-accounts/by-product/$idProd.json"
    Assert-Gte "product-account[$idProd] → cuenta 110 existe" '1' (@($accs | Where-Object { $_.idAccount -eq 110 }).Count)
}

# ════════════════════════════════════════════════════════════════════════════
# SECCION 2 — FACTURA DE COMPRA
# ════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 2 — Factura de Compra (id=$ID_PURCHASE_INVOICE)"

$pc = Invoke-ApiGet "/purchase-invoices/$ID_PURCHASE_INVOICE.json"
Assert-Http200 "GET /purchase-invoices/$ID_PURCHASE_INVOICE.json"
Assert-Equal 'PC statusInvoice = Confirmado' 'Confirmado' $pc.statusInvoice
Assert-Gte   'PC tiene idAccountingEntry'    '1'          ($pc.idAccountingEntry ? $pc.idAccountingEntry : 0)
Assert-Equal 'PC tiene 4 lineas de ingredientes' '4'      $pc.lines.Count
Write-Info '  Subtotal esperado: 75000  IVA: 9750  Total: 84750'

# ════════════════════════════════════════════════════════════════════════════
# SECCION 3 — LOTES DE INGREDIENTES
# ════════════════════════════════════════════════════════════════════════════

function Test-LotIngredient([string]$lotId, [string]$label, [string]$expQty, [string]$expCost, [string]$expProduct, [string]$expPc = '') {
    $lot = Invoke-ApiGet "/inventory-lots/$lotId.json"
    Assert-Http200     "GET /inventory-lots/$lotId.json ($label)"
    Assert-Equal       "$label statusLot = Disponible"       'Disponible' $lot.statusLot
    Assert-FloatEqual  "$label quantityAvailable = $expQty"  $expQty      $lot.quantityAvailable
    Assert-FloatEqual  "$label unitCost = $expCost"          $expCost     $lot.unitCost
    Assert-Equal       "$label sourceType = Compra"          'Compra'     $lot.sourceType
    Assert-Equal       "$label idProduct = $expProduct"      $expProduct  $lot.idProduct
    if ($expPc) {
        Assert-Equal "$label idPurchaseInvoice = $expPc" $expPc $lot.idPurchaseInvoice
    }
}

Write-Section "SECCION 3a — Lote Pan (id=$ID_LOT_PAN) — post-OP: 47 UNI"
Test-LotIngredient $ID_LOT_PAN 'Pan' $EXP_STOCK_PAN $EXP_COST_PAN '7' $ID_PURCHASE_INVOICE
Write-Info '  50 comprados - 3 consumidos en OP = 47 UNI'

Write-Section "SECCION 3b — Lote Salchicha (id=$ID_LOT_SALCHICHA) — post-OP: 47 UNI"
Test-LotIngredient $ID_LOT_SALCHICHA 'Salchicha' $EXP_STOCK_SALCHICHA $EXP_COST_SALCHICHA '8'
Write-Info '  50 comprados - 3 consumidos en OP = 47 UNI'

Write-Section "SECCION 3c — Lote Mostaza (id=$ID_LOT_MOSTAZA) — post-OP: 705 ML"
Test-LotIngredient $ID_LOT_MOSTAZA 'Mostaza' $EXP_STOCK_MOSTAZA $EXP_COST_MOSTAZA '9'
Write-Info '  750 ML - 45 ML (3x15ml por receta) = 705 ML'

Write-Section "SECCION 3d — Lote Catsup (id=$ID_LOT_CATSUP) — post-OP: 940 ML"
Test-LotIngredient $ID_LOT_CATSUP 'Catsup' $EXP_STOCK_CATSUP $EXP_COST_CATSUP '10'
Write-Info '  1000 ML - 60 ML (3x20ml por receta) = 940 ML'

Write-Section 'SECCION 3e — Stock global de ingredientes (/inventory-lots/stock/N.json)'
$stockRefs = @(
    @{ Id = 7;  Exp = $EXP_STOCK_PAN;       Name = 'Pan' }
    @{ Id = 8;  Exp = $EXP_STOCK_SALCHICHA; Name = 'Salchicha' }
    @{ Id = 9;  Exp = $EXP_STOCK_MOSTAZA;   Name = 'Mostaza' }
    @{ Id = 10; Exp = $EXP_STOCK_CATSUP;    Name = 'Catsup' }
)
foreach ($p in $stockRefs) {
    $stockVal = Invoke-ApiGet "/inventory-lots/stock/$($p.Id).json"
    Assert-Http200 "GET /inventory-lots/stock/$($p.Id).json ($($p.Name))"
    Assert-FloatEqual "$($p.Name) stock global = $($p.Exp)" $p.Exp ($null -ne $stockVal ? $stockVal : 0)
}

# ════════════════════════════════════════════════════════════════════════════
# SECCION 4 — LOTES POR PRODUCTO
# ════════════════════════════════════════════════════════════════════════════
Write-Section 'SECCION 4 — Lotes por producto — listados y filtros'

foreach ($idP in @(7, 8, 9, 10)) {
    $lots = @(Invoke-ApiGet "/inventory-lots/by-product/$idP.json")
    Assert-Http200 "GET /inventory-lots/by-product/$idP.json"
    Assert-Gte "producto $idP tiene al menos 1 lote" '1' $lots.Count
}

$lotsAll11 = @(Invoke-ApiGet '/inventory-lots/by-product/11.json')
Assert-Http200 'GET /inventory-lots/by-product/11.json'
$countPT = $lotsAll11.Count
Assert-Gte 'producto 11 (Hot Dog PT) tiene al menos 1 lote' '1' $countPT
Write-Info "  Lotes PT encontrados: $countPT"

foreach ($idP in @(7, 11)) {
    $lotsEmpty = @(Invoke-ApiGet "/inventory-lots/by-product/$idP.json?idWarehouse=9999")
    Assert-Http200 "GET /inventory-lots/by-product/$idP.json?idWarehouse=9999"
    Assert-Equal "producto $idP con almacen 9999 devuelve 0 lotes" '0' $lotsEmpty.Count
}

$suggestResp = Invoke-ApiGet '/inventory-lots/suggest/7.json'
Assert-Http200 'GET /inventory-lots/suggest/7.json (FEFO Pan)'
Assert-Equal 'FEFO suggest Pan = lote del caso'        "$ID_LOT_PAN"  $suggestResp.idInventoryLot
Assert-Equal 'FEFO suggest Pan statusLot = Disponible' 'Disponible'   $suggestResp.statusLot

$null = Invoke-ApiGet '/inventory-lots/suggest/7.json?idWarehouse=9999'
if ($script:HTTP_STATUS -eq 404) {
    Write-Ok 'suggest Pan almacen 9999 → HTTP 404 OK'
} else {
    Write-Fail "suggest Pan almacen 9999 → esperado 404, recibido $($script:HTTP_STATUS)"
}

# ════════════════════════════════════════════════════════════════════════════
# SECCION 5 — LOTE PT HOT DOG
# ════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 5 — Lote PT Hot Dog (id=$ID_LOT_PT) — stock final: -1"

$lotPT = Invoke-ApiGet "/inventory-lots/$ID_LOT_PT.json"
Assert-Http200 "GET /inventory-lots/$ID_LOT_PT.json"

Assert-Equal       'lote PT idProduct = 11'                  '11'              $lotPT.idProduct
Assert-Equal       "lote PT idWarehouse = $ID_WAREHOUSE"     "$ID_WAREHOUSE"   $lotPT.idWarehouse
Assert-FloatEqual  "lote PT unitCost = $EXP_COST_PT"         $EXP_COST_PT      ($null -ne $lotPT.unitCost ? $lotPT.unitCost : 0)
Assert-FloatEqual  "lote PT quantityAvailable = $EXP_STOCK_PT" $EXP_STOCK_PT   ($null -ne $lotPT.quantityAvailable ? $lotPT.quantityAvailable : 0)

if ($null -eq $lotPT.idPurchaseInvoice) {
    Write-Ok 'lote PT idPurchaseInvoice = null (origen: OP automatica)'
} else {
    Write-Fail "lote PT idPurchaseInvoice deberia ser null, recibido: $($lotPT.idPurchaseInvoice)"
}
Write-Info "  Almacen: $($lotPT.nameWarehouse)  status: $($lotPT.statusLot)"
Write-Info '  Formula: 3 (OP) - 3 (FV) + 1 (devolucion) - 2 (regalia) = -1'

$stockPtVal = Invoke-ApiGet '/inventory-lots/stock/11.json'
Assert-Http200 'GET /inventory-lots/stock/11.json (Hot Dog PT)'
Assert-FloatEqual "Hot Dog PT stock global = $EXP_STOCK_PT" $EXP_STOCK_PT ($null -ne $stockPtVal ? $stockPtVal : 0)

# ════════════════════════════════════════════════════════════════════════════
# SECCION 6 — PEDIDO DE VENTA
# ════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 6 — Pedido de Venta (id=$( $ID_SALES_ORDER ? $ID_SALES_ORDER : '(no encontrado)' ))"

if ($ID_SALES_ORDER) {
    $so = Invoke-ApiGet "/sales-orders/$ID_SALES_ORDER.json"
    Assert-Http200 "GET /sales-orders/$ID_SALES_ORDER.json"
    Assert-Equal 'pedido statusOrder = Completado' 'Completado' $so.statusOrder
    Assert-Equal 'pedido tiene 1 linea'            '1'          $so.lines.Count
    Assert-Equal 'pedido linea[0] idProduct = 11'  '11'         $so.lines[0].idProduct
    Write-Info "  Pedido vinculado a FV id=$ID_SALES_INVOICE (ciclo automatico completado)"
} else {
    Write-Warn 'idSalesOrder no encontrado — omitiendo verificacion del pedido'
}

# ════════════════════════════════════════════════════════════════════════════
# SECCION 7 — FACTURA DE VENTA
# ════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 7 — Factura de Venta (id=$ID_SALES_INVOICE)"

$fv = Invoke-ApiGet "/sales-invoices/$ID_SALES_INVOICE.json"
Assert-Http200 "GET /sales-invoices/$ID_SALES_INVOICE.json"

Assert-Equal       'FV statusInvoice = Confirmado'          'Confirmado'   $fv.statusInvoice
Assert-Gte         'FV tiene idAccountingEntry'             '1'            ($fv.idAccountingEntry ? $fv.idAccountingEntry : 0)
Assert-Equal       'FV tiene 1 linea (Hot Dog)'             '1'            $fv.lines.Count
Assert-Equal       'FV linea[0] idInventoryLot = lote PT'   "$ID_LOT_PT"   $fv.lines[0].idInventoryLot
Assert-FloatEqual  'FV linea[0] quantity = 3'               '3'            $fv.lines[0].quantity
Assert-FloatEqual  'FV linea[0] unitPrice = 3000'           '3000'         $fv.lines[0].unitPrice
Write-Info '  Asiento FV: DR 106 Caja 10170 / CR 117 Ingresos 9000 / CR 127 IVA 1170'
Write-Info '  Asiento COGS: DR 119 5085 / CR 109 Inventario 5085  (3 x 1695)'

# ════════════════════════════════════════════════════════════════════════════
# SECCION 8 — AJUSTE DE INVENTARIO (regalia: -2 hot dogs)
# ════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 8 — Ajuste de Inventario — Regalia (id=$ID_ADJUSTMENT)"

$adj = Invoke-ApiGet "/inventory-adjustments/$ID_ADJUSTMENT.json"
Assert-Http200 "GET /inventory-adjustments/$ID_ADJUSTMENT.json"

Assert-Equal       'ADJ statusAdjustment = Confirmado'     'Confirmado'   $adj.statusAdjustment
Assert-Gte         'ADJ tiene idAccountingEntry'           '1'            ($adj.idAccountingEntry ? $adj.idAccountingEntry : 0)
Assert-Equal       'ADJ tiene 1 linea'                     '1'            $adj.lines.Count
Assert-Equal       'ADJ linea[0] idInventoryLot = lote PT' "$ID_LOT_PT"   $adj.lines[0].idInventoryLot
Assert-FloatEqual  'ADJ linea[0] quantityDelta = -2'       '-2'           $adj.lines[0].quantityDelta
Write-Info '  Asiento regalia: DR 130 Merma 3390 / CR 109 Inventario 3390  (2 x 1695)'

# ════════════════════════════════════════════════════════════════════════════
# SECCION 9 — ALMACEN Y VERIFICACIONES CRUZADAS
# ════════════════════════════════════════════════════════════════════════════
Write-Section 'SECCION 9 — Almacen y verificaciones cruzadas'

$warehouses = @(Invoke-ApiGet '/warehouses/data.json')
Assert-Http200 'GET /warehouses/data.json'
$whFound = @($warehouses | Where-Object { $_.idWarehouse -eq $ID_WAREHOUSE })
Assert-Equal "almacen id=$ID_WAREHOUSE existe en catalogo" '1'    $whFound.Count
Assert-Equal "almacen $ID_WAREHOUSE isActive = true"       'True' "$($whFound[0].isActive)"

$allProds = @(
    @{ Id = 7;  Exp = $EXP_STOCK_PAN }
    @{ Id = 8;  Exp = $EXP_STOCK_SALCHICHA }
    @{ Id = 9;  Exp = $EXP_STOCK_MOSTAZA }
    @{ Id = 10; Exp = $EXP_STOCK_CATSUP }
    @{ Id = 11; Exp = $EXP_STOCK_PT }
)
foreach ($p in $allProds) {
    $lots   = @(Invoke-ApiGet "/inventory-lots/by-product/$($p.Id).json")
    $sumQty = ($lots | Measure-Object quantityAvailable -Sum).Sum
    if ($null -eq $sumQty) { $sumQty = 0 }
    Assert-FloatEqual "suma lotes producto $($p.Id) = stock global ($($p.Exp))" $p.Exp $sumQty
}

foreach ($idP in @(7, 8, 9, 10)) {
    $lotsWh  = @(Invoke-ApiGet "/inventory-lots/by-product/$idP.json?idWarehouse=$ID_WAREHOUSE")
    Assert-Http200 "GET /inventory-lots/by-product/$idP.json?idWarehouse=$ID_WAREHOUSE"
    $wrongWh = @($lotsWh | Where-Object { $_.idWarehouse -ne $ID_WAREHOUSE }).Count
    Assert-Equal "producto $idP — todos los lotes en almacen $ID_WAREHOUSE" '0' $wrongWh
}

# ════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ════════════════════════════════════════════════════════════════════════════
$totalChecks = $script:CHECKS_OK + $script:CHECKS_FAIL

Add-Content $OUTPUT_FILE ''
Add-Content $OUTPUT_FILE '# -- Resumen ------------------------------------------'
Add-Content $OUTPUT_FILE "#   OK:    $($script:CHECKS_OK)"
Add-Content $OUTPUT_FILE "#   FAIL:  $($script:CHECKS_FAIL)"
Add-Content $OUTPUT_FILE "#   Total: $totalChecks"

Write-Host ''
if ($script:CHECKS_FAIL -eq 0) {
    Write-Host '╔══════════════════════════════════════════════════════╗' -ForegroundColor Green
    Write-Host '║   ✅  TODAS LAS VERIFICACIONES PASARON              ║' -ForegroundColor Green
    Write-Host '╚══════════════════════════════════════════════════════╝' -ForegroundColor Green
} else {
    Write-Host '╔══════════════════════════════════════════════════════╗' -ForegroundColor Red
    Write-Host '║   ❌  HAY VERIFICACIONES FALLIDAS                   ║' -ForegroundColor Red
    Write-Host '╚══════════════════════════════════════════════════════╝' -ForegroundColor Red
}

Write-Host ("  {0,-28} {1}" -f 'OK:'    , $script:CHECKS_OK)
Write-Host ("  {0,-28} {1}" -f 'FAIL:'  , $script:CHECKS_FAIL)
Write-Host ("  {0,-28} {1}" -f 'Total:' , $totalChecks)
Write-Host ''
Write-Host '  IDs del caso:' -ForegroundColor DarkGray
Write-Host ("  {0,-30} {1}" -f 'idPurchaseInvoice:',      $ID_PURCHASE_INVOICE)
Write-Host ("  {0,-30} {1}" -f 'idLotPan:',               $ID_LOT_PAN)
Write-Host ("  {0,-30} {1}" -f 'idLotSalchicha:',         $ID_LOT_SALCHICHA)
Write-Host ("  {0,-30} {1}" -f 'idLotMostaza:',           $ID_LOT_MOSTAZA)
Write-Host ("  {0,-30} {1}" -f 'idLotCatsup:',            $ID_LOT_CATSUP)
Write-Host ("  {0,-30} {1}" -f 'idSalesOrder:',           ($ID_SALES_ORDER ? $ID_SALES_ORDER : '(no encontrado)'))
Write-Host ("  {0,-30} {1}" -f 'idSalesInvoice:',         $ID_SALES_INVOICE)
Write-Host ("  {0,-30} {1}" -f 'idLotPT (Hot Dog):',      $ID_LOT_PT)
Write-Host ("  {0,-30} {1}" -f 'idAdjustment (regalia):', $ID_ADJUSTMENT)
Write-Host ''
Write-Host '  Stock final:' -ForegroundColor DarkGray
Write-Host ("  {0,-30} {1}" -f 'Pan de Hot Dog  (id=7):',  "$EXP_STOCK_PAN UNI")
Write-Host ("  {0,-30} {1}" -f 'Salchicha       (id=8):',  "$EXP_STOCK_SALCHICHA UNI")
Write-Host ("  {0,-30} {1}" -f 'Mostaza         (id=9):',  "$EXP_STOCK_MOSTAZA ML")
Write-Host ("  {0,-30} {1}" -f 'Catsup          (id=10):', "$EXP_STOCK_CATSUP ML")
Write-Host ("  {0,-30} {1}" -f 'Hot Dog PT      (id=11):', "$EXP_STOCK_PT UNI")
Write-Host ''
Write-Host "  💾  Reporte guardado en: $OUTPUT_FILE" -ForegroundColor DarkGray
Write-Host ''

exit ($script:CHECKS_FAIL -eq 0 ? 0 : 1)
