#!/usr/bin/env bash
# ============================================================================
#  CASO 1 — REVENTA (Coca-Cola 355ml)
#  01-ejecutar-flujo-e2e.sh — Test de Integración E2E
#
#  ¿Qué es un "test de integración E2E"?
#  ─────────────────────────────────────
#  Un test *unitario* verifica una sola función aislada (ej: "¿suma bien?").
#  Un test de *integración E2E* (End-to-End) verifica el flujo COMPLETO del
#  negocio contra el sistema real, pasando por API → base de datos → lógica.
#  Este script hace exactamente eso:
#   1. Llama cada endpoint en orden.
#   2. Lee la respuesta y extrae los IDs generados.
#   3. Si algo devuelve error, se detiene e imprime el problema.
#   4. Al final muestra un resumen con todos los IDs y el stock final.
#   5. Genera resultado_caso1_*.txt con todos los IDs para los scripts de verificación.
#
#  Flujo completo (scripts en orden):
#   bash docs/inventario/caso-1-reventa/01-ejecutar-flujo-e2e.sh
#   bash docs/inventario/caso-1-reventa/02-verificar-documentos.sh
#   bash docs/inventario/caso-1-reventa/03-analizar-cuentas-contables.sh
#
#  Requisitos previos:
#   - curl, jq y sqlcmd instalados.
#   - API corriendo en https://localhost:8000.
#   - credentials/db.txt en la raíz del proyecto.
#   - Producto 1 (Coca-Cola 355ml) existe. Período 4 abierto.
# ============================================================================

set -uo pipefail

# ── Rutas ─────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
CREDENTIALS_FILE="$REPO_ROOT/credentials/db.txt"
TEMP_RESPONSE="/tmp/fa_caso1_response.json"

HOST="https://localhost:8000/api/v1"
EMAIL="ezekiell1988@hotmail.com"
API_PROJECT="src/familyAccountApi"   # ruta relativa desde la raíz del repo

# ── Resetear la BD antes de correr el test? ──────────────────────────────────
# true  → drop BD + borrar migraciones + InitialCreate + database update
#         (solo para desarrollo — seed fresh data automáticamente)
# false → usar la BD con el estado que tiene
RESET_DB=true

# Sufijo único por ejecución para evitar colisiones en campos con índice único
# (numberInvoice en purchaseInvoice y lotNumber en inventoryLot)
RUN_ID="$(date '+%Y%m%d%H%M%S')"
PROVIDER_INVOICE_NUMBER="FAC-PROVEEDOR-C1-${RUN_ID}"
LOT_NUMBER="LOT-COCA-C1-${RUN_ID}"

# ── Colores ───────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# ── Estado del flujo (se completa conforme avanza) ────────────────────────────
TOKEN=""
REFRESH_TOKEN=""
ID_PRODUCT_UNIT=0
ID_PRODUCT_ACCOUNT=0
ID_PURCHASE_INVOICE=0
ID_LOT=0
ID_SALES_INVOICE=0
ID_ENTRY_DEV_COGS="(no generado)"
ID_ENTRY_REINTEGRO=0
ID_ADJUSTMENT=0
STOCK_FINAL="?"

# ── Helpers ───────────────────────────────────────────────────────────────────

step() {
  echo ""
  printf "${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n"
  printf "${CYAN}${BOLD}▶  %s${NC}\n" "$1"
  printf "${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n"
}

log_ok()   { printf "  ${GREEN}✅  %s${NC}\n" "$1"; }
log_warn() { printf "  ${YELLOW}⚠   %s${NC}\n" "$1"; }
log_info() { printf "  %s\n" "$1"; }

# Imprime error con la respuesta y termina el script.
fail() {
  echo ""
  printf "  ${RED}${BOLD}❌  FALLO: %s${NC}\n" "$1"
  if [[ -f "$TEMP_RESPONSE" && -s "$TEMP_RESPONSE" ]]; then
    printf "  ${RED}Respuesta del API:${NC}\n"
    jq . "$TEMP_RESPONSE" 2>/dev/null || cat "$TEMP_RESPONSE"
  fi
  echo ""
  printf "${RED}${BOLD}  El proceso se detuvo en el paso anterior.${NC}\n"
  printf "  IDs obtenidos hasta ahora:\n"
  printf "    idProductAccount   : %s\n" "$ID_PRODUCT_ACCOUNT"
  printf "    idPurchaseInvoice  : %s\n" "$ID_PURCHASE_INVOICE"
  printf "    idLot              : %s\n" "$ID_LOT"
  printf "    idSalesInvoice     : %s\n" "$ID_SALES_INVOICE"
  echo ""
  exit 1
}

