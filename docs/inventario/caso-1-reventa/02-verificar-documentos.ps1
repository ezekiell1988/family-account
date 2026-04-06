#!/usr/bin/env pwsh
# ============================================================================
#  CASO 1 — REVENTA (Coca-Cola 355ml)
#  02-verificar-documentos.ps1 — Verificación de Documentos e Inventario
#
#  Propósito:
#   Descubre automáticamente los IDs de los documentos del Caso 1 consultando
#   los asientos contables y la API, sin depender de resultado_caso1_*.txt.
#   Verifica que los documentos se generaron correctamente y el inventario es
#   correcto. Genera verificacion_docs_caso1_*.txt con el reporte completo.
#
#  Uso:
#   pwsh docs/inventario/caso-1-reventa/02-verificar-documentos.ps1
# ============================================================================

$SCRIPT_DIR = $PSScriptRoot
$REPO_ROOT  = (Resolve-Path (Join-Path $SCRIPT_DIR "../../..")).Path
$CREDS_FILE = Join-Path $REPO_ROOT "credentials\db.txt"
$HOST_URL   = "https://localhost:8000/api/v1"
$EMAIL      = "ezekiell1988@hotmail.com"

# ── Contadores ────────────────────────────────────────────────────────────────
$script:CHECKS_OK   = 0
$script:CHECKS_FAIL = 0

# ── Archivo de reporte ────────────────────────────────────────────────────────
$RUN_TS      = (Get-Date -Format "yyyy-MM-dd_HH-mm-ss")
$OUTPUT_FILE = Join-Path $SCRIPT_DIR "verificacion_docs_caso1_$RUN_TS.txt"

# ── Helpers de presentación ───────────────────────────────────────────────────
function Write-Section($msg) {
    Write-Host ""
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "▶  $msg" -ForegroundColor Cyan
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Add-Content $OUTPUT_FILE ""
    Add-Content $OUTPUT_FILE "══ $msg ══"
}

function Write-Ok($msg) {
    Write-Host "  OK  $msg" -ForegroundColor Green
    $script:CHECKS_OK++
    Add-Content $OUTPUT_FILE "  [OK]   $msg"
}
function Write-Fail($msg) {
    Write-Host "  FAIL  $msg" -ForegroundColor Red
    $script:CHECKS_FAIL++
    Add-Content $OUTPUT_FILE "  [FAIL] $msg"
}
function Write-Info($msg) {
    Write-Host "  $msg" -ForegroundColor DarkGray
    Add-Content $OUTPUT_FILE "         $msg"
}
function Write-Warn($msg) {
    Write-Host "  W   $msg" -ForegroundColor Yellow
    Add-Content $OUTPUT_FILE "  [WARN] $msg"
}

# ── Assert helpers ────────────────────────────────────────────────────────────
function Assert-200($label) {
    if ($script:HTTP_STATUS -eq 200) { Write-Ok "$label — HTTP 200" }
    else { Write-Fail "$label — esperado HTTP 200, recibido $($script:HTTP_STATUS)" }
}

function Assert-Eq($label, $expected, $actual) {
    if ("$actual" -eq "$expected") { Write-Ok "${label}: $actual" }
    else { Write-Fail "${label}: esperado='$expected'  real='$actual'" }
}

function Assert-Gte($label, $expected, $actual) {
    $e = try { [decimal]$expected } catch { 0 }
    $a = try { [decimal]$actual   } catch { 0 }
    if ($a -ge $e) { Write-Ok "${label}: $actual (>= $expected)" }
    else { Write-Fail "${label}: esperado >= $expected  real='$actual'" }
}

function Assert-FloatEq($label, $expected, $actual) {
    $e = try { [decimal]$expected } catch { 0 }
    $a = try { [decimal]$actual   } catch { 0 }
    $diff = [Math]::Abs($a - $e)
    if ($diff -lt 0.01) { Write-Ok "${label}: $actual" }
    else { Write-Fail "${label}: esperado=$expected  real='$actual'" }
}

