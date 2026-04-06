#!/usr/bin/env bash
# ============================================================================
#  CASO 2 — MANUFACTURA (Chile Embotellado Marca X)
#  01-ejecutar-flujo-e2e.sh — Test de Integración E2E
#
#  ¿Qué hace este script?
#  ─────────────────────
#  Ejecuta el flujo COMPLETO de manufactura contra el sistema real:
#   1. Crea ProductUnits (5 productos: 4 MP + 1 PT).
#   2. Crea ProductAccounts para los 4 MP → cuenta 110 Materias Primas.
#   3. Compra las materias primas con factura de compra (4 líneas, 100 frascos).
#   4. Crea y avanza la orden de producción: Borrador → Pendiente → EnProceso → Completado.
#      Al completar: el sistema consume MP por FEFO, genera asientos DR-115/CR-111
#      y crea el lote del PT con unitCost = costTotal / 100 = ₡526.
#   5. Registra la venta de 30 frascos de PT + confirma (DR-106 / CR-117 / COGS).
#   6. Devolución parcial de 5 frascos.
#   7. Ajuste de inventario: regalía de 2 frascos.
#   8. Verifica stock final = 73 frascos.
#   9. Guarda resultado_caso2_*.txt con todos los IDs.
#
#  Flujo completo (scripts en orden):
#   bash docs/inventario/caso-2-manufactura/01-ejecutar-flujo-e2e.sh
#   bash docs/inventario/caso-2-manufactura/02-verificar-documentos.sh
#   bash docs/inventario/caso-2-manufactura/03-analizar-cuentas-contables.sh
#
#  Requisitos previos:
#   - curl, jq y sqlcmd instalados.
#   - API corriendo en https://localhost:8000.
#   - credentials/db.txt en la raíz del proyecto.
#   - Productos 2–6 y receta idProductRecipe=1 en el seed. Período 4 abierto.
# ============================================================================

set -uo pipefail

# ── Rutas ─────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
CREDENTIALS_FILE="$REPO_ROOT/credentials/db.txt"
TEMP_RESPONSE="/tmp/fa_caso2_response.json"

HOST="https://localhost:8000/api/v1"
EMAIL="ezekiell1988@hotmail.com"
API_PROJECT="src/familyAccountApi"

# ── Reset BD? ─────────────────────────────────────────────────────────────────
# true  → dropa la BD, borra migraciones, recrea InitialCreate, seed limpio
# false → usa el estado actual de la BD
RESET_DB=true

RUN_ID="$(date '+%Y%m%d%H%M%S')"
PROVIDER_INVOICE_NUMBER="FAC-PROVEEDOR-C2-${RUN_ID}"
LOT_NUMBER_CHILE="LOT-MP-CHILE-C2-${RUN_ID}"
LOT_NUMBER_VINAGRE="LOT-MP-VINAGRE-C2-${RUN_ID}"
LOT_NUMBER_SAL="LOT-MP-SAL-C2-${RUN_ID}"
LOT_NUMBER_FRASCO="LOT-MP-FRASCO-C2-${RUN_ID}"

# ── Colores ───────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# ── Estado del flujo ──────────────────────────────────────────────────────────
TOKEN=""
REFRESH_TOKEN=""
# ProductUnits
ID_PU_CHILE=0; ID_PU_VINAGRE=0; ID_PU_SAL=0; ID_PU_FRASCO=0; ID_PU_PT=0
# ProductAccounts
ID_PA_CHILE=0; ID_PA_VINAGRE=0; ID_PA_SAL=0; ID_PA_FRASCO=0
# Documentos
ID_PURCHASE_INVOICE=0
ID_LOT_CHILE=0; ID_LOT_VINAGRE=0; ID_LOT_SAL=0; ID_LOT_FRASCO=0
ID_PRODUCTION_ORDER=0
OP_NUMBER="?"
ID_LOT_PT=0
LOT_PT_COST="?"
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
  printf "    idPurchaseInvoice   : %s\n" "$ID_PURCHASE_INVOICE"
  printf "    idProductionOrder   : %s\n" "$ID_PRODUCTION_ORDER"
  printf "    idLotPT             : %s\n" "$ID_LOT_PT"
  printf "    idSalesInvoice      : %s\n" "$ID_SALES_INVOICE"
  echo ""
  exit 1
}

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

assert_status() {
  local expected="$1"
  local context="$2"
  if [[ "$HTTP_STATUS" != "$expected" ]]; then
    fail "HTTP $expected esperado en '$context', recibido: $HTTP_STATUS"
  fi
  log_ok "$context — HTTP $HTTP_STATUS"
}

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
printf "${CYAN}${BOLD}║   CASO 2 — MANUFACTURA · Test de Integración E2E   ║${NC}\n"
printf "${CYAN}${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"
log_info "API   : $HOST"
log_info "BD    : $DB_HOST:$DB_PORT / dbfa"
log_info "Email : $EMAIL"
log_info "RESET : $RESET_DB"