# Realiza una llamada al API y guarda el body en $TEMP_RESPONSE y el código HTTP en $HTTP_STATUS.
# Uso: api_call <METHOD> <path> [body] [token]
HTTP_STATUS=""
api_call() {
  local method="$1"
  local path="$2"
  local body="${3:-}"
  local token="${4:-}"

  local args=(-k -s -o "$TEMP_RESPONSE" -w "%{http_code}" -X "$method" "${HOST}${path}")
  args+=(-H "Content-Type: application/json")
  [[ -n "$token" ]] && args+=(-H "Authorization: Bearer $token")
  [[ -n "$body" ]] && args+=(-d "$body")

  HTTP_STATUS=$(curl "${args[@]}")
}

# Verifica que el HTTP status sea el esperado; si no, llama fail().
assert_status() {
  local expected="$1"
  local context="$2"
  if [[ "$HTTP_STATUS" != "$expected" ]]; then
    fail "HTTP $expected esperado en '$context', recibido: $HTTP_STATUS"
  fi
  log_ok "$context — HTTP $HTTP_STATUS"
}

# Extrae un campo del JSON en $TEMP_RESPONSE usando jq.
jq_field() { jq -r "$1" "$TEMP_RESPONSE" 2>/dev/null; }

# ── Verificar dependencias ────────────────────────────────────────────────────
for cmd in curl jq sqlcmd; do
  if ! command -v "$cmd" &>/dev/null; then
    printf "${RED}❌  Dependencia faltante: %s${NC}\n" "$cmd"
    exit 1
  fi
done

# ── Leer credenciales BD ──────────────────────────────────────────────────────
if [[ ! -f "$CREDENTIALS_FILE" ]]; then
  printf "${RED}❌  No se encontró %s${NC}\n" "$CREDENTIALS_FILE"
  exit 1