# ── API helper ────────────────────────────────────────────────────────────────
$script:HTTP_STATUS   = 0
$script:LAST_RESPONSE = $null

function Invoke-ApiGet($path) {
    $headers = @{ "Authorization" = "Bearer $script:TOKEN" }
    try {
        $result               = Invoke-RestMethod -Method GET -Uri "$HOST_URL$path" -Headers $headers -SkipCertificateCheck -StatusCodeVariable sc
        $script:HTTP_STATUS   = [int]$sc
        $script:LAST_RESPONSE = $result
        return $result
    }
    catch {
        $script:HTTP_STATUS = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
        $script:LAST_RESPONSE = $null
        try {
            $stream  = $_.Exception.Response.GetResponseStream()
            $reader  = [System.IO.StreamReader]::new($stream)
            $raw     = $reader.ReadToEnd()
            $script:LAST_RESPONSE = $raw | ConvertFrom-Json -ErrorAction SilentlyContinue
        } catch {}
        return $null
    }
}

function Invoke-ApiPost($path, $body) {
    $headers = @{ "Authorization" = "Bearer $script:TOKEN"; "Content-Type" = "application/json" }
    try {
        $result               = Invoke-RestMethod -Method POST -Uri "$HOST_URL$path" -Headers $headers -Body ([System.Text.Encoding]::UTF8.GetBytes($body)) -SkipCertificateCheck -StatusCodeVariable sc
        $script:HTTP_STATUS   = [int]$sc
        $script:LAST_RESPONSE = $result
        return $result
    }
    catch {
        $script:HTTP_STATUS = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
        $script:LAST_RESPONSE = $null
        try {
            $stream  = $_.Exception.Response.GetResponseStream()
            $reader  = [System.IO.StreamReader]::new($stream)
            $raw     = $reader.ReadToEnd()
            $script:LAST_RESPONSE = $raw | ConvertFrom-Json -ErrorAction SilentlyContinue
        } catch {}
        return $null
    }
}

# ── Leer credenciales ─────────────────────────────────────────────────────────
if (-not (Test-Path $CREDS_FILE)) {
    Write-Host "ERROR  No se encontro $CREDS_FILE" -ForegroundColor Red; exit 1
}
$creds   = Get-Content $CREDS_FILE
$DB_HOST = (($creds | Where-Object { $_ -match '^HOST:' })     -split '\s+')[1]
$DB_PORT = (($creds | Where-Object { $_ -match '^PORT:' })     -split '\s+')[1]
$DB_USER = (($creds | Where-Object { $_ -match '^USER:' })     -split '\s+')[1]
$DB_PASS = (($creds | Where-Object { $_ -match '^PASSWORD:' }) -split '\s+')[1]

# ── Autenticacion ─────────────────────────────────────────────────────────────
$PIN = "12345"
$sqlQ = "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');"
& sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlQ *>$null

$loginResp = Invoke-RestMethod -Method POST -Uri "$HOST_URL/auth/login" `
    -ContentType "application/json" `
    -Body "{`"emailUser`":`"$EMAIL`",`"pin`":`"$PIN`"}" `
    -SkipCertificateCheck
$script:TOKEN = $loginResp.accessToken
if (-not $script:TOKEN -or $script:TOKEN -eq "null") {
    Write-Host "ERROR  No se pudo obtener token" -ForegroundColor Red; exit 1
}

# ── Valores esperados ─────────────────────────────────────────────────────────
$EXP_UNIT_COST   = "1000"
$EXP_STOCK_FINAL = "91"
$EXP_QTY_RESERVED= "0"
$EXP_QTY_NET     = "91"
$EXP_STATUS_LOT  = "Disponible"
$EXP_SOURCE_TYPE = "Compra"
$ID_WAREHOUSE    = 1
$ID_FISCAL_PERIOD= 4

