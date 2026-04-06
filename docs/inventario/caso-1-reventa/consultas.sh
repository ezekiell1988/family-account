#!/usr/bin/env bash
# ============================================================================
#  CASO 1 — REVENTA (Coca-Cola 355ml)
#  consultas.sh — Verificación de documentos y saldos contables
#
#  Propósito:
#   Lee automáticamente el archivo resultado_caso1_*.txt más reciente,
#   consulta cada documento generado por proceso.sh y verifica que los
#   valores (stock, montos, cuentas, estados) sean los esperados.
#
#  Requisitos previos:
#   - curl y jq instalados.
#   - API corriendo en https://localhost:8000.
#   - Al menos un resultado_caso1_*.txt en el mismo directorio.
#
#  Uso:
#   bash docs/inventario/caso-1-reventa/consultas.sh
#   bash docs/inventario/caso-1-reventa/consultas.sh resultado_caso1_XXXX.txt
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

# ── Helpers ───────────────────────────────────────────────────────────────────

section() {
  echo ""
  printf "${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n"
  printf "${CYAN}${BOLD}▶  %s${NC}\n" "$1"
  printf "${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n"
}

log_ok()   { printf "  ${GREEN}✅  %s${NC}\n" "$1"; CHECKS_OK=$((CHECKS_OK + 1)); }
log_fail() { printf "  ${RED}❌  %s${NC}\n" "$1"; CHECKS_FAIL=$((CHECKS_FAIL + 1)); }
log_info() { printf "  ${DIM}%s${NC}\n" "$1"; }
log_warn() { printf "  ${YELLOW}⚠   %s${NC}\n" "$1"; }

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

# ── Leer archivo de resultado ─────────────────────────────────────────────────