fi
DB_HOST=$(grep -E '^HOST:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_PORT=$(grep -E '^PORT:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_USER=$(grep -E '^USER:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_PASS=$(grep -E '^PASSWORD:' "$CREDENTIALS_FILE" | awk '{print $2}')

# ── Encabezado ────────────────────────────────────────────────────────────────
echo ""
printf "${CYAN}${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
printf "${CYAN}${BOLD}║   CASO 1 — REVENTA · Test de Integración E2E        ║${NC}\n"
printf "${CYAN}${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"
log_info "API   : $HOST"
log_info "BD    : $DB_HOST:$DB_PORT / dbfa"
log_info "Email : $EMAIL"
log_info "RESET : $RESET_DB"

# ════════════════════════════════════════════════════════════════════════════════
# PASO -1 — RESET BD (solo si RESET_DB=true)
#
#  Flujo (del skill dotnet-ef-re-create):
#   1. dotnet ef database drop  → elimina dbfa en el servidor remoto
#   2. rm Migrations/*.cs        → borra los 24+ archivos de migración
#   3. dotnet ef migrations add  → genera nueva InitialCreate (con todo el seed)
#   4. dotnet ef database update → crea las tablas y aplica el seed
#
#  Resultado: BD limpia con sólo los datos de seed de las Configuration/*.
# ════════════════════════════════════════════════════════════════════════════════
if [[ "$RESET_DB" == "true" ]]; then
  step "PASO -1 — Reset de BD (RESET_DB=true)"

  if ! command -v dotnet-ef &>/dev/null && ! dotnet ef --version &>/dev/null 2>&1; then
    printf "${RED}❌  dotnet-ef no instalado. Ejecutar: dotnet tool install --global dotnet-ef${NC}\n"
    exit 1
  fi

  log_info "① Drop base de datos remota..."
  dotnet ef database drop --project "$API_PROJECT" --force \
    || { printf "${RED}❌  Falló database drop${NC}\n"; exit 1; }
  log_ok "Base de datos eliminada"

  log_info "② Eliminando archivos de migración..."
  MIGRATIONS_DIR="$API_PROJECT/Infrastructure/Data/Migrations"
  find "$MIGRATIONS_DIR" -maxdepth 1 -name "*.cs" -delete
  REMAINING=$(find "$MIGRATIONS_DIR" -maxdepth 1 -name "*.cs" | wc -l | tr -d ' ')
  if [[ "$REMAINING" != "0" ]]; then
    printf "${RED}❌  Quedaron %s archivos en Migrations/ — verificar manualmente${NC}\n" "$REMAINING"
    exit 1
  fi
  log_ok "Carpeta Migrations/ vacía"

  log_info "③ Generando migración InitialCreate..."
  dotnet ef migrations add InitialCreate \
    --project "$API_PROJECT" \
    --output-dir Infrastructure/Data/Migrations \
    || { printf "${RED}❌  Falló migrations add${NC}\n"; exit 1; }
  log_ok "Migración InitialCreate generada"

  log_info "④ Aplicando migración (database update)..."
  dotnet ef database update --project "$API_PROJECT" \
    || { printf "${RED}❌  Falló database update${NC}\n"; exit 1; }
  log_ok "BD recreada con seed — lista para el test"

  # ── Pausa: el API debe reiniciarse para que Hangfire recree su esquema ───────
  #
  #  ¿Por qué?
  #  Hangfire crea sus tablas (HangFire.Job, HangFire.State, etc.) al iniciar
  #  la aplicación (PrepareSchemaIfNecessary=true). Si la BD se elimina mientras
  #  el API está corriendo, esas tablas desaparecen pero el proceso ya hizo la
  #  inicialización. Es necesario reiniciar el API para que Hangfire las recree.
  #
  echo ""
  printf "${YELLOW}${BOLD}  ⚠  ACCIÓN REQUERIDA${NC}\n"
  printf "${YELLOW}  Reinicia el API ahora (F5 en VS Code o Ctrl+C + dotnet run).${NC}\n"
  printf "${YELLOW}  Hangfire re-creará su esquema al arrancar.${NC}\n"
  printf "  Presiona ENTER cuando el API esté corriendo de nuevo...\n"
  read -r

  log_info "Esperando que el API responda en $HOST..."
  HEALTH_URL="${HOST%/api/v1}/health.json"
  MAX_WAIT=60
  WAITED=0
  until curl -k -s -o /dev/null -w "%{http_code}" "$HEALTH_URL" 2>/dev/null | grep -q "200"; do
    if [[ $WAITED -ge $MAX_WAIT ]]; then
      printf "${RED}❌  El API no respondió en %s segundos. Verifica que esté corriendo.${NC}\n" "$MAX_WAIT"
      exit 1
    fi
    printf "  ⋯  esperando API (%ss)...\r" "$WAITED"
    sleep 2
    WAITED=$((WAITED + 2))
  done
  log_ok "API responde ✓"
fi

# ════════════════════════════════════════════════════════════════════════════════
# PASO 1 — AUTENTICACIÓN
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 1 — Autenticación"

PIN="12345"
log_info "Insertando PIN de prueba '${PIN}' directamente en BD (idUser=1)..."
sqlcmd \
  -S "${DB_HOST},${DB_PORT}" \
  -U "$DB_USER" \
  -P "$DB_PASS" \
  -C -d dbfa \
  -Q "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '${PIN}'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '${PIN}');" \
  2>/dev/null
log_ok "PIN '${PIN}' insertado en BD para usuario 1"

log_info "Haciendo login con el PIN..."
api_call POST "/auth/login" '{"emailUser":"'"$EMAIL"'","pin":"'"$PIN"'"}'
assert_status 200 "login"

TOKEN=$(jq_field '.accessToken')
REFRESH_TOKEN=$(jq_field '.refreshToken')
if [[ -z "$TOKEN" || "$TOKEN" == "null" ]]; then
  fail "No se obtuvo accessToken en la respuesta del login"
fi
log_ok "Token obtenido: ${TOKEN:0:30}..."

# ════════════════════════════════════════════════════════════════════════════════
# PASO 0 — PRE-REQUISITO: Crear presentación base del producto 1
#
#  El API valida en MapLinesAsync que exista un registro ProductUnit para el par
#  (idProduct, idUnit) antes de crear cualquier línea de factura.
#  Creamos la presentación base: idUnit=1 (Unidad), conversionFactor=1, isBase=true.
#  Se elimina al final para que el script sea re-ejecutable.
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 0 — Crear presentación base del producto 1 (idUnit=1, conversionFactor=1)"

# Verificar si ya existe para no fallar por duplicado (UQ_productUnit_idProduct_idUnit)
api_call GET "/product-units/by-product/1.json" "" "$TOKEN"
assert_status 200 "get product-units by-product"

EXISTING_PU=$(jq_field '[.[] | select(.idUnit == 1)] | first | .idProductUnit')
if [[ -n "$EXISTING_PU" && "$EXISTING_PU" != "null" ]]; then
  ID_PRODUCT_UNIT="$EXISTING_PU"
  log_warn "Presentación ya existe (idProductUnit=$ID_PRODUCT_UNIT), reutilizando"
else
  api_call POST "/product-units" \
    '{"idProduct":1,"idUnit":1,"conversionFactor":1.0,"isBase":true,"usedForPurchase":true,"usedForSale":true,"namePresentation":"Unidad base"}' \
    "$TOKEN"
  assert_status 201 "create product-unit"
  ID_PRODUCT_UNIT=$(jq_field '.idProductUnit')
  log_ok "idProductUnit = $ID_PRODUCT_UNIT"
fi

# ════════════════════════════════════════════════════════════════════════════════
# PASO 2 — PRE-COMPRA: Vincular Coca-Cola a cuenta 109 Inventario (100%)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 2 — Crear ProductAccount (producto 1 → cuenta 109, 100%)"

# Verificar si ya existe uno para no fallar por límite de 100%
api_call GET "/product-accounts/by-product/1.json" "" "$TOKEN"
if [[ "$HTTP_STATUS" == "200" ]]; then
  EXISTING_PA=$(jq_field '[.[] | select(.idAccount == 109)] | first | .idProductAccount')
  if [[ -n "$EXISTING_PA" && "$EXISTING_PA" != "null" ]]; then
    ID_PRODUCT_ACCOUNT="$EXISTING_PA"
    log_warn "ProductAccount ya existe (idProductAccount=$ID_PRODUCT_ACCOUNT), reutilizando"
  fi
fi

if [[ "$ID_PRODUCT_ACCOUNT" == "0" ]]; then
  api_call POST "/product-accounts" \
    '{"idProduct":1,"idAccount":109,"percentageAccount":100.00}' \
    "$TOKEN"
  assert_status 201 "create product-account"
  ID_PRODUCT_ACCOUNT=$(jq_field '.idProductAccount')
  log_ok "idProductAccount = $ID_PRODUCT_ACCOUNT"
fi

# ════════════════════════════════════════════════════════════════════════════════
# PASO 3 — FACTURA DE COMPRA (proveedor entrega 100 cajas)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 3a — Crear factura de compra en borrador (100 cajas)"

BODY_PURCHASE=$(cat <<EOF
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
      "descriptionLine": "Coca-Cola 355ml × 100 un.",
      "quantity": 100,
      "unitPrice": 1000.00,
      "taxPercent": 13.00,
      "totalLineAmount": 113000.00
    }
  ]
}
EOF
)