# ── Descubrir IDs desde asientos contables ────────────────────────────────────
$allEntries = Invoke-RestMethod -Method GET -Uri "$HOST_URL/accounting-entries/data.json" `
    -Headers @{ "Authorization" = "Bearer $script:TOKEN" } -SkipCertificateCheck

$ID_PURCHASE_INVOICE = ($allEntries | Where-Object { $_.numberEntry -like "FC-*" } |
    Sort-Object idAccountingEntry | Select-Object -Last 1).idOriginRecord
$ID_SALES_INVOICE = ($allEntries | Where-Object { $_.numberEntry -like "FV-*" } |
    Sort-Object idAccountingEntry | Select-Object -Last 1).idOriginRecord
$ID_ADJUSTMENT = ($allEntries | Where-Object { $_.numberEntry -like "AJ-*" } |
    Sort-Object idAccountingEntry | Select-Object -Last 1).idOriginRecord
$ID_ENTRY_DEV_ING = ($allEntries | Where-Object { $_.numberEntry -like "DEV-ING-FV-*" } |
    Sort-Object idAccountingEntry | Select-Object -Last 1).idAccountingEntry

$lotsDisc = Invoke-RestMethod -Method GET `
    -Uri "$HOST_URL/inventory-lots/by-product/1.json?idWarehouse=$ID_WAREHOUSE" `
    -Headers @{ "Authorization" = "Bearer $script:TOKEN" } -SkipCertificateCheck
$ID_LOT = (@($lotsDisc) | Where-Object { $_.idPurchaseInvoice -eq $ID_PURCHASE_INVOICE } |
    Sort-Object idInventoryLot | Select-Object -Last 1).idInventoryLot

# ── Inicializar archivo reporte ───────────────────────────────────────────────
@"
# ==================================================================
#  CASO 1 — REVENTA · Verificacion de Documentos e Inventario
#  Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
#  IDs descubiertos desde la API (sin resultado_caso1_*.txt)
# ==================================================================
"@ | Set-Content $OUTPUT_FILE -Encoding UTF8

# ── Cabecera ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗"
Write-Host "║   CASO 1 — REVENTA · Verificacion de Documentos     ║"
Write-Host "╚══════════════════════════════════════════════════════╝"
Write-Host ""
Write-Host "  IDs descubiertos:" -ForegroundColor DarkGray
Write-Host "    PC=$ID_PURCHASE_INVOICE  LOT=$ID_LOT  FV=$ID_SALES_INVOICE  ADJ=$ID_ADJUSTMENT  EntryDevIng=$(if ($ID_ENTRY_DEV_ING) { $ID_ENTRY_DEV_ING } else { '(no encontrado)' })" -ForegroundColor DarkGray
Write-Host "    Periodo fiscal: $ID_FISCAL_PERIOD" -ForegroundColor DarkGray

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 1 — CATÁLOGO
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 1 — Catalogo (configuracion seed)"

# 1-a  Producto 1
$resp = Invoke-ApiGet "/products/1.json"
Assert-200 "GET /products/1.json"
Assert-Eq "product.idProduct"     "1" "$($resp.idProduct)"
Assert-Eq "product.idProductType" "4" "$($resp.idProductType)"
Assert-Eq "product.idUnit"        "1" "$($resp.idUnit)"

# 1-b  Tipo factura compra id=1
$resp = Invoke-ApiGet "/purchase-invoice-types/data.json"
Assert-200 "GET /purchase-invoice-types/data.json"
$cnt = (@($resp) | Where-Object { $_.idPurchaseInvoiceType -eq 1 }).Count
Assert-Eq "purchase-invoice-type id=1 existe" "1" "$cnt"

# 1-c  Tipo factura venta id=1
$resp = Invoke-ApiGet "/sales-invoice-types/data.json"
Assert-200 "GET /sales-invoice-types/data.json"
$cnt = (@($resp) | Where-Object { $_.idSalesInvoiceType -eq 1 }).Count
Assert-Eq "sales-invoice-type id=1 existe" "1" "$cnt"

# 1-d  Tipo ajuste id=1
$resp = Invoke-ApiGet "/inventory-adjustment-types/data.json"
Assert-200 "GET /inventory-adjustment-types/data.json"
$cnt = (@($resp) | Where-Object { $_.idInventoryAdjustmentType -eq 1 }).Count
Assert-Eq "inventory-adjustment-type id=1 existe" "1" "$cnt"

# 1-e  ProductAccounts vacíos (se borró en PASO 4)
$resp = Invoke-ApiGet "/product-accounts/by-product/1.json"
Assert-200 "GET /product-accounts/by-product/1.json"
$paCount = if ($resp) { @($resp).Count } else { 0 }
Assert-Eq "product-accounts vacio post-DELETE" "0" "$paCount"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 2 — FACTURA DE COMPRA
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 2 — Factura de Compra (id=$ID_PURCHASE_INVOICE)"

$resp = Invoke-ApiGet "/purchase-invoices/$ID_PURCHASE_INVOICE.json"
Assert-200 "GET /purchase-invoices/$ID_PURCHASE_INVOICE.json"

$pcStatus = $resp.statusInvoice
$pcEntry  = $resp.idAccountingEntry
$pcLot    = if ($resp.lines)                     { $resp.lines[0].lotNumber }
            elseif ($resp.purchaseInvoiceLines)  { $resp.purchaseInvoiceLines[0].lotNumber }
            else { $null }

Assert-Eq  "PC statusInvoice = Confirmado" "Confirmado" "$pcStatus"
Assert-Gte "PC tiene idAccountingEntry"    "1"          "$($pcEntry ?? 0)"
Write-Info "  PC numero de lote en linea: $(if ($pcLot) { $pcLot } else { '(campo no expuesto directamente)' })"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3a — Stock total global
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 3a — Stock total global del producto 1"

$resp = Invoke-ApiGet "/inventory-lots/stock/1.json"
Assert-200 "GET /inventory-lots/stock/1.json"
$stockActual = if ($resp -is [decimal] -or $resp -is [int] -or $resp -is [double]) { $resp }
               else { try { [decimal]$resp } catch { 0 } }
Assert-FloatEq "stock total = $EXP_STOCK_FINAL u." $EXP_STOCK_FINAL $stockActual
Write-Info "  Formula: 100 (compra) - 10 (venta) + 3 (devolucion) - 2 (regalia) = 91"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3b — Lote específico
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 3b — Lote especifico (id=$ID_LOT) — detalle completo"

$resp = Invoke-ApiGet "/inventory-lots/$ID_LOT.json"
Assert-200 "GET /inventory-lots/$ID_LOT.json"

Assert-Eq       "lote statusLot"                      $EXP_STATUS_LOT  "$($resp.statusLot)"
Assert-FloatEq  "lote quantityAvailable"               $EXP_STOCK_FINAL "$($resp.quantityAvailable ?? 0)"
Assert-FloatEq  "lote quantityReserved"                $EXP_QTY_RESERVED "$($resp.quantityReserved ?? 0)"
Assert-FloatEq  "lote quantityAvailableNet"            $EXP_QTY_NET     "$($resp.quantityAvailableNet ?? 0)"
Assert-FloatEq  "lote unitCost"                        $EXP_UNIT_COST   "$($resp.unitCost ?? 0)"
Assert-Eq       "lote sourceType = Compra"             $EXP_SOURCE_TYPE "$($resp.sourceType)"
Assert-Eq       "lote idProduct = 1"                   "1"              "$($resp.idProduct)"
Assert-Eq       "lote idWarehouse = $ID_WAREHOUSE"     "$ID_WAREHOUSE"  "$($resp.idWarehouse)"
$whNameLen = if ($resp.nameWarehouse) { $resp.nameWarehouse.Length } else { 0 }
Assert-Gte      "lote nameWarehouse no vacio"          "1"              "$whNameLen"
Assert-Eq       "lote origen = idPurchaseInvoice=$ID_PURCHASE_INVOICE" "$ID_PURCHASE_INVOICE" "$($resp.idPurchaseInvoice)"
Write-Info "  Almacen: $($resp.nameWarehouse) (id=$($resp.idWarehouse))"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3c — Lotes por producto (todos almacenes)
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 3c — Lotes por producto (todos los almacenes)"

$resp = Invoke-ApiGet "/inventory-lots/by-product/1.json"
Assert-200 "GET /inventory-lots/by-product/1.json"

$lotCount = @($resp).Count
Assert-Gte "al menos 1 lote activo del producto 1" "1" "$lotCount"

$sumFromList = (@($resp) | Measure-Object -Property quantityAvailable -Sum).Sum
Assert-FloatEq "suma quantityAvailable en lista = stock total" $EXP_STOCK_FINAL "$sumFromList"
Write-Info "  Lotes encontrados: $lotCount  |  Suma quantityAvailable: $sumFromList"

$firstLotId = @($resp)[0].idInventoryLot
Assert-Eq "lote[0] en lista = lote del caso" "$ID_LOT" "$firstLotId"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3d — Lotes por almacén 1
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 3d — Lotes por almacen (idWarehouse=$ID_WAREHOUSE)"

$resp = Invoke-ApiGet "/inventory-lots/by-product/1.json?idWarehouse=$ID_WAREHOUSE"
Assert-200 "GET /inventory-lots/by-product/1.json?idWarehouse=$ID_WAREHOUSE"

$lotCountWh = @($resp).Count
Assert-Gte "al menos 1 lote en almacen $ID_WAREHOUSE" "1" "$lotCountWh"

$sumWh = (@($resp) | Measure-Object -Property quantityAvailable -Sum).Sum
Assert-FloatEq "stock en almacen $ID_WAREHOUSE = $EXP_STOCK_FINAL" $EXP_STOCK_FINAL "$sumWh"
Write-Info "  Lotes en almacen ${ID_WAREHOUSE}: $lotCountWh  |  Suma: $sumWh"

$wrongWh = (@($resp) | Where-Object { $_.idWarehouse -ne $ID_WAREHOUSE }).Count
Assert-Eq "ningun lote en almacen diferente a $ID_WAREHOUSE" "0" "$wrongWh"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3e — Almacén inexistente → lista vacía
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 3e — Almacen inexistente → lista vacia"

$resp = Invoke-ApiGet "/inventory-lots/by-product/1.json?idWarehouse=9999"
Assert-200 "GET /inventory-lots/by-product/1.json?idWarehouse=9999"
$emptyCount = if ($resp) { @($resp).Count } else { 0 }
Assert-Eq "almacen 9999 devuelve 0 lotes" "0" "$emptyCount"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3f — Lote sugerido FEFO sin filtro
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 3f — Lote sugerido FEFO (sin filtro almacen)"

$resp = Invoke-ApiGet "/inventory-lots/suggest/1.json"
Assert-200 "GET /inventory-lots/suggest/1.json"

Assert-Eq       "lote sugerido = lote del caso"        "$ID_LOT"        "$($resp.idInventoryLot)"
Assert-Eq       "lote sugerido statusLot = Disponible" "Disponible"     "$($resp.statusLot)"
Assert-FloatEq  "lote sugerido quantityAvailableNet"   $EXP_QTY_NET     "$($resp.quantityAvailableNet ?? 0)"
Write-Info "  FEFO sugerido: id=$($resp.idInventoryLot)  disponibleNeto=$($resp.quantityAvailableNet)"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3g — Lote sugerido FEFO filtrado por almacén
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 3g — Lote sugerido FEFO (almacen $ID_WAREHOUSE)"

$resp = Invoke-ApiGet "/inventory-lots/suggest/1.json?idWarehouse=$ID_WAREHOUSE"
Assert-200 "GET /inventory-lots/suggest/1.json?idWarehouse=$ID_WAREHOUSE"

Assert-Eq "lote sugerido almacen $ID_WAREHOUSE = lote del caso" "$ID_LOT"       "$($resp.idInventoryLot)"
Assert-Eq "lote sugerido pertenece al almacen $ID_WAREHOUSE"    "$ID_WAREHOUSE" "$($resp.idWarehouse)"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3h — Lote sugerido en almacén inexistente → 404
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 3h — Lote sugerido en almacen inexistente → 404"

$null = Invoke-ApiGet "/inventory-lots/suggest/1.json?idWarehouse=9999"
if ($script:HTTP_STATUS -eq 404) { Write-Ok "Almacen 9999 sin lotes → HTTP 404" }
else { Write-Fail "Almacen 9999 sin lotes → esperado HTTP 404, recibido $($script:HTTP_STATUS)" }

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3i — Catálogo de almacenes
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 3i — Catalogo de almacenes"

$resp = Invoke-ApiGet "/warehouses/data.json"
Assert-200 "GET /warehouses/data.json"

$whCount  = @($resp).Count
Assert-Gte "al menos 1 almacen registrado" "1" "$whCount"

$whItem   = @($resp) | Where-Object { $_.idWarehouse -eq $ID_WAREHOUSE } | Select-Object -First 1
$whExists = if ($whItem) { 1 } else { 0 }
Assert-Eq "almacen id=$ID_WAREHOUSE existe en catalogo" "1" "$whExists"

Assert-Eq "almacen $ID_WAREHOUSE isActive = true" "true" "$($whItem.isActive.ToString().ToLower())"
Write-Info "  Almacen ${ID_WAREHOUSE}: '$($whItem.nameWarehouse)'  activo=$($whItem.isActive)  total almacenes=$whCount"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 4 — FACTURA DE VENTA
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 4 — Factura de Venta (id=$ID_SALES_INVOICE)"

$resp = Invoke-ApiGet "/sales-invoices/$ID_SALES_INVOICE.json"
Assert-200 "GET /sales-invoices/$ID_SALES_INVOICE.json"

Assert-Eq  "FV statusInvoice = Confirmado" "Confirmado" "$($resp.statusInvoice)"
Assert-Gte "FV tiene idAccountingEntry"    "1"          "$($resp.idAccountingEntry ?? 0)"
Write-Info "  FV idAccountingEntry (ingreso FV-): $($resp.idAccountingEntry)"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 4b — Guardia devolución parcial (>10 cajas → 422)
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 4b — Guardia devolucion parcial (cantidad > vendida → debe fallar)"

Write-Info "Probando guardia: devolver 11 cajas (>10 vendidas) desde lote $ID_LOT"

$todayStr = (Get-Date -Format "yyyy-MM-dd")
$guardBody = "{`"dateReturn`":`"$todayStr`",`"descriptionReturn`":`"Test guardia — debe fallar`",`"refundMode`":`"EfectivoInmediato`",`"lines`":[{`"idInventoryLot`":$ID_LOT,`"quantity`":11,`"totalLineAmount`":16500}]}"
$null = Invoke-ApiPost "/sales-invoices/$ID_SALES_INVOICE/partial-return" $guardBody

