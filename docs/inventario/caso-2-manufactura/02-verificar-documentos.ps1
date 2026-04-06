#!/usr/bin/env pwsh
# ============================================================================
#  CASO 2 — MANUFACTURA (Chile Embotellado Marca X)
#  02-verificar-documentos.ps1 — Verificación de Documentos e Inventario
#
#  Propósito:
#   Descubre automáticamente los IDs desde los asientos contables y la API.
#   Verifica documentos, inventario, lotes MP consumidos, lote PT, FV y
#   devolución. Genera verificacion_docs_caso2_*.txt con reporte completo.
#
#  Uso:
#   pwsh docs/inventario/caso-2-manufactura/02-verificar-documentos.ps1
# ============================================================================

param()

$SCRIPT_DIR = $PSScriptRoot
$REPO_ROOT  = (Resolve-Path (Join-Path $SCRIPT_DIR "../../..")).Path
$CREDS_FILE = Join-Path $REPO_ROOT "credentials\db.txt"
$HOST_URL   = "https://localhost:8000/api/v1"
$EMAIL      = "ezekiell1988@hotmail.com"

$checksOk   = 0
$checksFail = 0

$runTs      = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputFile = Join-Path $SCRIPT_DIR "verificacion_docs_caso2_${runTs}.txt"

# ── Helpers ──────────────────────────────────────────────────────────────────
function Section([string]$title) {
    Write-Host ""
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "▶  $title" -ForegroundColor Cyan
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Add-Content $outputFile ""
    Add-Content $outputFile "══ $title ══"
}

function Log-Ok([string]$msg)   { Write-Host "  ✅  $msg" -ForegroundColor Green;  $script:checksOk++;   Add-Content $outputFile "  [OK]   $msg" }
function Log-Fail([string]$msg) { Write-Host "  ❌  $msg" -ForegroundColor Red;    $script:checksFail++; Add-Content $outputFile "  [FAIL] $msg" }
function Log-Info([string]$msg) { Write-Host "  $msg" -ForegroundColor DarkGray;   Add-Content $outputFile "         $msg" }
function Log-Warn([string]$msg) { Write-Host "  ⚠   $msg" -ForegroundColor Yellow; Add-Content $outputFile "  [WARN] $msg" }

$script:HTTP_STATUS = 0
$script:LAST_RESP   = $null