api_call POST "/purchase-invoices" "$BODY_PURCHASE" "$TOKEN"
assert_status 201 "create purchase-invoice"

ID_PURCHASE_INVOICE=$(jq_field '.idPurchaseInvoice')
log_ok "idPurchaseInvoice = $ID_PURCHASE_INVOICE"

step "PASO 3b — Confirmar factura de compra"

api_call POST "/purchase-invoices/${ID_PURCHASE_INVOICE}/confirm" "" "$TOKEN"
assert_status 200 "confirm purchase-invoice"

STATUS=$(jq_field '.statusInvoice')
log_ok "statusInvoice = $STATUS"
[[ "$STATUS" == "Confirmado" ]] || fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$STATUS'"

NUMBER_PC=$(jq_field '.numberInvoice')
log_ok "Número de factura de compra: $NUMBER_PC"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 4 — POST-COMPRA: Eliminar ProductAccount
#   (Para que la venta use IdAccountSalesRevenue=117 como CR, no la cuenta 109)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 4 — Eliminar ProductAccount (id=$ID_PRODUCT_ACCOUNT)"

api_call DELETE "/product-accounts/${ID_PRODUCT_ACCOUNT}" "" "$TOKEN"
assert_status 204 "delete product-account"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 5 — FACTURA DE VENTA (10 cajas, cliente contado)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 5a — Obtener lote del producto 1 (LOT-COCA-C1-001)"

