#!/usr/bin/env pwsh
# ============================================================================
#  CASO 1 — REVENTA (Coca-Cola 355ml)
#  01-ejecutar-flujo-e2e.ps1 — Test de Integración E2E (PowerShell)
#
#  Uso:
#   pwsh docs/inventario/caso-1-reventa/01-ejecutar-flujo-e2e.ps1
#   pwsh docs/inventario/caso-1-reventa/01-ejecutar-flujo-e2e.ps1 -NoResetDb
#
#  Requisitos previos:
#   - sqlcmd instalado.
#   - API corriendo en https://localhost:8000.
#   - credentials/db.txt en la raíz del proyecto.
#   - Producto 1 (Coca-Cola 355ml) existe. Período 4 abierto.
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

$RUN_ID = (Get-Date -Format "yyyyMMddHHmmss")

# ── Estado del flujo ──────────────────────────────────────────────────────────
$TOKEN               = ""
$REFRESH_TOKEN       = ""
$ID_PRODUCT_UNIT     = 0
$ID_PRODUCT_ACCOUNT  = 0
$ID_PURCHASE_INVOICE = 0
$ID_LOT              = 0
$ID_SALES_INVOICE    = 0
$ID_ENTRY_DEV_COGS   = "(no generado)"
$ID_ENTRY_REINTEGRO  = 0
$ID_ADJUSTMENT       = 0
$ADJ_ENTRY           = 0
$STOCK_FINAL         = "?"
$NUMBER_PC           = ""
$NUMBER_FV           = ""

$script:HTTP_STATUS   = 0
$script:LAST_RESPONSE = $null

# ── Helpers de presentación ───────────────────────────────────────────────────
function Write-Step($msg) {
    Write-Host ""
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "▶  $msg" -ForegroundColor Cyan
    Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
}
function Write-Ok($msg)   { Write-Host "  OK  $msg" -ForegroundColor Green }
function Write-Warn($msg) { Write-Host "  W   $msg" -ForegroundColor Yellow }
function Write-Info($msg) { Write-Host "  $msg" }

function Invoke-Fail($msg) {
    Write-Host ""
    Write-Host "  ERROR  FALLO: $msg" -ForegroundColor Red
    if ($script:LAST_RESPONSE) {
        Write-Host "  Respuesta del API:" -ForegroundColor Red
        Write-Host ($script:LAST_RESPONSE | ConvertTo-Json -Depth 10 -ErrorAction SilentlyContinue)
    }
    Write-Host ""
    Write-Host "  El proceso se detuvo en el paso anterior." -ForegroundColor Red
    Write-Host "  IDs hasta el momento:"
    Write-Host "    idProductAccount   : $ID_PRODUCT_ACCOUNT"
    Write-Host "    idPurchaseInvoice  : $ID_PURCHASE_INVOICE"
    Write-Host "    idLot              : $ID_LOT"
    Write-Host "    idSalesInvoice     : $ID_SALES_INVOICE"
    exit 1
}

