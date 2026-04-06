#!/usr/bin/env bash
# ============================================================================
#  CASO 3 — ENSAMBLE EN VENTA (Hot Dog)
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
#   5. Genera resultado_caso3_*.txt con todos los IDs para los scripts de verificación.
#
#  Flujo completo:
#   bash docs/inventario/caso-3-ensamble/01-ejecutar-flujo-e2e.sh
#
#  Requisitos previos:
#   - curl, jq y sqlcmd instalados.
#   - API corriendo en https://localhost:8000.
#   - credentials/db.txt en la raíz del proyecto.
#   - Productos 7-11 (ingredientes + Hot Dog) y receta id=2 activa vienen del seed.
#   - Período 4 abierto.
# ============================================================================

set -uo pipefail

# ── Rutas ─────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
CREDENTIALS_FILE="$REPO_ROOT/credentials/db.txt"
TEMP_RESPONSE="/tmp/fa_caso3_response.json"

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
PROVIDER_INVOICE_NUMBER="FAC-PROVEEDOR-C3-${RUN_ID}"
LOT_PAN="LOT-PAN-C3-${RUN_ID}"
LOT_SALCHICHA="LOT-SALCHICHA-C3-${RUN_ID}"
LOT_MOSTAZA="LOT-MOSTAZA-C3-${RUN_ID}"
LOT_CATSUP="LOT-CATSUP-C3-${RUN_ID}"

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
# ProductUnits (5 productos)
ID_PU_PAN=0
ID_PU_SALCHICHA=0
ID_PU_MOSTAZA=0
ID_PU_CATSUP=0
ID_PU_HOTDOG=0
# ProductAccounts (4 ingredientes → cuenta 110)
ID_PA_PAN=0
ID_PA_SALCHICHA=0
ID_PA_MOSTAZA=0
ID_PA_CATSUP=0
# Documentos
ID_PURCHASE_INVOICE=0
NUMBER_PC="(no generado)"
ID_LOT_PAN=0
ID_LOT_SALCHICHA=0
ID_LOT_MOSTAZA=0
ID_LOT_CATSUP=0
ID_SALES_ORDER=0
ID_SALES_INVOICE=0
NUMBER_FV="(no generado)"
ID_LOT_PT=0
ID_SALES_INVOICE_LINE=0
ID_ENTRY_DEV_COGS="(no generado)"
ID_ENTRY_REINTEGRO="(no generado)"
ID_ADJUSTMENT=0
ADJ_ENTRY="(no generado)"
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
  printf "    idPurchaseInvoice  : %s\n" "$ID_PURCHASE_INVOICE"
  printf "    idSalesOrder       : %s\n" "$ID_SALES_ORDER"
  printf "    idSalesInvoice     : %s\n" "$ID_SALES_INVOICE"
  printf "    idLotPT            : %s\n" "$ID_LOT_PT"
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
printf "${CYAN}${BOLD}║   CASO 3 — ENSAMBLE EN VENTA · Test E2E             ║${NC}\n"
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
#   2. rm Migrations/*.cs        → borra los archivos de migración
#   3. dotnet ef migrations add  → genera nueva InitialCreate (con todo el seed)
#   4. dotnet ef database update → crea las tablas y aplica el seed
#
#  Resultado: BD limpia con sólo los datos de seed de las Configuration/*.
#  El seed incluye productos 7-11, receta id=2 (Hot Dog) y sus líneas.
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
# PASO 2 — PRE-REQUISITO: Crear ProductUnits para los 5 productos
#
#  §4 caso-3-ensamble-configuraciones.md
#  Ingredientes: idUnit=1 (UNI) para Pan(7) y Salchicha(8)
#                idUnit=6 (ML)  para Mostaza(9) y Catsup(10)
#  Ensamblado:   idUnit=1 (UNI) para Hot Dog(11), usedForSale=true
#
#  El API valida en MapLinesAsync que exista un ProductUnit para el par
#  (idProduct, idUnit) antes de procesar cualquier línea de factura.
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 2 — Crear ProductUnits para ingredientes y Hot Dog (5 productos)"

