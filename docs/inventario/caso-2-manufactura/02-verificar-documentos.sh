#!/usr/bin/env bash
# ============================================================================
#  CASO 2 — MANUFACTURA (Chile Embotellado Marca X)
#  02-verificar-documentos.sh — Verificación de Documentos e Inventario
#
#  Propósito:
#   Descubre automáticamente los IDs de los documentos del Caso 2 consultando
#   los asientos contables y la API, sin depender de resultado_caso2_*.txt.
#   Verifica que los documentos se generaron correctamente y el inventario es
#   correcto. Genera verificacion_docs_caso2_*.txt con el reporte completo.
#   El análisis de cuentas contables (T-accounts, saldos DR/CR) se hace en
#   03-analizar-cuentas-contables.sh.
#
#  Requisitos previos:
#   - curl, jq y sqlcmd instalados.
#   - API corriendo en https://localhost:8000.
#   - Credenciales en credentials/db.txt.
#
#  Uso:
#   bash docs/inventario/caso-2-manufactura/02-verificar-documentos.sh
# ============================================================================

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TEMP_RESPONSE="/tmp/fa_caso2_consultas.json"
HOST="https://localhost:8000/api/v1"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
DIM='\033[2m'
NC='\033[0m'

CHECKS_OK=0
CHECKS_FAIL=0

RUN_TS="$(date '+%Y-%m-%d_%H-%M-%S')"
OUTPUT_FILE="$SCRIPT_DIR/verificacion_docs_caso2_${RUN_TS}.txt"

# ── Helpers ────────────────────────────────────────────────────────────────────

section() {
  echo ""
  printf "${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n"
  printf "${CYAN}${BOLD}▶  %s${NC}\n" "$1"
  printf "${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n"
  { echo ""; echo "══ $1 ══"; } >> "$OUTPUT_FILE"
}

log_ok()   { printf "  ${GREEN}✅  %s${NC}\n" "$1"; CHECKS_OK=$((CHECKS_OK + 1)); echo "  [OK]   $1" >> "$OUTPUT_FILE"; }
log_fail() { printf "  ${RED}❌  %s${NC}\n" "$1"; CHECKS_FAIL=$((CHECKS_FAIL + 1)); echo "  [FAIL] $1" >> "$OUTPUT_FILE"; }
log_info() { printf "  ${DIM}%s${NC}\n" "$1"; echo "         $1" >> "$OUTPUT_FILE"; }
log_warn() { printf "  ${YELLOW}⚠   %s${NC}\n" "$1"; echo "  [WARN] $1" >> "$OUTPUT_FILE"; }

HTTP_STATUS=""
api_get() {
  local path="$1"
  HTTP_STATUS=$(curl -k -s -o "$TEMP_RESPONSE" -w "%{http_code}" \
    -H "Authorization: Bearer $TOKEN" \
    "${HOST}${path}")
}

jq_field() { jq -r "$1" "$TEMP_RESPONSE" 2>/dev/null; }

assert_200() {
  local label="$1"
  if [[ "$HTTP_STATUS" == "200" ]]; then
    log_ok "$label — HTTP 200"
  else
    log_fail "$label — esperado HTTP 200, recibido $HTTP_STATUS"
  fi
}

assert_eq() {
  local label="$1" expected="$2" actual="$3"
  if [[ "$actual" == "$expected" ]]; then
    log_ok "$label: $actual"
  else
    log_fail "$label: esperado='$expected'  real='$actual'"
  fi
}

assert_gte() {
  local label="$1" expected="$2" actual="$3"
  if awk "BEGIN {exit !($actual >= $expected)}"; then
    log_ok "$label: $actual (>= $expected)"
  else
    log_fail "$label: esperado >= $expected  real='$actual'"
  fi
}

assert_float_eq() {
  local label="$1" expected="$2" actual="$3"
  if awk "BEGIN {a=($actual)+0; e=($expected)+0; diff=a-e; if(diff<0)diff=-diff; exit !(diff<0.01)}"; then
    log_ok "$label: $actual"
  else
    log_fail "$label: esperado=$expected  real='$actual'"
  fi
}

# ── Autenticación ──────────────────────────────────────────────────────────────