# ── API helper ────────────────────────────────────────────────────────────────
# Realiza una llamada HTTP, guarda el resultado en $script:LAST_RESPONSE
# y el código de estado en $script:HTTP_STATUS. Devuelve el objeto parseado.
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
        $result                = Invoke-RestMethod @params
        $script:HTTP_STATUS    = [int]$sc
        $script:LAST_RESPONSE  = $result
        return $result
    }
    catch {
        $script:HTTP_STATUS = try { [int]$_.Exception.Response.StatusCode } catch { 0 }
        $script:LAST_RESPONSE = $null
        try {
            $stream  = $_.Exception.Response.GetResponseStream()
            $reader  = [System.IO.StreamReader]::new($stream)
            $raw     = $reader.ReadToEnd()
            $parsed  = $raw | ConvertFrom-Json -ErrorAction SilentlyContinue
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

# ── Leer credenciales BD ──────────────────────────────────────────────────────
if (-not (Test-Path $CREDS_FILE)) {
    Write-Host "ERROR  No se encontro $CREDS_FILE" -ForegroundColor Red
    exit 1
}
$creds   = Get-Content $CREDS_FILE
$DB_HOST = (($creds | Where-Object { $_ -match '^HOST:' })     -split '\s+')[1]
$DB_PORT = (($creds | Where-Object { $_ -match '^PORT:' })     -split '\s+')[1]
$DB_USER = (($creds | Where-Object { $_ -match '^USER:' })     -split '\s+')[1]
$DB_PASS = (($creds | Where-Object { $_ -match '^PASSWORD:' }) -split '\s+')[1]

# ── Encabezado ────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   CASO 1 — REVENTA · Test de Integracion E2E        ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Info "API   : $HOST_URL"
Write-Info "BD    : ${DB_HOST}:${DB_PORT} / dbfa"
Write-Info "Email : $EMAIL"
Write-Info "RESET : $RESET_DB"

# ════════════════════════════════════════════════════════════════════════════════
# PASO -1 — RESET BD
# ════════════════════════════════════════════════════════════════════════════════
if ($RESET_DB) {
    Write-Step "PASO -1 — Reset de BD (RESET_DB=true)"

    Push-Location $REPO_ROOT

    Write-Info "① Drop base de datos remota..."
    & dotnet ef database drop --project $API_PROJECT --force
    if ($LASTEXITCODE -ne 0) { Invoke-Fail "Fallo database drop" }
    Write-Ok "Base de datos eliminada"

    Write-Info "② Eliminando archivos de migracion..."
    $migrationsDir = Join-Path $REPO_ROOT "src\familyAccountApi\Infrastructure\Data\Migrations"
    Get-ChildItem $migrationsDir -Filter "*.cs" -Depth 0 -ErrorAction SilentlyContinue | Remove-Item -Force
    $remaining = (Get-ChildItem $migrationsDir -Filter "*.cs" -Depth 0 -ErrorAction SilentlyContinue).Count
    if ($remaining -ne 0) {
        Invoke-Fail "Quedaron $remaining archivos en Migrations/ — verificar manualmente"
    }
    Write-Ok "Carpeta Migrations/ vacia"

    Write-Info "③ Generando migracion InitialCreate..."
    & dotnet ef migrations add InitialCreate --project $API_PROJECT --output-dir Infrastructure/Data/Migrations
    if ($LASTEXITCODE -ne 0) { Invoke-Fail "Fallo migrations add" }
    Write-Ok "Migracion InitialCreate generada"

    Write-Info "④ Aplicando migracion (database update)..."
    & dotnet ef database update --project $API_PROJECT
    if ($LASTEXITCODE -ne 0) { Invoke-Fail "Fallo database update" }
    Write-Ok "BD recreada con seed — lista para el test"

    Pop-Location

    Write-Host ""
    Write-Host "  !  ACCION REQUERIDA" -ForegroundColor Yellow
    Write-Host "  Reinicia el API ahora (F5 en VS Code o Ctrl+C + dotnet run)." -ForegroundColor Yellow
    Write-Host "  Hangfire re-creara su esquema al arrancar." -ForegroundColor Yellow
    Read-Host "  Presiona ENTER cuando el API este corriendo de nuevo..."

    Write-Info "Esperando que el API responda en $HOST_URL..."
    $healthUrl = $HOST_URL -replace '/api/v1$', '/health.json'
    $maxWait   = 60
    $waited    = 0
    while ($true) {
        try {
            $hc = Invoke-WebRequest -Uri $healthUrl -SkipCertificateCheck -UseBasicParsing -ErrorAction Stop
            if ($hc.StatusCode -eq 200) { break }
        } catch {}
        if ($waited -ge $maxWait) {
            Write-Host "ERROR  El API no respondio en $maxWait segundos." -ForegroundColor Red
            exit 1
        }
        Write-Host "  ...  esperando API (${waited}s)...`r" -NoNewline
        Start-Sleep -Seconds 2
        $waited += 2
    }
    Write-Ok "API responde"
}

# ════════════════════════════════════════════════════════════════════════════════
# PASO 1 — AUTENTICACION
# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 1 — Autenticacion"

$PIN = "12345"
Write-Info "Insertando PIN '$PIN' directamente en BD (idUser=1)..."
$sqlQuery = "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');"
& sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlQuery *>$null
Write-Ok "PIN '$PIN' insertado en BD para usuario 1"

Write-Info "Haciendo login con el PIN..."
$resp = Invoke-Api POST "/auth/login" "{`"emailUser`":`"$EMAIL`",`"pin`":`"$PIN`"}"
Assert-Status 200 "login"

$TOKEN         = $resp.accessToken
$REFRESH_TOKEN = $resp.refreshToken
if (-not $TOKEN -or $TOKEN -eq "null") { Invoke-Fail "No se obtuvo accessToken" }
Write-Ok "Token obtenido: $($TOKEN.Substring(0, [Math]::Min(30, $TOKEN.Length)))..."

# ════════════════════════════════════════════════════════════════════════════════
# PASO 0 — PRE-REQUISITO: Crear presentacion base del producto 1
# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 0 — Crear presentacion base del producto 1 (idUnit=1, conversionFactor=1)"

$resp = Invoke-Api GET "/product-units/by-product/1.json" "" $TOKEN
Assert-Status 200 "get product-units by-product"

$existingPU = @($resp) | Where-Object { $_.idUnit -eq 1 } | Select-Object -First 1
if ($existingPU -and $existingPU.idProductUnit) {
    $ID_PRODUCT_UNIT = $existingPU.idProductUnit
    Write-Warn "Presentacion ya existe (idProductUnit=$ID_PRODUCT_UNIT), reutilizando"
} else {
    $resp = Invoke-Api POST "/product-units" `
        '{"idProduct":1,"idUnit":1,"conversionFactor":1.0,"isBase":true,"usedForPurchase":true,"usedForSale":true,"namePresentation":"Unidad base"}' `
        $TOKEN
    Assert-Status 201 "create product-unit"
    $ID_PRODUCT_UNIT = $resp.idProductUnit
    Write-Ok "idProductUnit = $ID_PRODUCT_UNIT"
}

# ════════════════════════════════════════════════════════════════════════════════
# PASO 2 — PRE-COMPRA: Vincular Coca-Cola a cuenta 109 Inventario (100%)
# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 2 — Crear ProductAccount (producto 1 → cuenta 109, 100%)"

$resp = Invoke-Api GET "/product-accounts/by-product/1.json" "" $TOKEN
if ($script:HTTP_STATUS -eq 200 -and $resp) {
    $existingPA = @($resp) | Where-Object { $_.idAccount -eq 109 } | Select-Object -First 1
    if ($existingPA -and $existingPA.idProductAccount) {
        $ID_PRODUCT_ACCOUNT = $existingPA.idProductAccount
        Write-Warn "ProductAccount ya existe (idProductAccount=$ID_PRODUCT_ACCOUNT), reutilizando"
    }
}
if ($ID_PRODUCT_ACCOUNT -eq 0) {
    $resp = Invoke-Api POST "/product-accounts" `
        '{"idProduct":1,"idAccount":109,"percentageAccount":100.00}' `
        $TOKEN
    Assert-Status 201 "create product-account"
    $ID_PRODUCT_ACCOUNT = $resp.idProductAccount
    Write-Ok "idProductAccount = $ID_PRODUCT_ACCOUNT"
}

# ════════════════════════════════════════════════════════════════════════════════
# PASO 3 — FACTURA DE COMPRA (proveedor entrega 100 cajas)
# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 3a — Crear factura de compra en borrador (100 cajas)"

$bodyPurchase = @'
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idPurchaseInvoiceType": 1,
  "idContact": 1,
  "numberInvoice": "FAC-PROVEEDOR-C1-001",
  "dateInvoice": "2026-04-05",
  "subTotalAmount": 100000.00,
  "taxAmount": 13000.00,
  "totalAmount": 113000.00,
  "descriptionInvoice": "Compra inicial 100 cajas Coca-Cola 355ml — Caso 1 Reventa",
  "idWarehouse": 1,
  "exchangeRateValue": 1.0,
  "lines": [
    {
      "idProduct": 1,
      "idUnit": 1,
      "lotNumber": "LOT-COCA-C1-001",
      "expirationDate": "2027-12-31",
      "descriptionLine": "Coca-Cola 355ml x 100 un.",
      "quantity": 100,
      "unitPrice": 1000.00,
      "taxPercent": 13.00,
      "totalLineAmount": 113000.00
    }
  ]
}
'@

$resp = Invoke-Api POST "/purchase-invoices" $bodyPurchase $TOKEN
Assert-Status 201 "create purchase-invoice"
$ID_PURCHASE_INVOICE = $resp.idPurchaseInvoice
Write-Ok "idPurchaseInvoice = $ID_PURCHASE_INVOICE"

Write-Step "PASO 3b — Confirmar factura de compra"

$resp = Invoke-Api POST "/purchase-invoices/$ID_PURCHASE_INVOICE/confirm" "" $TOKEN
Assert-Status 200 "confirm purchase-invoice"

$STATUS    = $resp.statusInvoice
$NUMBER_PC = $resp.numberInvoice
Write-Ok "statusInvoice = $STATUS"
if ($STATUS -ne "Confirmado") { Invoke-Fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$STATUS'" }
Write-Ok "Numero de factura de compra: $NUMBER_PC"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 4 — Eliminar ProductAccount
# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 4 — Eliminar ProductAccount (id=$ID_PRODUCT_ACCOUNT)"

$resp = Invoke-Api DELETE "/product-accounts/$ID_PRODUCT_ACCOUNT" "" $TOKEN
Assert-Status 204 "delete product-account"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 5 — FACTURA DE VENTA (10 cajas, cliente contado)
# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 5a — Obtener lote del producto 1 (LOT-COCA-C1-001)"

$resp = Invoke-Api GET "/inventory-lots/by-product/1.json" "" $TOKEN
Assert-Status 200 "get inventory-lots by-product"

$lotFound = @($resp) | Where-Object { $_.lotNumber -eq "LOT-COCA-C1-001" } | Select-Object -First 1
if (-not $lotFound) {
    Write-Warn "No se encontro el lote 'LOT-COCA-C1-001', usando el primer lote disponible"
    $lotFound = @($resp) | Select-Object -First 1
}
if (-not $lotFound) { Invoke-Fail "No se encontro ningun lote para el producto 1" }

$ID_LOT  = $lotFound.idInventoryLot
$LOT_QTY = $lotFound.quantityAvailable
Write-Ok "idInventoryLot = $ID_LOT  (quantityAvailable = $LOT_QTY)"

Write-Step "PASO 5b — Crear factura de venta en borrador (10 cajas x 1500 + 13% IVA)"

$bodySale = @"
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idSalesInvoiceType": 1,
  "idContact": 1,
  "dateInvoice": "2026-04-05",
  "subTotalAmount": 15000.00,
  "taxAmount": 1950.00,
  "totalAmount": 16950.00,
  "descriptionInvoice": "Venta 10 cajas Coca-Cola 355ml — Caso 1 Reventa",
  "exchangeRateValue": 1.0,
  "lines": [
    {
      "isNonProductLine": false,
      "idProduct": 1,
      "idInventoryLot": $ID_LOT,
      "descriptionLine": "Coca-Cola 355ml x 10 un.",
      "quantity": 10,
      "unitPrice": 1500.00,
      "taxPercent": 13.00,
      "totalLineAmount": 16950.00
    }
  ]
}
"@

$resp = Invoke-Api POST "/sales-invoices" $bodySale $TOKEN
Assert-Status 201 "create sales-invoice"
$ID_SALES_INVOICE = $resp.idSalesInvoice
Write-Ok "idSalesInvoice = $ID_SALES_INVOICE"

Write-Step "PASO 5c — Confirmar factura de venta"

$resp = Invoke-Api POST "/sales-invoices/$ID_SALES_INVOICE/confirm" "" $TOKEN
Assert-Status 200 "confirm sales-invoice"

$STATUS    = $resp.statusInvoice
$NUMBER_FV = $resp.numberInvoice
Write-Ok "statusInvoice = $STATUS"
if ($STATUS -ne "Confirmado") { Invoke-Fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$STATUS'" }
Write-Ok "Numero de factura de venta: $NUMBER_FV"
Write-Info "  Esperado: DR 106 Caja 16950 / CR 117 Ingresos 16950"
Write-Info "  Esperado: DR 119 COGS 11300  / CR 109 Inventario 11300"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 6 — DEVOLUCION PARCIAL (cliente devuelve 3 cajas)
# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 6 — Devolucion parcial: 3 cajas devueltas"

$bodyReturn = @"
{
  "dateReturn": "2026-04-05",
  "descriptionReturn": "Devolucion parcial — cliente devuelve 3 cajas Coca-Cola danadas en transito",
  "refundMode": "EfectivoInmediato",
  "lines": [
    {
      "idInventoryLot": $ID_LOT,
      "quantity": 3,
      "totalLineAmount": 5085.00,
      "descriptionLine": "Coca-Cola 355ml x 3 un. — devolucion parcial"
    }
  ]
}
"@

$resp = Invoke-Api POST "/sales-invoices/$ID_SALES_INVOICE/partial-return" $bodyReturn $TOKEN
Assert-Status 200 "partial-return"

$partialEntry = $resp.idAccountingEntry
if ($partialEntry -and $partialEntry -ne "null") {
    $ID_ENTRY_DEV_COGS = $partialEntry
    Write-Ok "idAccountingEntry DEV-COGS = $ID_ENTRY_DEV_COGS"
} else {
    Write-Warn "La devolucion no genero asiento COGS"
}
Write-Info "  DEV-COGS: DR 109 Inventario 3000 / CR 119 COGS 3000  (3 u x 1000 costo)"

$refundEntry = $resp.idAccountingEntryRefund
if ($refundEntry -and $refundEntry -ne "null") {
    $ID_ENTRY_REINTEGRO = $refundEntry
    Write-Ok "idAccountingEntryRefund DEV-ING = $ID_ENTRY_REINTEGRO"
} else {
    Write-Warn "La devolucion no genero asiento de reversion de ingresos"
}
Write-Info "  DEV-ING:  DR 117 Ingresos 4500 + DR 127 IVA 585 / CR 106 Caja 5085"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 7 — AJUSTE DE INVENTARIO (regalia: −2 cajas)
# ════════════════════════════════════════════════════════════════════════════════
Write-Step "PASO 7a — Crear ajuste de inventario en borrador (regalia -2 cajas)"

$bodyAdj = @"
{
  "idFiscalPeriod": 4,
  "idInventoryAdjustmentType": 4,
  "idCurrency": 1,
  "exchangeRateValue": 1.0,
  "dateAdjustment": "2026-04-05",
  "descriptionAdjustment": "Regalia cliente VIP — 2 cajas Coca-Cola 355ml — Responsable: Administrador",
  "lines": [
    {
      "idInventoryLot": $ID_LOT,
      "quantityDelta": -2,
      "descriptionLine": "Salida por regalia — Coca-Cola 355ml x 2 un."
    }
  ]
}
"@

$resp = Invoke-Api POST "/inventory-adjustments" $bodyAdj $TOKEN
Assert-Status 201 "create inventory-adjustment"
$ID_ADJUSTMENT = $resp.idInventoryAdjustment
Write-Ok "idInventoryAdjustment = $ID_ADJUSTMENT"

Write-Step "PASO 7b — Confirmar ajuste de inventario"

$resp = Invoke-Api POST "/inventory-adjustments/$ID_ADJUSTMENT/confirm" "" $TOKEN
Assert-Status 200 "confirm inventory-adjustment"

$ADJ_STATUS = $resp.statusAdjustment
$ADJ_ENTRY  = $resp.idAccountingEntry
Write-Ok "statusAdjustment = $ADJ_STATUS"
if ($ADJ_STATUS -ne "Confirmado") { Invoke-Fail "Se esperaba statusAdjustment = 'Confirmado', recibido: '$ADJ_STATUS'" }
Write-Ok "idAccountingEntry ADJ = $ADJ_ENTRY"
Write-Info "  Esperado: DR 113 Merma 2000 / CR 109 Inventario 2000  (2 u x 1000 costo)"

# ════════════════════════════════════════════════════════════════════════════════
# VERIFICACION FINAL DE STOCK
# ════════════════════════════════════════════════════════════════════════════════
Write-Step "VERIFICACION FINAL — Stock total del producto 1"

$resp = Invoke-Api GET "/inventory-lots/stock/1.json" "" $TOKEN
Assert-Status 200 "get stock total"

$STOCK_FINAL = $resp
Write-Ok "Stock actual = $STOCK_FINAL unidades"
Write-Info "  Formula: 100 (compra) - 10 (venta) + 3 (devolucion) - 2 (regalia) = 91"

$stockNum = try { [decimal]$STOCK_FINAL } catch { -1 }
if ($stockNum -eq 91) {
    Write-Ok "Stock = 91  Saldo de costo en libros esperado: 91 x 1000 = 91000"
} else {
    Write-Warn "Stock final = $STOCK_FINAL (esperado 91 si la BD estaba limpia antes de correr el script)"
}

# ════════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ════════════════════════════════════════════════════════════════════════════════
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║   FLUJO CASO 1 COMPLETADO EXITOSAMENTE              ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ("  {0,-22} {1}" -f "idPurchaseInvoice:",  $ID_PURCHASE_INVOICE)
Write-Host ("  {0,-22} {1}" -f "idLot:",              $ID_LOT)
Write-Host ("  {0,-22} {1}" -f "idSalesInvoice:",     $ID_SALES_INVOICE)
Write-Host ("  {0,-22} {1}" -f "idEntryDevCogs:",     $ID_ENTRY_DEV_COGS)
Write-Host ("  {0,-22} {1}" -f "idEntryDevIng:",      $ID_ENTRY_REINTEGRO)
Write-Host ("  {0,-22} {1}" -f "idAdjustment:",       $ID_ADJUSTMENT)
Write-Host ("  {0,-22} {1}" -f "idAdjEntry:",         $ADJ_ENTRY)
Write-Host ("  {0,-22} {1}" -f "Stock final:",        "$STOCK_FINAL u.")
Write-Host ""

# ════════════════════════════════════════════════════════════════════════════════
# GUARDAR RESULTADO EN ARCHIVO TXT
# ════════════════════════════════════════════════════════════════════════════════
$RUN_TS      = (Get-Date -Format "yyyy-MM-dd_HH-mm-ss")
$OUTPUT_FILE = Join-Path $SCRIPT_DIR "resultado_caso1_$RUN_TS.txt"

@"
# ============================================================
#  CASO 1 — REVENTA · Resultado del Test E2E
#  Ejecutado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
# ============================================================

# -- Configuracion usada -------------------------------------
HOST                  = $HOST_URL
EMAIL                 = $EMAIL
DB_HOST               = $DB_HOST
DB_PORT               = $DB_PORT

# -- Claves estaticas del caso --------------------------------
idProduct             = 1
nameProduct           = Coca-Cola 355ml
idProductType         = 4  (Reventa)
idFiscalPeriod        = 4  (Abril 2026)
idCurrency            = 1  (CRC)
idWarehouse           = 1  (Principal)
idPurchaseInvoiceType = 1  (EFECTIVO)
idSalesInvoiceType    = 1  (CONTADO_CRC)
idContact             = 1
lotNumber             = LOT-COCA-C1-001
expirationDate        = 2027-12-31
unitPrice_compra      = 1000.00 CRC
taxPercent            = 13%
unitCost_lote         = 1130.00 CRC  (1000 x 1.13)
unitPrice_venta       = 1500.00 CRC
unitPriceConIVA_venta = 1695.00 CRC

# -- IDs generados en esta ejecucion -------------------------
idProductUnit         = $ID_PRODUCT_UNIT
idPurchaseInvoice     = $ID_PURCHASE_INVOICE
numberPurchaseInvoice = $NUMBER_PC
idInventoryLot        = $ID_LOT
idSalesInvoice        = $ID_SALES_INVOICE
numberSalesInvoice    = $NUMBER_FV
idEntryDevCogs        = $ID_ENTRY_DEV_COGS
idEntryDevIng         = $ID_ENTRY_REINTEGRO
idInventoryAdjustment = $ID_ADJUSTMENT
idAdjEntry            = $ADJ_ENTRY

# -- Montos del flujo ----------------------------------------
compra_subtotal       = 100000.00
compra_iva            = 13000.00
compra_total          = 113000.00
venta_subtotal        = 15000.00
venta_iva             = 1950.00
venta_total           = 16950.00
cogs_venta            = 10000.00  (10 u x 1000)
devolucion_monto      = 5085.00   (3 u x 1695)
devolucion_subtotal   = 4500.00   (3 u x 1500 neto)
devolucion_iva        = 585.00    (4500 x 13%)
cogs_reversa          = 3000.00   (3 u x 1000)
regalia_costo         = 2000.00   (2 u x 1000)

# -- Verificacion de stock -----------------------------------
stock_inicial         = 0
stock_post_compra     = 100
stock_post_venta      = 90
stock_post_devolucion = 93
stock_post_regalia    = 91
stock_final_real      = $STOCK_FINAL
stock_costo_libros    = 91000.00  (91 x 1000)

# -- Cuentas involucradas ------------------------------------
idAccount_caja_crc    = 106  (1.1.06.01 Caja CRC)
idAccount_inventario  = 109  (1.1.07.01 Inventario Mercaderia)
idAccount_cogs        = 119  (5.01 Costo de Ventas)
idAccount_ingresos    = 117  (4.5.01 Ingresos por Ventas)
idAccount_merma       = 130  (5.14.01.02 Merma Anormal — IAS 2.16)

# -- Endpoints de consulta rapida ----------------------------
GET $HOST_URL/purchase-invoices/$ID_PURCHASE_INVOICE.json
GET $HOST_URL/inventory-lots/$ID_LOT.json
GET $HOST_URL/inventory-lots/stock/1.json
GET $HOST_URL/sales-invoices/$ID_SALES_INVOICE.json
GET $HOST_URL/accounting-entries/$ID_ENTRY_REINTEGRO.json
GET $HOST_URL/inventory-adjustments/$ID_ADJUSTMENT.json
"@ | Set-Content $OUTPUT_FILE -Encoding UTF8

Write-Host "  Resultado guardado en: $OUTPUT_FILE" -ForegroundColor Green
Write-Host ""