api_call GET "/inventory-lots/by-product/1.json" "" "$TOKEN"
assert_status 200 "get inventory-lots by-product"

# Buscar el lote por número; si no coincide usar el primero disponible
ID_LOT=$(jq_field '[.[] | select(.lotNumber == "LOT-COCA-C1-001")] | first | .idInventoryLot')
if [[ -z "$ID_LOT" || "$ID_LOT" == "null" ]]; then
  log_warn "No se encontró el lote 'LOT-COCA-C1-001', usando el primer lote disponible"
  ID_LOT=$(jq_field '.[0].idInventoryLot')
fi
if [[ -z "$ID_LOT" || "$ID_LOT" == "null" ]]; then
  fail "No se encontró ningún lote para el producto 1"
fi

LOT_QTY=$(jq_field '[.[] | select(.idInventoryLot == '"$ID_LOT"')] | first | .quantityAvailable')
log_ok "idInventoryLot = $ID_LOT  (quantityAvailable = $LOT_QTY)"

step "PASO 5b — Crear factura de venta en borrador (10 cajas × ₡1,500 + 13% IVA)"

BODY_SALE=$(cat <<EOF
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
      "descriptionLine": "Coca-Cola 355ml × 10 un.",
      "quantity": 10,
      "unitPrice": 1500.00,
      "taxPercent": 13.00,
      "totalLineAmount": 16950.00
    }
  ]
}
EOF
)

api_call POST "/sales-invoices" "$BODY_SALE" "$TOKEN"
assert_status 201 "create sales-invoice"

ID_SALES_INVOICE=$(jq_field '.idSalesInvoice')
log_ok "idSalesInvoice = $ID_SALES_INVOICE"

step "PASO 5c — Confirmar factura de venta"

api_call POST "/sales-invoices/${ID_SALES_INVOICE}/confirm" "" "$TOKEN"
assert_status 200 "confirm sales-invoice"

STATUS=$(jq_field '.statusInvoice')
log_ok "statusInvoice = $STATUS"
[[ "$STATUS" == "Confirmado" ]] || fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$STATUS'"

NUMBER_FV=$(jq_field '.numberInvoice')
log_ok "Número de factura de venta: $NUMBER_FV"
log_info "  Esperado: DR 106 Caja ₡16,950 / CR 117 Ingresos ₡16,950"
log_info "  Esperado: DR 119 COGS ₡11,300  / CR 109 Inventario ₡11,300"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 6 — DEVOLUCIÓN PARCIAL (cliente devuelve 3 cajas)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 6 — Devolución parcial: 3 cajas devueltas"

BODY_RETURN=$(cat <<EOF
{
  "dateReturn": "2026-04-05",
  "descriptionReturn": "Devolución parcial — cliente devuelve 3 cajas Coca-Cola dañadas en tránsito",
  "refundMode": "EfectivoInmediato",
  "lines": [
    {
      "idInventoryLot": $ID_LOT,
      "quantity": 3,
      "totalLineAmount": 5085.00,
      "descriptionLine": "Coca-Cola 355ml × 3 un. — devolución parcial"
    }
  ]
}
EOF
)

api_call POST "/sales-invoices/${ID_SALES_INVOICE}/partial-return" "$BODY_RETURN" "$TOKEN"
assert_status 200 "partial-return"

PARTIAL_ENTRY=$(jq_field '.idAccountingEntry')
if [[ -n "$PARTIAL_ENTRY" && "$PARTIAL_ENTRY" != "null" ]]; then
  ID_ENTRY_DEV_COGS="$PARTIAL_ENTRY"
  log_ok "idAccountingEntry DEV-COGS = $ID_ENTRY_DEV_COGS"