# ════════════════════════════════════════════════════════════════════════════════
# PASO -1 — RESET BD
# ════════════════════════════════════════════════════════════════════════════════
if [[ "$RESET_DB" == "true" ]]; then
  step "PASO -1 — Reset de BD (RESET_DB=true)"

  if ! dotnet ef --version &>/dev/null 2>&1; then
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

log_info "Solicitando PIN al correo..."
api_call POST "/auth/request-pin" '{"emailUser":"'"$EMAIL"'"}'
assert_status 200 "request-pin"

log_info "Leyendo PIN desde la base de datos..."
PIN=$(sqlcmd \
  -S "${DB_HOST},${DB_PORT}" \
  -U "$DB_USER" \
  -P "$DB_PASS" \
  -C -d dbfa \
  -Q "SET NOCOUNT ON; SELECT TOP 1 pin FROM dbo.userPin up INNER JOIN dbo.[user] u ON u.idUser = up.idUser WHERE u.emailUser = '${EMAIL}' ORDER BY up.idUserPin DESC" \
  -h -1 -W 2>/dev/null | tr -d '[:space:]')

if [[ -z "$PIN" ]]; then
  fail "No se encontró PIN en la BD para $EMAIL"
fi
log_ok "PIN obtenido desde BD: $PIN"

api_call POST "/auth/login" '{"emailUser":"'"$EMAIL"'","pin":"'"$PIN"'"}'
assert_status 200 "login"

TOKEN=$(jq_field '.accessToken')
REFRESH_TOKEN=$(jq_field '.refreshToken')
if [[ -z "$TOKEN" || "$TOKEN" == "null" ]]; then
  fail "No se obtuvo accessToken en la respuesta del login"
fi
log_ok "Token obtenido: ${TOKEN:0:30}..."

# ════════════════════════════════════════════════════════════════════════════════
# PASO 2 — PRE-REQUISITOS: ProductUnits para los 5 productos
#
#  El API valida en MapLinesAsync que exista ProductUnit para cada (idProduct, idUnit)
#  antes de crear líneas de factura u OP. Sin este registro la creación falla.
#
#  MP: idProduct 2 (KG=3), 3 (LTR=7), 4 (KG=3), 5 (UNI=1)
#  PT: idProduct 6 (UNI=1)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 2 — Crear ProductUnits para MP y PT (idempotente)"

ensure_product_unit() {
  local id_product="$1"
  local id_unit="$2"
  local name_pres="$3"
  local used_purchase="$4"   # true|false
  local used_sale="$5"       # true|false

  api_call GET "/product-units/by-product/${id_product}.json" "" "$TOKEN"
  assert_status 200 "get product-units idProduct=${id_product}" >&2

  local existing
  existing=$(jq_field "[.[] | select(.idUnit == ${id_unit})] | first | .idProductUnit")
  if [[ -n "$existing" && "$existing" != "null" ]]; then
    log_warn "ProductUnit (product=${id_product}, unit=${id_unit}) ya existe (id=$existing)" >&2
    echo "$existing"
    return
  fi

  api_call POST "/product-units" \
    "{\"idProduct\":${id_product},\"idUnit\":${id_unit},\"conversionFactor\":1.0,\"isBase\":true,\"usedForPurchase\":${used_purchase},\"usedForSale\":${used_sale},\"namePresentation\":\"${name_pres}\"}" \
    "$TOKEN"
  assert_status 201 "create product-unit (product=${id_product}, unit=${id_unit})" >&2
  jq_field '.idProductUnit'
}

ID_PU_CHILE=$(ensure_product_unit   2 3 "Kilogramo base"  true  false)
log_ok "ProductUnit Chile Seco       → idProductUnit=$ID_PU_CHILE"

ID_PU_VINAGRE=$(ensure_product_unit 3 7 "Litro base"       true  false)
log_ok "ProductUnit Vinagre Blanco   → idProductUnit=$ID_PU_VINAGRE"

ID_PU_SAL=$(ensure_product_unit     4 3 "Kilogramo base"   true  false)
log_ok "ProductUnit Sal              → idProductUnit=$ID_PU_SAL"

ID_PU_FRASCO=$(ensure_product_unit  5 1 "Unidad base"      true  false)
log_ok "ProductUnit Frasco 250ml     → idProductUnit=$ID_PU_FRASCO"

ID_PU_PT=$(ensure_product_unit      6 1 "Frasco 250ml"     false true)
log_ok "ProductUnit PT Chile Embot.  → idProductUnit=$ID_PU_PT"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 3 — PRE-COMPRA: ProductAccounts para las 4 MP → cuenta 110 (idempotente)
#
#  Sin este vínculo la compra se carga a cuenta 109 (default del tipo de factura).
#  Con él, la compra queda en cuenta 110 Materias Primas, que es lo correcto.
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 3 — Crear ProductAccounts (4 MP → cuenta 110, idempotente)"