EMAIL="ezekiell1988@hotmail.com"
CREDENTIALS_FILE="$(cd "$SCRIPT_DIR/../../.." && pwd)/credentials/db.txt"
DB_HOST=$(grep -E '^HOST:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_PORT=$(grep -E '^PORT:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_USER=$(grep -E '^USER:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_PASS=$(grep -E '^PASSWORD:' "$CREDENTIALS_FILE" | awk '{print $2}')

curl -k -s -X POST "${HOST}/auth/request-pin" -H "Content-Type: application/json" \
  -d "{\"emailUser\":\"$EMAIL\"}" > /dev/null

PIN=$(sqlcmd -S "${DB_HOST},${DB_PORT}" -U "$DB_USER" -P "$DB_PASS" -C -d dbfa \
  -Q "SET NOCOUNT ON; SELECT TOP 1 pin FROM dbo.userPin up JOIN dbo.[user] u ON u.idUser=up.idUser WHERE u.emailUser='${EMAIL}' ORDER BY up.idUserPin DESC" \
  -h -1 -W 2>/dev/null | tr -d '[:space:]')

TOKEN=$(curl -k -s -X POST "${HOST}/auth/login" -H "Content-Type: application/json" \
  -d "{\"emailUser\":\"$EMAIL\",\"pin\":\"$PIN\"}" | jq -r '.accessToken')

if [[ -z "$TOKEN" || "$TOKEN" == "null" ]]; then
  printf "${RED}❌  No se pudo obtener token${NC}\n"; exit 1
fi

# Valores esperados y constantes
EXP_UNIT_COST_PT="526"
EXP_STOCK_PT_FINAL="73"
ID_WAREHOUSE="1"

# ── Descubrir IDs desde asientos contables ─────────────────────────────────────
TEMP_ENTRIES="/tmp/fa_caso2_entries_disco.json"
curl -k -s -H "Authorization: Bearer $TOKEN" "${HOST}/accounting-entries/data.json" > "$TEMP_ENTRIES"

ID_PURCHASE_INVOICE=$(jq -r '[.[] | select(.numberEntry | startswith("FC-"))] | sort_by(.idAccountingEntry) | last | .idOriginRecord // empty' "$TEMP_ENTRIES")
ID_PRODUCTION_ORDER=$(jq -r '[.[] | select(.originModule == "ProductionOrder")] | sort_by(.idAccountingEntry) | last | .idOriginRecord // empty' "$TEMP_ENTRIES")
ID_SALES_INVOICE=$(jq -r '[.[] | select(.numberEntry | startswith("FV-"))] | sort_by(.idAccountingEntry) | last | .idOriginRecord // empty' "$TEMP_ENTRIES")
ID_ADJUSTMENT=$(jq -r '[.[] | select(.numberEntry | startswith("AJ-"))] | sort_by(.idAccountingEntry) | last | .idOriginRecord // empty' "$TEMP_ENTRIES")
ID_ENTRY_DEV_ING=$(jq -r '[.[] | select(.numberEntry | startswith("DEV-ING-FV-"))] | sort_by(.idAccountingEntry) | last | .idAccountingEntry // empty' "$TEMP_ENTRIES")
rm -f "$TEMP_ENTRIES"

# Lotes de MP — el más reciente vinculado a la FC de MP
_tmp_lot="/tmp/fa_caso2_lot_disco.json"
for _pid_var in "2:ID_LOT_CHILE" "3:ID_LOT_VINAGRE" "4:ID_LOT_SAL" "5:ID_LOT_FRASCO"; do
  _pid="${_pid_var%%:*}"
  _var="${_pid_var##*:}"
  curl -k -s -H "Authorization: Bearer $TOKEN" \
    "${HOST}/inventory-lots/by-product/${_pid}.json?idWarehouse=${ID_WAREHOUSE}" > "$_tmp_lot"
  _lid=$(jq -r "[.[] | select(.idPurchaseInvoice == ${ID_PURCHASE_INVOICE})] | sort_by(.idInventoryLot) | last | .idInventoryLot // empty" "$_tmp_lot")
  printf -v "$_var" '%s' "$_lid"
done
rm -f "$_tmp_lot"

# Lote PT — sugerido FEFO para producto 6
curl -k -s -H "Authorization: Bearer $TOKEN" "${HOST}/inventory-lots/suggest/6.json" > "$TEMP_RESPONSE"
ID_LOT_PT=$(jq -r '.idInventoryLot // empty' "$TEMP_RESPONSE")

# Período fiscal — desde la orden de producción
curl -k -s -H "Authorization: Bearer $TOKEN" "${HOST}/production-orders/${ID_PRODUCTION_ORDER}.json" > "$TEMP_RESPONSE"
ID_FISCAL_PERIOD=$(jq -r '.idFiscalPeriod // empty' "$TEMP_RESPONSE")
[[ -z "$ID_FISCAL_PERIOD" || "$ID_FISCAL_PERIOD" == "null" ]] && ID_FISCAL_PERIOD="4"

# ── Cabecera ───────────────────────────────────────────────────────────────────
printf "${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
printf "${BOLD}║   CASO 2 — MANUFACTURA · Verificación de Documentos ║${NC}\n"
printf "${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"

{
  echo "# =================================================================="
  echo "#  CASO 2 — MANUFACTURA · Verificación de Documentos e Inventario"
  echo "#  Generado: $(date '+%Y-%m-%d %H:%M:%S')"
  echo "#  IDs descubiertos desde la API (sin resultado_caso2_*.txt)"
  echo "# =================================================================="
} > "$OUTPUT_FILE"

printf "\n"
printf "  ${DIM}IDs descubiertos:${NC}\n"
printf "  ${DIM}  FC=%s  OP=%s  LotPT=%s${NC}\n" "$ID_PURCHASE_INVOICE" "$ID_PRODUCTION_ORDER" "$ID_LOT_PT"
printf "  ${DIM}  FV=%s  ADJ=%s  EntryDevIng=%s${NC}\n" \
  "$ID_SALES_INVOICE" "$ID_ADJUSTMENT" "${ID_ENTRY_DEV_ING:-(no encontrado)}"
printf "  ${DIM}  Período fiscal: %s${NC}\n" "$ID_FISCAL_PERIOD"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 1 — CATÁLOGO (seed y configuración)
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 1 — Catálogo (seed y configuración)"

# 1-a  MP: productos 2–5 con idProductType=1 (Materia Prima)
for PROD_ID in 2 3 4 5; do
  api_get "/products/${PROD_ID}.json"
  assert_200 "GET /products/${PROD_ID}.json"
  assert_eq "product[${PROD_ID}].idProductType = 1 (MP)" "1" "$(jq_field '.idProductType')"
done

# 1-b  PT: producto 6 con idProductType=3 (Producto Terminado)
api_get "/products/6.json"
assert_200 "GET /products/6.json"
assert_eq "product[6].idProductType = 3 (PT)"      "3" "$(jq_field '.idProductType')"
assert_eq "product[6].idProduct = 6"               "6" "$(jq_field '.idProduct')"

# 1-c  Receta activa del PT (idProductRecipe=1)
api_get "/product-recipes/by-output/6.json"
assert_200 "GET /product-recipes/by-output/6.json"
RECIPE_ACTIVE_COUNT=$(jq_field '[.[] | select(.isActive == true)] | length' 2>/dev/null || echo "0")
assert_gte "receta activa del PT existe" "1" "$RECIPE_ACTIVE_COUNT"
RECIPE_QTY=$(jq_field '[.[] | select(.isActive == true)] | first | .quantityOutput')
assert_float_eq "receta.quantityOutput = 1.0" "1.0" "${RECIPE_QTY:-0}"
RECIPE_LINES=$(jq_field '[.[] | select(.isActive == true)] | first | .lines | length' 2>/dev/null || echo "0")
assert_gte "receta tiene 4 ingredientes" "4" "$RECIPE_LINES"
log_info "  Receta activa: quantityOutput=$RECIPE_QTY  ingredientes=$RECIPE_LINES"

# 1-d  Tipos de factura de compra / venta / ajuste
api_get "/purchase-invoice-types/data.json"
assert_200 "GET /purchase-invoice-types/data.json"
COUNT=$(jq_field '[.[] | select(.idPurchaseInvoiceType == 1)] | length')
assert_eq "purchase-invoice-type id=1 existe" "1" "$COUNT"

api_get "/sales-invoice-types/data.json"
assert_200 "GET /sales-invoice-types/data.json"
COUNT=$(jq_field '[.[] | select(.idSalesInvoiceType == 1)] | length')
assert_eq "sales-invoice-type id=1 existe" "1" "$COUNT"

api_get "/inventory-adjustment-types/data.json"
assert_200 "GET /inventory-adjustment-types/data.json"
COUNT_MERMA=$(jq_field '[.[] | select(.idInventoryAdjustmentType == 1)] | length')
assert_eq "adjustment-type id=1 (MERMA) existe" "1" "$COUNT_MERMA"
COUNT_PROD=$(jq_field '[.[] | select(.idInventoryAdjustmentType == 2)] | length')
assert_eq "adjustment-type id=2 (PRODUCCION) existe" "1" "$COUNT_PROD"

# 1-e  ProductAccounts de los MP → deben existir (cuenta 110, 100%)
for PROD_ID in 2 3 4 5; do
  api_get "/product-accounts/by-product/${PROD_ID}.json"
  assert_200 "GET /product-accounts/by-product/${PROD_ID}.json"
  PA_110=$(jq_field '[.[] | select(.idAccount == 110)] | length')
  assert_gte "product-accounts[${PROD_ID}] → cta 110 existe" "1" "${PA_110:-0}"
done

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 2 — FACTURA DE COMPRA DE MP
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 2 — Factura de Compra MP (id=$ID_PURCHASE_INVOICE)"

api_get "/purchase-invoices/${ID_PURCHASE_INVOICE}.json"
assert_200 "GET /purchase-invoices/${ID_PURCHASE_INVOICE}.json"

PC_STATUS=$(jq_field '.statusInvoice')
PC_ENTRY=$(jq_field '.idAccountingEntry // .accountingEntryId // empty' | grep -v null | head -1)
PC_LINE_COUNT=$(jq_field '.lines | length // .purchaseInvoiceLines | length // 0' 2>/dev/null || echo "0")

assert_eq  "FC statusInvoice = Confirmado"  "Confirmado" "$PC_STATUS"
assert_gte "FC tiene idAccountingEntry"     "1"          "${PC_ENTRY:-0}"
log_info   "  FC idAccountingEntry: ${PC_ENTRY:-(no expuesto)}"
log_info   "  FC líneas (si expuesto): ${PC_LINE_COUNT}"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3 — LOTES DE MATERIAS PRIMAS
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 3 — Lotes de MP (creados al confirmar FC)"

check_mp_lot() {
  local prod_id="$1"
  local lot_id="$2"
  local exp_qty="$3"
  local exp_cost="$4"
  local name="$5"

  api_get "/inventory-lots/${lot_id}.json"
  assert_200 "GET /inventory-lots/${lot_id}.json ($name)"

  LOT_STATUS=$(jq_field '.statusLot'         | grep -v null | head -1)
  LOT_QTY=$(jq_field    '.quantityAvailable' | grep -v null | head -1)
  LOT_COST=$(jq_field   '.unitCost'          | grep -v null | head -1)
  LOT_PROD=$(jq_field   '.idProduct'         | grep -v null | head -1)
  LOT_SRC=$(jq_field    '.sourceType'        | grep -v null | head -1)
  LOT_PC=$(jq_field     '.idPurchaseInvoice' | grep -v null | head -1)

  # Después de producción los MP deberían estar agotados (qty ≈ 0)
  assert_eq       "${name} statusLot"          "Disponible"          "$LOT_STATUS"
  assert_float_eq "${name} quantityAvailable"  "$exp_qty"            "${LOT_QTY:-999}"
  assert_float_eq "${name} unitCost"           "$exp_cost"           "${LOT_COST:-0}"
  assert_eq       "${name} idProduct = ${prod_id}" "$prod_id"        "${LOT_PROD}"
  assert_eq       "${name} sourceType = Compra"    "Compra"          "$LOT_SRC"
  assert_eq       "${name} origen = FC ${ID_PURCHASE_INVOICE}" "$ID_PURCHASE_INVOICE" "$LOT_PC"
}

# Después de completar la OP, los lotes MP quedaron en 0 (consumidos por FEFO)
check_mp_lot 2 "$ID_LOT_CHILE"   0 1000 "Lote Chile Seco"
check_mp_lot 3 "$ID_LOT_VINAGRE" 0 500  "Lote Vinagre Blanco"
check_mp_lot 4 "$ID_LOT_SAL"     0 200  "Lote Sal"
check_mp_lot 5 "$ID_LOT_FRASCO"  0 300  "Lote Frasco 250ml"

# Stock global de MP (todos deben haberse consumido)
for PROD_ID in 2 3 4 5; do
  api_get "/inventory-lots/stock/${PROD_ID}.json"
  STMP=$(cat "$TEMP_RESPONSE" | tr -d '[:space:]')
  assert_float_eq "Stock MP idProduct=${PROD_ID} = 0 (consumido en OP)" "0" "${STMP:-999}"
done

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 4 — ORDEN DE PRODUCCIÓN
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 4 — Orden de Producción (id=$ID_PRODUCTION_ORDER)"

api_get "/production-orders/${ID_PRODUCTION_ORDER}.json"
assert_200 "GET /production-orders/${ID_PRODUCTION_ORDER}.json"

OP_STATUS=$(jq_field '.statusProductionOrder')
OP_NUMBER=$(jq_field '.numberProductionOrder')
OP_PRODUCT=$(jq_field '.lines[0].idProduct')
OP_QTY_REQ=$(jq_field '.lines[0].quantityRequired')
OP_QTY_PROD=$(jq_field '.lines[0].quantityProduced')
OP_WH=$(jq_field '.idWarehouse')

assert_eq       "OP statusProductionOrder = Completado" "Completado"   "$OP_STATUS"
assert_eq       "OP idProduct = 6 (PT)"                "6"            "$OP_PRODUCT"
assert_float_eq "OP quantityRequired = 100"            "100"           "${OP_QTY_REQ:-0}"
assert_float_eq "OP quantityProduced = 100"            "100"           "${OP_QTY_PROD:-0}"
assert_eq       "OP idWarehouse = $ID_WAREHOUSE"       "$ID_WAREHOUSE" "$OP_WH"

# Número de OP debe ser correlativo (no "BORRADOR")
if [[ "$OP_NUMBER" != "BORRADOR" && -n "$OP_NUMBER" ]]; then
  log_ok "OP numberProductionOrder asignado: $OP_NUMBER"
else
  log_fail "OP sigue en estado BORRADOR o número vacío: '$OP_NUMBER'"
fi

log_info "  OP: status=$OP_STATUS  número=$OP_NUMBER  qty=$OP_QTY_PROD"

# Lista de OPs del período — debe aparecer la nuestra
api_get "/production-orders/by-period/${ID_FISCAL_PERIOD}.json"
assert_200 "GET /production-orders/by-period/${ID_FISCAL_PERIOD}.json"
OP_IN_PERIOD=$(jq_field "[.[] | select(.idProductionOrder == ${ID_PRODUCTION_ORDER})] | length")
assert_eq "OP aparece en listado del período $ID_FISCAL_PERIOD" "1" "$OP_IN_PERIOD"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 5 — LOTE DEL PRODUCTO TERMINADO
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 5 — Lote PT (creado al completar OP, id=$ID_LOT_PT)"

api_get "/inventory-lots/${ID_LOT_PT}.json"
assert_200 "GET /inventory-lots/${ID_LOT_PT}.json"

LOT_PT_STATUS=$(jq_field '.statusLot'          | grep -v null | head -1)
LOT_PT_QTY=$(jq_field    '.quantityAvailable'  | grep -v null | head -1)
LOT_PT_RESERVED=$(jq_field '.quantityReserved' | grep -v null | head -1)
LOT_PT_COST=$(jq_field   '.unitCost'           | grep -v null | head -1)
LOT_PT_SOURCE=$(jq_field '.sourceType'         | grep -v null | head -1)
LOT_PT_PROD=$(jq_field   '.idProduct'          | grep -v null | head -1)
LOT_PT_WH=$(jq_field     '.idWarehouse'        | grep -v null | head -1)
LOT_PT_WH_NAME=$(jq_field '.nameWarehouse'     | grep -v null | head -1)

assert_eq       "lote PT statusLot = Disponible"         "Disponible"        "$LOT_PT_STATUS"
assert_float_eq "lote PT quantityAvailable = 73"         "$EXP_STOCK_PT_FINAL" "${LOT_PT_QTY:-0}"
assert_float_eq "lote PT quantityReserved = 0"           "0"                 "${LOT_PT_RESERVED:-0}"
assert_float_eq "lote PT unitCost = ₡526"                "$EXP_UNIT_COST_PT" "${LOT_PT_COST:-0}"
assert_eq       "lote PT sourceType = Producción"        "Producción"        "$LOT_PT_SOURCE"
assert_eq       "lote PT idProduct = 6"                  "6"                 "$LOT_PT_PROD"
assert_eq       "lote PT idWarehouse = $ID_WAREHOUSE"    "$ID_WAREHOUSE"     "$LOT_PT_WH"
assert_gte      "lote PT nameWarehouse no vacío"         "1"                 "${#LOT_PT_WH_NAME}"
log_info "  Almacén PT: '${LOT_PT_WH_NAME:-?}' (id=$LOT_PT_WH)  cost=₡$LOT_PT_COST"

# Stock global PT
api_get "/inventory-lots/stock/6.json"
assert_200 "GET /inventory-lots/stock/6.json"
STOCK_PT_REAL=$(cat "$TEMP_RESPONSE" | tr -d '[:space:]')
assert_float_eq "stock PT total = 73 frascos" "$EXP_STOCK_PT_FINAL" "$STOCK_PT_REAL"
log_info "  Fórmula: 100 (producción) − 30 (venta) + 5 (devolución) − 2 (regalía) = 73"

# Lote sugerido FEFO para PT → debe ser el lote creado por producción
api_get "/inventory-lots/suggest/6.json"
assert_200 "GET /inventory-lots/suggest/6.json"
SUGGEST_ID=$(jq_field '.idInventoryLot' | grep -v null | head -1)
assert_eq "lote sugerido PT = lote de producción" "$ID_LOT_PT" "$SUGGEST_ID"

# Lote PT filtrado por almacén
api_get "/inventory-lots/by-product/6.json?idWarehouse=${ID_WAREHOUSE}"
assert_200 "GET /inventory-lots/by-product/6.json?idWarehouse=$ID_WAREHOUSE"
LOT_COUNT_WH=$(jq_field 'length')
assert_gte "al menos 1 lote PT en almacén $ID_WAREHOUSE" "1" "$LOT_COUNT_WH"
SUM_WH=$(jq '[.[].quantityAvailable] | add // 0' "$TEMP_RESPONSE")
assert_float_eq "stock PT en almacén $ID_WAREHOUSE = 73" "$EXP_STOCK_PT_FINAL" "$SUM_WH"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 6 — FACTURA DE VENTA (30 frascos PT)
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 6 — Factura de Venta PT (id=$ID_SALES_INVOICE)"

api_get "/sales-invoices/${ID_SALES_INVOICE}.json"
assert_200 "GET /sales-invoices/${ID_SALES_INVOICE}.json"

FV_STATUS=$(jq_field '.statusInvoice')
ID_ENTRY_FV=$(jq_field '.idAccountingEntry // empty' | grep -v null | head -1)

assert_eq  "FV statusInvoice = Confirmado"  "Confirmado" "$FV_STATUS"
assert_gte "FV tiene idAccountingEntry"      "1"          "${ID_ENTRY_FV:-0}"
log_info   "  FV idAccountingEntry (ingreso FV-): ${ID_ENTRY_FV:-?}"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 6b — GUARDIA DE DEVOLUCIÓN PARCIAL
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 6b — Guardia devolución parcial (cantidad > vendida → debe fallar)"

log_info "Probando guardia: devolver 31 frascos (>30 vendidos) desde lote PT $ID_LOT_PT"

HTTP_STATUS_EXCESS=$(curl -k -s -o "$TEMP_RESPONSE" -w "%{http_code}" \
  -X POST "${HOST}/sales-invoices/${ID_SALES_INVOICE}/partial-return" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"dateReturn\":\"$(date '+%Y-%m-%d')\",\"descriptionReturn\":\"Test guardia — debe fallar\",\"refundMode\":\"EfectivoInmediato\",\"lines\":[{\"idInventoryLot\":${ID_LOT_PT},\"quantity\":31,\"totalLineAmount\":52500}]}")

if [[ "$HTTP_STATUS_EXCESS" == "422" ]]; then
  ERR_MSG=$(jq -r '.error // empty' "$TEMP_RESPONSE" 2>/dev/null | head -1)
  log_ok  "Guardia exceso: HTTP 422 — devolver 31 frascos rechazado correctamente"
  [[ -n "$ERR_MSG" ]] && log_info "  Mensaje API: $ERR_MSG"
elif [[ "$HTTP_STATUS_EXCESS" == "200" ]]; then
  log_fail "Guardia exceso: HTTP 200 — la API ACEPTÓ 31 frascos (regresión)"
else
  log_fail "Guardia exceso: HTTP inesperado $HTTP_STATUS_EXCESS (esperado 422)"
fi

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 6c — ASIENTO DEV-ING-FV (reversión ingresos + IVA)
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 6c — Asiento DEV-ING-FV (reintegro automático de la devolución)"

if [[ -n "$ID_ENTRY_DEV_ING" && "$ID_ENTRY_DEV_ING" != "null" && "$ID_ENTRY_DEV_ING" != "0" ]]; then
  api_get "/accounting-entries/${ID_ENTRY_DEV_ING}.json"
  assert_200 "GET /accounting-entries/${ID_ENTRY_DEV_ING}.json"

  ENTRY_NUMBER=$(jq_field '.numberEntry'    | grep -v null | head -1)
  ENTRY_STATUS=$(jq_field '.statusEntry'    | grep -v null | head -1)
  ENTRY_MODULE=$(jq_field '.originModule')
  ENTRY_RECORD=$(jq_field '.idOriginRecord')

  if [[ "$ENTRY_NUMBER" == DEV-ING-FV-* ]]; then
    log_ok "numberEntry empieza con DEV-ING-FV-: $ENTRY_NUMBER"
  else
    log_fail "numberEntry inesperado: '$ENTRY_NUMBER' (esperado DEV-ING-FV-...)"
  fi
  assert_eq "statusEntry = Publicado"           "Publicado"          "$ENTRY_STATUS"
  assert_eq "originModule = SalesReturnPartial" "SalesReturnPartial" "$ENTRY_MODULE"
  assert_eq "idOriginRecord = idSalesInvoice"   "$ID_SALES_INVOICE"  "$ENTRY_RECORD"

  TOTAL_DR=$(jq '[.lines[].debitAmount]  | add // 0' "$TEMP_RESPONSE")
  TOTAL_CR=$(jq '[.lines[].creditAmount] | add // 0' "$TEMP_RESPONSE")
  assert_float_eq "DEV-ING-FV ΣDR = ΣCR (partida doble)" "$TOTAL_DR" "$TOTAL_CR"
  assert_float_eq "DEV-ING-FV total = ₡8,475 (5 × ₡1,695)" "8475" "$TOTAL_DR"

  # DR 117 Ingresos (5 × 1500 = 7500)
  DR_117=$(jq '[.lines[] | select(.idAccount == 117) | .debitAmount] | add // 0' "$TEMP_RESPONSE")
  assert_float_eq "DEV-ING-FV DR cta 117 Ingresos = ₡7,500" "7500" "$DR_117"

  # DR 127 IVA (7500 × 13% = 975)
  DR_127=$(jq '[.lines[] | select(.idAccount == 127) | .debitAmount] | add // 0' "$TEMP_RESPONSE")
  assert_float_eq "DEV-ING-FV DR cta 127 IVA = ₡975" "975" "$DR_127"

  # CR 106 Caja (5085... espera 8475 total)
  CR_106=$(jq '[.lines[] | select(.idAccount == 106) | .creditAmount] | add // 0' "$TEMP_RESPONSE")
  assert_float_eq "DEV-ING-FV CR cta 106 Caja = ₡8,475" "8475" "$CR_106"
else
  log_warn "idEntryDevIng no disponible en resultado — omitiendo verificación DEV-ING-FV"
fi

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 7 — AJUSTE DE INVENTARIO (regalía 2 frascos)
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 7 — Ajuste de Inventario / Regalía (id=$ID_ADJUSTMENT)"

api_get "/inventory-adjustments/${ID_ADJUSTMENT}.json"
assert_200 "GET /inventory-adjustments/${ID_ADJUSTMENT}.json"

ADJ_STATUS=$(jq_field '.statusAdjustment // empty' | grep -v null | head -1)
ADJ_DELTA=$(jq_field  '.inventoryAdjustmentLines[0].quantityDelta // .lines[0].quantityDelta // empty' | grep -v null | head -1)
ADJ_ENTRY=$(jq_field  '.idAccountingEntry // empty' | grep -v null | head -1)

assert_eq       "ajuste statusAdjustment = Confirmado" "Confirmado" "$ADJ_STATUS"
assert_float_eq "ajuste línea delta = -2"              "-2"         "${ADJ_DELTA:-0}"
assert_gte      "ajuste tiene idAccountingEntry"       "1"          "${ADJ_ENTRY:-0}"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 8 — ÓRDENES DE PRODUCCIÓN (acumulado del período)
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 8 — Órdenes de Producción del período $ID_FISCAL_PERIOD"

api_get "/production-orders/by-period/${ID_FISCAL_PERIOD}.json"
assert_200 "GET /production-orders/by-period/${ID_FISCAL_PERIOD}.json"
PO_COUNT=$(jq_field 'length')
assert_gte "al menos 1 OP en el período" "1" "$PO_COUNT"

PO_COMPLETADAS=$(jq_field '[.[] | select(.statusProductionOrder == "Completado")] | length')
assert_gte "al menos 1 OP Completada en el período" "1" "$PO_COMPLETADAS"
log_info "  Total OPs en período: $PO_COUNT  |  Completadas: $PO_COMPLETADAS"

# La OP del caso produce idProduct=6 (PT)
PO_PRODUCT6=$(jq_field "[.[] | select(.lines[0].idProduct == 6)] | length")
assert_gte "al menos 1 OP con idProduct=6 (PT)" "1" "${PO_PRODUCT6:-0}"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 9 — TABLA DE CONCILIACIÓN DE INVENTARIO
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 9 — Tabla de Conciliación de Inventario PT"

printf "\n"
printf "  ${BOLD}%-40s %10s %12s %14s${NC}\n" "CONCEPTO" "UNIDADES" "C.UNIT" "TOTAL"
printf "  %-40s %10s %12s %14s\n" \
  "$(printf '%.0s─' {1..40})" "$(printf '%.0s─' {1..10})" \
  "$(printf '%.0s─' {1..12})" "$(printf '%.0s─' {1..14})"
printf "  %-40s %10s %12s %14s\n" "(+) Producción 100 frascos"     "+100"  "₡526" "₡52,600"
printf "  %-40s %10s %12s %14s\n" "(-) Venta 30 frascos"           " -30"  "₡526" "-₡15,780"
printf "  %-40s %10s %12s %14s\n" "(+) Devolución parcial 5 fracs" "  +5"  "₡526"  "₡2,630"
printf "  %-40s %10s %12s %14s\n" "(-) Regalía 2 frascos"          "  -2"  "₡526"  "-₡1,052"
printf "  %-40s %10s %12s %14s\n" \
  "$(printf '%.0s─' {1..40})" "$(printf '%.0s─' {1..10})" \
  "$(printf '%.0s─' {1..12})" "$(printf '%.0s─' {1..14})"
printf "  ${BOLD}%-40s %10s %12s %14s${NC}\n" "SALDO FINAL PT" "73" "₡526" "₡38,398"
printf "\n"

# Verificación final de stock PT
api_get "/inventory-lots/stock/6.json"
STOCK_PT_CHECK=$(cat "$TEMP_RESPONSE" | tr -d '[:space:]')
assert_float_eq "Stock PT físico final = 73 frascos" "$EXP_STOCK_PT_FINAL" "$STOCK_PT_CHECK"

# Costo en libros: 73 × 526
api_get "/inventory-lots/${ID_LOT_PT}.json"
LOT_COST_FINAL=$(jq_field '.unitCost // empty' | grep -v null | head -1)
LOT_QTY_FINAL=$(jq_field  '.quantityAvailable // empty' | grep -v null | head -1)
if [[ -n "$LOT_COST_FINAL" && -n "$LOT_QTY_FINAL" ]]; then
  COSTO_LIBROS=$(awk "BEGIN {printf \"%.2f\", $LOT_QTY_FINAL * $LOT_COST_FINAL}")
  assert_float_eq "Costo en libros PT = 73 × ₡526 = ₡38,398" "38398" "$COSTO_LIBROS"
fi

# MP todos consumidos
for PROD_ID in 2 3 4 5; do
  api_get "/inventory-lots/stock/${PROD_ID}.json"
  STMP=$(cat "$TEMP_RESPONSE" | tr -d '[:space:]')
  assert_float_eq "Stock MP idProduct=${PROD_ID} = 0 (consumido)" "0" "${STMP:-999}"
done

# ═══════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ═══════════════════════════════════════════════════════════════════════════════

echo ""
printf "${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"

if [[ $CHECKS_FAIL -eq 0 ]]; then
  printf "${BOLD}${GREEN}║   ✅  TODAS LAS VERIFICACIONES PASARON             ║${NC}\n"
else
  printf "${BOLD}${RED}║   ⚠   ALGUNAS VERIFICACIONES FALLARON              ║${NC}\n"
fi

printf "${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"
printf "  ${GREEN}✅  Exitosas : %d${NC}\n" "$CHECKS_OK"
if [[ $CHECKS_FAIL -gt 0 ]]; then
  printf "  ${RED}❌  Fallidas : %d${NC}\n" "$CHECKS_FAIL"
fi
echo ""

{
  echo ""
  echo "# ── RESUMEN ────────────────────────────────────────────────────────"
  echo "  Verificaciones exitosas : $CHECKS_OK"
  echo "  Verificaciones fallidas : $CHECKS_FAIL"
} >> "$OUTPUT_FILE"
printf "  Reporte guardado en: %s\n\n" "$OUTPUT_FILE"

[[ $CHECKS_FAIL -eq 0 ]]