if ($script:HTTP_STATUS -eq 422) {
    $errMsg = $script:LAST_RESPONSE.error
    Write-Ok "Guardia exceso: HTTP 422 — devolver 11 cajas rechazado correctamente"
    if ($errMsg) { Write-Info "  Mensaje API: $errMsg" }
} elseif ($script:HTTP_STATUS -eq 200) {
    Write-Fail "Guardia exceso: HTTP 200 — la API ACEPTO 11 cajas (regresion Bug 4)"
    Write-Warn "  Verificar que PartialReturnAsync valida quantity <= salesLine.QuantityBase"
} else {
    Write-Fail "Guardia exceso: HTTP inesperado $($script:HTTP_STATUS) (esperado 422)"
}

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 4c — Asiento DEV-ING-FV
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 4c — Asiento DEV-ING-FV (reintegro automatico de la devolucion)"

if ($ID_ENTRY_DEV_ING -and $ID_ENTRY_DEV_ING -ne "0") {
    $resp = Invoke-ApiGet "/accounting-entries/$ID_ENTRY_DEV_ING.json"
    Assert-200 "GET /accounting-entries/$ID_ENTRY_DEV_ING.json"

    if ($resp.numberEntry -like "DEV-ING-FV-*") { Write-Ok "numberEntry empieza con DEV-ING-FV-: $($resp.numberEntry)" }
    else { Write-Fail "numberEntry inesperado: '$($resp.numberEntry)' (esperado DEV-ING-FV-...)" }

    Assert-Eq "statusEntry = Publicado"              "Publicado"          "$($resp.statusEntry)"
    Assert-Eq "originModule = SalesReturnPartial"    "SalesReturnPartial" "$($resp.originModule)"
    Assert-Eq "idOriginRecord = idSalesInvoice"      "$ID_SALES_INVOICE"  "$($resp.idOriginRecord)"

    $linesCount = @($resp.lines).Count
    Assert-Gte "DEV-ING-FV tiene al menos 3 lineas" "3" "$linesCount"

    $totalDR = (@($resp.lines) | Measure-Object -Property debitAmount  -Sum).Sum
    $totalCR = (@($resp.lines) | Measure-Object -Property creditAmount -Sum).Sum
    Assert-FloatEq "DEV-ING-FV ΣDR = ΣCR (partida doble)" $totalDR $totalCR
    Assert-FloatEq "DEV-ING-FV total = 5085 (3 x 1695)"   "5085"   $totalDR

    $DR_117 = (@($resp.lines) | Where-Object { $_.idAccount -eq 117 } | Measure-Object -Property debitAmount -Sum).Sum
    Assert-FloatEq "DEV-ING-FV DR cta 117 Ingresos = 4500" "4500" "$DR_117"

    $DR_127 = (@($resp.lines) | Where-Object { $_.idAccount -eq 127 } | Measure-Object -Property debitAmount -Sum).Sum
    Assert-FloatEq "DEV-ING-FV DR cta 127 IVA = 585" "585" "$DR_127"

    $CR_106 = (@($resp.lines) | Where-Object { $_.idAccount -eq 106 } | Measure-Object -Property creditAmount -Sum).Sum
    Assert-FloatEq "DEV-ING-FV CR cta 106 Caja = 5085" "5085" "$CR_106"
} else {
    Write-Warn "idEntryDevIng no disponible — omitiendo verificacion DEV-ING-FV"
    Write-Info "  Asegurate de que el resultado_caso1_*.txt incluye 'idEntryDevIng'"
}

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 5 — Órdenes de producción (no aplica Caso 1)
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 5 — Ordenes de Produccion (esperado: NINGUNA del producto 1)"