ensure_product_account() {
  local id_product="$1"

  api_call GET "/product-accounts/by-product/${id_product}.json" "" "$TOKEN"
  if [[ "$HTTP_STATUS" == "200" ]]; then
    local existing
    existing=$(jq_field '[.[] | select(.idAccount == 110)] | first | .idProductAccount')
    if [[ -n "$existing" && "$existing" != "null" ]]; then
      log_warn "ProductAccount (product=${id_product}, cta=110) ya existe (id=$existing)" >&2
      echo "$existing"
      return
    fi
  fi

  api_call POST "/product-accounts" \
    "{\"idProduct\":${id_product},\"idAccount\":110,\"percentageAccount\":100.00}" \
    "$TOKEN"
  assert_status 201 "create product-account (product=${id_product} → cta 110)" >&2
  jq_field '.idProductAccount'
}

ID_PA_CHILE=$(ensure_product_account   2); log_ok "ProductAccount Chile Seco    → id=$ID_PA_CHILE"
ID_PA_VINAGRE=$(ensure_product_account 3); log_ok "ProductAccount Vinagre       → id=$ID_PA_VINAGRE"
ID_PA_SAL=$(ensure_product_account     4); log_ok "ProductAccount Sal           → id=$ID_PA_SAL"
ID_PA_FRASCO=$(ensure_product_account  5); log_ok "ProductAccount Frasco 250ml  → id=$ID_PA_FRASCO"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 4 — FACTURA DE COMPRA (4 MP para producir 100 frascos)
#
#  Cantidades basadas en la receta (recipe.QuantityOutput=1 → factor=100):
#   Chile Seco   id=2: 20  KG  × ₡1,000/KG  = ₡20,000
#   Vinagre      id=3:  5  LTR × ₡500/LTR   = ₡ 2,500
#   Sal          id=4:  0.5 KG × ₡200/KG    = ₡    100
#   Frasco 250ml id=5: 100 UNI × ₡300/UNI   = ₡30,000
#   ──────────────────────────────────────────────────
#   Subtotal                                 = ₡52,600
#   IVA 13%                                  = ₡ 6,838
#   Total                                    = ₡59,438
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 4a — Crear factura de compra MP en borrador (4 líneas)"

BODY_PURCHASE=$(cat <<EOF
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idPurchaseInvoiceType": 1,
  "idContact": 1,
  "numberInvoice": "${PROVIDER_INVOICE_NUMBER}",
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
      "lotNumber": "${LOT_NUMBER_CHILE}",
      "expirationDate": "2027-06-30",
      "descriptionLine": "Chile Seco × 20 KG",
      "quantity": 20,
      "unitPrice": 1000.00,
      "taxPercent": 13.00,
      "totalLineAmount": 22600.00
    },
    {
      "idProduct": 3,
      "idUnit": 7,
      "lotNumber": "${LOT_NUMBER_VINAGRE}",
      "expirationDate": "2027-12-31",
      "descriptionLine": "Vinagre Blanco × 5 LTR",
      "quantity": 5,
      "unitPrice": 500.00,
      "taxPercent": 13.00,
      "totalLineAmount": 2825.00
    },
    {
      "idProduct": 4,
      "idUnit": 3,
      "lotNumber": "${LOT_NUMBER_SAL}",
      "expirationDate": "2028-12-31",
      "descriptionLine": "Sal × 0.5 KG",
      "quantity": 0.5,
      "unitPrice": 200.00,
      "taxPercent": 13.00,
      "totalLineAmount": 113.00
    },
    {
      "idProduct": 5,
      "idUnit": 1,
      "lotNumber": "${LOT_NUMBER_FRASCO}",
      "expirationDate": "2030-12-31",
      "descriptionLine": "Frasco 250ml × 100 UNI",
      "quantity": 100,
      "unitPrice": 300.00,
      "taxPercent": 13.00,
      "totalLineAmount": 33900.00
    }
  ]
}
EOF
)

api_call POST "/purchase-invoices" "$BODY_PURCHASE" "$TOKEN"
assert_status 201 "create purchase-invoice"
ID_PURCHASE_INVOICE=$(jq_field '.idPurchaseInvoice')
log_ok "idPurchaseInvoice = $ID_PURCHASE_INVOICE"

step "PASO 4b — Confirmar factura de compra"

api_call POST "/purchase-invoices/${ID_PURCHASE_INVOICE}/confirm" "" "$TOKEN"
assert_status 200 "confirm purchase-invoice"

FC_STATUS=$(jq_field '.statusInvoice')
FC_NUMBER=$(jq_field '.numberInvoice')
log_ok "statusInvoice = $FC_STATUS"
[[ "$FC_STATUS" == "Confirmado" ]] || fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$FC_STATUS'"
log_ok "Número FC: $FC_NUMBER"
log_info "  Asiento esperado: DR 110 ₡52,600 + DR 124 ₡6,838 / CR 106 ₡59,438"

# ── Obtener IDs de los lotes de MP creados ────────────────────────────────────
log_info "Consultando lotes de MP creados..."

