#!/usr/bin/env bash
# ============================================================================
#  CASO 1 — REVENTA (Coca-Cola 355ml)
#  02-verificar-documentos.sh — Verificación de Documentos e Inventario
#
#  Propósito:
#   Descubre automáticamente los IDs de los documentos del Caso 1 consultando
#   los asientos contables y la API, sin depender de resultado_caso1_*.txt.
#   Verifica que los documentos se generaron correctamente y el inventario es
#   correcto. Genera verificacion_docs_caso1_*.txt con el reporte completo.
#   El análisis de cuentas contables (T-accounts, saldos DR/CR) se hace en
#   03-analizar-cuentas-contables.sh.
#
#  Requisitos previos:
#   - curl, jq y sqlcmd instalados.
#   - API corriendo en https://localhost:8000.
#   - Credenciales en credentials/db.txt.
#
#  Uso:
#   bash docs/inventario/caso-1-reventa/02-verificar-documentos.sh
# ============================================================================

set -uo pipefail

# ── Rutas ─────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TEMP_RESPONSE="/tmp/fa_caso1_consultas.json"
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

# ── Archivo de reporte ─────────────────────────────────────────────────────
RUN_TS="$(date '+%Y-%m-%d_%H-%M-%S')"
OUTPUT_FILE="$SCRIPT_DIR/verificacion_docs_caso1_${RUN_TS}.txt"

# ── Helpers ────────────────────────────────────────────────────────────────

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
    log_fail "$label — esperado HTTP 200, recibido $HTTP_STATUS"
  fi
}

# Verifica que un campo tenga el valor esperado.
assert_eq() {
  local label="$1"
  local expected="$2"
  local actual="$3"
  if [[ "$actual" == "$expected" ]]; then
    log_ok "$label: $actual"
  else
    log_fail "$label: esperado='$expected'  real='$actual'"
  fi
}

# Verifica que un número sea mayor o igual al esperado.
assert_gte() {
  local label="$1"
  local expected="$2"
  local actual="$3"
  # Comparar como floats usando awk
  if awk "BEGIN {exit !($actual >= $expected)}"; then
    log_ok "$label: $actual (>= $expected)"
  else
    log_fail "$label: esperado >= $expected  real='$actual'"
  fi
}