$resp = Invoke-ApiGet "/production-orders/by-period/$ID_FISCAL_PERIOD.json"
Assert-200 "GET /production-orders/by-period/$ID_FISCAL_PERIOD.json"
$poP1 = if ($resp) { (@($resp) | Where-Object { $_.idProduct -eq 1 }).Count } else { 0 }
if ($poP1 -eq 0) { Write-Ok "Sin ordenes de produccion del producto 1 (Reventa pura)" }
else { Write-Fail "Existen $poP1 OPs del producto 1 — no deberia en Caso 1" }

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 6 — Ajuste de inventario (regalía)
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 6 — Ajuste de Inventario / Regalia (id=$ID_ADJUSTMENT)"

$resp = Invoke-ApiGet "/inventory-adjustments/$ID_ADJUSTMENT.json"
Assert-200 "GET /inventory-adjustments/$ID_ADJUSTMENT.json"

$adjStatus = $resp.statusAdjustment ?? $resp.status
$adjDelta  = if ($resp.inventoryAdjustmentLines) { $resp.inventoryAdjustmentLines[0].quantityDelta }
             elseif ($resp.lines) { $resp.lines[0].quantityDelta } else { 0 }
$adjEntry  = $resp.idAccountingEntry ?? $resp.accountingEntryId