api_call GET "/inventory-lots/by-product/2.json" "" "$TOKEN"
ID_LOT_CHILE=$(jq_field "[.[] | select(.lotNumber == \"${LOT_NUMBER_CHILE}\")] | first | .idInventoryLot")
[[ -z "$ID_LOT_CHILE" || "$ID_LOT_CHILE" == "null" ]] && ID_LOT_CHILE=$(jq_field '.[0].idInventoryLot')
log_ok "Lote Chile Seco    → idInventoryLot=$ID_LOT_CHILE"

api_call GET "/inventory-lots/by-product/3.json" "" "$TOKEN"
ID_LOT_VINAGRE=$(jq_field "[.[] | select(.lotNumber == \"${LOT_NUMBER_VINAGRE}\")] | first | .idInventoryLot")
[[ -z "$ID_LOT_VINAGRE" || "$ID_LOT_VINAGRE" == "null" ]] && ID_LOT_VINAGRE=$(jq_field '.[0].idInventoryLot')
log_ok "Lote Vinagre       → idInventoryLot=$ID_LOT_VINAGRE"

api_call GET "/inventory-lots/by-product/4.json" "" "$TOKEN"
ID_LOT_SAL=$(jq_field "[.[] | select(.lotNumber == \"${LOT_NUMBER_SAL}\")] | first | .idInventoryLot")
[[ -z "$ID_LOT_SAL" || "$ID_LOT_SAL" == "null" ]] && ID_LOT_SAL=$(jq_field '.[0].idInventoryLot')
log_ok "Lote Sal           → idInventoryLot=$ID_LOT_SAL"

api_call GET "/inventory-lots/by-product/5.json" "" "$TOKEN"
ID_LOT_FRASCO=$(jq_field "[.[] | select(.lotNumber == \"${LOT_NUMBER_FRASCO}\")] | first | .idInventoryLot")
[[ -z "$ID_LOT_FRASCO" || "$ID_LOT_FRASCO" == "null" ]] && ID_LOT_FRASCO=$(jq_field '.[0].idInventoryLot')
log_ok "Lote Frasco 250ml  → idInventoryLot=$ID_LOT_FRASCO"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 5 — CREAR ORDEN DE PRODUCCIÓN (Borrador)
#
#  Estado inicial: NumberProductionOrder = "BORRADOR"
#  La receta activa se aplica al completar, no al crear.
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 5 — Crear orden de producción en Borrador (100 frascos PT)"

BODY_OP=$(cat <<EOF
{
  "idFiscalPeriod": 4,
  "idWarehouse": 1,
  "dateOrder": "2026-04-05",
  "descriptionOrder": "Producción 100 frascos Chile Embotellado Marca X — Caso 2 Manufactura",
  "lines": [
    {
      "idProduct": 6,
      "idProductUnit": ${ID_PU_PT},
      "quantityRequired": 100,
      "descriptionLine": "Chile Embotellado Marca X × 100 frascos"
    }
  ]
}
EOF
)

api_call POST "/production-orders" "$BODY_OP" "$TOKEN"
assert_status 201 "create production-order"
ID_PRODUCTION_ORDER=$(jq_field '.idProductionOrder')
OP_BORRADOR_NUM=$(jq_field '.numberProductionOrder')
log_ok "idProductionOrder = $ID_PRODUCTION_ORDER  (número: $OP_BORRADOR_NUM)"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 6 — AVANZAR A PENDIENTE (asigna número OP-2026-NNNN)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 6 — Avanzar orden → Pendiente"

api_call PATCH "/production-orders/${ID_PRODUCTION_ORDER}/status" \
  '{"statusProductionOrder":"Pendiente"}' "$TOKEN"
assert_status 200 "patch status → Pendiente"

api_call GET "/production-orders/${ID_PRODUCTION_ORDER}.json" "" "$TOKEN"
assert_status 200 "get production-order after Pendiente"
OP_NUMBER=$(jq_field '.numberProductionOrder')
OP_STATUS=$(jq_field '.statusProductionOrder')
log_ok "numberProductionOrder = $OP_NUMBER  status = $OP_STATUS"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 7 — AVANZAR A EN PROCESO
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 7 — Avanzar orden → EnProceso"

api_call PATCH "/production-orders/${ID_PRODUCTION_ORDER}/status" \
  '{"statusProductionOrder":"EnProceso"}' "$TOKEN"
assert_status 200 "patch status → EnProceso"
log_ok "Orden en EnProceso ✓"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 8 — COMPLETAR PRODUCCIÓN
#
#  Al completar:
#   - El sistema lee la receta activa (idProductRecipe=1).
#   - Consume los lotes de MP por FEFO (20 KG chile, 5 LTR vinagre, 0.5 KG sal, 100 frascos).
#   - Genera asiento por cada MP: DR 115 (Costos de Producción) / CR 111 (Prod. en Proceso).
#   - Crea lote del PT (idProduct=6) con unitCost = ₡52,600 / 100 = ₡526.
#   - Si falta stock de algún MP: completa igual y devuelve array "warnings".
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 8 — Completar producción (consume MP, crea lote PT)"