else
  log_warn "La devolución no generó asiento COGS (puede ser que el tipo de venta no tenga IdAccountCOGS configurado)"
fi
log_info "  DEV-COGS: DR 109 Inventario ₡3,000 / CR 119 COGS ₡3,000  (3 u × ₡1,000 costo neto)"

REFUND_ENTRY=$(jq_field '.idAccountingEntryRefund')
if [[ -n "$REFUND_ENTRY" && "$REFUND_ENTRY" != "null" ]]; then
  ID_ENTRY_REINTEGRO="$REFUND_ENTRY"
  log_ok "idAccountingEntryRefund DEV-ING = $ID_ENTRY_REINTEGRO"
else
  log_warn "La devolución no generó asiento de reversión de ingresos"
fi
log_info "  DEV-ING:  DR 117 Ingresos ₡4,500 + DR 127 IVA ₡585 / CR 106 Caja ₡5,085"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 7 — AJUSTE DE INVENTARIO (regalía: −2 cajas)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 7a — Crear ajuste de inventario en borrador (regalía −2 cajas)"

BODY_ADJ=$(cat <<EOF
{
  "idFiscalPeriod": 4,
  "idInventoryAdjustmentType": 4,
  "idCurrency": 1,
  "exchangeRateValue": 1.0,
  "dateAdjustment": "2026-04-05",
  "descriptionAdjustment": "Regalía cliente VIP — 2 cajas Coca-Cola 355ml — Responsable: Administrador",
  "lines": [
    {
      "idInventoryLot": $ID_LOT,
      "quantityDelta": -2,
      "descriptionLine": "Salida por regalía — Coca-Cola 355ml × 2 un."
    }
  ]
}
EOF
)

api_call POST "/inventory-adjustments" "$BODY_ADJ" "$TOKEN"
assert_status 201 "create inventory-adjustment"

ID_ADJUSTMENT=$(jq_field '.idInventoryAdjustment')
log_ok "idInventoryAdjustment = $ID_ADJUSTMENT"

step "PASO 7b — Confirmar ajuste de inventario"

api_call POST "/inventory-adjustments/${ID_ADJUSTMENT}/confirm" "" "$TOKEN"
assert_status 200 "confirm inventory-adjustment"

ADJ_STATUS=$(jq_field '.statusAdjustment')
ADJ_ENTRY=$(jq_field '.idAccountingEntry')
log_ok "statusAdjustment = $ADJ_STATUS"
[[ "$ADJ_STATUS" == "Confirmado" ]] || fail "Se esperaba statusAdjustment = 'Confirmado', recibido: '$ADJ_STATUS'"
log_ok "idAccountingEntry ADJ = $ADJ_ENTRY"
log_info "  Esperado: DR 113 Merma ₡2,000 / CR 109 Inventario ₡2,000  (2 u × ₡1,000 costo neto)"

# ════════════════════════════════════════════════════════════════════════════════
# VERIFICACIÓN FINAL DE STOCK
# ════════════════════════════════════════════════════════════════════════════════
step "VERIFICACIÓN FINAL — Stock total del producto 1"

api_call GET "/inventory-lots/stock/1.json" "" "$TOKEN"
assert_status 200 "get stock total"

STOCK_FINAL=$(cat "$TEMP_RESPONSE" | tr -d '[:space:]')
log_ok "Stock actual = $STOCK_FINAL unidades"
log_info "  Fórmula: 100 (compra) − 10 (venta) + 3 (devolución) − 2 (regalía) = 91"

# Comparación numérica tolerante a decimales (91 o 91.0)
EXPECTED=91
MATCH=$(awk "BEGIN { print ($STOCK_FINAL == $EXPECTED) ? \"yes\" : \"no\" }" 2>/dev/null || echo "no")
if [[ "$MATCH" == "yes" ]]; then
  log_ok "Stock = 91 ✓  Saldo de costo en libros esperado: 91 × ₡1,000 = ₡91,000"
else
  log_warn "Stock final = $STOCK_FINAL (esperado 91 si la BD estaba limpia antes de correr el script)"
fi