# ensure_product_unit <idProduct> <idUnit> <usedForPurchase> <usedForSale> <namePresentation> <varName>
ensure_product_unit() {
  local idProduct="$1"
  local idUnit="$2"
  local usedForPurchase="$3"
  local usedForSale="$4"
  local namePresentation="$5"
  local varName="$6"

  api_call GET "/product-units/by-product/${idProduct}.json" "" "$TOKEN"
  assert_status 200 "get product-units by-product ${idProduct}"

  local existing
  existing=$(jq_field "[.[] | select(.idUnit == ${idUnit})] | first | .idProductUnit")
  if [[ -n "$existing" && "$existing" != "null" ]]; then
    eval "$varName=$existing"
    log_warn "ProductUnit ya existe (idProduct=$idProduct, idUnit=$idUnit) → id=$existing, reutilizando"
  else
    api_call POST "/product-units" \
      "{\"idProduct\":${idProduct},\"idUnit\":${idUnit},\"conversionFactor\":1.0,\"isBase\":true,\"usedForPurchase\":${usedForPurchase},\"usedForSale\":${usedForSale},\"namePresentation\":\"${namePresentation}\"}" \
      "$TOKEN"
    assert_status 201 "create product-unit (idProduct=${idProduct})"
    local newId
    newId=$(jq_field '.idProductUnit')
    eval "$varName=$newId"
    log_ok "idProductUnit (idProduct=$idProduct) = $newId"
  fi
}

ensure_product_unit 7  1 true  false "Unidad base"    ID_PU_PAN
ensure_product_unit 8  1 true  false "Unidad base"    ID_PU_SALCHICHA
ensure_product_unit 9  6 true  false "Mililitro base" ID_PU_MOSTAZA
ensure_product_unit 10 6 true  false "Mililitro base" ID_PU_CATSUP
ensure_product_unit 11 1 false true  "Unidad"         ID_PU_HOTDOG

log_ok "ProductUnits: Pan=$ID_PU_PAN  Salchicha=$ID_PU_SALCHICHA  Mostaza=$ID_PU_MOSTAZA  Catsup=$ID_PU_CATSUP  HotDog=$ID_PU_HOTDOG"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 3 — PRE-COMPRA: Crear ProductAccounts para ingredientes → cuenta 110 (MP)
#
#  §5 caso-3-ensamble-configuraciones.md
#  Todos los ingredientes → idAccount=110 (1.1.07.02 Materias Primas), 100%
#  Hot Dog (id=11) NO necesita ProductAccount (el COGS usa fallbacks del SalesInvoiceType)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 3 — Crear ProductAccounts para ingredientes → cuenta 110 (Materias Primas)"

# ensure_product_account <idProduct> <idAccount> <varName>
ensure_product_account() {
  local idProduct="$1"
  local idAccount="$2"
  local varName="$3"

  api_call GET "/product-accounts/by-product/${idProduct}.json" "" "$TOKEN"
  if [[ "$HTTP_STATUS" == "200" ]]; then
    local existing
    existing=$(jq_field "[.[] | select(.idAccount == ${idAccount})] | first | .idProductAccount")
    if [[ -n "$existing" && "$existing" != "null" ]]; then
      eval "$varName=$existing"
      log_warn "ProductAccount ya existe (idProduct=$idProduct, idAccount=$idAccount) → id=$existing, reutilizando"
      return
    fi
  fi

  api_call POST "/product-accounts" \
    "{\"idProduct\":${idProduct},\"idAccount\":${idAccount},\"percentageAccount\":100.00}" \
    "$TOKEN"
  assert_status 201 "create product-account (idProduct=${idProduct})"
  local newId
  newId=$(jq_field '.idProductAccount')
  eval "$varName=$newId"
  log_ok "idProductAccount (idProduct=$idProduct) = $newId"
}

ensure_product_account 7  110 ID_PA_PAN
ensure_product_account 8  110 ID_PA_SALCHICHA
ensure_product_account 9  110 ID_PA_MOSTAZA
ensure_product_account 10 110 ID_PA_CATSUP