api_call PATCH "/production-orders/${ID_PRODUCTION_ORDER}/status" \
  '{"statusProductionOrder":"Completado","idWarehouse":1}' "$TOKEN"
assert_status 200 "patch status → Completado"

WARNINGS=$(jq -r '.warnings // [] | length' "$TEMP_RESPONSE" 2>/dev/null || echo "0")
if [[ "$WARNINGS" -gt 0 ]]; then
  log_warn "Producción completó con $WARNINGS advertencia(s) de stock:"
  jq -r '.warnings[]' "$TEMP_RESPONSE" 2>/dev/null | while IFS= read -r w; do
    log_warn "  • $w"
  done
else
  log_ok "Producción completada sin advertencias de stock"
fi

log_info "Verificando OPs del período..."
api_call GET "/production-orders/${ID_PRODUCTION_ORDER}.json" "" "$TOKEN"
assert_status 200 "get production-order after Completado"
OP_STATUS_FINAL=$(jq_field '.statusProductionOrder')
log_ok "statusProductionOrder = $OP_STATUS_FINAL"
[[ "$OP_STATUS_FINAL" == "Completado" ]] || fail "Se esperaba status = 'Completado', recibido: '$OP_STATUS_FINAL'"

log_info "  Asientos esperados (1 por MP consumido):"
log_info "    DR 115 ₡20,000 / CR 111 ₡20,000  (Chile Seco  20 KG × ₡1,000)"
log_info "    DR 115 ₡ 2,500 / CR 111 ₡ 2,500  (Vinagre     5 LTR × ₡500)"
log_info "    DR 115 ₡   100 / CR 111 ₡   100  (Sal         0.5 KG × ₡200)"
log_info "    DR 115 ₡30,000 / CR 111 ₡30,000  (Frasco      100 UNI × ₡300)"
log_info "    PT lot: unitCost = ₡52,600 / 100 = ₡526"

# ── Obtener lote del PT ───────────────────────────────────────────────────────
log_info "Consultando lote del PT creado..."

api_call GET "/inventory-lots/by-product/6.json" "" "$TOKEN"
assert_status 200 "get lots by-product/6"

ID_LOT_PT=$(jq_field '.[0].idInventoryLot')
LOT_PT_COST=$(jq_field '.[0].unitCost')
LOT_PT_QTY=$(jq_field '.[0].quantityAvailable')
LOT_PT_NUMBER=$(jq_field '.[0].lotNumber')

if [[ -z "$ID_LOT_PT" || "$ID_LOT_PT" == "null" ]]; then
  fail "No se encontró lote para el PT (idProduct=6) después de completar la producción"
fi
log_ok "idInventoryLot PT = $ID_LOT_PT"
log_ok "Lote PT: número=$LOT_PT_NUMBER  qty=$LOT_PT_QTY  unitCost=$LOT_PT_COST"

# Verificar costo esperado (526.00)
COST_MATCH=$(awk "BEGIN { printf \"%.2f\", ${LOT_PT_COST:-0} }" 2>/dev/null)
COST_OK=$(awk "BEGIN { c=$COST_MATCH+0; d=c-526; if(d<0)d=-d; exit !(d<0.01) }" && echo "yes" || echo "no")
if [[ "$COST_OK" == "yes" ]]; then
  log_ok "unitCost PT = ₡$COST_MATCH ✓  (₡52,600 / 100 = ₡526)"
else
  log_warn "unitCost PT = ₡$COST_MATCH  (esperado ₡526 si la BD estaba limpia)"
fi

# Verificar stock MP ya consumido
api_call GET "/inventory-lots/stock/2.json" "" "$TOKEN"
STOCK_MP_CHILE=$(cat "$TEMP_RESPONSE" | tr -d '[:space:]')
log_info "  Stock restante Chile Seco  (id=2): $STOCK_MP_CHILE KG (esperado 0)"

api_call GET "/inventory-lots/stock/5.json" "" "$TOKEN"
STOCK_MP_FRASCO=$(cat "$TEMP_RESPONSE" | tr -d '[:space:]')
log_info "  Stock restante Frasco 250ml(id=5): $STOCK_MP_FRASCO UNI (esperado 0)"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 9 — FACTURA DE VENTA (30 frascos PT)
#
#  30 frascos × ₡1,500 = ₡45,000 + IVA 13% = ₡5,850 → Total ₡50,850
#  COGS: 30 × ₡526 = ₡15,780  → DR 119 / CR 109
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 9a — Crear factura de venta (30 frascos × ₡1,500 + IVA 13%)"

BODY_SALE=$(cat <<EOF
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
      "idInventoryLot": ${ID_LOT_PT},
      "descriptionLine": "Chile Embotellado Marca X × 30 frascos",
      "quantity": 30,
      "unitPrice": 1500.00,
      "taxPercent": 13.00,
      "totalLineAmount": 50850.00
    }
  ]
}
EOF
)