Assert-Eq      "ajuste statusAdjustment = Confirmado" "Confirmado" "$adjStatus"
Assert-FloatEq "ajuste linea delta = -2"              "-2"         "$($adjDelta ?? 0)"
Assert-Gte     "ajuste tiene idAccountingEntry"       "1"          "$($adjEntry ?? 0)"

# ════════════════════════════════════════════════════════════════════════════════
# SECCIÓN 7 — Conciliación de inventario
# ════════════════════════════════════════════════════════════════════════════════
Write-Section "SECCION 7 — Tabla de Conciliacion de Inventario"

Write-Host ""
Write-Host ("  {0,-35} {1,10} {2,12} {3,12}" -f "CONCEPTO", "UNIDADES", "C.UNIT", "TOTAL")
Write-Host ("  {0,-35} {1,10} {2,12} {3,12}" -f ("-"*35), ("-"*10), ("-"*12), ("-"*12))
Write-Host ("  {0,-35} {1,10} {2,12} {3,12}" -f "(+) Compra inicial",    "+100", "1000", "100000")
Write-Host ("  {0,-35} {1,10} {2,12} {3,12}" -f "(-) Venta 10 cajas",     "-10", "1000", "-10000")
Write-Host ("  {0,-35} {1,10} {2,12} {3,12}" -f "(+) Devolucion parcial",  "+3", "1000",   "3000")
Write-Host ("  {0,-35} {1,10} {2,12} {3,12}" -f "(-) Regalia 2 cajas",     "-2", "1000",  "-2000")
Write-Host ("  {0,-35} {1,10} {2,12} {3,12}" -f ("-"*35), ("-"*10), ("-"*12), ("-"*12))
Write-Host ("  {0,-35} {1,10} {2,12} {3,12}" -f "SALDO FINAL", "91", "1000", "91000")
Write-Host ""