function Api-Get([string]$path) {
    $headers = @{ "Authorization" = "Bearer $TOKEN" }
    try {
        $r = Invoke-RestMethod -Method GET -Uri "$HOST_URL$path" -Headers $headers `
             -SkipCertificateCheck -ErrorAction Stop -StatusCodeVariable "sc"
        $script:HTTP_STATUS = [int]$sc
        $script:LAST_RESP   = $r
        return $r
    } catch {
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = [System.IO.StreamReader]::new($stream)
            $errText = $reader.ReadToEnd(); $reader.Close()
            try { $script:LAST_RESP = $errText | ConvertFrom-Json } catch { $script:LAST_RESP = $errText }
        } catch {}
        $code = $_.Exception.Response.StatusCode.value__
        $script:HTTP_STATUS = if ($code) { [int]$code } else { 0 }
        return $null
    }
}

function Assert-200([string]$label) {
    if ($script:HTTP_STATUS -eq 200) { Log-Ok "$label — HTTP 200" }
    else { Log-Fail "$label — esperado HTTP 200, recibido $($script:HTTP_STATUS)" }
}

function Assert-Eq([string]$label, $expected, $actual) {
    if ("$actual" -eq "$expected") { Log-Ok "${label}: $actual" }
    else { Log-Fail "${label}: esperado='$expected'  real='$actual'" }
}

function Assert-Gte([string]$label, [double]$expected, $actual) {
    $a = [double]$actual
    if ($a -ge $expected) { Log-Ok "${label}: $actual (>= $expected)" }
    else { Log-Fail "${label}: esperado >= $expected  real='$actual'" }
}

function Assert-FloatEq([string]$label, [double]$expected, $actual) {
    $a    = [double]$actual
    $diff = [Math]::Abs($a - $expected)
    if ($diff -lt 0.01) { Log-Ok "${label}: $actual" }
    else { Log-Fail "${label}: esperado=$expected  real='$actual'" }
}

# ── Leer credenciales BD ──────────────────────────────────────────────────────
$credsContent = Get-Content $CREDS_FILE
$DB_HOST = ($credsContent | Select-String '^HOST:').ToString().Split()[1].Trim()
$DB_PORT = ($credsContent | Select-String '^PORT:').ToString().Split()[1].Trim()
$DB_USER = ($credsContent | Select-String '^USER:').ToString().Split()[1].Trim()
$DB_PASS = ($credsContent | Select-String '^PASSWORD:').ToString().Split()[1].Trim()

# ── Autenticación ─────────────────────────────────────────────────────────────
$PIN = "12345"
sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa `
    -Q "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');" 2>$null

$loginResp = Invoke-RestMethod -Method POST -Uri "$HOST_URL/auth/login" `
    -Body (@{emailUser=$EMAIL;pin=$PIN}|ConvertTo-Json -Compress) `
    -ContentType "application/json" -SkipCertificateCheck -ErrorAction Stop

$TOKEN = $loginResp.accessToken
if (-not $TOKEN -or $TOKEN -eq "null") {
    Write-Host "❌  No se pudo obtener token" -ForegroundColor Red; exit 1
}

# ── Constantes esperadas ──────────────────────────────────────────────────────
$EXP_UNIT_COST_PT    = 526
$EXP_STOCK_PT_FINAL  = 73
$ID_WAREHOUSE        = 1

# ── Inicializar archivo de reporte ────────────────────────────────────────────
@(
    "# ==================================================================",
    "#  CASO 2 — MANUFACTURA · Verificacion de Documentos e Inventario",
    "#  Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "#  IDs descubiertos desde la API (sin resultado_caso2_*.txt)",
    "# =================================================================="
) | Set-Content $outputFile -Encoding UTF8

# ── Descubrir IDs desde asientos contables ────────────────────────────────────
$allEntries = Api-Get "/accounting-entries/data.json"

$ID_PURCHASE_INVOICE = ($allEntries |
    Where-Object { $_.numberEntry -like "FC-*" } |
    Sort-Object idAccountingEntry | Select-Object -Last 1).idOriginRecord

$ID_PRODUCTION_ORDER = ($allEntries |
    Where-Object { $_.originModule -eq "ProductionOrder" } |
    Sort-Object idAccountingEntry | Select-Object -Last 1).idOriginRecord

$ID_SALES_INVOICE    = ($allEntries |
    Where-Object { $_.numberEntry -like "FV-*" } |
    Sort-Object idAccountingEntry | Select-Object -Last 1).idOriginRecord

$ID_ADJUSTMENT       = ($allEntries |
    Where-Object { $_.numberEntry -like "AJ-*" } |
    Sort-Object idAccountingEntry | Select-Object -Last 1).idOriginRecord

$ID_ENTRY_DEV_ING    = ($allEntries |
    Where-Object { $_.numberEntry -like "DEV-ING-FV-*" } |
    Sort-Object idAccountingEntry | Select-Object -Last 1).idAccountingEntry

# Lotes de MP vinculados a la FC de compra
$ID_LOT_CHILE   = 0; $ID_LOT_VINAGRE = 0; $ID_LOT_SAL = 0; $ID_LOT_FRASCO = 0
foreach ($entry in @(@(2,"ID_LOT_CHILE"), @(3,"ID_LOT_VINAGRE"), @(4,"ID_LOT_SAL"), @(5,"ID_LOT_FRASCO"))) {
    $prodId = $entry[0]; $varName = $entry[1]
    $lots = Api-Get "/inventory-lots/by-product/${prodId}.json?idWarehouse=$ID_WAREHOUSE"
    $found = $lots | Where-Object { $_.idPurchaseInvoice -eq $ID_PURCHASE_INVOICE } |
             Sort-Object idInventoryLot | Select-Object -Last 1
    Set-Variable -Name $varName -Value $found.idInventoryLot
}

# Lote PT — sugerido FEFO para producto 6
$suggestPT = Api-Get "/inventory-lots/suggest/6.json"
$ID_LOT_PT = $suggestPT.idInventoryLot

# Período fiscal
$opData = Api-Get "/production-orders/$ID_PRODUCTION_ORDER.json"
$ID_FISCAL_PERIOD = if ($opData.idFiscalPeriod) { $opData.idFiscalPeriod } else { 4 }

# ── Encabezado ────────────────────────────────────────────────────────────────
Write-Host "╔══════════════════════════════════════════════════════╗"
Write-Host "║   CASO 2 — MANUFACTURA · Verificacion de Documentos ║"
Write-Host "╚══════════════════════════════════════════════════════╝"
Write-Host ""
Write-Host "  IDs descubiertos:" -ForegroundColor DarkGray
Write-Host "    FC=$ID_PURCHASE_INVOICE  OP=$ID_PRODUCTION_ORDER  LotPT=$ID_LOT_PT" -ForegroundColor DarkGray
Write-Host "    FV=$ID_SALES_INVOICE  ADJ=$ID_ADJUSTMENT  EntryDevIng=$(if ($ID_ENTRY_DEV_ING) { $ID_ENTRY_DEV_ING } else { '(no encontrado)' })" -ForegroundColor DarkGray
Write-Host "    Periodo fiscal: $ID_FISCAL_PERIOD" -ForegroundColor DarkGray

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 1 — CATÁLOGO
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 1 — Catalogo (seed y configuracion)"

# MP productos 2-5 idProductType=1
foreach ($prodId in @(2,3,4,5)) {
    $p = Api-Get "/products/${prodId}.json"
    Assert-200 "GET /products/${prodId}.json"
    Assert-Eq "product[${prodId}].idProductType = 1 (MP)" 1 $p.idProductType
}

# PT producto 6 idProductType=3
$p6 = Api-Get "/products/6.json"
Assert-200 "GET /products/6.json"
Assert-Eq "product[6].idProductType = 3 (PT)" 3 $p6.idProductType
Assert-Eq "product[6].idProduct = 6"          6 $p6.idProduct

# Receta activa del PT
$recipes = Api-Get "/product-recipes/by-output/6.json"
Assert-200 "GET /product-recipes/by-output/6.json"
$activeRecipes = @($recipes | Where-Object { $_.isActive -eq $true })
Assert-Gte "receta activa del PT existe" 1 $activeRecipes.Count
if ($activeRecipes.Count -gt 0) {
    $recipe = $activeRecipes[0]
    Assert-FloatEq "receta.quantityOutput = 1.0" 1.0 $recipe.quantityOutput
    $recipeLines = @($recipe.lines).Count
    Assert-Gte "receta tiene 4 ingredientes" 4 $recipeLines
    Log-Info "  Receta activa: quantityOutput=$($recipe.quantityOutput)  ingredientes=$recipeLines"
}

# Tipos de facturas
$purchaseTypes = Api-Get "/purchase-invoice-types/data.json"
Assert-200 "GET /purchase-invoice-types/data.json"
$ptCount = @($purchaseTypes | Where-Object { $_.idPurchaseInvoiceType -eq 1 }).Count
Assert-Eq "purchase-invoice-type id=1 existe" 1 $ptCount

$salesTypes = Api-Get "/sales-invoice-types/data.json"
Assert-200 "GET /sales-invoice-types/data.json"
$stCount = @($salesTypes | Where-Object { $_.idSalesInvoiceType -eq 1 }).Count
Assert-Eq "sales-invoice-type id=1 existe" 1 $stCount

$adjTypes = Api-Get "/inventory-adjustment-types/data.json"
Assert-200 "GET /inventory-adjustment-types/data.json"
$mermaCount = @($adjTypes | Where-Object { $_.idInventoryAdjustmentType -eq 1 }).Count
Assert-Eq "adjustment-type id=1 (MERMA) existe" 1 $mermaCount
$prodCount = @($adjTypes | Where-Object { $_.idInventoryAdjustmentType -eq 2 }).Count
Assert-Eq "adjustment-type id=2 (PRODUCCION) existe" 1 $prodCount

# ProductAccounts MP → cta 110
foreach ($prodId in @(2,3,4,5)) {
    $pas = Api-Get "/product-accounts/by-product/${prodId}.json"
    Assert-200 "GET /product-accounts/by-product/${prodId}.json"
    $pa110 = @($pas | Where-Object { $_.idAccount -eq 110 }).Count
    Assert-Gte "product-accounts[${prodId}] -> cta 110 existe" 1 $pa110
}

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 2 — FACTURA DE COMPRA DE MP
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 2 — Factura de Compra MP (id=$ID_PURCHASE_INVOICE)"

$fc = Api-Get "/purchase-invoices/${ID_PURCHASE_INVOICE}.json"
Assert-200 "GET /purchase-invoices/${ID_PURCHASE_INVOICE}.json"

$fcStatus = $fc.statusInvoice
$fcEntry  = $fc.idAccountingEntry
Assert-Eq  "FC statusInvoice = Confirmado" "Confirmado" $fcStatus
Assert-Gte "FC tiene idAccountingEntry" 1 ([double]($fcEntry ?? 0))
Log-Info   "  FC idAccountingEntry: $(if ($fcEntry) { $fcEntry } else { '(no expuesto)' })"
Log-Info   "  FC líneas (si expuesto): $(@($fc.lines).Count)"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3 — LOTES DE MP (después de producción → qty debe ser 0)
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 3 — Lotes de MP (creados al confirmar FC, consumidos en OP)"

function Check-MpLot([int]$prodId, $lotId, [double]$expQty, [double]$expCost, [string]$name) {
    $lot = Api-Get "/inventory-lots/${lotId}.json"
    Assert-200 "GET /inventory-lots/${lotId}.json ($name)"
    Assert-Eq       "${name} statusLot"           "Disponible" $lot.statusLot
    Assert-FloatEq  "${name} quantityAvailable"   $expQty      ([double]($lot.quantityAvailable ?? 999))
    Assert-FloatEq  "${name} unitCost"            $expCost     ([double]($lot.unitCost ?? 0))
    Assert-Eq       "${name} idProduct = ${prodId}" $prodId    $lot.idProduct
    Assert-Eq       "${name} sourceType = Compra" "Compra"     $lot.sourceType
    Assert-Eq       "${name} origen = FC ${ID_PURCHASE_INVOICE}" $ID_PURCHASE_INVOICE $lot.idPurchaseInvoice
}

Check-MpLot 2 $ID_LOT_CHILE   0 1000 "Lote Chile Seco"
Check-MpLot 3 $ID_LOT_VINAGRE 0 500  "Lote Vinagre Blanco"
Check-MpLot 4 $ID_LOT_SAL     0 200  "Lote Sal"
Check-MpLot 5 $ID_LOT_FRASCO  0 300  "Lote Frasco 250ml"

foreach ($prodId in @(2,3,4,5)) {
    $st = Api-Get "/inventory-lots/stock/${prodId}.json"
    Assert-FloatEq "Stock MP idProduct=${prodId} = 0 (consumido en OP)" 0 ([double]($st ?? 0))
}

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 4 — ORDEN DE PRODUCCIÓN
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 4 — Orden de Produccion (id=$ID_PRODUCTION_ORDER)"

$op = Api-Get "/production-orders/${ID_PRODUCTION_ORDER}.json"
Assert-200 "GET /production-orders/${ID_PRODUCTION_ORDER}.json"

Assert-Eq       "OP statusProductionOrder = Completado" "Completado" $op.statusProductionOrder
Assert-Eq       "OP idProduct = 6 (PT)"                6            $op.lines[0].idProduct
Assert-FloatEq  "OP quantityRequired = 100"            100          ([double]($op.lines[0].quantityRequired ?? 0))
Assert-FloatEq  "OP quantityProduced = 100"            100          ([double]($op.lines[0].quantityProduced ?? 0))
Assert-Eq       "OP idWarehouse = $ID_WAREHOUSE"       $ID_WAREHOUSE $op.idWarehouse

if ($op.numberProductionOrder -ne "BORRADOR" -and $op.numberProductionOrder) {
    Log-Ok "OP numberProductionOrder asignado: $($op.numberProductionOrder)"
} else {
    Log-Fail "OP sigue en BORRADOR o numero vacio: '$($op.numberProductionOrder)'"
}
Log-Info "  OP: status=$($op.statusProductionOrder)  numero=$($op.numberProductionOrder)"

$opsByPeriod = Api-Get "/production-orders/by-period/${ID_FISCAL_PERIOD}.json"
Assert-200 "GET /production-orders/by-period/${ID_FISCAL_PERIOD}.json"
$opInPeriod = @($opsByPeriod | Where-Object { $_.idProductionOrder -eq $ID_PRODUCTION_ORDER }).Count
Assert-Eq "OP aparece en listado del periodo $ID_FISCAL_PERIOD" 1 $opInPeriod

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 5 — LOTE DEL PT
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 5 — Lote PT (creado al completar OP, id=$ID_LOT_PT)"

$lotPT = Api-Get "/inventory-lots/${ID_LOT_PT}.json"
Assert-200 "GET /inventory-lots/${ID_LOT_PT}.json"

Assert-Eq       "lote PT statusLot = Disponible"         "Disponible"          $lotPT.statusLot
Assert-FloatEq  "lote PT quantityAvailable = 73"         $EXP_STOCK_PT_FINAL   ([double]($lotPT.quantityAvailable ?? 0))
Assert-FloatEq  "lote PT quantityReserved = 0"           0                     ([double]($lotPT.quantityReserved ?? 0))
Assert-FloatEq  "lote PT unitCost = 526"                 $EXP_UNIT_COST_PT     ([double]($lotPT.unitCost ?? 0))
Assert-Eq       "lote PT sourceType = Producción"        "Producción"          $lotPT.sourceType
Assert-Eq       "lote PT idProduct = 6"                  6                     $lotPT.idProduct
Assert-Eq       "lote PT idWarehouse = $ID_WAREHOUSE"    $ID_WAREHOUSE         $lotPT.idWarehouse
Assert-Gte      "lote PT nameWarehouse no vacío"          1                     $lotPT.nameWarehouse.Length
Log-Info "  Almacen PT: '$($lotPT.nameWarehouse)' (id=$($lotPT.idWarehouse))  cost=₡$($lotPT.unitCost)"

$stockPTRaw = Api-Get "/inventory-lots/stock/6.json"
Assert-200 "GET /inventory-lots/stock/6.json"
Assert-FloatEq "stock PT total = 73 frascos" $EXP_STOCK_PT_FINAL ([double]($stockPTRaw ?? 0))
Log-Info "  Formula: 100 (produccion) - 30 (venta) + 5 (devolucion) - 2 (regalia) = 73"

$suggestPT2 = Api-Get "/inventory-lots/suggest/6.json"
Assert-200 "GET /inventory-lots/suggest/6.json"
Assert-Eq "lote sugerido PT = lote de produccion" $ID_LOT_PT $suggestPT2.idInventoryLot

$lotsByWH = Api-Get "/inventory-lots/by-product/6.json?idWarehouse=${ID_WAREHOUSE}"
Assert-200 "GET /inventory-lots/by-product/6.json?idWarehouse=$ID_WAREHOUSE"
$lotCountWH = @($lotsByWH).Count
Assert-Gte "al menos 1 lote PT en almacen $ID_WAREHOUSE" 1 $lotCountWH
$sumWH = ($lotsByWH | ForEach-Object { [double]$_.quantityAvailable } | Measure-Object -Sum).Sum
Assert-FloatEq "stock PT en almacen $ID_WAREHOUSE = 73" $EXP_STOCK_PT_FINAL $sumWH

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 6 — FACTURA DE VENTA
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 6 — Factura de Venta PT (id=$ID_SALES_INVOICE)"

$fv = Api-Get "/sales-invoices/${ID_SALES_INVOICE}.json"
Assert-200 "GET /sales-invoices/${ID_SALES_INVOICE}.json"

Assert-Eq  "FV statusInvoice = Confirmado" "Confirmado" $fv.statusInvoice
Assert-Gte "FV tiene idAccountingEntry" 1 ([double]($fv.idAccountingEntry ?? 0))
Log-Info   "  FV idAccountingEntry (ingreso FV-): $(if ($fv.idAccountingEntry) { $fv.idAccountingEntry } else { '?' })"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 6b — GUARDIA DE DEVOLUCIÓN EXCEDIDA
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 6b — Guardia devolucion parcial (cantidad > vendida)"

Log-Info "Probando guardia: devolver 31 frascos (>30 vendidos)"

$guardBody = @"
{
  "dateReturn":"$(Get-Date -Format 'yyyy-MM-dd')",
  "descriptionReturn":"Test guardia — debe fallar",
  "refundMode":"EfectivoInmediato",
  "lines":[{"idInventoryLot":$ID_LOT_PT,"quantity":31,"totalLineAmount":52500}]
}
"@

$headers = @{ "Authorization" = "Bearer $TOKEN"; "Content-Type" = "application/json" }
try {
    Invoke-RestMethod -Method POST -Uri "$HOST_URL/sales-invoices/$ID_SALES_INVOICE/partial-return" `
        -Body $guardBody -Headers $headers -SkipCertificateCheck -ErrorAction Stop | Out-Null
    Log-Fail "Guardia exceso: HTTP 200 — la API ACEPTO 31 frascos (regresion)"
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    if ($code -eq 422) { Log-Ok "Guardia exceso: HTTP 422 — devolver 31 frascos rechazado correctamente" }
    else { Log-Fail "Guardia exceso: HTTP inesperado $code (esperado 422)" }
}

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 6c — ASIENTO DEV-ING-FV
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 6c — Asiento DEV-ING-FV (reintegro automatico de la devolucion)"

if ($ID_ENTRY_DEV_ING -and $ID_ENTRY_DEV_ING -ne 0) {
    $entryDevIng = Api-Get "/accounting-entries/${ID_ENTRY_DEV_ING}.json"
    Assert-200 "GET /accounting-entries/${ID_ENTRY_DEV_ING}.json"

    $entryNumber = $entryDevIng.numberEntry
    $entryStatus = $entryDevIng.statusEntry
    $entryModule = $entryDevIng.originModule
    $entryRecord = $entryDevIng.idOriginRecord

    if ($entryNumber -like "DEV-ING-FV-*") { Log-Ok "numberEntry empieza con DEV-ING-FV-: $entryNumber" }
    else { Log-Fail "numberEntry inesperado: '$entryNumber' (esperado DEV-ING-FV-...)" }

    Assert-Eq "statusEntry = Publicado"           "Publicado"          $entryStatus
    Assert-Eq "originModule = SalesReturnPartial" "SalesReturnPartial" $entryModule
    Assert-Eq "idOriginRecord = idSalesInvoice"   $ID_SALES_INVOICE    $entryRecord

    $lines       = $entryDevIng.lines
    $totalDR     = ($lines | ForEach-Object { [double]$_.debitAmount }  | Measure-Object -Sum).Sum
    $totalCR     = ($lines | ForEach-Object { [double]$_.creditAmount } | Measure-Object -Sum).Sum
    Assert-FloatEq "DEV-ING-FV SigmaDR = SigmaCR (partida doble)" $totalDR $totalCR
    Assert-FloatEq "DEV-ING-FV total = 8475 (5 × 1695)"           8475     $totalDR

    $dr117 = ($lines | Where-Object { $_.idAccount -eq 117 } | ForEach-Object { [double]$_.debitAmount } | Measure-Object -Sum).Sum
    Assert-FloatEq "DEV-ING-FV DR cta 117 Ingresos = 7500" 7500 $dr117

    $dr127 = ($lines | Where-Object { $_.idAccount -eq 127 } | ForEach-Object { [double]$_.debitAmount } | Measure-Object -Sum).Sum
    Assert-FloatEq "DEV-ING-FV DR cta 127 IVA = 975" 975 $dr127

    $cr106 = ($lines | Where-Object { $_.idAccount -eq 106 } | ForEach-Object { [double]$_.creditAmount } | Measure-Object -Sum).Sum
    Assert-FloatEq "DEV-ING-FV CR cta 106 Caja = 8475" 8475 $cr106
} else {
    Log-Warn "idEntryDevIng no disponible — omitiendo verificacion DEV-ING-FV"
}

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 7 — AJUSTE DE INVENTARIO (regalía)
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 7 — Ajuste de Inventario / Regalia (id=$ID_ADJUSTMENT)"

$adj = Api-Get "/inventory-adjustments/${ID_ADJUSTMENT}.json"
Assert-200 "GET /inventory-adjustments/${ID_ADJUSTMENT}.json"

$adjStatus = $adj.statusAdjustment
$adjLines  = $adj.inventoryAdjustmentLines
if (-not $adjLines) { $adjLines = $adj.lines }
$adjDelta  = if ($adjLines -and @($adjLines).Count -gt 0) { [double]$adjLines[0].quantityDelta } else { 0 }
$adjEntry  = $adj.idAccountingEntry

Assert-Eq       "ajuste statusAdjustment = Confirmado" "Confirmado" $adjStatus
Assert-FloatEq  "ajuste linea delta = -2"              -2           $adjDelta
Assert-Gte      "ajuste tiene idAccountingEntry"       1            ([double]($adjEntry ?? 0))

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 8 — ÓRDENES DE PRODUCCIÓN DEL PERÍODO
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 8 — Ordenes de Produccion del periodo $ID_FISCAL_PERIOD"

$opsAll = Api-Get "/production-orders/by-period/${ID_FISCAL_PERIOD}.json"
Assert-200 "GET /production-orders/by-period/${ID_FISCAL_PERIOD}.json"
$opCount        = @($opsAll).Count
$opCompletadas  = @($opsAll | Where-Object { $_.statusProductionOrder -eq "Completado" }).Count
$opProduct6     = @($opsAll | Where-Object { $_.lines[0].idProduct -eq 6 }).Count

Assert-Gte "al menos 1 OP en el periodo"         1 $opCount
Assert-Gte "al menos 1 OP Completada"            1 $opCompletadas
Assert-Gte "al menos 1 OP con idProduct=6 (PT)"  1 $opProduct6
Log-Info   "  Total OPs en periodo: $opCount  |  Completadas: $opCompletadas"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 9 — TABLA DE CONCILIACIÓN
# ═══════════════════════════════════════════════════════════════════════════════
Section "SECCION 9 — Tabla de Conciliacion de Inventario PT"

Write-Host ""
$sep40 = "─" * 40; $sep10 = "─" * 10; $sep12 = "─" * 12; $sep14 = "─" * 14
Write-Host ("  {0,-40} {1,10} {2,12} {3,14}" -f "CONCEPTO","UNIDADES","C.UNIT","TOTAL")
Write-Host ("  {0,-40} {1,10} {2,12} {3,14}" -f $sep40,$sep10,$sep12,$sep14)
Write-Host ("  {0,-40} {1,10} {2,12} {3,14}" -f "(+) Produccion 100 frascos",  "+100", "₡526", "₡52,600")
Write-Host ("  {0,-40} {1,10} {2,12} {3,14}" -f "(-) Venta 30 frascos",         " -30", "₡526", "-₡15,780")
Write-Host ("  {0,-40} {1,10} {2,12} {3,14}" -f "(+) Devolucion parcial 5 frascos", "  +5", "₡526", "₡2,630")
Write-Host ("  {0,-40} {1,10} {2,12} {3,14}" -f "(-) Regalia 2 frascos",        "  -2", "₡526", "-₡1,052")
Write-Host ("  {0,-40} {1,10} {2,12} {3,14}" -f $sep40,$sep10,$sep12,$sep14)
Write-Host ("  {0,-40} {1,10} {2,12} {3,14}" -f "SALDO FINAL PT",               "73",  "₡526", "₡38,398")
Write-Host ""

$stockFinalCheck = Api-Get "/inventory-lots/stock/6.json"
Assert-FloatEq "Stock PT fisico final = 73 frascos" $EXP_STOCK_PT_FINAL ([double]($stockFinalCheck ?? 0))

$lotFinalData = Api-Get "/inventory-lots/${ID_LOT_PT}.json"
if ($lotFinalData -and $lotFinalData.unitCost -and $lotFinalData.quantityAvailable) {
    $costoLibros = [double]$lotFinalData.quantityAvailable * [double]$lotFinalData.unitCost
    Assert-FloatEq "Costo en libros PT = 73 × ₡526 = ₡38,398" 38398 $costoLibros
}

foreach ($prodId in @(2,3,4,5)) {
    $stmp = Api-Get "/inventory-lots/stock/${prodId}.json"
    Assert-FloatEq "Stock MP idProduct=${prodId} = 0 (consumido)" 0 ([double]($stmp ?? 0))
}

# ═══════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ═══════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗"
if ($checksFail -eq 0) {
    Write-Host "║   ✅  TODAS LAS VERIFICACIONES PASARON             ║" -ForegroundColor Green
} else {
    Write-Host "║   ⚠   ALGUNAS VERIFICACIONES FALLARON              ║" -ForegroundColor Red
}
Write-Host "╚══════════════════════════════════════════════════════╝"
Write-Host "  ✅  Exitosas : $checksOk"   -ForegroundColor Green
if ($checksFail -gt 0) { Write-Host "  ❌  Fallidas : $checksFail" -ForegroundColor Red }
Write-Host ""

@(
    "",
    "# ── RESUMEN ────────────────────────────────────────────────────────",
    "  Verificaciones exitosas : $checksOk",
    "  Verificaciones fallidas : $checksFail"
) | Add-Content $outputFile

Write-Host "  Reporte guardado en: $outputFile"
Write-Host ""

exit ($checksFail -gt 0 ? 1 : 0)