# Verifica que un número float sea aprox igual (±0.01).
assert_float_eq() {
  local label="$1"
  local expected="$2"
  local actual="$3"
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
#  unitCost en la BD = precio de compra sin IVA (1000), no el costo con IVA.
EXP_UNIT_COST="1000"
EXP_STOCK_FINAL="91"
EXP_QTY_RESERVED="0"
EXP_QTY_NET="91"
EXP_STATUS_LOT="Disponible"
EXP_SOURCE_TYPE="Compra"
ID_WAREHOUSE="1"

# ── Descubrir IDs desde asientos contables ─────────────────────────────────────
TEMP_ENTRIES="/tmp/fa_caso1_entries_disco.json"
curl -k -s -H "Authorization: Bearer $TOKEN" "${HOST}/accounting-entries/data.json" > "$TEMP_ENTRIES"

ID_PURCHASE_INVOICE=$(jq -r '[.[] | select(.numberEntry | startswith("FC-"))] | sort_by(.idAccountingEntry) | last | .idOriginRecord // empty' "$TEMP_ENTRIES")
ID_SALES_INVOICE=$(jq -r '[.[] | select(.numberEntry | startswith("FV-"))] | sort_by(.idAccountingEntry) | last | .idOriginRecord // empty' "$TEMP_ENTRIES")
ID_ADJUSTMENT=$(jq -r '[.[] | select(.numberEntry | startswith("AJ-"))] | sort_by(.idAccountingEntry) | last | .idOriginRecord // empty' "$TEMP_ENTRIES")
ID_ENTRY_DEV_ING=$(jq -r '[.[] | select(.numberEntry | startswith("DEV-ING-FV-"))] | sort_by(.idAccountingEntry) | last | .idAccountingEntry // empty' "$TEMP_ENTRIES")
rm -f "$TEMP_ENTRIES"

# Lote del producto 1 — el más reciente vinculado a la FC de compra
_tmp_lot="/tmp/fa_caso1_lot_disco.json"
curl -k -s -H "Authorization: Bearer $TOKEN" \
  "${HOST}/inventory-lots/by-product/1.json?idWarehouse=${ID_WAREHOUSE}" > "$_tmp_lot"
ID_LOT=$(jq -r "[.[] | select(.idPurchaseInvoice == ${ID_PURCHASE_INVOICE})] | sort_by(.idInventoryLot) | last | .idInventoryLot // empty" "$_tmp_lot")
rm -f "$_tmp_lot"

# Período fiscal — por defecto 4
ID_FISCAL_PERIOD="4"

# ── Cabecera ───────────────────────────────────────────────────────────────────
printf "${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
printf "${BOLD}║   CASO 1 — REVENTA · Verificación de Documentos     ║${NC}\n"
printf "${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"

{
  echo "# =================================================================="
  echo "#  CASO 1 — REVENTA · Verificación de Documentos e Inventario"
  echo "#  Generado: $(date '+%Y-%m-%d %H:%M:%S')"
  echo "#  IDs descubiertos desde la API (sin resultado_caso1_*.txt)"
  echo "# =================================================================="
} > "$OUTPUT_FILE"

printf "\n"
printf "  ${DIM}IDs descubiertos:${NC}\n"
printf "  ${DIM}  PC=%s  LOT=%s  FV=%s  ADJ=%s  EntryDevIng=%s${NC}\n" \
  "$ID_PURCHASE_INVOICE" "$ID_LOT" "$ID_SALES_INVOICE" "$ID_ADJUSTMENT" "${ID_ENTRY_DEV_ING:-(no encontrado)}"
printf "  ${DIM}  Período fiscal: %s${NC}\n" "$ID_FISCAL_PERIOD"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 1 — CATÁLOGO
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 1 — Catálogo (configuración seed)"

# 1-a  Producto 1
api_get "/products/1.json"
assert_200 "GET /products/1.json"
assert_eq "product.idProduct"     "1"     "$(jq_field '.idProduct')"
assert_eq "product.idProductType" "4"     "$(jq_field '.idProductType')"
assert_eq "product.idUnit"        "1"     "$(jq_field '.idUnit')"

# 1-b  Tipos de factura de compra (idPurchaseInvoiceType=1 debe existir)
api_get "/purchase-invoice-types/data.json"
assert_200 "GET /purchase-invoice-types/data.json"
COUNT=$(jq_field '[.[] | select(.idPurchaseInvoiceType == 1)] | length')
assert_eq "purchase-invoice-type id=1 existe" "1" "$COUNT"

# 1-c  Tipos de factura de venta (idSalesInvoiceType=1 debe existir)
api_get "/sales-invoice-types/data.json"
assert_200 "GET /sales-invoice-types/data.json"
COUNT=$(jq_field '[.[] | select(.idSalesInvoiceType == 1)] | length')
assert_eq "sales-invoice-type id=1 existe" "1" "$COUNT"

# 1-d  Tipos de ajuste (idInventoryAdjustmentType=1 debe existir)
api_get "/inventory-adjustment-types/data.json"
assert_200 "GET /inventory-adjustment-types/data.json"
COUNT=$(jq_field '[.[] | select(.idInventoryAdjustmentType == 1)] | length')
assert_eq "inventory-adjustment-type id=1 existe" "1" "$COUNT"

# 1-e  ProductAccounts del producto 1 → debe estar vacío (se borró en PASO 4)
api_get "/product-accounts/by-product/1.json"
assert_200 "GET /product-accounts/by-product/1.json"
PA_COUNT=$(jq_field 'length')
assert_eq "product-accounts vacío post-DELETE" "0" "$PA_COUNT"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 2 — FACTURA DE COMPRA
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 2 — Factura de Compra (id=$ID_PURCHASE_INVOICE)"

api_get "/purchase-invoices/${ID_PURCHASE_INVOICE}.json"
assert_200 "GET /purchase-invoices/${ID_PURCHASE_INVOICE}.json"

PC_STATUS=$(jq_field '.statusInvoice')
PC_TOTAL=$(jq_field '.totalAmount // .total // .totalWithTax // empty' | grep -v null | head -1)
PC_ENTRY=$(jq_field '.idAccountingEntry // .accountingEntryId // empty' | grep -v null | head -1)
PC_LOT_NUMBER=$(jq_field '.lines[0].lotNumber // .purchaseInvoiceLines[0].lotNumber // empty' | grep -v null | head -1)

assert_eq  "PC statusInvoice = Confirmado"    "Confirmado" "$PC_STATUS"
assert_gte "PC tiene idAccountingEntry"       "1"          "${PC_ENTRY:-0}"

log_info "  PC número de lote en línea: ${PC_LOT_NUMBER:-'(campo no expuesto directamente)'}"
log_info "  PC total (si expuesto): ${PC_TOTAL:-'(verificar en asiento PC-)'}"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 3 — INVENTARIO COMPLETO
# ═══════════════════════════════════════════════════════════════════════════════

# 3-a  Stock total global del producto 1
# ─────────────────────────────────────────────────────────────────────────────
section "SECCIÓN 3a — Stock total global del producto 1"

api_get "/inventory-lots/stock/1.json"
assert_200 "GET /inventory-lots/stock/1.json"
STOCK_ACTUAL=$(jq_field '. | if type == "number" then . else empty end')
[[ -z "$STOCK_ACTUAL" ]] && STOCK_ACTUAL=$(cat "$TEMP_RESPONSE" | tr -d '"' | xargs)
assert_float_eq "stock total = $EXP_STOCK_FINAL u." "$EXP_STOCK_FINAL" "$STOCK_ACTUAL"
log_info "  Fórmula: 100 (compra) − 10 (venta) + 3 (devolución) − 2 (regalía) = 91"

# 3-b  Lote específico — todos los campos relevantes
# ─────────────────────────────────────────────────────────────────────────────
section "SECCIÓN 3b — Lote específico (id=$ID_LOT) — detalle completo"

api_get "/inventory-lots/${ID_LOT}.json"
assert_200 "GET /inventory-lots/${ID_LOT}.json"

LOT_STATUS=$(jq_field '.statusLot'          | grep -v null | head -1)
LOT_QTY=$(jq_field    '.quantityAvailable'  | grep -v null | head -1)
LOT_RESERVED=$(jq_field '.quantityReserved' | grep -v null | head -1)
LOT_NET=$(jq_field    '.quantityAvailableNet' | grep -v null | head -1)
LOT_COST=$(jq_field   '.unitCost'           | grep -v null | head -1)
LOT_SOURCE=$(jq_field '.sourceType'         | grep -v null | head -1)
LOT_PROD=$(jq_field   '.idProduct'          | grep -v null | head -1)
LOT_WH=$(jq_field     '.idWarehouse'        | grep -v null | head -1)
LOT_WH_NAME=$(jq_field '.nameWarehouse'     | grep -v null | head -1)
LOT_PC=$(jq_field     '.idPurchaseInvoice'  | grep -v null | head -1)

assert_eq       "lote statusLot"            "$EXP_STATUS_LOT"  "$LOT_STATUS"
assert_float_eq "lote quantityAvailable"    "$EXP_STOCK_FINAL" "${LOT_QTY:-0}"
assert_float_eq "lote quantityReserved"     "$EXP_QTY_RESERVED" "${LOT_RESERVED:-0}"
assert_float_eq "lote quantityAvailableNet" "$EXP_QTY_NET"     "${LOT_NET:-0}"
assert_float_eq "lote unitCost"             "$EXP_UNIT_COST"   "${LOT_COST:-0}"
assert_eq       "lote sourceType = Compra"  "$EXP_SOURCE_TYPE" "$LOT_SOURCE"
assert_eq       "lote idProduct = 1"        "1"                "$LOT_PROD"
assert_eq       "lote idWarehouse = $ID_WAREHOUSE" "$ID_WAREHOUSE" "$LOT_WH"
assert_gte      "lote nameWarehouse no vacío" "1" "${#LOT_WH_NAME}"
assert_eq       "lote origen = idPurchaseInvoice=$ID_PURCHASE_INVOICE" \
                "$ID_PURCHASE_INVOICE" "$LOT_PC"
log_info "  Almacén: ${LOT_WH_NAME:-?} (id=$LOT_WH)"

# 3-c  Lotes por producto — sin filtro (todos los almacenes)
# ─────────────────────────────────────────────────────────────────────────────
section "SECCIÓN 3c — Lotes por producto (todos los almacenes)"

api_get "/inventory-lots/by-product/1.json"
assert_200 "GET /inventory-lots/by-product/1.json"

LOT_COUNT=$(jq_field 'length')
assert_gte "al menos 1 lote activo del producto 1" "1" "$LOT_COUNT"

# Suma total desde la lista de lotes (debe coincidir con /stock/)
SUM_FROM_LIST=$(jq '[.[].quantityAvailable] | add // 0' "$TEMP_RESPONSE")
assert_float_eq "suma quantityAvailable en lista = stock total" "$EXP_STOCK_FINAL" "$SUM_FROM_LIST"
log_info "  Lotes encontrados: $LOT_COUNT  |  Suma quantityAvailable: $SUM_FROM_LIST"

# Orden FEFO: si hay fecha de vencimiento, el primero fue el más antiguo
FIRST_LOT_ID=$(jq_field '.[0].idInventoryLot')
assert_eq "lote[0] en lista = lote del caso" "$ID_LOT" "$FIRST_LOT_ID"

# 3-d  Lotes por producto filtrado por almacén 1
# ─────────────────────────────────────────────────────────────────────────────
section "SECCIÓN 3d — Lotes por almacén (idWarehouse=$ID_WAREHOUSE)"

api_get "/inventory-lots/by-product/1.json?idWarehouse=${ID_WAREHOUSE}"
assert_200 "GET /inventory-lots/by-product/1.json?idWarehouse=$ID_WAREHOUSE"

LOT_COUNT_WH=$(jq_field 'length')
assert_gte "al menos 1 lote en almacén $ID_WAREHOUSE" "1" "$LOT_COUNT_WH"

SUM_WH=$(jq '[.[].quantityAvailable] | add // 0' "$TEMP_RESPONSE")
assert_float_eq "stock en almacén $ID_WAREHOUSE = $EXP_STOCK_FINAL" "$EXP_STOCK_FINAL" "$SUM_WH"
log_info "  Lotes en almacén $ID_WAREHOUSE: $LOT_COUNT_WH  |  Suma: $SUM_WH"

# Todos los lotes deben pertenecer al almacén correcto
WRONG_WH=$(jq "[.[] | select(.idWarehouse != $ID_WAREHOUSE)] | length" "$TEMP_RESPONSE")
assert_eq "ningún lote en almacén diferente a $ID_WAREHOUSE" "0" "$WRONG_WH"

# 3-e  Verificar que almacén no existente devuelve lista vacía (no 404)
# ─────────────────────────────────────────────────────────────────────────────
section "SECCIÓN 3e — Almacén inexistente → lista vacía"

api_get "/inventory-lots/by-product/1.json?idWarehouse=9999"
assert_200 "GET /inventory-lots/by-product/1.json?idWarehouse=9999"
EMPTY_COUNT=$(jq_field 'length')
assert_eq "almacén 9999 devuelve 0 lotes" "0" "$EMPTY_COUNT"

# 3-f  Lote sugerido FEFO — sin filtro de almacén
# ─────────────────────────────────────────────────────────────────────────────
section "SECCIÓN 3f — Lote sugerido FEFO (sin filtro almacén)"

api_get "/inventory-lots/suggest/1.json"
assert_200 "GET /inventory-lots/suggest/1.json"

SUGGEST_ID=$(jq_field '.idInventoryLot'       | grep -v null | head -1)
SUGGEST_QTY=$(jq_field '.quantityAvailableNet' | grep -v null | head -1)
SUGGEST_STATUS=$(jq_field '.statusLot'         | grep -v null | head -1)

assert_eq       "lote sugerido = lote del caso"       "$ID_LOT"          "$SUGGEST_ID"
assert_eq       "lote sugerido statusLot = Disponible" "Disponible"       "$SUGGEST_STATUS"
assert_float_eq "lote sugerido quantityAvailableNet"   "$EXP_QTY_NET"    "${SUGGEST_QTY:-0}"
log_info "  FEFO sugerido: id=$SUGGEST_ID  disponibleNeto=$SUGGEST_QTY"

# 3-g  Lote sugerido FEFO — filtrado por almacén 1
# ─────────────────────────────────────────────────────────────────────────────
section "SECCIÓN 3g — Lote sugerido FEFO (almacén $ID_WAREHOUSE)"

api_get "/inventory-lots/suggest/1.json?idWarehouse=${ID_WAREHOUSE}"
assert_200 "GET /inventory-lots/suggest/1.json?idWarehouse=$ID_WAREHOUSE"

SUGGEST_WH_ID=$(jq_field '.idInventoryLot'  | grep -v null | head -1)
SUGGEST_WH_WH=$(jq_field '.idWarehouse'     | grep -v null | head -1)
assert_eq "lote sugerido almacén $ID_WAREHOUSE = lote del caso" "$ID_LOT" "$SUGGEST_WH_ID"
assert_eq "lote sugerido pertenece al almacén $ID_WAREHOUSE"    "$ID_WAREHOUSE" "$SUGGEST_WH_WH"

# 3-h  Lote sugerido en almacén inexistente → 404
# ─────────────────────────────────────────────────────────────────────────────
section "SECCIÓN 3h — Lote sugerido en almacén inexistente → 404"

api_get "/inventory-lots/suggest/1.json?idWarehouse=9999"
if [[ "$HTTP_STATUS" == "404" ]]; then
  log_ok "Almacén 9999 sin lotes → HTTP 404 ✓"
else
  log_fail "Almacén 9999 sin lotes → esperado HTTP 404, recibido $HTTP_STATUS"
fi

# 3-i  Catálogo de almacenes — el almacén del lote debe existir
# ─────────────────────────────────────────────────────────────────────────────
section "SECCIÓN 3i — Catálogo de almacenes"

api_get "/warehouses/data.json"
assert_200 "GET /warehouses/data.json"

WH_COUNT=$(jq_field 'length')
assert_gte "al menos 1 almacén registrado" "1" "$WH_COUNT"

WH_EXISTS=$(jq "[.[] | select(.idWarehouse == $ID_WAREHOUSE)] | length" "$TEMP_RESPONSE")
assert_eq "almacén id=$ID_WAREHOUSE existe en catálogo" "1" "$WH_EXISTS"

WH_NAME=$(jq_field "[.[] | select(.idWarehouse == $ID_WAREHOUSE)] | first | .nameWarehouse")
WH_ACTIVE=$(jq_field "[.[] | select(.idWarehouse == $ID_WAREHOUSE)] | first | .isActive")
assert_eq "almacén $ID_WAREHOUSE isActive = true" "true" "$WH_ACTIVE"
log_info  "  Almacén $ID_WAREHOUSE: '$WH_NAME'  activo=$WH_ACTIVE  total almacenes=$WH_COUNT"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 4 — FACTURA DE VENTA
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 4 — Factura de Venta (id=$ID_SALES_INVOICE)"

api_get "/sales-invoices/${ID_SALES_INVOICE}.json"
assert_200 "GET /sales-invoices/${ID_SALES_INVOICE}.json"

FV_STATUS=$(jq_field '.statusInvoice')
# FV expone un único idAccountingEntry (el de ingreso FV-)
ID_ENTRY_FV_FROM_FV=$(jq_field '.idAccountingEntry // empty' | grep -v null | head -1)

assert_eq  "FV statusInvoice = Confirmado"  "Confirmado" "$FV_STATUS"
assert_gte "FV tiene idAccountingEntry"      "1"          "${ID_ENTRY_FV_FROM_FV:-0}"
log_info   "  FV idAccountingEntry (ingreso FV-): ${ID_ENTRY_FV_FROM_FV:-?}"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 4b — GUARDIA DE DEVOLUCIÓN PARCIAL
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 4b — Guardia devolución parcial (cantidad > vendida → debe fallar)"

# PartialReturnAsync valida que quantity <= salesLine.QuantityBase.
# Se vendieron 10 cajas del lote; intentar devolver 11 debe ser rechazado con HTTP 422.
log_info "Probando guardia: devolver 11 cajas (>10 vendidas) desde lote $ID_LOT"

HTTP_STATUS_EXCESS=$(curl -k -s -o "$TEMP_RESPONSE" -w "%{http_code}" \
  -X POST "${HOST}/sales-invoices/${ID_SALES_INVOICE}/partial-return" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"dateReturn\":\"$(date '+%Y-%m-%d')\",\"descriptionReturn\":\"Test guardia — debe fallar\",\"refundMode\":\"EfectivoInmediato\",\"lines\":[{\"idInventoryLot\":${ID_LOT},\"quantity\":11,\"totalLineAmount\":16500}]}")

if [[ "$HTTP_STATUS_EXCESS" == "422" ]]; then
  ERR_MSG=$(jq -r '.error // empty' "$TEMP_RESPONSE" 2>/dev/null | head -1)
  log_ok  "Guardia exceso: HTTP 422 — devolver 11 cajas rechazado correctamente"
  [[ -n "$ERR_MSG" ]] && log_info "  Mensaje API: $ERR_MSG"
elif [[ "$HTTP_STATUS_EXCESS" == "200" ]]; then
  log_fail "Guardia exceso: HTTP 200 — la API ACEPTÓ 11 cajas (regresión Bug 4)"
  log_warn "  Verificar que PartialReturnAsync valida quantity <= salesLine.QuantityBase"
else
  log_fail "Guardia exceso: HTTP inesperado $HTTP_STATUS_EXCESS (esperado 422)"
fi

# ───────────────────────────────────────────────────────────────────────────────
section "SECCIÓN 4c — Asento DEV-ING-FV (reintegro automático de la devolución)"

if [[ -n "$ID_ENTRY_DEV_ING" && "$ID_ENTRY_DEV_ING" != "null" && "$ID_ENTRY_DEV_ING" != "0" ]]; then
  api_get "/accounting-entries/${ID_ENTRY_DEV_ING}.json"
  assert_200 "GET /accounting-entries/${ID_ENTRY_DEV_ING}.json"

  ENTRY_NUMBER=$(jq_field '.numberEntry'     | grep -v null | head -1)
  ENTRY_STATUS=$(jq_field '.statusEntry'     | grep -v null | head -1)
  ENTRY_MODULE=$(jq_field '.originModule'    | grep -v null | head -1)
  ENTRY_RECORD=$(jq_field '.idOriginRecord'  | grep -v null | head -1)

  # Número debe empezar con DEV-ING-FV-
  if [[ "$ENTRY_NUMBER" == DEV-ING-FV-* ]]; then
    log_ok "numberEntry empieza con DEV-ING-FV-: $ENTRY_NUMBER"
  else
    log_fail "numberEntry inesperado: '$ENTRY_NUMBER' (esperado DEV-ING-FV-...)"
  fi
  assert_eq "statusEntry = Publicado"              "Publicado"          "$ENTRY_STATUS"
  assert_eq "originModule = SalesReturnPartial"    "SalesReturnPartial" "$ENTRY_MODULE"
  assert_eq "idOriginRecord = idSalesInvoice"      "$ID_SALES_INVOICE"  "$ENTRY_RECORD"

  # Verificar montos por cuenta
  LINES_COUNT=$(jq_field '.lines | length')
  assert_gte "DEV-ING-FV tiene al menos 3 líneas (ingresos + IVA + caja)" "3" "$LINES_COUNT"

  TOTAL_DR=$(jq '[.lines[].debitAmount]  | add // 0' "$TEMP_RESPONSE")
  TOTAL_CR=$(jq '[.lines[].creditAmount] | add // 0' "$TEMP_RESPONSE")
  assert_float_eq "DEV-ING-FV ΣDR = ΣCR (partida doble)" "$TOTAL_DR" "$TOTAL_CR"
  assert_float_eq "DEV-ING-FV total = ₡5,085 (3 × ₡1,695)" "5085" "$TOTAL_DR"

  # DR cuenta 117 (ingresos netos 3 × 1500)
  DR_117=$(jq '[.lines[] | select(.idAccount == 117) | .debitAmount] | add // 0' "$TEMP_RESPONSE")
  assert_float_eq "DEV-ING-FV DR cta 117 Ingresos = ₡4,500" "4500" "$DR_117"

  # DR cuenta 127 (IVA revertido 4500 × 13%)
  DR_127=$(jq '[.lines[] | select(.idAccount == 127) | .debitAmount] | add // 0' "$TEMP_RESPONSE")
  assert_float_eq "DEV-ING-FV DR cta 127 IVA = ₡585" "585" "$DR_127"

  # CR cuenta 106 (salida caja)
  CR_106=$(jq '[.lines[] | select(.idAccount == 106) | .creditAmount] | add // 0' "$TEMP_RESPONSE")
  assert_float_eq "DEV-ING-FV CR cta 106 Caja = ₡5,085" "5085" "$CR_106"
else
  log_warn "idEntryDevIng no disponible en resultado — omitiendo verificación DEV-ING-FV"
  log_info "  Asegúrate de que el resultado_caso1_*.txt incluye 'idEntryDevIng'"
fi

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 5 — ÓRDENES DE PRODUCCIÓN (no aplica Caso 1)
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 5 — Órdenes de Producción (esperado: NINGUNA del producto 1)"

api_get "/production-orders/by-period/${ID_FISCAL_PERIOD}.json"
assert_200 "GET /production-orders/by-period/${ID_FISCAL_PERIOD}.json"
PO_PRODUCT1=$(jq_field '[.[] | select(.idProduct == 1)] | length' 2>/dev/null || echo "0")
if [[ "${PO_PRODUCT1:-0}" == "0" ]]; then
  log_ok "Sin órdenes de producción del producto 1 ✓ (Reventa pura)"
else
  log_fail "Existen ${PO_PRODUCT1} OPs del producto 1 — no debería en Caso 1"
fi

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 6 — AJUSTE DE INVENTARIO (regalía)
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 6 — Ajuste de Inventario / Regalía (id=$ID_ADJUSTMENT)"

api_get "/inventory-adjustments/${ID_ADJUSTMENT}.json"
assert_200 "GET /inventory-adjustments/${ID_ADJUSTMENT}.json"

ADJ_STATUS=$(jq_field '.statusAdjustment // .status // empty'  | grep -v null | head -1)
ADJ_DELTA=$(jq_field  '.inventoryAdjustmentLines[0].quantityDelta // .lines[0].quantityDelta // empty' | grep -v null | head -1)
ADJ_ENTRY=$(jq_field  '.idAccountingEntry // .accountingEntryId // empty'   | grep -v null | head -1)

assert_eq      "ajuste statusAdjustment = Confirmado" "Confirmado" "$ADJ_STATUS"
assert_float_eq "ajuste línea delta = -2"             "-2"         "${ADJ_DELTA:-0}"
assert_gte     "ajuste tiene idAccountingEntry"       "1"          "${ADJ_ENTRY:-0}"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 7 — CONCILIACIÓN DE INVENTARIO
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 7 — Tabla de Conciliación de Inventario"

printf "\n"
printf "  ${BOLD}%-35s %10s %12s %12s${NC}\n" "CONCEPTO" "UNIDADES" "C.UNIT" "TOTAL"
printf "  %-35s %10s %12s %12s\n" "$(printf '%.0s─' {1..35})" "$(printf '%.0s─' {1..10})" "$(printf '%.0s─' {1..12})" "$(printf '%.0s─' {1..12})"
printf "  %-35s %10s %12s %12s\n" "(+) Compra inicial"     "+100" "₡1,000" "₡100,000"
  printf "  %-35s %10s %12s %12s\n" "(-) Venta 10 cajas"      "-10" "₡1,000" "-₡10,000"
  printf "  %-35s %10s %12s %12s\n" "(+) Devolución parcial"   "+3" "₡1,000"   "₡3,000"
  printf "  %-35s %10s %12s %12s\n" "(-) Regalía 2 cajas"      "-2" "₡1,000"  "-₡2,000"
  printf "  %-35s %10s %12s %12s\n" "$(printf '%.0s─' {1..35})" "$(printf '%.0s─' {1..10})" "$(printf '%.0s─' {1..12})" "$(printf '%.0s─' {1..12})"
  printf "  ${BOLD}%-35s %10s %12s %12s${NC}\n" "SALDO FINAL" "91" "₡1,000" "₡91,000"
printf "\n"

# Verificación final de stock
api_get "/inventory-lots/stock/1.json"
STOCK_FINAL_CHECK=$(jq_field '.totalQuantity // .quantity // .stock // . | if type == "number" then . else empty end')
[[ -z "$STOCK_FINAL_CHECK" ]] && STOCK_FINAL_CHECK=$(cat "$TEMP_RESPONSE" | tr -d '"' | xargs)
assert_float_eq "Stock físico final = 91 u." "$EXP_STOCK_FINAL" "$STOCK_FINAL_CHECK"

# Costo en libros (91 × 1000 = 91000)
api_get "/inventory-lots/${ID_LOT}.json"
LOT_COST_FINAL=$(jq_field '.unitCost // .costPerUnit // empty' | grep -v null | head -1)
LOT_QTY_FINAL=$(jq_field  '.quantityAvailable // .quantity // empty' | grep -v null | head -1)
if [[ -n "$LOT_COST_FINAL" && -n "$LOT_QTY_FINAL" ]]; then
  COSTO_LIBROS=$(awk "BEGIN {printf \"%.2f\", $LOT_QTY_FINAL * $LOT_COST_FINAL}")
  assert_float_eq "Costo en libros = 91×₡1,000 = ₡91,000" "91000" "$COSTO_LIBROS"
fi

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