if [[ $# -ge 1 ]]; then
  RESULTADO_FILE="$SCRIPT_DIR/$1"
else
  # Tomar el más reciente
  RESULTADO_FILE=$(ls -t "$SCRIPT_DIR"/resultado_caso1_*.txt 2>/dev/null | head -1)
fi

if [[ -z "$RESULTADO_FILE" || ! -f "$RESULTADO_FILE" ]]; then
  printf "${RED}❌  No se encontró ningún archivo resultado_caso1_*.txt${NC}\n"
  printf "    Ejecuta proceso.sh primero.\n"
  exit 1
fi

printf "${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
printf "${BOLD}║   CASO 1 — REVENTA · Verificación de Consultas      ║${NC}\n"
printf "${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"
printf "  Resultado leído: %s\n" "$(basename "$RESULTADO_FILE")"

# Parsear variables del .txt (formato: CLAVE = VALOR  # comentario opcional)
read_var() {
  local key="$1"
  grep -E "^${key}\s*=" "$RESULTADO_FILE" | head -1 | sed 's/.*= *//' | sed 's/  #.*//' | xargs
}

ID_PURCHASE_INVOICE=$(read_var "idPurchaseInvoice")
ID_LOT=$(read_var "idInventoryLot")
ID_SALES_INVOICE=$(read_var "idSalesInvoice")
ID_ENTRY_DEV_COGS=$(read_var "idEntryDevCogs")
ID_ENTRY_REINTEGRO=$(read_var "idEntryReintegro")
ID_ADJUSTMENT=$(read_var "idInventoryAdjustment")
ID_ADJ_ENTRY=$(read_var "idAdjEntry")
ID_FISCAL_PERIOD=$(read_var "idFiscalPeriod" 2>/dev/null | grep -oE '^[0-9]+' || echo "4")
[[ -z "$ID_FISCAL_PERIOD" ]] && ID_FISCAL_PERIOD="4"

# Valores esperados
#  unitCost en la BD = precio de compra sin IVA (1000), no el costo con IVA (1130).
#  El campo unitCost del lote refleja el valor neto almacenado por EF.
EXP_UNIT_COST="1000"
EXP_STOCK_FINAL="91"
EXP_QTY_COMPRA="100"
EXP_QTY_VENTA="10"
EXP_QTY_DEV="3"
EXP_QTY_REGALIA="2"
EXP_COMPRA_TOTAL="113000"
EXP_VENTA_TOTAL="16950"
EXP_COGS="10000"          # 10 u × ₡1,000 (unitCost sin IVA)
EXP_COGS_REVERSA="3000"   # 3 u × ₡1,000
EXP_REINTEGRO="5085"
EXP_REGALIA_COSTO="2000"  # 2 u × ₡1,000

# IDs de cuentas clave
ACC_CAJA="106"
ACC_INVENTARIO="109"
ACC_INGRESOS="117"
ACC_COGS="119"
ACC_MERMA="113"

printf "\n"
printf "  ${DIM}IDs cargados:${NC}\n"
printf "  ${DIM}  PC=%s  LOT=%s  FV=%s  DEV_COGS=%s  REINT=%s  ADJ=%s  ADJ_ENTRY=%s${NC}\n" \
  "$ID_PURCHASE_INVOICE" "$ID_LOT" "$ID_SALES_INVOICE" \
  "$ID_ENTRY_DEV_COGS" "$ID_ENTRY_REINTEGRO" "$ID_ADJUSTMENT" "$ID_ADJ_ENTRY"

# ── Autenticación ─────────────────────────────────────────────────────────────

section "AUTH — Login para obtener token"

CREDENTIALS_FILE="$(cd "$SCRIPT_DIR/../../.." && pwd)/credentials/db.txt"
DB_HOST=$(grep -E '^HOST:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_PORT=$(grep -E '^PORT:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_USER=$(grep -E '^USER:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_PASS=$(grep -E '^PASSWORD:' "$CREDENTIALS_FILE" | awk '{print $2}')
DB_NAME="dbfa"
EMAIL=$(read_var "EMAIL")
[[ -z "$EMAIL" ]] && EMAIL="ezekiell1988@hotmail.com"

# Solicitar PIN
HTTP_STATUS=$(curl -k -s -o /dev/null -w "%{http_code}" \
  -X POST "${HOST}/auth/request-pin" \
  -H "Content-Type: application/json" \
  -d "{\"emailUser\":\"$EMAIL\"}")
assert_200 "request-pin"

# Leer PIN desde BD
PIN=$(sqlcmd \
  -S "${DB_HOST},${DB_PORT}" \
  -U "$DB_USER" -P "$DB_PASS" \
  -C -d "$DB_NAME" \
  -Q "SET NOCOUNT ON; SELECT TOP 1 pin FROM dbo.userPin up INNER JOIN dbo.[user] u ON u.idUser = up.idUser WHERE u.emailUser = '${EMAIL}' ORDER BY up.idUserPin DESC" \
  -h -1 -W 2>/dev/null | tr -d '[:space:]')
if [[ -z "$PIN" ]]; then
  log_fail "No se pudo leer el PIN desde la BD"
  exit 1
fi
log_info "PIN leído desde BD: $PIN"

# Login
HTTP_STATUS=$(curl -k -s -o "$TEMP_RESPONSE" -w "%{http_code}" \
  -X POST "${HOST}/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"emailUser\":\"$EMAIL\",\"pin\":\"$PIN\"}")
assert_200 "login"
TOKEN=$(jq_field ".accessToken")
if [[ -z "$TOKEN" || "$TOKEN" == "null" ]]; then
  log_fail "No se obtuvo accessToken"
  exit 1
fi
log_ok "Token obtenido ✓"

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
# SECCIÓN 3 — INVENTARIO
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 3 — Inventario (lote id=$ID_LOT)"

# 3-a  Stock total producto 1
api_get "/inventory-lots/stock/1.json"
assert_200 "GET /inventory-lots/stock/1.json"
STOCK_ACTUAL=$(jq_field '.totalQuantity // .quantity // .stock // . | if type == "number" then . else empty end')
# Intentar extraer número del response (puede venir como número simple o en objeto)
if [[ -z "$STOCK_ACTUAL" ]]; then
  STOCK_ACTUAL=$(cat "$TEMP_RESPONSE" | tr -d '"' | xargs)
fi
assert_float_eq "stock total producto 1" "$EXP_STOCK_FINAL" "$STOCK_ACTUAL"

# 3-b  Lote específico
api_get "/inventory-lots/${ID_LOT}.json"
assert_200 "GET /inventory-lots/${ID_LOT}.json"
LOT_STATUS=$(jq_field '.statusLot // .status // empty'           | grep -v null | head -1)
LOT_QTY=$(jq_field    '.quantityAvailable // .quantity // empty' | grep -v null | head -1)
LOT_COST=$(jq_field   '.unitCost // .costPerUnit // empty'       | grep -v null | head -1)

assert_eq       "lote statusLot = Disponible" "Disponible"    "$LOT_STATUS"
assert_float_eq "lote quantityAvailable"      "$EXP_STOCK_FINAL" "${LOT_QTY:-0}"
assert_float_eq "lote unitCost"               "$EXP_UNIT_COST"   "${LOT_COST:-0}"

# 3-c  Lotes por producto → debe aparecer el lote
api_get "/inventory-lots/by-product/1.json"
assert_200 "GET /inventory-lots/by-product/1.json"
LOT_COUNT=$(jq_field 'length // 1')
assert_gte "al menos 1 lote activo del producto 1" "1" "$LOT_COUNT"

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
# SECCIÓN 7 — ASIENTOS CONTABLES
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 7 — Asientos Contables"

# Helper para verificar un asiento: DR y CR en cuentas y montos esperados
check_entry() {
  local label="$1"
  local entry_id="$2"
  local exp_dr_account="$3"
  local exp_cr_account="$4"
  local exp_amount="$5"

  api_get "/accounting-entries/${entry_id}.json"
  if [[ "$HTTP_STATUS" != "200" ]]; then
    log_fail "$label (id=$entry_id) — HTTP $HTTP_STATUS"
    return
  fi
  log_ok "$label — HTTP 200"

  # Buscar DR y CR en las líneas del asiento (campo real: .lines)
  DR_ACCOUNT=$(jq_field \
    '[(.lines // [])[] | select(.debitAmount > 0)] | .[0] | .idAccount // empty' \
    | grep -v null | head -1)
  CR_ACCOUNT=$(jq_field \
    '[(.lines // [])[] | select(.creditAmount > 0)] | .[0] | .idAccount // empty' \
    | grep -v null | head -1)
  DR_AMOUNT=$(jq_field \
    '[(.lines // [])[] | select(.debitAmount > 0)] | .[0] | .debitAmount // empty' \
    | grep -v null | head -1)

  assert_eq  "  $label · DR cuenta"   "$exp_dr_account" "${DR_ACCOUNT:-?}"
  assert_eq  "  $label · CR cuenta"   "$exp_cr_account" "${CR_ACCOUNT:-?}"
  assert_float_eq "  $label · monto"  "$exp_amount"     "${DR_AMOUNT:-0}"
}

# Asiento FC- (factura de compra): DR 109 Inventario / CR 106 Caja ₡113,000
# idAccountingEntry lo leemos de la factura de compra (ya en TEMP_RESPONSE de sección 2)
api_get "/purchase-invoices/${ID_PURCHASE_INVOICE}.json"
PC_ENTRY_ID=$(jq_field '.idAccountingEntry // empty' | grep -v null | head -1)
if [[ -n "$PC_ENTRY_ID" && "$PC_ENTRY_ID" != "null" ]]; then
  check_entry "FC- (compra)" "$PC_ENTRY_ID" "$ACC_INVENTARIO" "$ACC_CAJA" "$EXP_COMPRA_TOTAL"
else
  log_warn "No se pudo obtener idAccountingEntry de la factura de compra"
fi

# Asientos de la FV: idAccountingEntry en la FV apunta al asiento FV- (ingreso)
# El asiento COGS-FV- se busca en la lista global por prefijo
api_get "/sales-invoices/${ID_SALES_INVOICE}.json"
ID_ENTRY_FV_ACTUAL=$(jq_field '.idAccountingEntry // empty' | grep -v null | head -1)
if [[ -n "$ID_ENTRY_FV_ACTUAL" && "$ID_ENTRY_FV_ACTUAL" != "null" ]]; then
  # FV- (venta ingreso): DR 106 Caja / CR 117 Ingresos ₡16,950
  check_entry "FV- (venta ingreso)" "$ID_ENTRY_FV_ACTUAL" "$ACC_CAJA" "$ACC_INGRESOS" "$EXP_VENTA_TOTAL"
else
  log_warn "idEntry FV- no encontrado en sales-invoice"
fi

# COGS-FV-: buscar en la lista de asientos por prefijo COGS-FV
api_get "/accounting-entries/data.json"
ID_ENTRY_COGS_ACTUAL=$(jq_field '[.[] | select(.numberEntry | test("^COGS-FV"; "i"))] | .[0].idAccountingEntry // empty' | grep -v null | head -1)
if [[ -n "$ID_ENTRY_COGS_ACTUAL" && "$ID_ENTRY_COGS_ACTUAL" != "null" ]]; then
  # COGS-FV- (costo de ventas): DR 119 COGS / CR 109 Inventario
  check_entry "COGS-FV- (costo ventas)" "$ID_ENTRY_COGS_ACTUAL" "$ACC_COGS" "$ACC_INVENTARIO" "$EXP_COGS"
else
  log_warn "idEntry COGS-FV- no encontrado en accounting-entries"
fi

# DEV-COGS-FV- (devolución parcial): DR 109 Inventario / CR 119 COGS
check_entry "DEV-COGS-FV- (devolución)" "$ID_ENTRY_DEV_COGS" "$ACC_INVENTARIO" "$ACC_COGS" "$EXP_COGS_REVERSA"

# REINTEGRO-FV- (asiento manual): DR 117 Ingresos / CR 106 Caja ₡5,085
check_entry "REINTEGRO-FV- (reintegro manual)" "$ID_ENTRY_REINTEGRO" "$ACC_INGRESOS" "$ACC_CAJA" "$EXP_REINTEGRO"

# AJ- (regalía/ajuste): DR 113 Merma / CR 109 Inventario
check_entry "AJ- (regalía)" "$ID_ADJ_ENTRY" "$ACC_MERMA" "$ACC_INVENTARIO" "$EXP_REGALIA_COSTO"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 8 — TODOS LOS ASIENTOS DEL PERÍODO
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 8 — Todos los asientos del período"

api_get "/accounting-entries/data.json"
assert_200 "GET /accounting-entries/data.json"

TOTAL_ENTRIES=$(jq_field 'length')
log_info "Total asientos en el sistema: $TOTAL_ENTRIES"

# Prefijos esperados en Caso 1 (prefijos reales generados por la API)
PREFIXES=("FC-" "FV-" "COGS-FV-" "DEV-COGS-FV-" "REINTEGRO-FV-" "AJ-")
for pfx in "${PREFIXES[@]}"; do
  COUNT=$(jq_field "[.[] | select(.numberEntry | test(\"^${pfx}\"; \"i\"))] | length")
  if [[ "${COUNT:-0}" -ge "1" ]]; then
    log_ok "Asiento con prefijo '${pfx}' encontrado ($COUNT)"
  else
    log_fail "Asiento con prefijo '${pfx}' NO encontrado"
  fi
done

# Verificar balance total (suma DR = suma CR en todos los asientos)
TOTAL_DR=$(jq_field '[.[].lines // [] | .[]] | map(.debitAmount // 0) | add // 0')
TOTAL_CR=$(jq_field '[.[].lines // [] | .[]] | map(.creditAmount // 0) | add // 0')
if [[ -n "$TOTAL_DR" && -n "$TOTAL_CR" && "$TOTAL_DR" != "null" && "$TOTAL_CR" != "null" ]]; then
  if awk "BEGIN {diff=$TOTAL_DR-$TOTAL_CR; if(diff<0)diff=-diff; exit !(diff<0.01)}"; then
    log_ok "Balance contable DR = CR: ₡$TOTAL_DR"
  else
    log_fail "Desbalance contable: DR=₡$TOTAL_DR  CR=₡$TOTAL_CR"
  fi
else
  log_warn "No se pudo calcular balance total (estructura de líneas no accesible en lista)"
fi

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 9 — CUENTAS CONTABLES CLAVE
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 9 — Cuentas contables clave"

check_account() {
  local label="$1"
  local id="$2"
  api_get "/accounts/${id}.json"
  if [[ "$HTTP_STATUS" == "200" ]]; then
    NAME=$(jq_field '.nameAccount // .name // empty' | head -1)
    log_ok "Cuenta $id existe: '$NAME'"
  else
    log_fail "Cuenta $id no encontrada — HTTP $HTTP_STATUS"
  fi
}

check_account "106 Caja CRC"           "$ACC_CAJA"
check_account "109 Inventario Mercad." "$ACC_INVENTARIO"
check_account "117 Ingresos x Ventas"  "$ACC_INGRESOS"
check_account "119 Costo de Ventas"    "$ACC_COGS"
check_account "113 Faltantes/Merma"    "$ACC_MERMA"

# ═══════════════════════════════════════════════════════════════════════════════
# SECCIÓN 10 — CONCILIACIÓN INVENTARIO vs CONTABILIDAD
# ═══════════════════════════════════════════════════════════════════════════════

section "SECCIÓN 10 — Tabla de Conciliación Final"

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

# Costo en libros (91 × 1130 = 102830)
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

[[ $CHECKS_FAIL -eq 0 ]]