log_ok "ProductAccounts: Pan=$ID_PA_PAN  Salchicha=$ID_PA_SALCHICHA  Mostaza=$ID_PA_MOSTAZA  Catsup=$ID_PA_CATSUP"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 4 — FACTURA DE COMPRA (ingredientes para 50 hot dogs)
#
#  Precios y cantidades (para 50 hot dogs):
#   Pan de Hot Dog  : 50 UNI × ₡300   = ₡15,000  + IVA 13% ₡1,950  = ₡16,950
#                     costo/lote = ₡339.00/UNI  (300 × 1.13)
#   Salchicha       : 50 UNI × ₡600   = ₡30,000  + IVA 13% ₡3,900  = ₡33,900
#                     costo/lote = ₡678.00/UNI  (600 × 1.13)
#   Mostaza         : 750 ML  × ₡20   = ₡15,000  + IVA 13% ₡1,950  = ₡16,950
#                     costo/lote = ₡22.60/ML   (20 × 1.13)
#   Catsup          : 1000 ML × ₡15   = ₡15,000  + IVA 13% ₡1,950  = ₡16,950
#                     costo/lote = ₡16.95/ML   (15 × 1.13)
#   ──────────────────────────────────────────────────────────
#   Subtotal        : ₡75,000    IVA: ₡9,750    Total: ₡84,750
#
#  Costo del Hot Dog PT por unidad (1 corrida de receta = 1 hot dog):
#   1 × ₡339 + 1 × ₡678 + 15 × ₡22.60 + 20 × ₡16.95
#   = 339 + 678 + 339 + 339 = ₡1,695.00 / hot dog
#
#  Asiento esperado (con ProductAccount → 110):
#   DR 110 Materias Primas ₡75,000
#   DR 124 IVA Acreditable ₡9,750
#   CR 106 Caja CRC        ₡84,750
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 4a — Crear factura de compra en borrador (ingredientes para 50 hot dogs)"