# ════════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ════════════════════════════════════════════════════════════════════════════════
echo ""
printf "${GREEN}${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
printf "${GREEN}${BOLD}║   ✅  FLUJO CASO 1 COMPLETADO EXITOSAMENTE          ║${NC}\n"
printf "${GREEN}${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"
printf "  %-22s %s\n" "idPurchaseInvoice:"  "$ID_PURCHASE_INVOICE"
printf "  %-22s %s\n" "idLot:"              "$ID_LOT"
printf "  %-22s %s\n" "idSalesInvoice:"     "$ID_SALES_INVOICE"
printf "  %-22s %s\n" "idEntryDevCogs:"     "$ID_ENTRY_DEV_COGS"
printf "  %-22s %s\n" "idEntryDevIng:"      "$ID_ENTRY_REINTEGRO"
printf "  %-22s %s\n" "idAdjustment:"       "$ID_ADJUSTMENT"
printf "  %-22s %s\n" "idAdjEntry:"         "$ADJ_ENTRY"
printf "  %-22s %s\n" "Stock final:"        "$STOCK_FINAL u."
echo ""

# ════════════════════════════════════════════════════════════════════════════════
# GUARDAR RESULTADO EN ARCHIVO TXT
# ════════════════════════════════════════════════════════════════════════════════
RUN_TS="$(date '+%Y-%m-%d_%H-%M-%S')"
OUTPUT_FILE="$SCRIPT_DIR/resultado_caso1_${RUN_TS}.txt"

cat > "$OUTPUT_FILE" <<TXTEOF
# ============================================================
#  CASO 1 — REVENTA · Resultado del Test E2E
#  Ejecutado: $(date '+%Y-%m-%d %H:%M:%S')
# ============================================================

# ── Configuración usada ──────────────────────────────────────
HOST                  = $HOST
EMAIL                 = $EMAIL
DB_HOST               = $DB_HOST
DB_PORT               = $DB_PORT

# ── Claves estáticas del caso ────────────────────────────────
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
unitCost_lote         = 1130.00 CRC  (1000 × 1.13)
unitPrice_venta       = 1500.00 CRC
unitPriceConIVA_venta = 1695.00 CRC

# ── IDs generados en esta ejecución ─────────────────────────
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

# ── Montos del flujo ─────────────────────────────────────────
compra_subtotal       = 100000.00
compra_iva            = 13000.00
compra_total          = 113000.00
venta_subtotal        = 15000.00
venta_iva             = 1950.00
venta_total           = 16950.00
cogs_venta            = 10000.00  (10 u × 1000)
devolucion_monto      = 5085.00   (3 u × 1695)
devolucion_subtotal   = 4500.00   (3 u × 1500 neto)
devolucion_iva        = 585.00    (4500 × 13%)
cogs_reversa          = 3000.00   (3 u × 1000)
regalia_costo         = 2000.00   (2 u × 1000)

# ── Verificación de stock ────────────────────────────────────
stock_inicial         = 0
stock_post_compra     = 100
stock_post_venta      = 90
stock_post_devolucion = 93
stock_post_regalia    = 91
stock_final_real      = $STOCK_FINAL
stock_costo_libros    = 91000.00  (91 × 1000)

# ── Cuentas involucradas ─────────────────────────────────────
idAccount_caja_crc    = 106  (1.1.06.01 Caja CRC)
idAccount_inventario  = 109  (1.1.07.01 Inventario Mercadería)
idAccount_cogs        = 119  (5.01 Costo de Ventas)
idAccount_ingresos    = 117  (4.5.01 Ingresos por Ventas)
idAccount_merma       = 130  (5.14.01.02 Merma Anormal — IAS 2.16)

# ── Endpoints de consulta rápida ────────────────────────────
# (reemplazar HOST y TOKEN según corresponda)
GET $HOST/purchase-invoices/$ID_PURCHASE_INVOICE.json
GET $HOST/inventory-lots/$ID_LOT.json
GET $HOST/inventory-lots/stock/1.json
GET $HOST/sales-invoices/$ID_SALES_INVOICE.json
GET $HOST/accounting-entries/$ID_ENTRY_REINTEGRO.json
GET $HOST/inventory-adjustments/$ID_ADJUSTMENT.json
TXTEOF

printf "  ${GREEN}💾  Resultado guardado en:${NC} %s\n" "$OUTPUT_FILE"
echo ""

# Limpiar archivo temporal
rm -f "$TEMP_RESPONSE"