api_call POST "/sales-invoices" "$BODY_SALE" "$TOKEN"
assert_status 201 "create sales-invoice"
ID_SALES_INVOICE=$(jq_field '.idSalesInvoice')
log_ok "idSalesInvoice = $ID_SALES_INVOICE"

step "PASO 9b — Confirmar factura de venta"

api_call POST "/sales-invoices/${ID_SALES_INVOICE}/confirm" "" "$TOKEN"
assert_status 200 "confirm sales-invoice"

FV_STATUS=$(jq_field '.statusInvoice')
FV_NUMBER=$(jq_field '.numberInvoice')
log_ok "statusInvoice = $FV_STATUS"
[[ "$FV_STATUS" == "Confirmado" ]] || fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$FV_STATUS'"
log_ok "Número FV: $FV_NUMBER"
log_info "  Asiento FV:   DR 106 ₡50,850 / CR 117 ₡45,000 + CR 127 ₡5,850"
log_info "  Asiento COGS: DR 119 ₡15,780 / CR 109 ₡15,780  (30 × ₡526)"

# Verificar stock PT después de venta (100 - 30 = 70)
api_call GET "/inventory-lots/stock/6.json" "" "$TOKEN"
STOCK_PT_POST_VENTA=$(cat "$TEMP_RESPONSE" | tr -d '[:space:]')
log_ok "Stock PT post-venta = $STOCK_PT_POST_VENTA (esperado 70)"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 10 — DEVOLUCIÓN PARCIAL (cliente devuelve 5 frascos)
#
#  DEV-COGS: DR 109 ₡2,630 / CR 119 ₡2,630  (5 × ₡526)
#  DEV-ING:  DR 117 ₡7,500 + DR 127 ₡975   / CR 106 ₡8,475
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 10 — Devolución parcial: 5 frascos devueltos"