# Stock final
$resp = Invoke-ApiGet "/inventory-lots/stock/1.json"
Assert-200 "GET /inventory-lots/stock/1.json (final)"
$stockFinalCheck = try { [decimal]$resp } catch { 0 }
Assert-FloatEq "Stock fisico final = 91 u." $EXP_STOCK_FINAL $stockFinalCheck

# Costo en libros
$resp = Invoke-ApiGet "/inventory-lots/$ID_LOT.json"
$lotCostFinal = $resp.unitCost ?? $resp.costPerUnit
$lotQtyFinal  = $resp.quantityAvailable ?? $resp.quantity
if ($lotCostFinal -and $lotQtyFinal) {
    $costoLibros = [decimal]$lotQtyFinal * [decimal]$lotCostFinal
    Assert-FloatEq "Costo en libros = 91x1000 = 91000" "91000" "$costoLibros"
}

# ════════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ════════════════════════════════════════════════════════════════════════════════
Write-Host ""
if ($script:CHECKS_FAIL -eq 0) {
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║   TODAS LAS VERIFICACIONES PASARON                  ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Green
} else {
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║   ALGUNAS VERIFICACIONES FALLARON                   ║" -ForegroundColor Red
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Red
}
Write-Host "  Exitosas : $($script:CHECKS_OK)" -ForegroundColor Green
if ($script:CHECKS_FAIL -gt 0) {
    Write-Host "  Fallidas : $($script:CHECKS_FAIL)" -ForegroundColor Red
}

Add-Content $OUTPUT_FILE ""
Add-Content $OUTPUT_FILE "# ── RESUMEN ──────────────────────────────────────────────────"
Add-Content $OUTPUT_FILE "  Verificaciones exitosas : $($script:CHECKS_OK)"
Add-Content $OUTPUT_FILE "  Verificaciones fallidas : $($script:CHECKS_FAIL)"

Write-Host ""
Write-Host "  Reporte guardado en: $OUTPUT_FILE" -ForegroundColor DarkGray
Write-Host ""

exit $script:CHECKS_FAIL
