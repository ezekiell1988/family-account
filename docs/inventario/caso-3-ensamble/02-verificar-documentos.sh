#!/usr/bin/env bash
# ============================================================================
#  CASO 3 — ENSAMBLE EN VENTA (Hot Dog)
#  02-verificar-documentos.sh — Verificación de Inventarios y Documentos
#
#  Propósito:
#   Descubre automáticamente los IDs de los documentos del Caso 3 consultando
#   los asientos contables y la API, sin depender de resultado_caso3_*.txt.
#   Verifica que todos los inventarios sean correctos tras el flujo completo:
#     - Factura de compra de ingredientes (50 hot dogs)
#     - Pedido de venta (3 hot dogs) → ciclo automático: OP + FV confirmada
#     - Devolución parcial (1 hot dog devuelto → lote PT +1)
#     - Regalía (2 hot dogs → lote PT −2)
#   Genera verificacion_docs_caso3_*.txt con el reporte completo.
#
#  Inventario esperado al final del flujo:
#   Pan de Hot Dog  (id=7)  : 50 − 3 =  47 UNI  (consumidos en OP)
#   Salchicha       (id=8)  : 50 − 3 =  47 UNI  (consumidos en OP)
#   Mostaza         (id=9)  : 750 − 45 = 705 ML  (consumidos en OP: 3×15ml)
#   Catsup          (id=10) : 1000 − 60 = 940 ML  (consumidos en OP: 3×20ml)
#   Hot Dog PT      (id=11) : 3 − 3 + 1 − 2 = −1 UNI
#
#  Requisitos previos:
#   - curl, jq y sqlcmd instalados.
#   - API corriendo en https://localhost:8000.
#   - Credenciales en credentials/db.txt.
#   - Flujo del caso 3 ejecutado con 01-ejecutar-flujo-e2e.sh.
#
#  Uso:
#   bash docs/inventario/caso-3-ensamble/02-verificar-documentos.sh
# ============================================================================

set -uo pipefail

# ── Rutas ─────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TEMP_RESPONSE="/tmp/fa_caso3_consultas.json"
HOST="https://localhost:8000/api/v1"

# ── Colores ───────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
DIM='\033[2m'
NC='\033[0m'

# ── Contadores de verificaciones ──────────────────────────────────────────────
CHECKS_OK=0
CHECKS_FAIL=0

# ── Archivo de reporte ────────────────────────────────────────────────────────
RUN_TS="$(date '+%Y-%m-%d_%H-%M-%S')"
OUTPUT_FILE="$SCRIPT_DIR/verificacion_docs_caso3_${RUN_TS}.txt"

# ── Helpers ───────────────────────────────────────────────────────────────────

section() {
  echo ""
  printf "${CYAN}${BOLD}── %s ${NC}\n" "$1"
  echo "" >> "$OUTPUT_FILE"
  echo "── $1" >> "$OUTPUT_FILE"
}

log_ok()   { printf "  ${GREEN}✅  %s${NC}\n" "$1"; CHECKS_OK=$((CHECKS_OK + 1));   echo "  [OK]   $1" >> "$OUTPUT_FILE"; }
log_fail() { printf "  ${RED}❌  %s${NC}\n" "$1";   CHECKS_FAIL=$((CHECKS_FAIL + 1)); echo "  [FAIL] $1" >> "$OUTPUT_FILE"; }
log_info() { printf "  ${DIM}%s${NC}\n" "$1";                                         echo "         $1" >> "$OUTPUT_FILE"; }
log_warn() { printf "  ${YELLOW}⚠   %s${NC}\n" "$1";                                  echo "  [WARN] $1" >> "$OUTPUT_FILE"; }

# GET al API con token. Guarda body en $TEMP_RESPONSE, status en $HTTP_STATUS.
HTTP_STATUS=""
api_get() {
  local path="$1"
  HTTP_STATUS=$(curl -k -s -o "$TEMP_RESPONSE" -w "%{http_code}" \
    -H "Authorization: Bearer $TOKEN" \
    "${HOST}${path}")
}

# Extrae campo jq del último response.
jq_field() { jq -r "$1" "$TEMP_RESPONSE" 2>/dev/null; }

# Verifica que HTTP status sea 200.
assert_200() {
  local label="$1"
  if [[ "$HTTP_STATUS" == "200" ]]; then
    log_ok "$label — HTTP 200"
  else
    log_fail "$label — HTTP $HTTP_STATUS (esperado 200)"
  fi
}

# Verifica que un campo tenga el valor esperado.
assert_eq() {
  local label="$1"
  local expected="$2"
  local actual="$3"
  if [[ "$actual" == "$expected" ]]; then
    log_ok "$label = $actual"
  else
    log_fail "$label: esperado '$expected', recibido '$actual'"
  fi
}

# Verifica que un número sea mayor o igual al esperado.
assert_gte() {
  local label="$1"
  local expected="$2"
  local actual="$3"
  local result
  result=$(awk "BEGIN { print ($actual >= $expected) ? \"yes\" : \"no\" }" 2>/dev/null || echo "no")
  if [[ "$result" == "yes" ]]; then
    log_ok "$label >= $expected  (actual=$actual)"
  else
    log_fail "$label: esperado >= $expected, recibido '$actual'"
  fi
}