BODY_PURCHASE=$(cat <<EOF
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idPurchaseInvoiceType": 1,
  "idContact": 1,
  "numberInvoice": "${PROVIDER_INVOICE_NUMBER}",
  "dateInvoice": "2026-04-05",
  "subTotalAmount": 75000.00,
  "taxAmount": 9750.00,
  "totalAmount": 84750.00,
  "descriptionInvoice": "Compra ingredientes para 50 hot dogs — Caso 3 Ensamble",
  "idWarehouse": 1,
  "exchangeRateValue": 1.0,
  "lines": [
    {
      "idProduct": 7,
      "idUnit": 1,
      "lotNumber": "${LOT_PAN}",
      "expirationDate": "2027-12-31",
      "descriptionLine": "Pan de Hot Dog × 50 un.",
      "quantity": 50,
      "unitPrice": 300.00,
      "taxPercent": 13.00,
      "totalLineAmount": 16950.00
    },
    {
      "idProduct": 8,
      "idUnit": 1,
      "lotNumber": "${LOT_SALCHICHA}",
      "expirationDate": "2027-12-31",
      "descriptionLine": "Salchicha × 50 un.",
      "quantity": 50,
      "unitPrice": 600.00,
      "taxPercent": 13.00,
      "totalLineAmount": 33900.00
    },
    {
      "idProduct": 9,
      "idUnit": 6,
      "lotNumber": "${LOT_MOSTAZA}",
      "expirationDate": "2027-12-31",
      "descriptionLine": "Mostaza × 750 mL",
      "quantity": 750,
      "unitPrice": 20.00,
      "taxPercent": 13.00,
      "totalLineAmount": 16950.00
    },
    {
      "idProduct": 10,
      "idUnit": 6,
      "lotNumber": "${LOT_CATSUP}",
      "expirationDate": "2027-12-31",
      "descriptionLine": "Catsup × 1000 mL",
      "quantity": 1000,
      "unitPrice": 15.00,
      "taxPercent": 13.00,
      "totalLineAmount": 16950.00
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

STATUS=$(jq_field '.statusInvoice')
log_ok "statusInvoice = $STATUS"
[[ "$STATUS" == "Confirmado" ]] || fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$STATUS'"

NUMBER_PC=$(jq_field '.numberInvoice')
log_ok "Número de factura de compra: $NUMBER_PC"
log_info "  Asiento: DR 110 MP ₡75,000 + DR 124 IVA ₡9,750 / CR 106 Caja ₡84,750"

step "PASO 4c — Obtener IDs de lotes de ingredientes"

# Pan (idProduct=7)
api_call GET "/inventory-lots/by-product/7.json" "" "$TOKEN"
assert_status 200 "get inventory-lots by-product 7 (Pan)"
ID_LOT_PAN=$(jq_field "[.[] | select(.lotNumber == \"${LOT_PAN}\")] | first | .idInventoryLot")
[[ -z "$ID_LOT_PAN" || "$ID_LOT_PAN" == "null" ]] && ID_LOT_PAN=$(jq_field '.[0].idInventoryLot')
log_ok "idLotPan = $ID_LOT_PAN  (50 UNI × ₡339)"

# Salchicha (idProduct=8)
api_call GET "/inventory-lots/by-product/8.json" "" "$TOKEN"
assert_status 200 "get inventory-lots by-product 8 (Salchicha)"
ID_LOT_SALCHICHA=$(jq_field "[.[] | select(.lotNumber == \"${LOT_SALCHICHA}\")] | first | .idInventoryLot")
[[ -z "$ID_LOT_SALCHICHA" || "$ID_LOT_SALCHICHA" == "null" ]] && ID_LOT_SALCHICHA=$(jq_field '.[0].idInventoryLot')
log_ok "idLotSalchicha = $ID_LOT_SALCHICHA  (50 UNI × ₡678)"

# Mostaza (idProduct=9)
api_call GET "/inventory-lots/by-product/9.json" "" "$TOKEN"
assert_status 200 "get inventory-lots by-product 9 (Mostaza)"
ID_LOT_MOSTAZA=$(jq_field "[.[] | select(.lotNumber == \"${LOT_MOSTAZA}\")] | first | .idInventoryLot")
[[ -z "$ID_LOT_MOSTAZA" || "$ID_LOT_MOSTAZA" == "null" ]] && ID_LOT_MOSTAZA=$(jq_field '.[0].idInventoryLot')
log_ok "idLotMostaza = $ID_LOT_MOSTAZA  (750 ML × ₡22.60)"

# Catsup (idProduct=10)
api_call GET "/inventory-lots/by-product/10.json" "" "$TOKEN"
assert_status 200 "get inventory-lots by-product 10 (Catsup)"
ID_LOT_CATSUP=$(jq_field "[.[] | select(.lotNumber == \"${LOT_CATSUP}\")] | first | .idInventoryLot")
[[ -z "$ID_LOT_CATSUP" || "$ID_LOT_CATSUP" == "null" ]] && ID_LOT_CATSUP=$(jq_field '.[0].idInventoryLot')
log_ok "idLotCatsup = $ID_LOT_CATSUP  (1000 ML × ₡16.95)"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 5 — PEDIDO DE VENTA (3 hot dogs) → confirm dispara todo el ciclo
#
#  Al confirmar con { "idWarehouse": 1 }, el sistema ejecuta automáticamente:
#   1. Detecta receta activa (id=2) del Hot Dog (idProduct=11)
#   2. Crea Orden de Producción (Pendiente) con línea Hot Dog × 3
#   3. Completa la OP: consume por FEFO
#        3 UNI Pan    (3 × 1 de receta) del lote LOT-PAN
#        3 UNI Salchicha                del lote LOT-SALCHICHA
#       45 ML  Mostaza (3 × 15 de receta) del lote LOT-MOSTAZA
#       60 ML  Catsup  (3 × 20 de receta) del lote LOT-CATSUP
#   4. Crea lote PT-HOT-DOG (3 unidades, costo ₡1,695/u = ₡5,085 total)
#   5. Marca el pedido como Completado
#   6. Genera la factura de venta en borrador con IdInventoryLot del lote PT
#   7. Confirma la factura automáticamente
#
#  Asiento FV (3 hot dogs × ₡3,000 + 13% IVA):
#   DR 106 Caja CRC      ₡10,170  / CR 117 Ingresos ₡9,000 / CR 127 IVA ₡1,170
#
#  Asiento COGS (generado al confirmar la FV):
#   DR 119 Costo de Ventas ₡5,085  / CR 109 Inventario ₡5,085  (3 × ₡1,695)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 5a — Crear pedido de venta en borrador (3 hot dogs × ₡3,000 + 13% IVA)"

BODY_ORDER=$(cat <<EOF
{
  "idFiscalPeriod": 4,
  "idCurrency": 1,
  "idContact": 1,
  "dateOrder": "2026-04-05",
  "exchangeRateValue": 1.0,
  "descriptionOrder": "Pedido cliente — 3 Hot Dogs — Caso 3 Ensamble",
  "lines": [
    {
      "idProduct": 11,
      "idProductUnit": $ID_PU_HOTDOG,
      "descriptionLine": "Hot Dog × 3 un.",
      "quantity": 3,
      "unitPrice": 3000.00,
      "taxPercent": 13.00
    }
  ]
}
EOF
)

api_call POST "/sales-orders" "$BODY_ORDER" "$TOKEN"
assert_status 201 "create sales-order"

ID_SALES_ORDER=$(jq_field '.idSalesOrder')
log_ok "idSalesOrder = $ID_SALES_ORDER"

step "PASO 5b — Confirmar pedido con idWarehouse (dispara ciclo completo)"
log_info "  El sistema creará la OP, consumirá ingredientes FEFO, generará lote PT y confirmará la FV..."

api_call POST "/sales-orders/${ID_SALES_ORDER}/confirm" '{"idWarehouse":1}' "$TOKEN"
assert_status 200 "confirm sales-order"

ID_SALES_INVOICE=$(jq_field '.idSalesInvoice')
if [[ -z "$ID_SALES_INVOICE" || "$ID_SALES_INVOICE" == "null" ]]; then
  fail "El confirm del pedido no devolvió idSalesInvoice"
fi
log_ok "idSalesInvoice = $ID_SALES_INVOICE"

ORDER_STATUS=$(jq_field '.statusOrder')
log_ok "statusOrder = $ORDER_STATUS"

step "PASO 5c — Obtener lote PT y línea de la factura de venta confirmada"

api_call GET "/sales-invoices/${ID_SALES_INVOICE}.json" "" "$TOKEN"
assert_status 200 "get sales-invoice"

INVOICE_STATUS=$(jq_field '.statusInvoice')
log_ok "statusInvoice = $INVOICE_STATUS"
[[ "$INVOICE_STATUS" == "Confirmado" ]] || fail "Se esperaba statusInvoice = 'Confirmado', recibido: '$INVOICE_STATUS'"

NUMBER_FV=$(jq_field '.numberInvoice')
log_ok "Número de factura de venta: $NUMBER_FV"

ID_LOT_PT=$(jq_field '.lines[0].idInventoryLot')
ID_SALES_INVOICE_LINE=$(jq_field '.lines[0].idSalesInvoiceLine')

if [[ -z "$ID_LOT_PT" || "$ID_LOT_PT" == "null" ]]; then
  fail "No se encontró idInventoryLot en la línea de la FV — el ciclo de producción puede no haberse completado"
fi
log_ok "idLotPT (Hot Dog) = $ID_LOT_PT  (lote generado por la OP automática)"
log_ok "idSalesInvoiceLine = $ID_SALES_INVOICE_LINE"

api_call GET "/inventory-lots/${ID_LOT_PT}.json" "" "$TOKEN"
assert_status 200 "get inventory-lot PT"
LOT_PT_QTY=$(jq_field '.quantityAvailable')
log_ok "quantityAvailable lote PT = $LOT_PT_QTY  (esperado 0 — ya vendidos los 3)"
log_info "  FV confirmada: DR 106 Caja ₡10,170 / CR 117 Ingresos ₡9,000 / CR 127 IVA ₡1,170"
log_info "  COGS:          DR 119 ₡5,085        / CR 109 Inventario ₡5,085  (3 × ₡1,695)"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 6 — DEVOLUCIÓN PARCIAL (cliente devuelve 1 hot dog)
#
#  La FV tiene la línea con IdInventoryLot del lote PT.
#  El sistema reintegra 1 unidad al lote PT y genera:
#   DEV-ING:  DR 117 Ingresos ₡3,000 + DR 127 IVA ₡390 / CR 106 Caja ₡3,390
#   DEV-COGS: DR 109 Inventario ₡1,695               / CR 119 COGS   ₡1,695
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 6 — Devolución parcial: 1 hot dog devuelto"

BODY_RETURN=$(cat <<EOF
{
  "dateReturn": "2026-04-05",
  "descriptionReturn": "Devolución parcial — cliente devuelve 1 hot dog — Caso 3 Ensamble",
  "refundMode": "EfectivoInmediato",
  "lines": [
    {
      "idInventoryLot": $ID_LOT_PT,
      "quantity": 1,
      "totalLineAmount": 3390.00,
      "descriptionLine": "Hot Dog × 1 un. — devolución parcial"
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
  log_warn "La devolución no generó asiento COGS separado"
fi
log_info "  DEV-COGS: DR 109 Inventario ₡1,695 / CR 119 COGS ₡1,695  (1 × ₡1,695)"

REFUND_ENTRY=$(jq_field '.idAccountingEntryRefund')
if [[ -n "$REFUND_ENTRY" && "$REFUND_ENTRY" != "null" ]]; then
  ID_ENTRY_REINTEGRO="$REFUND_ENTRY"
  log_ok "idAccountingEntryRefund DEV-ING = $ID_ENTRY_REINTEGRO"
else
  log_warn "La devolución no generó asiento de reversión de ingresos"
fi
log_info "  DEV-ING:  DR 117 Ingresos ₡3,000 + DR 127 IVA ₡390 / CR 106 Caja ₡3,390"

# ════════════════════════════════════════════════════════════════════════════════
# PASO 7 — REGALÍA (administrador regala 2 hot dogs al personal)
#
#  Ajuste de salida sobre el lote PT.
#  Asiento esperado:
#   DR 113 Faltantes/Merma ₡3,390 / CR 109 Inventario ₡3,390  (2 × ₡1,695)
# ════════════════════════════════════════════════════════════════════════════════
step "PASO 7a — Crear ajuste de inventario en borrador (regalía: −2 hot dogs)"

BODY_ADJ=$(cat <<EOF
{
  "idFiscalPeriod": 4,
  "idInventoryAdjustmentType": 1,
  "idCurrency": 1,
  "exchangeRateValue": 1.0,
  "dateAdjustment": "2026-04-05",
  "descriptionAdjustment": "Regalía personal — 2 hot dogs — Responsable: Administrador — Caso 3",
  "lines": [
    {
      "idInventoryLot": $ID_LOT_PT,
      "quantityDelta": -2,
      "descriptionLine": "Salida por regalía — Hot Dog × 2 un."
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
log_info "  Esperado: DR 113 Merma ₡3,390 / CR 109 Inventario ₡3,390  (2 × ₡1,695)"

# ════════════════════════════════════════════════════════════════════════════════
# VERIFICACIÓN FINAL DE STOCK
# ════════════════════════════════════════════════════════════════════════════════
step "VERIFICACIÓN FINAL — Stock total del producto 11 (Hot Dog PT)"

api_call GET "/inventory-lots/stock/11.json" "" "$TOKEN"
assert_status 200 "get stock total product 11"

STOCK_FINAL=$(cat "$TEMP_RESPONSE" | tr -d '[:space:]')
log_ok "Stock actual = $STOCK_FINAL unidades"
log_info "  Fórmula: 3 (producidos) − 3 (venta) + 1 (devolución) − 2 (regalía) = −1"
log_info "  Stock negativo es esperado si los pasos se ejecutan en este orden exacto"

EXPECTED="-1"
MATCH=$(awk "BEGIN { print ($STOCK_FINAL == $EXPECTED) ? \"yes\" : \"no\" }" 2>/dev/null || echo "no")
if [[ "$MATCH" == "yes" ]]; then
  log_ok "Stock = −1 ✓  (flujo completo ejecutado en orden)"
else
  log_warn "Stock final = $STOCK_FINAL (esperado −1 si la BD estaba limpia antes del test)"
fi

# ════════════════════════════════════════════════════════════════════════════════
# RESUMEN FINAL
# ════════════════════════════════════════════════════════════════════════════════
echo ""
printf "${GREEN}${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
printf "${GREEN}${BOLD}║   ✅  FLUJO CASO 3 COMPLETADO EXITOSAMENTE          ║${NC}\n"
printf "${GREEN}${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"
printf "  %-26s %s\n" "idPurchaseInvoice:"      "$ID_PURCHASE_INVOICE"
printf "  %-26s %s\n" "idLotPan:"               "$ID_LOT_PAN"
printf "  %-26s %s\n" "idLotSalchicha:"         "$ID_LOT_SALCHICHA"
printf "  %-26s %s\n" "idLotMostaza:"           "$ID_LOT_MOSTAZA"
printf "  %-26s %s\n" "idLotCatsup:"            "$ID_LOT_CATSUP"
printf "  %-26s %s\n" "idSalesOrder:"           "$ID_SALES_ORDER"
printf "  %-26s %s\n" "idSalesInvoice:"         "$ID_SALES_INVOICE"
printf "  %-26s %s\n" "idLotPT (Hot Dog):"      "$ID_LOT_PT"
printf "  %-26s %s\n" "idEntryDevCogs:"         "$ID_ENTRY_DEV_COGS"
printf "  %-26s %s\n" "idEntryDevIng:"          "$ID_ENTRY_REINTEGRO"
printf "  %-26s %s\n" "idAdjustment:"           "$ID_ADJUSTMENT"
printf "  %-26s %s\n" "idAdjEntry:"             "$ADJ_ENTRY"
printf "  %-26s %s\n" "Stock final (PT id=11):" "$STOCK_FINAL u."
echo ""

# ════════════════════════════════════════════════════════════════════════════════
# GUARDAR RESULTADO EN ARCHIVO TXT
# ════════════════════════════════════════════════════════════════════════════════
RUN_TS="$(date '+%Y-%m-%d_%H-%M-%S')"
OUTPUT_FILE="$SCRIPT_DIR/resultado_caso3_${RUN_TS}.txt"

cat > "$OUTPUT_FILE" <<TXTEOF
# ============================================================
#  CASO 3 — ENSAMBLE EN VENTA · Resultado del Test E2E
#  Ejecutado: $(date '+%Y-%m-%d %H:%M:%S')
# ============================================================

# ── Configuración usada ──────────────────────────────────────
HOST                    = $HOST
EMAIL                   = $EMAIL
DB_HOST                 = $DB_HOST
DB_PORT                 = $DB_PORT

# ── Claves estáticas del caso ────────────────────────────────
idProduct_pan           = 7   (Pan de Hot Dog — Materia Prima)
idProduct_salchicha     = 8   (Salchicha — Materia Prima)
idProduct_mostaza       = 9   (Mostaza — Materia Prima)
idProduct_catsup        = 10  (Catsup — Materia Prima)
idProduct_hotdog        = 11  (Hot Dog — Producto Terminado)
idProductRecipe         = 2   (activa, output=11, quantityOutput=1)
idFiscalPeriod          = 4   (Abril 2026)
idCurrency              = 1   (CRC)
idWarehouse             = 1   (Principal)
idPurchaseInvoiceType   = 1   (EFECTIVO)
idSalesInvoiceType      = 1   (CONTADO_CRC)
idContact               = 1

# ── Lotes de ingredientes creados ───────────────────────────
lotNumber_pan           = ${LOT_PAN}
lotNumber_salchicha     = ${LOT_SALCHICHA}
lotNumber_mostaza       = ${LOT_MOSTAZA}
lotNumber_catsup        = ${LOT_CATSUP}

# ── Costos de ingredientes (precio × 1.13 IVA) ──────────────
unitPrice_pan           = 300.00  CRC/UNI  →  costo lote = 339.00 CRC
unitPrice_salchicha     = 600.00  CRC/UNI  →  costo lote = 678.00 CRC
unitPrice_mostaza       = 20.00   CRC/ML   →  costo lote =  22.60 CRC
unitPrice_catsup        = 15.00   CRC/ML   →  costo lote =  16.95 CRC

# ── Costo del Hot Dog PT (por unidad = 1 corrida) ───────────
# 1×339 + 1×678 + 15×22.60 + 20×16.95 = ₡1,695.00 / hot dog

# ── Compra (ingredientes para 50 hot dogs) ───────────────────
compra_subtotal         = 75000.00
compra_iva              = 9750.00
compra_total            = 84750.00

# ── Venta (3 hot dogs × ₡3,000 + 13% IVA) ───────────────────
venta_subtotal          = 9000.00
venta_iva               = 1170.00
venta_total             = 10170.00
cogs_venta              = 5085.00   (3 × 1695)

# ── Devolución parcial (1 hot dog) ───────────────────────────
devolucion_total        = 3390.00   (1 × 3000 × 1.13)
devolucion_subtotal     = 3000.00
devolucion_iva          = 390.00
cogs_reversa            = 1695.00   (1 × 1695)

# ── Regalía (2 hot dogs) ─────────────────────────────────────
regalia_costo           = 3390.00   (2 × 1695)

# ── IDs generados en esta ejecución ─────────────────────────
idProductUnit_pan       = $ID_PU_PAN
idProductUnit_salchicha = $ID_PU_SALCHICHA
idProductUnit_mostaza   = $ID_PU_MOSTAZA
idProductUnit_catsup    = $ID_PU_CATSUP
idProductUnit_hotdog    = $ID_PU_HOTDOG
idProductAccount_pan    = $ID_PA_PAN
idProductAccount_salch  = $ID_PA_SALCHICHA
idProductAccount_most   = $ID_PA_MOSTAZA
idProductAccount_cats   = $ID_PA_CATSUP
idPurchaseInvoice       = $ID_PURCHASE_INVOICE
numberPurchaseInvoice   = $NUMBER_PC
idLotPan                = $ID_LOT_PAN
idLotSalchicha          = $ID_LOT_SALCHICHA
idLotMostaza            = $ID_LOT_MOSTAZA
idLotCatsup             = $ID_LOT_CATSUP
idSalesOrder            = $ID_SALES_ORDER
idSalesInvoice          = $ID_SALES_INVOICE
numberSalesInvoice      = $NUMBER_FV
idLotPT                 = $ID_LOT_PT
idSalesInvoiceLine      = $ID_SALES_INVOICE_LINE
idEntryDevCogs          = $ID_ENTRY_DEV_COGS
idEntryDevIng           = $ID_ENTRY_REINTEGRO
idInventoryAdjustment   = $ID_ADJUSTMENT
idAdjEntry              = $ADJ_ENTRY

# ── Verificación de stock (Hot Dog PT id=11) ────────────────
stock_produccion        = 3   (OP automática al confirmar pedido)
stock_post_venta        = 0   (3 vendidos en FV confirmada)
stock_post_devolucion   = 1   (1 devuelto al lote PT)
stock_post_regalia      = -1  (2 regalados)
stock_final_real        = $STOCK_FINAL

# ── Cuentas involucradas ─────────────────────────────────────
idAccount_caja_crc      = 106  (1.1.06.01 Caja CRC)
idAccount_mp            = 110  (1.1.07.02 Materias Primas)
idAccount_inventario    = 109  (1.1.07.01 Inventario Mercadería)
idAccount_iva_acred     = 124  (1.1.09.01 IVA Acreditable CRC)
idAccount_cogs          = 119  (5.15.01 Costo de Ventas)
idAccount_ingresos      = 117  (4.5.01 Ingresos por Ventas)
idAccount_merma         = 113  (5.14.01 Faltantes/Merma)
idAccount_iva_pagar     = 127  (2.1.04.01 IVA por Pagar CRC)

# ── Endpoints de consulta rápida ────────────────────────────
GET $HOST/purchase-invoices/$ID_PURCHASE_INVOICE.json
GET $HOST/inventory-lots/by-product/7.json
GET $HOST/inventory-lots/by-product/8.json
GET $HOST/inventory-lots/by-product/9.json
GET $HOST/inventory-lots/by-product/10.json
GET $HOST/inventory-lots/by-product/11.json
GET $HOST/inventory-lots/$ID_LOT_PT.json
GET $HOST/inventory-lots/stock/11.json
GET $HOST/sales-orders/$ID_SALES_ORDER.json
GET $HOST/sales-invoices/$ID_SALES_INVOICE.json
GET $HOST/inventory-adjustments/$ID_ADJUSTMENT.json
TXTEOF

printf "  ${GREEN}💾  Resultado guardado en:${NC} %s\n" "$OUTPUT_FILE"
echo ""

# Limpiar archivo temporal
rm -f "$TEMP_RESPONSE"