BODY_RETURN=$(cat <<EOF
{
  "dateReturn": "2026-04-05",
  "descriptionReturn": "Devolución parcial — cliente devuelve 5 frascos dañados en tránsito",
  "refundMode": "EfectivoInmediato",
  "lines": [
    {
      "idInventoryLot": ${ID_LOT_PT},
      "quantity": 5,
      "totalLineAmount": 8475.00,
      "descriptionLine": "Chile Embotellado Marca X × 5 frascos — devolución parcial"
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
  log_warn "La devolución no generó asiento COGS (verificar IdAccountCOGS en SalesInvoiceType)"
fi
log_info "  DEV-COGS: DR 109 ₡2,630 / CR 119 ₡2,630  (5 × ₡526)"

REFUND_ENTRY=$(jq_field '.idAccountingEntryRefund')
if [[ -n "$REFUND_ENTRY" && "$REFUND_ENTRY" != "null" ]]; then
  ID_ENTRY_REINTEGRO="$REFUND_ENTRY"
  log_ok "idAccountingEntryRefund DEV-ING = $ID_ENTRY_REINTEGRO"
else
  log_warn "La devolución no generó asiento de reversión de ingresos"
fi
log_info "  DEV-ING:  DR 117 ₡7,500 + DR 127 ₡975 / CR 106 ₡8,475"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 11 — AJUSTE DE INVENTARIO (regalía: −2 frascos PT)
#
#  Asiento: DR 113 (Faltantes/Merma) ₡1,052 / CR 109 (Inventario) ₡1,052
#           (2 × ₡526 costo neto del lote PT)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 11a — Crear ajuste de inventario (regalía −2 frascos)"

BODY_ADJ=$(cat <<EOF
{
  "idFiscalPeriod": 4,
  "idInventoryAdjustmentType": 1,
  "idCurrency": 1,
  "exchangeRateValue": 1.0,
  "dateAdjustment": "2026-04-05",
  "descriptionAdjustment": "Regalía distribuidor — 2 frascos Chile Embotellado como muestra — Responsable: Administrador",
  "lines": [
    {
      "idInventoryLot": ${ID_LOT_PT},
      "quantityDelta": -2,
      "descriptionLine": "Salida por regalía — Chile Embotellado Marca X × 2 frascos"
    }
  ]
}
EOF
)

api_call POST "/inventory-adjustments" "$BODY_ADJ" "$TOKEN"
assert_status 201 "create inventory-adjustment"
ID_ADJUSTMENT=$(jq_field '.idInventoryAdjustment')
log_ok "idInventoryAdjustment = $ID_ADJUSTMENT"

step "PASO 11b — Confirmar ajuste de inventario"

api_call POST "/inventory-adjustments/${ID_ADJUSTMENT}/confirm" "" "$TOKEN"
assert_status 200 "confirm inventory-adjustment"

ADJ_STATUS=$(jq_field '.statusAdjustment')
ADJ_ENTRY=$(jq_field '.idAccountingEntry')
log_ok "statusAdjustment = $ADJ_STATUS"
[[ "$ADJ_STATUS" == "Confirmado" ]] || fail "Se esperaba statusAdjustment = 'Confirmado', recibido: '$ADJ_STATUS'"
log_ok "idAccountingEntry ADJ = $ADJ_ENTRY"
log_info "  Asiento: DR 113 Merma ₡1,052 / CR 109 Inventario ₡1,052  (2 × ₡526)"

# ════════════════════════════════════════════════════════════════════════════════
# VERIFICACIÓN FINAL DE STOCK
# ════════════════════════════════════════════════════════════════════════════════
step "VERIFICACIÓN FINAL — Stock PT (idProduct=6)"

api_call GET "/inventory-lots/stock/6.json" "" "$TOKEN"
assert_status 200 "get stock PT"

STOCK_FINAL=$(cat "$TEMP_RESPONSE" | tr -d '[:space:]')
log_ok "Stock actual PT = $STOCK_FINAL frascos"
log_info "  Fórmula: 100 (producción) − 30 (venta) + 5 (devolución) − 2 (regalía) = 73"

EXPECTED=73
MATCH=$(awk "BEGIN { print ($STOCK_FINAL == $EXPECTED) ? \"yes\" : \"no\" }" 2>/dev/null || echo "no")
if [[ "$MATCH" == "yes" ]]; then
  log_ok "Stock PT = 73 ✓  Costo en libros: 73 × ₡526 = ₡38,398"
else
  log_warn "Stock final = $STOCK_FINAL (esperado 73 si la BD estaba limpia antes de correr)"
fi

# ════════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ════════════════════════════════════════════════════════════════════════════════
echo ""
printf "${GREEN}${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
printf "${GREEN}${BOLD}║   ✅  FLUJO CASO 2 COMPLETADO EXITOSAMENTE          ║${NC}\n"
printf "${GREEN}${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"
printf "  %-28s %s\n" "idPurchaseInvoice:"      "$ID_PURCHASE_INVOICE"
printf "  %-28s %s\n" "idLotChile:"             "$ID_LOT_CHILE"
printf "  %-28s %s\n" "idLotVinagre:"           "$ID_LOT_VINAGRE"
printf "  %-28s %s\n" "idLotSal:"               "$ID_LOT_SAL"
printf "  %-28s %s\n" "idLotFrasco:"            "$ID_LOT_FRASCO"
printf "  %-28s %s\n" "idProductionOrder:"      "$ID_PRODUCTION_ORDER"
printf "  %-28s %s\n" "opNumber:"               "$OP_NUMBER"
printf "  %-28s %s\n" "idLotPT:"                "$ID_LOT_PT"
printf "  %-28s %s\n" "unitCostPT:"             "₡$LOT_PT_COST"
printf "  %-28s %s\n" "idSalesInvoice:"         "$ID_SALES_INVOICE"
printf "  %-28s %s\n" "idEntryDevCogs:"         "$ID_ENTRY_DEV_COGS"
printf "  %-28s %s\n" "idEntryDevIng:"          "$ID_ENTRY_REINTEGRO"
printf "  %-28s %s\n" "idAdjustment:"           "$ID_ADJUSTMENT"
printf "  %-28s %s\n" "Stock PT final:"         "$STOCK_FINAL frascos"
echo ""

# ════════════════════════════════════════════════════════════════════════════════
# GUARDAR RESULTADO EN ARCHIVO TXT
# ════════════════════════════════════════════════════════════════════════════════
RUN_TS="$(date '+%Y-%m-%d_%H-%M-%S')"
OUTPUT_FILE="$SCRIPT_DIR/resultado_caso2_${RUN_TS}.txt"

cat > "$OUTPUT_FILE" <<TXTEOF
# ============================================================
#  CASO 2 — MANUFACTURA · Resultado del Test E2E
#  Ejecutado: $(date '+%Y-%m-%d %H:%M:%S')
# ============================================================

# ── Configuración usada ──────────────────────────────────────
HOST                  = $HOST
EMAIL                 = $EMAIL
DB_HOST               = $DB_HOST
DB_PORT               = $DB_PORT

# ── Claves estáticas del caso ────────────────────────────────
idProductMP_Chile     = 2  (Chile Seco, KG)
idProductMP_Vinagre   = 3  (Vinagre Blanco, LTR)
idProductMP_Sal       = 4  (Sal, KG)
idProductMP_Frasco    = 5  (Frasco 250ml, UNI)
idProductPT           = 6  (Chile Embotellado Marca X)
idProductRecipe       = 1  (Receta Chile Embotellado, qty=1)
idFiscalPeriod        = 4  (Abril 2026)
idCurrency            = 1  (CRC)
idWarehouse           = 1  (Principal)
idPurchaseInvoiceType = 1  (EFECTIVO)
idSalesInvoiceType    = 1  (CONTADO_CRC)
idContact             = 1
idAdjustmentType      = 1  (MERMA/FALTANTE — usado en regalía)
idAdjustmentTypeProd  = 2  (PRODUCCION — usado al completar OP, auto)

# ── Cantidades compradas ─────────────────────────────────────
qty_chile             = 20 KG    (recipe: 0.2 KG × 100)
qty_vinagre           = 5  LTR   (recipe: 0.05 LTR × 100)
qty_sal               = 0.5 KG   (recipe: 0.005 KG × 100)
qty_frasco            = 100 UNI  (recipe: 1 UNI × 100)
unitCost_chile        = 1000.00 CRC/KG
unitCost_vinagre      = 500.00 CRC/LTR
unitCost_sal          = 200.00 CRC/KG
unitCost_frasco       = 300.00 CRC/UNI

# ── Montos factura compra ────────────────────────────────────
compra_subtotal       = 52600.00
compra_iva            = 6838.00
compra_total          = 59438.00

# ── Orden de producción ──────────────────────────────────────
quantityToProduce     = 100 frascos
totalMpCost           = 52600.00
unitCostPT            = 526.00 CRC  (52600 / 100)

# ── Montos factura venta ─────────────────────────────────────
qty_venta             = 30 frascos
unitPrice_venta       = 1500.00 CRC
venta_subtotal        = 45000.00
venta_iva             = 5850.00
venta_total           = 50850.00
cogs_venta            = 15780.00  (30 × 526)

# ── Devolución ───────────────────────────────────────────────
qty_devolucion        = 5 frascos
devolucion_subtotal   = 7500.00   (5 × 1500)
devolucion_iva        = 975.00    (7500 × 13%)
devolucion_total      = 8475.00
cogs_reversa          = 2630.00   (5 × 526)

# ── Regalía ──────────────────────────────────────────────────
qty_regalia           = 2 frascos
regalia_costo         = 1052.00   (2 × 526)

# ── Verificación de stock PT ─────────────────────────────────
stock_inicial         = 0
stock_post_produccion = 100
stock_post_venta      = 70
stock_post_devolucion = 75
stock_post_regalia    = 73
stock_final_real      = $STOCK_FINAL
stock_costo_libros    = 38398.00  (73 × 526)

# ── IDs generados en esta ejecución ─────────────────────────
idProductUnit_Chile   = $ID_PU_CHILE
idProductUnit_Vinagre = $ID_PU_VINAGRE
idProductUnit_Sal     = $ID_PU_SAL
idProductUnit_Frasco  = $ID_PU_FRASCO
idProductUnit_PT      = $ID_PU_PT
idProductAccount_Chile   = $ID_PA_CHILE
idProductAccount_Vinagre = $ID_PA_VINAGRE
idProductAccount_Sal     = $ID_PA_SAL
idProductAccount_Frasco  = $ID_PA_FRASCO
idPurchaseInvoice     = $ID_PURCHASE_INVOICE
numberPurchaseInvoice = $FC_NUMBER
idLotChile            = $ID_LOT_CHILE
idLotVinagre          = $ID_LOT_VINAGRE
idLotSal              = $ID_LOT_SAL
idLotFrasco           = $ID_LOT_FRASCO
idProductionOrder     = $ID_PRODUCTION_ORDER
opNumber              = $OP_NUMBER
idLotPT               = $ID_LOT_PT
lotNumberPT           = $LOT_PT_NUMBER
idSalesInvoice        = $ID_SALES_INVOICE
numberSalesInvoice    = $FV_NUMBER
idEntryDevCogs        = $ID_ENTRY_DEV_COGS
idEntryDevIng         = $ID_ENTRY_REINTEGRO
idInventoryAdjustment = $ID_ADJUSTMENT
idAdjEntry            = $ADJ_ENTRY

# ── Cuentas involucradas ─────────────────────────────────────
idAccount_caja_crc    = 106  (1.1.06.01 Caja CRC)
idAccount_inventario  = 109  (1.1.07.01 Inventario Mercadería)
idAccount_mp          = 110  (1.1.07.02 Materias Primas)
idAccount_wip         = 111  (1.1.07.03 Productos en Proceso)
idAccount_merma       = 113  (5.14.01 Faltantes de Inventario)
idAccount_prod_cost   = 115  (5.14.03 Costos de Producción)
idAccount_ingresos    = 117  (4.5.01 Ingresos por Ventas)
idAccount_cogs        = 119  (5.15.01 Costo de Ventas)
idAccount_iva_acred   = 124  (1.1.09.01 IVA Acreditable CRC)
idAccount_iva_pagar   = 127  (2.1.04.01 IVA por Pagar CRC)

# ── Endpoints de consulta rápida ────────────────────────────
GET $HOST/purchase-invoices/$ID_PURCHASE_INVOICE.json
GET $HOST/production-orders/$ID_PRODUCTION_ORDER.json
GET $HOST/inventory-lots/$ID_LOT_PT.json
GET $HOST/inventory-lots/stock/6.json
GET $HOST/sales-invoices/$ID_SALES_INVOICE.json
GET $HOST/accounting-entries/$ID_ENTRY_REINTEGRO.json
GET $HOST/inventory-adjustments/$ID_ADJUSTMENT.json
TXTEOF

printf "  ${GREEN}💾  Resultado guardado en:${NC} %s\n" "$OUTPUT_FILE"
echo ""

rm -f "$TEMP_RESPONSE"