# Verifica que un número float sea aprox igual (±0.01).
assert_float_eq() {
  local label="$1"
  local expected="$2"
  local actual="$3"
  local result
  result=$(awk "BEGIN {
    diff = $actual - $expected
    if (diff < 0) diff = -diff
    print (diff <= 0.01) ? \"yes\" : \"no\"
  }" 2>/dev/null || echo "no")
  if [[ "$result" == "yes" ]]; then
    log_ok "$label = $actual"
  else
    log_fail "$label: esperado ≈ $expected, recibido '$actual'"
  fi
}

# ── Autenticación ─────────────────────────────────────────────────────────────
EMAIL="ezekiell1988@hotmail.com"
CREDENTIALS_FILE="$(cd "$SCRIPT_DIR/../../.." && pwd)/credentials/db.txt"
DB_HOST=$(grep -E '^HOST:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_PORT=$(grep -E '^PORT:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_USER=$(grep -E '^USER:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_PASS=$(grep -E '^PASSWORD:' "$CREDENTIALS_FILE" | awk '{print $2}')

PIN="12345"
sqlcmd -S "${DB_HOST},${DB_PORT}" -U "$DB_USER" -P "$DB_PASS" -C -d dbfa \
  -Q "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '${PIN}';
      INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '${PIN}');" \
  2>/dev/null

TOKEN=$(curl -k -s -X POST "${HOST}/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"emailUser\":\"${EMAIL}\",\"pin\":\"${PIN}\"}" \
  | jq -r '.accessToken // empty')

if [[ -z "$TOKEN" || "$TOKEN" == "null" ]]; then
  printf "${RED}❌  No se pudo obtener token${NC}\n"; exit 1
fi

# ── Constantes del caso ───────────────────────────────────────────────────────
ID_WAREHOUSE="1"

# Costos de lotes de ingredientes — el API almacena el precio raw (sin IVA)
EXP_COST_PAN="300"         # precio unitario sin IVA
EXP_COST_SALCHICHA="600"   # precio unitario sin IVA
EXP_COST_MOSTAZA="20"      # precio por ML sin IVA
EXP_COST_CATSUP="15"       # precio por ML sin IVA
EXP_COST_PT="1500"         # 1×300 + 1×600 + 15×20 + 20×15 (sin IVA)

# Stock final esperado de ingredientes (50 comprados − consumidos para 3 hot dogs)
EXP_STOCK_PAN="47"         # 50 − 3
EXP_STOCK_SALCHICHA="47"   # 50 − 3
EXP_STOCK_MOSTAZA="705"    # 750 − 45 (3 × 15ml)
EXP_STOCK_CATSUP="940"     # 1000 − 60 (3 × 20ml)
EXP_STOCK_PT="-1"          # 3 producidos − 3 venta + 1 dev − 2 regalía

# ── Descubrir IDs desde asientos contables ────────────────────────────────────
TEMP_ENTRIES="/tmp/fa_caso3_entries_disco.json"
curl -k -s -H "Authorization: Bearer $TOKEN" "${HOST}/accounting-entries/data.json" > "$TEMP_ENTRIES"

ID_PURCHASE_INVOICE=$(jq -r '[.[] | select(.numberEntry | startswith("FC-"))] | sort_by(.idAccountingEntry) | last | .idOriginRecord // empty' "$TEMP_ENTRIES")
ID_SALES_INVOICE=$(jq -r '[.[] | select(.numberEntry | startswith("FV-"))] | sort_by(.idAccountingEntry) | last | .idOriginRecord // empty' "$TEMP_ENTRIES")
ID_ADJUSTMENT=$(jq -r '[.[] | select(.numberEntry | startswith("AJ-"))] | sort_by(.idAccountingEntry) | last | .idOriginRecord // empty' "$TEMP_ENTRIES")
rm -f "$TEMP_ENTRIES"

# Lote Pan (idProduct=7) — vinculado a la FC de compra
_tmp_lot="/tmp/fa_caso3_lot_disco.json"
curl -k -s -H "Authorization: Bearer $TOKEN" \
  "${HOST}/inventory-lots/by-product/7.json?idWarehouse=${ID_WAREHOUSE}" > "$_tmp_lot"
ID_LOT_PAN=$(jq -r "[.[] | select(.idPurchaseInvoice == ${ID_PURCHASE_INVOICE})] | sort_by(.idInventoryLot) | last | .idInventoryLot // empty" "$_tmp_lot" 2>/dev/null)
[[ -z "$ID_LOT_PAN" || "$ID_LOT_PAN" == "null" ]] && ID_LOT_PAN=$(jq -r '.[0].idInventoryLot // empty' "$_tmp_lot")

# Lote Salchicha (idProduct=8)
curl -k -s -H "Authorization: Bearer $TOKEN" \
  "${HOST}/inventory-lots/by-product/8.json?idWarehouse=${ID_WAREHOUSE}" > "$_tmp_lot"
ID_LOT_SALCHICHA=$(jq -r "[.[] | select(.idPurchaseInvoice == ${ID_PURCHASE_INVOICE})] | sort_by(.idInventoryLot) | last | .idInventoryLot // empty" "$_tmp_lot" 2>/dev/null)
[[ -z "$ID_LOT_SALCHICHA" || "$ID_LOT_SALCHICHA" == "null" ]] && ID_LOT_SALCHICHA=$(jq -r '.[0].idInventoryLot // empty' "$_tmp_lot")

# Lote Mostaza (idProduct=9)
curl -k -s -H "Authorization: Bearer $TOKEN" \
  "${HOST}/inventory-lots/by-product/9.json?idWarehouse=${ID_WAREHOUSE}" > "$_tmp_lot"
ID_LOT_MOSTAZA=$(jq -r "[.[] | select(.idPurchaseInvoice == ${ID_PURCHASE_INVOICE})] | sort_by(.idInventoryLot) | last | .idInventoryLot // empty" "$_tmp_lot" 2>/dev/null)
[[ -z "$ID_LOT_MOSTAZA" || "$ID_LOT_MOSTAZA" == "null" ]] && ID_LOT_MOSTAZA=$(jq -r '.[0].idInventoryLot // empty' "$_tmp_lot")

# Lote Catsup (idProduct=10)
curl -k -s -H "Authorization: Bearer $TOKEN" \
  "${HOST}/inventory-lots/by-product/10.json?idWarehouse=${ID_WAREHOUSE}" > "$_tmp_lot"
ID_LOT_CATSUP=$(jq -r "[.[] | select(.idPurchaseInvoice == ${ID_PURCHASE_INVOICE})] | sort_by(.idInventoryLot) | last | .idInventoryLot // empty" "$_tmp_lot" 2>/dev/null)
[[ -z "$ID_LOT_CATSUP" || "$ID_LOT_CATSUP" == "null" ]] && ID_LOT_CATSUP=$(jq -r '.[0].idInventoryLot // empty' "$_tmp_lot")

# Lote PT Hot Dog (idProduct=11) — el más reciente, generado por la OP automática
curl -k -s -H "Authorization: Bearer $TOKEN" \
  "${HOST}/inventory-lots/by-product/11.json?idWarehouse=${ID_WAREHOUSE}" > "$_tmp_lot"
ID_LOT_PT=$(jq -r 'sort_by(.idInventoryLot) | last | .idInventoryLot // empty' "$_tmp_lot" 2>/dev/null)
rm -f "$_tmp_lot"

# Sales Invoice → Sales Order
_tmp_fv="/tmp/fa_caso3_fv_disco.json"
curl -k -s -H "Authorization: Bearer $TOKEN" \
  "${HOST}/sales-invoices/${ID_SALES_INVOICE}.json" > "$_tmp_fv"
ID_SALES_ORDER=$(jq -r '.idSalesOrder // empty' "$_tmp_fv" 2>/dev/null)
rm -f "$_tmp_fv"

ID_FISCAL_PERIOD="4"

# ── Cabecera ──────────────────────────────────────────────────────────────────
printf "${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
printf "${BOLD}║   CASO 3 — ENSAMBLE · Verificación de Inventarios   ║${NC}\n"
printf "${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"

{
  echo "# =================================================================="
  echo "# CASO 3 — ENSAMBLE EN VENTA · Verificación de Inventarios"
  echo "# Ejecutado: $(date '+%Y-%m-%d %H:%M:%S')"
  echo "# =================================================================="
} > "$OUTPUT_FILE"

printf "\n"
printf "  ${DIM}IDs descubiertos:${NC}\n"
printf "  ${DIM}  PC=%s  FV=%s  SO=%s  ADJ=%s${NC}\n" \
  "$ID_PURCHASE_INVOICE" "$ID_SALES_INVOICE" "${ID_SALES_ORDER:-(no encontrado)}" "$ID_ADJUSTMENT"
printf "  ${DIM}  LotPan=%s  LotSalchicha=%s  LotMostaza=%s  LotCatsup=%s  LotPT=%s${NC}\n" \
  "$ID_LOT_PAN" "$ID_LOT_SALCHICHA" "$ID_LOT_MOSTAZA" "$ID_LOT_CATSUP" "$ID_LOT_PT"

# ═════════════════════════════════════════════════════════════════════════════
# SECCIÓN 1 — CATÁLOGO (seed)
# ═════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 1 — Catálogo (configuración seed)"

# 1-a  Productos ingredientes (tipo=1, TrackInventory=true)
for id_prod in 7 8 9 10; do
  api_get "/products/${id_prod}.json"
  assert_200 "GET /products/${id_prod}.json"
  PROD_ID=$(jq_field '.idProduct')
  PROD_TYPE=$(jq_field '.idProductType')
  assert_eq "product[${id_prod}].idProductType = 1 (Materia Prima)" "1" "$PROD_TYPE"
done

# 1-b  Producto ensamblado (tipo=3, TrackInventory=true)
api_get "/products/11.json"
assert_200 "GET /products/11.json"
PROD_TYPE_PT=$(jq_field '.idProductType')
assert_eq "product[11].idProductType = 3 (Producto Terminado)" "3" "$PROD_TYPE_PT"

# 1-c  Receta activa del Hot Dog (idProductRecipe=2)
api_get "/product-recipes/by-output/11.json"
assert_200 "GET /product-recipes/by-output/11.json"
RECIPE_ACTIVE=$(jq_field '[.[] | select(.isActive == true)] | length')
assert_gte "receta activa para producto 11 existe" "1" "$RECIPE_ACTIVE"
RECIPE_ID=$(jq_field '[.[] | select(.isActive == true)] | first | .idProductRecipe')
RECIPE_QTY=$(jq_field '[.[] | select(.isActive == true)] | first | .quantityOutput')
log_info "  Receta id=$RECIPE_ID  quantityOutput=$RECIPE_QTY"

# 1-d  Tipo de factura de compra (id=1 debe existir)
api_get "/purchase-invoice-types/data.json"
assert_200 "GET /purchase-invoice-types/data.json"
COUNT=$(jq_field '[.[] | select(.idPurchaseInvoiceType == 1)] | length')
assert_eq "purchase-invoice-type id=1 existe" "1" "$COUNT"

# 1-e  Tipo de factura de venta (id=1 debe existir)
api_get "/sales-invoice-types/data.json"
assert_200 "GET /sales-invoice-types/data.json"
COUNT=$(jq_field '[.[] | select(.idSalesInvoiceType == 1)] | length')
assert_eq "sales-invoice-type id=1 existe" "1" "$COUNT"

# 1-f  Tipo de ajuste (id=1 debe existir)
api_get "/inventory-adjustment-types/data.json"
assert_200 "GET /inventory-adjustment-types/data.json"
COUNT=$(jq_field '[.[] | select(.idInventoryAdjustmentType == 1)] | length')
assert_eq "inventory-adjustment-type id=1 existe" "1" "$COUNT"

# 1-g  ProductAccounts de ingredientes → deben apuntar a cuenta 110 (MP)
for id_prod in 7 8 9 10; do
  api_get "/product-accounts/by-product/${id_prod}.json"
  assert_200 "GET /product-accounts/by-product/${id_prod}.json"
  ACC_COUNT=$(jq_field '[.[] | select(.idAccount == 110)] | length')
  assert_gte "product-account[${id_prod}] → cuenta 110 existe" "1" "$ACC_COUNT"
done

# ═════════════════════════════════════════════════════════════════════════════
# SECCIÓN 2 — FACTURA DE COMPRA
# ═════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 2 — Factura de Compra (id=$ID_PURCHASE_INVOICE)"

api_get "/purchase-invoices/${ID_PURCHASE_INVOICE}.json"
assert_200 "GET /purchase-invoices/${ID_PURCHASE_INVOICE}.json"

PC_STATUS=$(jq_field '.statusInvoice')
PC_ENTRY=$(jq_field '.idAccountingEntry // .accountingEntryId // empty' | grep -v null | head -1)

assert_eq  "PC statusInvoice = Confirmado"  "Confirmado" "$PC_STATUS"
assert_gte "PC tiene idAccountingEntry"     "1"          "${PC_ENTRY:-0}"

# Debe tener 4 líneas (una por ingrediente)
PC_LINES=$(jq_field '.lines | length')
assert_eq "PC tiene 4 líneas de ingredientes" "4" "${PC_LINES:-0}"
log_info "  Subtotal esperado: ₡75,000  IVA: ₡9,750  Total: ₡84,750"

# ═════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3 — LOTES DE INGREDIENTES
# ═════════════════════════════════════════════════════════════════════════════

# 3-a  Lote Pan (id=7) — post OP: 50 − 3 = 47 UNI
section "SECCIÓN 3a — Lote Pan de Hot Dog (id=$ID_LOT_PAN) — post-OP: 47 UNI"

api_get "/inventory-lots/${ID_LOT_PAN}.json"
assert_200 "GET /inventory-lots/${ID_LOT_PAN}.json"

LOT_STATUS=$(jq_field '.statusLot')
LOT_QTY=$(jq_field '.quantityAvailable'   | grep -v null | head -1)
LOT_COST=$(jq_field '.unitCost'           | grep -v null | head -1)
LOT_SOURCE=$(jq_field '.sourceType'       | grep -v null | head -1)
LOT_PC=$(jq_field '.idPurchaseInvoice'    | grep -v null | head -1)
LOT_PROD=$(jq_field '.idProduct'          | grep -v null | head -1)

assert_eq       "lote Pan statusLot = Disponible"  "Disponible"       "$LOT_STATUS"
assert_float_eq "lote Pan quantityAvailable = $EXP_STOCK_PAN" "$EXP_STOCK_PAN" "${LOT_QTY:-0}"
assert_float_eq "lote Pan unitCost = $EXP_COST_PAN"           "$EXP_COST_PAN"  "${LOT_COST:-0}"
assert_eq       "lote Pan sourceType = Compra"     "Compra"           "$LOT_SOURCE"
assert_eq       "lote Pan idProduct = 7"           "7"                "$LOT_PROD"
assert_eq       "lote Pan idPurchaseInvoice = $ID_PURCHASE_INVOICE" \
                "$ID_PURCHASE_INVOICE" "$LOT_PC"
log_info "  50 comprados − 3 consumidos en OP = $LOT_QTY UNI"

# 3-b  Lote Salchicha (id=8) — post OP: 50 − 3 = 47 UNI
section "SECCIÓN 3b — Lote Salchicha (id=$ID_LOT_SALCHICHA) — post-OP: 47 UNI"

api_get "/inventory-lots/${ID_LOT_SALCHICHA}.json"
assert_200 "GET /inventory-lots/${ID_LOT_SALCHICHA}.json"

LOT_STATUS=$(jq_field '.statusLot')
LOT_QTY=$(jq_field '.quantityAvailable'   | grep -v null | head -1)
LOT_COST=$(jq_field '.unitCost'           | grep -v null | head -1)
LOT_SOURCE=$(jq_field '.sourceType'       | grep -v null | head -1)
LOT_PROD=$(jq_field '.idProduct'          | grep -v null | head -1)

assert_eq       "lote Salchicha statusLot = Disponible"  "Disponible"          "$LOT_STATUS"
assert_float_eq "lote Salchicha quantityAvailable = $EXP_STOCK_SALCHICHA" "$EXP_STOCK_SALCHICHA" "${LOT_QTY:-0}"
assert_float_eq "lote Salchicha unitCost = $EXP_COST_SALCHICHA"           "$EXP_COST_SALCHICHA"  "${LOT_COST:-0}"
assert_eq       "lote Salchicha sourceType = Compra"     "Compra"              "$LOT_SOURCE"
assert_eq       "lote Salchicha idProduct = 8"           "8"                    "$LOT_PROD"
log_info "  50 comprados − 3 consumidos en OP = $LOT_QTY UNI"

# 3-c  Lote Mostaza (id=9) — post OP: 750 − 45 = 705 ML
section "SECCIÓN 3c — Lote Mostaza (id=$ID_LOT_MOSTAZA) — post-OP: 705 ML"

api_get "/inventory-lots/${ID_LOT_MOSTAZA}.json"
assert_200 "GET /inventory-lots/${ID_LOT_MOSTAZA}.json"

LOT_STATUS=$(jq_field '.statusLot')
LOT_QTY=$(jq_field '.quantityAvailable'   | grep -v null | head -1)
LOT_COST=$(jq_field '.unitCost'           | grep -v null | head -1)
LOT_SOURCE=$(jq_field '.sourceType'       | grep -v null | head -1)
LOT_PROD=$(jq_field '.idProduct'          | grep -v null | head -1)

assert_eq       "lote Mostaza statusLot = Disponible"  "Disponible"        "$LOT_STATUS"
assert_float_eq "lote Mostaza quantityAvailable = $EXP_STOCK_MOSTAZA" "$EXP_STOCK_MOSTAZA" "${LOT_QTY:-0}"
assert_float_eq "lote Mostaza unitCost = $EXP_COST_MOSTAZA"           "$EXP_COST_MOSTAZA"  "${LOT_COST:-0}"
assert_eq       "lote Mostaza sourceType = Compra"     "Compra"            "$LOT_SOURCE"
assert_eq       "lote Mostaza idProduct = 9"           "9"                  "$LOT_PROD"
log_info "  750 ML comprados − 45 ML (3 × 15ml por receta) = $LOT_QTY ML"

# 3-d  Lote Catsup (id=10) — post OP: 1000 − 60 = 940 ML
section "SECCIÓN 3d — Lote Catsup (id=$ID_LOT_CATSUP) — post-OP: 940 ML"

api_get "/inventory-lots/${ID_LOT_CATSUP}.json"
assert_200 "GET /inventory-lots/${ID_LOT_CATSUP}.json"

LOT_STATUS=$(jq_field '.statusLot')
LOT_QTY=$(jq_field '.quantityAvailable'   | grep -v null | head -1)
LOT_COST=$(jq_field '.unitCost'           | grep -v null | head -1)
LOT_SOURCE=$(jq_field '.sourceType'       | grep -v null | head -1)
LOT_PROD=$(jq_field '.idProduct'          | grep -v null | head -1)

assert_eq       "lote Catsup statusLot = Disponible"  "Disponible"       "$LOT_STATUS"
assert_float_eq "lote Catsup quantityAvailable = $EXP_STOCK_CATSUP" "$EXP_STOCK_CATSUP" "${LOT_QTY:-0}"
assert_float_eq "lote Catsup unitCost = $EXP_COST_CATSUP"           "$EXP_COST_CATSUP"  "${LOT_COST:-0}"
assert_eq       "lote Catsup sourceType = Compra"     "Compra"           "$LOT_SOURCE"
assert_eq       "lote Catsup idProduct = 10"          "10"                "$LOT_PROD"
log_info "  1000 ML comprados − 60 ML (3 × 20ml por receta) = $LOT_QTY ML"

# 3-e  Stock total global por cada ingrediente (endpoint /stock/)
section "SECCIÓN 3e — Stock global de ingredientes (/inventory-lots/stock/N.json)"

for entry in "7:$EXP_STOCK_PAN:Pan" "8:$EXP_STOCK_SALCHICHA:Salchicha" "9:$EXP_STOCK_MOSTAZA:Mostaza" "10:$EXP_STOCK_CATSUP:Catsup"; do
  id_p="${entry%%:*}"
  rest="${entry#*:}"
  exp_qty="${rest%%:*}"
  name="${rest##*:}"

  api_get "/inventory-lots/stock/${id_p}.json"
  assert_200 "GET /inventory-lots/stock/${id_p}.json ($name)"
  STOCK_ACT=$(cat "$TEMP_RESPONSE" | tr -d '"[:space:]')
  assert_float_eq "$name stock global = $exp_qty" "$exp_qty" "${STOCK_ACT:-0}"
done

# ═════════════════════════════════════════════════════════════════════════════
# SECCIÓN 4 — LOTES POR PRODUCTO (listas y filtros)
# ═════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 4 — Lotes por producto — listados y filtros"

# 4-a  Ingredientes: cada uno debe tener al menos 1 lote activo
for id_p in 7 8 9 10; do
  api_get "/inventory-lots/by-product/${id_p}.json"
  assert_200 "GET /inventory-lots/by-product/${id_p}.json"
  COUNT=$(jq_field 'length')
  assert_gte "producto $id_p tiene al menos 1 lote" "1" "$COUNT"
done

# 4-b  Hot Dog PT: debe tener al menos 1 lote generado por OP
api_get "/inventory-lots/by-product/11.json"
assert_200 "GET /inventory-lots/by-product/11.json"
COUNT_PT=$(jq_field 'length')
assert_gte "producto 11 (Hot Dog PT) tiene al menos 1 lote" "1" "$COUNT_PT"
log_info "  Lotes PT encontrados: $COUNT_PT"

# 4-c  Almacén inexistente → lista vacía para ingredientes y PT
for id_p in 7 11; do
  api_get "/inventory-lots/by-product/${id_p}.json?idWarehouse=9999"
  assert_200 "GET /inventory-lots/by-product/${id_p}.json?idWarehouse=9999"
  EMPTY=$(jq_field 'length')
  assert_eq "producto $id_p con almacén 9999 devuelve 0 lotes" "0" "$EMPTY"
done

# 4-d  Lote sugerido FEFO ingrediente Pan (debe ser el lote de compra)
api_get "/inventory-lots/suggest/7.json"
assert_200 "GET /inventory-lots/suggest/7.json (FEFO Pan)"
SUGGEST_ID=$(jq_field '.idInventoryLot'        | grep -v null | head -1)
SUGGEST_STATUS=$(jq_field '.statusLot'         | grep -v null | head -1)
assert_eq "FEFO suggest Pan = lote del caso"       "$ID_LOT_PAN"  "$SUGGEST_ID"
assert_eq "FEFO suggest Pan statusLot = Disponible" "Disponible"  "$SUGGEST_STATUS"

# 4-e  Lote sugerido en almacén inexistente → 404
api_get "/inventory-lots/suggest/7.json?idWarehouse=9999"
if [[ "$HTTP_STATUS" == "404" ]]; then
  log_ok "suggest Pan almacén 9999 → HTTP 404 ✓"
else
  log_fail "suggest Pan almacén 9999 → esperado 404, recibido $HTTP_STATUS"
fi

# ═════════════════════════════════════════════════════════════════════════════
# SECCIÓN 5 — LOTE PT HOT DOG (detalle completo)
# ═════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 5 — Lote PT Hot Dog (id=$ID_LOT_PT) — stock final: −1"

api_get "/inventory-lots/${ID_LOT_PT}.json"
assert_200 "GET /inventory-lots/${ID_LOT_PT}.json"

PT_STATUS=$(jq_field '.statusLot'            | grep -v null | head -1)
PT_QTY=$(jq_field    '.quantityAvailable'    | grep -v null | head -1)
PT_COST=$(jq_field   '.unitCost'             | grep -v null | head -1)
PT_PROD=$(jq_field   '.idProduct'            | grep -v null | head -1)
PT_WH=$(jq_field     '.idWarehouse'          | grep -v null | head -1)
PT_WH_NAME=$(jq_field '.nameWarehouse'       | grep -v null | head -1)
PT_PC=$(jq_field     '.idPurchaseInvoice'    | grep -v null | head -1)

# El lote PT fue generado por la OP, no por una compra → idPurchaseInvoice debe ser null
assert_eq       "lote PT idProduct = 11"              "11"                "$PT_PROD"
assert_eq       "lote PT idWarehouse = $ID_WAREHOUSE" "$ID_WAREHOUSE"     "$PT_WH"
assert_float_eq "lote PT unitCost = $EXP_COST_PT"     "$EXP_COST_PT"      "${PT_COST:-0}"
assert_float_eq "lote PT quantityAvailable = $EXP_STOCK_PT" "$EXP_STOCK_PT" "${PT_QTY:-0}"
if [[ -z "$PT_PC" || "$PT_PC" == "null" ]]; then
  log_ok "lote PT idPurchaseInvoice = null (origen: OP automática)"
else
  log_fail "lote PT idPurchaseInvoice debería ser null, recibido: $PT_PC"
fi
log_info "  Almacén: ${PT_WH_NAME:-?}  status: ${PT_STATUS:-?}"
log_info "  Fórmula: 3 (OP) − 3 (FV) + 1 (devolución) − 2 (regalía) = −1"

# Stock total global del PT
api_get "/inventory-lots/stock/11.json"
assert_200 "GET /inventory-lots/stock/11.json (Hot Dog PT)"
STOCK_PT_GLOBAL=$(cat "$TEMP_RESPONSE" | tr -d '"[:space:]')
assert_float_eq "Hot Dog PT stock global = $EXP_STOCK_PT" "$EXP_STOCK_PT" "${STOCK_PT_GLOBAL:-0}"

# ═════════════════════════════════════════════════════════════════════════════
# SECCIÓN 6 — PEDIDO DE VENTA
# ═════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 6 — Pedido de Venta (id=${ID_SALES_ORDER:-(no encontrado)})"

if [[ -n "$ID_SALES_ORDER" && "$ID_SALES_ORDER" != "null" ]]; then
  api_get "/sales-orders/${ID_SALES_ORDER}.json"
  assert_200 "GET /sales-orders/${ID_SALES_ORDER}.json"

  SO_STATUS=$(jq_field '.statusOrder')
  SO_LINES=$(jq_field '.lines | length')
  SO_PROD=$(jq_field '.lines[0].idProduct')

  assert_eq "pedido statusOrder = Completado" "Completado" "$SO_STATUS"
  assert_eq "pedido tiene 1 línea"            "1"          "${SO_LINES:-0}"
  assert_eq "pedido línea[0] idProduct = 11"  "11"         "$SO_PROD"
  log_info "  Pedido vinculado a FV id=$ID_SALES_INVOICE (ciclo automático completado)"
else
  log_warn "idSalesOrder no encontrado — omitiendo verificación del pedido"
fi

# ═════════════════════════════════════════════════════════════════════════════
# SECCIÓN 7 — FACTURA DE VENTA
# ═════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 7 — Factura de Venta (id=$ID_SALES_INVOICE)"

api_get "/sales-invoices/${ID_SALES_INVOICE}.json"
assert_200 "GET /sales-invoices/${ID_SALES_INVOICE}.json"

FV_STATUS=$(jq_field '.statusInvoice')
FV_ENTRY=$(jq_field '.idAccountingEntry // empty' | grep -v null | head -1)
FV_LINES=$(jq_field '.lines | length')
FV_LOT_IN_LINE=$(jq_field '.lines[0].idInventoryLot // empty' | grep -v null | head -1)
FV_QTY=$(jq_field '.lines[0].quantity // empty' | grep -v null | head -1)
FV_UNIT_PRICE=$(jq_field '.lines[0].unitPrice // empty' | grep -v null | head -1)

assert_eq  "FV statusInvoice = Confirmado"         "Confirmado" "$FV_STATUS"
assert_gte "FV tiene idAccountingEntry"             "1"          "${FV_ENTRY:-0}"
assert_eq  "FV tiene 1 línea (Hot Dog)"             "1"          "${FV_LINES:-0}"
assert_eq  "FV línea[0] idInventoryLot = lote PT"  "$ID_LOT_PT" "${FV_LOT_IN_LINE:-null}"
assert_float_eq "FV línea[0] quantity = 3"         "3"           "${FV_QTY:-0}"
assert_float_eq "FV línea[0] unitPrice = 3000"     "3000"        "${FV_UNIT_PRICE:-0}"
log_info "  Asiento FV: DR 106 Caja ₡10,170 / CR 117 Ingresos ₡9,000 / CR 127 IVA ₡1,170"
log_info "  Asiento COGS: DR 119 ₡5,085 / CR 109 Inventario ₡5,085  (3 × ₡1,695)"

# ═════════════════════════════════════════════════════════════════════════════
# SECCIÓN 8 — AJUSTE DE INVENTARIO (regalía: −2 hot dogs)
# ═════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 8 — Ajuste de Inventario — Regalía (id=$ID_ADJUSTMENT)"

api_get "/inventory-adjustments/${ID_ADJUSTMENT}.json"
assert_200 "GET /inventory-adjustments/${ID_ADJUSTMENT}.json"

ADJ_STATUS=$(jq_field '.statusAdjustment')
ADJ_ENTRY=$(jq_field '.idAccountingEntry // empty' | grep -v null | head -1)
ADJ_LINES=$(jq_field '.lines | length')
ADJ_LOT=$(jq_field '.lines[0].idInventoryLot // empty' | grep -v null | head -1)
ADJ_DELTA=$(jq_field '.lines[0].quantityDelta // empty' | grep -v null | head -1)

assert_eq       "ADJ statusAdjustment = Confirmado"      "Confirmado" "$ADJ_STATUS"
assert_gte      "ADJ tiene idAccountingEntry"             "1"          "${ADJ_ENTRY:-0}"
assert_eq       "ADJ tiene 1 línea"                      "1"          "${ADJ_LINES:-0}"
assert_eq       "ADJ línea[0] idInventoryLot = lote PT"  "$ID_LOT_PT" "${ADJ_LOT:-null}"
assert_float_eq "ADJ línea[0] quantityDelta = −2"        "-2"          "${ADJ_DELTA:-0}"
log_info "  Asiento regalía: DR 113 Merma ₡3,390 / CR 109 Inventario ₡3,390  (2 × ₡1,695)"

# ═════════════════════════════════════════════════════════════════════════════
# SECCIÓN 9 — ALMACÉN Y VERIFICACIONES CRUZADAS
# ═════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 9 — Almacén y verificaciones cruzadas"

# 9-a  Catálogo de almacenes
api_get "/warehouses/data.json"
assert_200 "GET /warehouses/data.json"

WH_EXISTS=$(jq "[.[] | select(.idWarehouse == $ID_WAREHOUSE)] | length" "$TEMP_RESPONSE")
assert_eq "almacén id=$ID_WAREHOUSE existe en catálogo" "1" "$WH_EXISTS"

WH_NAME=$(jq_field "[.[] | select(.idWarehouse == $ID_WAREHOUSE)] | first | .nameWarehouse")
WH_ACTIVE=$(jq_field "[.[] | select(.idWarehouse == $ID_WAREHOUSE)] | first | .isActive")
assert_eq "almacén $ID_WAREHOUSE isActive = true" "true" "$WH_ACTIVE"
log_info  "  Almacén $ID_WAREHOUSE: '$WH_NAME'"

# 9-b  Consistencia: suma lotes ingredientes = stock global
for entry in "7:$EXP_STOCK_PAN" "8:$EXP_STOCK_SALCHICHA" "9:$EXP_STOCK_MOSTAZA" "10:$EXP_STOCK_CATSUP" "11:$EXP_STOCK_PT"; do
  id_p="${entry%%:*}"
  exp="${entry##*:}"

  api_get "/inventory-lots/by-product/${id_p}.json"
  SUM=$(jq '[.[].quantityAvailable] | add // 0' "$TEMP_RESPONSE")
  assert_float_eq "suma lotes producto $id_p = stock global ($exp)" "$exp" "$SUM"
done

# 9-c  Todos los lotes de ingredientes pertenecen al almacén correcto
for id_p in 7 8 9 10; do
  api_get "/inventory-lots/by-product/${id_p}.json?idWarehouse=$ID_WAREHOUSE"
  assert_200 "GET /inventory-lots/by-product/${id_p}.json?idWarehouse=$ID_WAREHOUSE"
  WRONG_WH=$(jq "[.[] | select(.idWarehouse != $ID_WAREHOUSE)] | length" "$TEMP_RESPONSE")
  assert_eq "producto $id_p — todos los lotes en almacén $ID_WAREHOUSE" "0" "$WRONG_WH"
done

# ═════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ═════════════════════════════════════════════════════════════════════════════

echo ""
{
  echo ""
  echo "# ── Resumen ──────────────────────────────────────────────"
  echo "#   OK:   $CHECKS_OK"
  echo "#   FAIL: $CHECKS_FAIL"
  echo "#   Total: $((CHECKS_OK + CHECKS_FAIL))"
} >> "$OUTPUT_FILE"

if [[ $CHECKS_FAIL -eq 0 ]]; then
  printf "${GREEN}${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
  printf "${GREEN}${BOLD}║   ✅  TODAS LAS VERIFICACIONES PASARON              ║${NC}\n"
  printf "${GREEN}${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"
else
  printf "${RED}${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
  printf "${RED}${BOLD}║   ❌  HAY VERIFICACIONES FALLIDAS                   ║${NC}\n"
  printf "${RED}${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"
fi

printf "  %-28s %s\n" "OK:"    "$CHECKS_OK"
printf "  %-28s %s\n" "FAIL:"  "$CHECKS_FAIL"
printf "  %-28s %s\n" "Total:" "$((CHECKS_OK + CHECKS_FAIL))"
echo ""
printf "  ${DIM}IDs del caso:${NC}\n"
printf "  %-30s %s\n" "idPurchaseInvoice:"      "$ID_PURCHASE_INVOICE"
printf "  %-30s %s\n" "idLotPan:"               "$ID_LOT_PAN"
printf "  %-30s %s\n" "idLotSalchicha:"         "$ID_LOT_SALCHICHA"
printf "  %-30s %s\n" "idLotMostaza:"           "$ID_LOT_MOSTAZA"
printf "  %-30s %s\n" "idLotCatsup:"            "$ID_LOT_CATSUP"
printf "  %-30s %s\n" "idSalesOrder:"           "${ID_SALES_ORDER:-(no encontrado)}"
printf "  %-30s %s\n" "idSalesInvoice:"         "$ID_SALES_INVOICE"
printf "  %-30s %s\n" "idLotPT (Hot Dog):"      "$ID_LOT_PT"
printf "  %-30s %s\n" "idAdjustment (regalía):" "$ID_ADJUSTMENT"
echo ""
printf "  ${DIM}Stock final:${NC}\n"
printf "  %-30s %s\n" "Pan de Hot Dog  (id=7):"  "$EXP_STOCK_PAN UNI"
printf "  %-30s %s\n" "Salchicha       (id=8):"  "$EXP_STOCK_SALCHICHA UNI"
printf "  %-30s %s\n" "Mostaza         (id=9):"  "$EXP_STOCK_MOSTAZA ML"
printf "  %-30s %s\n" "Catsup          (id=10):" "$EXP_STOCK_CATSUP ML"
printf "  %-30s %s\n" "Hot Dog PT      (id=11):" "$EXP_STOCK_PT UNI"
echo ""
printf "  ${DIM}Reporte guardado en: %s${NC}\n" "$OUTPUT_FILE"
echo ""

rm -f "$TEMP_RESPONSE"
[[ $CHECKS_FAIL -eq 0 ]] && exit 0 || exit 1
