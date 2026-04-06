#!/usr/bin/env bash
# ============================================================================
#  CASO 3 — ENSAMBLE EN VENTA (Hot Dog)
#  03-analizar-cuentas-contables.sh — Análisis de T-accounts y Saldos
#
#  Propósito:
#   Descarga todos los asientos del período, construye los T-accounts por
#   cuenta, calcula saldos netos y los compara contra los valores esperados
#   del Caso 3. Genera cuentas_caso3_*.txt con el reporte completo.
#   La nota contable al final se calcula dinámicamente desde los asientos
#   reales, no tiene valores quemados.
#
#  Asientos generados en el flujo (unitCost de lotes = precio pre-IVA):
#
#   FC-XXXXXX (Factura Compra ingredientes para 50 hot dogs;
#              ProductAccount → cta 110 para los 4 ingredientes):
#     DR 110=75,000   DR 124=9,750   CR 106=84,750
#
#   PROD-OP (4 asientos, uno por ingrediente consumido para 3 hot dogs):
#     DR 115=  900   CR 111=  900   (3 panes      × ₡300)
#     DR 115=1,800   CR 111=1,800   (3 salchichas  × ₡600)
#     DR 115=  900   CR 111=  900   (45 ml mostaza × ₡20)
#     DR 115=  900   CR 111=  900   (60 ml catsup  × ₡15)
#     Acumulado: DR 115=4,500 / CR 111=4,500
#     PT lote unitCost = 4,500 / 3 = ₡1,500 por hot dog
#
#   FV-XXXXXXXX-XXX (Factura Venta 3 hot dogs × ₡3,000 + 13% IVA):
#     DR 106=10,170   CR 117=9,000   CR 127=1,170
#
#   COGS-FV-XXXXXXX (COGS automático al confirmar FV; descuenta lote PT):
#     DR 119=4,500   CR 109=4,500   (3 × ₡1,500)
#
#   DEV-COGS-FV-XXX (reversión COGS al devolver 1 hot dog via partial-return):
#     DR 109=1,500   CR 119=1,500   (1 × ₡1,500)
#
#   DEV-ING-FV-XXX (reversión ingresos + IVA devolución 1 hot dog):
#     DR 117=3,000   DR 127=390     CR 106=3,390
#
#   AJ-XXXXXX (ajuste regalía 2 hot dogs sobre lote PT; AdjType → 109/113):
#     DR 113=3,000   CR 109=3,000   (2 × ₡1,500)
#
#  T-accounts esperados:
#   106: DR= 10,170   CR= 88,140   Net=CR  77,970
#   109: DR=  1,500   CR=  7,500   Net=CR   6,000
#   110: DR= 75,000   CR=      0   Net=DR  75,000
#   111: DR=      0   CR=  4,500   Net=CR   4,500
#   113: DR=  3,000   CR=      0   Net=DR   3,000
#   115: DR=  4,500   CR=      0   Net=DR   4,500
#   117: DR=  3,000   CR=  9,000   Net=CR   6,000
#   119: DR=  4,500   CR=  1,500   Net=DR   3,000
#   124: DR=  9,750   CR=      0   Net=DR   9,750
#   127: DR=    390   CR=  1,170   Net=CR     780
#   ΣDR = ΣCR = 111,810 ✓
#
#  Uso:
#   bash docs/inventario/caso-3-ensamble/03-analizar-cuentas-contables.sh
# ============================================================================

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TEMP_ENTRIES="/tmp/fa_caso3_entries.json"
HOST="https://localhost:8000/api/v1"

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

CHECKS_OK=0
CHECKS_FAIL=0

log_ok()   { printf "  ${GREEN}✅  %s${NC}\n" "$1"; CHECKS_OK=$((CHECKS_OK+1)); }
log_fail() { printf "  ${RED}❌  %s${NC}\n" "$1"; CHECKS_FAIL=$((CHECKS_FAIL+1)); }
log_info() { printf "  %s\n" "$1"; }

assert_float_eq() {
  local label="$1" expected="$2" actual="$3"
  if awk "BEGIN {a=($actual)+0; e=($expected)+0; d=a-e; if(d<0)d=-d; exit !(d<0.01)}"; then
    log_ok "$label: $actual"
  else
    log_fail "$label: esperado=$expected  real=$actual"
  fi
}

# ── Auth ──────────────────────────────────────────────────────────────────────
CREDENTIALS_FILE="$(cd "$SCRIPT_DIR/../../.." && pwd)/credentials/db.txt"
DB_HOST=$(grep -E '^HOST:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_PORT=$(grep -E '^PORT:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_USER=$(grep -E '^USER:'     "$CREDENTIALS_FILE" | awk '{print $2}')
DB_PASS=$(grep -E '^PASSWORD:' "$CREDENTIALS_FILE" | awk '{print $2}')
EMAIL="ezekiell1988@hotmail.com"

PIN="12345"
sqlcmd -S "${DB_HOST},${DB_PORT}" -U "$DB_USER" -P "$DB_PASS" -C -d dbfa \
  -Q "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '${PIN}'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '${PIN}');" \
  2>/dev/null

TOKEN=$(curl -k -s -X POST "${HOST}/auth/login" -H "Content-Type: application/json" \
  -d "{\"emailUser\":\"$EMAIL\",\"pin\":\"$PIN\"}" | jq -r '.accessToken')

if [[ -z "$TOKEN" || "$TOKEN" == "null" ]]; then
  printf "${RED}❌  No se pudo obtener token${NC}\n"; exit 1
fi

# ── Descargar todos los asientos ──────────────────────────────────────────────
curl -k -s -H "Authorization: Bearer $TOKEN" \
  "${HOST}/accounting-entries/data.json" > "$TEMP_ENTRIES"

TOTAL=$(jq 'length' "$TEMP_ENTRIES")

# ── Construir T-accounts con jq ───────────────────────────────────────────────
T_ACCOUNTS=$(jq -r '
  [ .[] | .numberEntry as $num | .lines[] |
    { account: (.idAccount | tostring),
      dr: .debitAmount,
      cr: .creditAmount,
      entry: $num }
  ]
  | group_by(.account)
  | map({
      account: .[0].account,
      totalDR: ([.[].dr] | add // 0),
      totalCR: ([.[].cr] | add // 0)
    })
  | .[]
  | "\(.account)\t\(.totalDR)\t\(.totalCR)"
' "$TEMP_ENTRIES")

# ── Nombres y tipos de cuentas ────────────────────────────────────────────────

ACC_NAME[106]="1.1.06.01  Caja CRC (₡)"
ACC_NAME[109]="1.1.07.01  Inventario de Mercadería"
ACC_NAME[110]="1.1.07.02  Materias Primas"
ACC_NAME[111]="1.1.07.03  Productos en Proceso"
ACC_NAME[113]="5.14.01    Faltantes de Inventario (Merma)"
ACC_NAME[115]="5.14.03    Costos de Producción"
ACC_NAME[117]="4.5.01     Ingresos por Ventas — Mercadería"
ACC_NAME[119]="5.15.01    Costo de Ventas — Mercadería"
ACC_NAME[124]="1.1.09.01  IVA Acreditable CRC (₡)"
ACC_NAME[127]="2.1.04.01  IVA por Pagar CRC (₡)"

ACC_NORMAL[106]="dr"   # Activo
ACC_NORMAL[109]="dr"   # Activo
ACC_NORMAL[110]="dr"   # Activo (MP)
ACC_NORMAL[111]="cr"   # Activo (WIP — acumula costo producción como CR)
ACC_NORMAL[113]="dr"   # Gasto
ACC_NORMAL[115]="dr"   # Gasto (Costos de Producción)
ACC_NORMAL[117]="cr"   # Ingreso
ACC_NORMAL[119]="dr"   # Gasto
ACC_NORMAL[124]="dr"   # Activo (crédito fiscal)
ACC_NORMAL[127]="cr"   # Pasivo

# ── Saldos esperados para el Caso 3 ──────────────────────────────────────────
#
#  FC (ingredientes 50 hot dogs, con ProductAccount → 110):
#    DR 110=75000   DR 124=9750   CR 106=84750
#
#  PROD-OP (4 asientos, acumulado para 3 hot dogs):
#    DR 115=4500   CR 111=4500
#    (PT unitCost = 4500 / 3 = ₡1,500/hot dog — precio pre-IVA)
#
#  FV (3 hot dogs × ₡3,000 + 13%):
#    DR 106=10170   CR 117=9000   CR 127=1170
#
#  COGS-FV (3 × ₡1,500):
#    DR 119=4500   CR 109=4500
#
#  DEV-COGS-FV (1 × ₡1,500; partial-return en lote PT):
#    DR 109=1500   CR 119=1500
#
#  DEV-ING-FV (1 × ₡3,000 + IVA):
#    DR 117=3000   DR 127=390   CR 106=3390
#
#  AJ-regalía (2 × ₡1,500 sobre lote PT; AdjType → 109/113):
#    DR 113=3000   CR 109=3000
#

EXP_DR[106]=10170;   EXP_CR[106]=88140;   EXP_NETO[106]=77970;  EXP_NETO_TIPO[106]="CR"
EXP_DR[109]=1500;    EXP_CR[109]=7500;    EXP_NETO[109]=6000;   EXP_NETO_TIPO[109]="CR"
EXP_DR[110]=75000;   EXP_CR[110]=0;       EXP_NETO[110]=75000;  EXP_NETO_TIPO[110]="DR"
EXP_DR[111]=0;       EXP_CR[111]=4500;    EXP_NETO[111]=4500;   EXP_NETO_TIPO[111]="CR"
EXP_DR[113]=3000;    EXP_CR[113]=0;       EXP_NETO[113]=3000;   EXP_NETO_TIPO[113]="DR"
EXP_DR[115]=4500;    EXP_CR[115]=0;       EXP_NETO[115]=4500;   EXP_NETO_TIPO[115]="DR"
EXP_DR[117]=3000;    EXP_CR[117]=9000;    EXP_NETO[117]=6000;   EXP_NETO_TIPO[117]="CR"
EXP_DR[119]=4500;    EXP_CR[119]=1500;    EXP_NETO[119]=3000;   EXP_NETO_TIPO[119]="DR"
EXP_DR[124]=9750;    EXP_CR[124]=0;       EXP_NETO[124]=9750;   EXP_NETO_TIPO[124]="DR"
EXP_DR[127]=390;     EXP_CR[127]=1170;    EXP_NETO[127]=780;    EXP_NETO_TIPO[127]="CR"

# ── Preparar archivo de reporte ───────────────────────────────────────────────
RUN_TS="$(date '+%Y-%m-%d_%H-%M-%S')"
OUTPUT_FILE="$SCRIPT_DIR/cuentas_caso3_${RUN_TS}.txt"

{
  echo "# =================================================================="
  echo "#  CASO 3 — ENSAMBLE EN VENTA · Análisis de T-accounts"
  echo "#  Generado: $(date '+%Y-%m-%d %H:%M:%S')"
  echo "# =================================================================="
  echo ""
  echo "  Total de asientos en el sistema: $TOTAL"
  echo ""
  echo "# ── Detalle de asientos (número | cuenta | DR | CR) ──────────────"
  jq -r '
    .[] | .numberEntry as $num |
    .lines[] |
    "  " + $num + "  cta=" + (.idAccount | tostring) +
    "  DR=" + (.debitAmount | tostring) + "  CR=" + (.creditAmount | tostring)
  ' "$TEMP_ENTRIES"
  echo ""
  echo "# ── T-accounts por cuenta ────────────────────────────────────────"
  printf "  %-10s %-44s %12s %12s %15s\n" "CUENTA" "NOMBRE" "TOTAL DR" "TOTAL CR" "SALDO NETO"
  printf "  %s\n" "$(printf '%.0s─' {1..97})"
} > "$OUTPUT_FILE"

# ── Encabezado en pantalla ────────────────────────────────────────────────────
printf "\n${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n"
printf "${CYAN}${BOLD}▶  CASO 3 — Análisis de T-accounts contables${NC}\n"
printf "${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n\n"
printf "  Total asientos en el sistema: %s\n\n" "$TOTAL"
printf "  %-10s %-44s %12s %12s %15s\n" "CUENTA" "NOMBRE" "TOTAL DR" "TOTAL CR" "SALDO NETO"
printf "  %s\n" "$(printf '%.0s─' {1..97})"

# ── Procesar cada cuenta ──────────────────────────────────────────────────────
while IFS=$'\t' read -r acc dr cr; do
  REAL_DR[$acc]=$dr
  REAL_CR[$acc]=$cr

  name="${ACC_NAME[$acc]:-cuenta $acc}"
  net=$(awk "BEGIN {printf \"%.2f\", $dr - $cr}")
  if awk "BEGIN {exit !($net >= 0)}"; then
    net_label="DR $net"
  else
    abs_net=$(awk "BEGIN {printf \"%.2f\", -($net)}")
    net_label="CR $abs_net"
  fi
  printf "  %-10s %-44s %12.2f %12.2f %15s\n" "$acc" "$name" "$dr" "$cr" "$net_label"
  echo "  $(printf '%-10s' "$acc") $(printf '%-44s' "$name") $(printf '%12.2f' "$dr") $(printf '%12.2f' "$cr") $(printf '%15s' "$net_label")" >> "$OUTPUT_FILE"
done <<< "$T_ACCOUNTS"

printf "  %s\n\n" "$(printf '%.0s─' {1..97})"
{ echo "  $(printf '%.0s─' {1..97})"; echo ""; } >> "$OUTPUT_FILE"

# ── Totales de partida doble ──────────────────────────────────────────────────
GRAND_DR=$(jq '[.[].lines[].debitAmount]  | add // 0' "$TEMP_ENTRIES")
GRAND_CR=$(jq '[.[].lines[].creditAmount] | add // 0' "$TEMP_ENTRIES")

printf "  %-56s %12.2f %12.2f\n\n" "TOTAL (partida doble)" "$GRAND_DR" "$GRAND_CR"
{
  printf "  %-56s %12.2f %12.2f\n\n" "TOTAL (partida doble)" "$GRAND_DR" "$GRAND_CR"
  echo "# ── Verificaciones de saldos esperados ──────────────────────────"
} >> "$OUTPUT_FILE"

# ── Verificaciones por cuenta ─────────────────────────────────────────────────
printf "${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n"
printf "${CYAN}${BOLD}▶  Verificaciones de saldos esperados${NC}\n"
printf "${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n"

check_account_balance() {
  local acc="$1"
  local name="${ACC_NAME[$acc]:-$acc}"
  local real_dr="${REAL_DR[$acc]:-0}"
  local real_cr="${REAL_CR[$acc]:-0}"
  local exp_dr="${EXP_DR[$acc]}"
  local exp_cr="${EXP_CR[$acc]}"
  local exp_neto="${EXP_NETO[$acc]}"
  local exp_tipo="${EXP_NETO_TIPO[$acc]}"

  printf "\n  ${BOLD}Cuenta %s — %s${NC}\n" "$acc" "$name"
  echo "" >> "$OUTPUT_FILE"
  echo "  Cuenta $acc — $name" >> "$OUTPUT_FILE"

  assert_float_eq "    DR total" "$exp_dr" "$real_dr"
  assert_float_eq "    CR total" "$exp_cr" "$real_cr"

  local real_neto
  real_neto=$(awk "BEGIN {printf \"%.2f\", $real_dr - $real_cr}")
  local sign abs_real
  if awk "BEGIN {exit !($real_neto >= 0)}"; then
    sign="DR"; abs_real="$real_neto"
  else
    sign="CR"; abs_real=$(awk "BEGIN {printf \"%.2f\", -($real_neto)}")
  fi
  local label="${sign} ${abs_real}"
  local exp_label="${exp_tipo} ${exp_neto}"

  if awk "BEGIN {a=$abs_real+0; e=$exp_neto+0; d=a-e; if(d<0)d=-d; exit !(d<0.01 && \"$sign\" == \"$exp_tipo\")}"; then
    log_ok  "    Saldo neto = $label (esperado $exp_label)"
    echo "    [OK] Saldo neto = $label" >> "$OUTPUT_FILE"
  else
    log_fail "    Saldo neto = $label (esperado $exp_label)"
    echo "    [FAIL] Saldo neto = $label (esperado $exp_label)" >> "$OUTPUT_FILE"
  fi
}

check_account_balance 106   # Caja CRC
check_account_balance 109   # Inventario (COGS y regalía del PT)
check_account_balance 110   # Materias Primas
check_account_balance 111   # Productos en Proceso (WIP)
check_account_balance 113   # Faltantes/Merma
check_account_balance 115   # Costos de Producción
check_account_balance 117   # Ingresos por Ventas
check_account_balance 119   # Costo de Ventas
check_account_balance 124   # IVA Acreditable
check_account_balance 127   # IVA por Pagar

# ── Verificar asientos de producción (originModule=ProductionOrder) ────────────
printf "\n  ${BOLD}Asientos PROD-OP — producción automática al confirmar pedido (DR 115 / CR 111)${NC}\n"
echo "" >> "$OUTPUT_FILE"
echo "  Asientos PROD-OP (originModule=ProductionOrder)" >> "$OUTPUT_FILE"

PROD_COUNT=$(jq '[.[] | select(.originModule == "ProductionOrder")] | length' "$TEMP_ENTRIES")
if [[ "${PROD_COUNT:-0}" -ge 1 ]]; then
  log_ok "Asientos de producción encontrados: $PROD_COUNT (esperado=4, uno por ingrediente)"
  PROD_DR_115=$(jq '[.[] | select(.originModule == "ProductionOrder") | .lines[] | select(.idAccount == 115) | .debitAmount] | add // 0' "$TEMP_ENTRIES")
  PROD_CR_111=$(jq '[.[] | select(.originModule == "ProductionOrder") | .lines[] | select(.idAccount == 111) | .creditAmount] | add // 0' "$TEMP_ENTRIES")
  assert_float_eq "    Producción: ΣDR 115 (Costos Producción) = ₡4,500" "4500" "$PROD_DR_115"
  assert_float_eq "    Producción: ΣCR 111 (Prod. en Proceso) = ₡4,500"  "4500" "$PROD_CR_111"
  assert_float_eq "    Producción: DR 115 = CR 111 (partida doble OK)"    "$PROD_DR_115" "$PROD_CR_111"
  echo "    Producción asientos=$PROD_COUNT  DR115=$PROD_DR_115  CR111=$PROD_CR_111" >> "$OUTPUT_FILE"
else
  log_fail "No se encontraron asientos de producción (originModule=ProductionOrder)"
  echo "    [FAIL] No se encontraron asientos de producción" >> "$OUTPUT_FILE"
fi

# ── Verificar asiento COGS-FV (DR 119 / CR 109) ──────────────────────────────
printf "\n  ${BOLD}Asiento COGS-FV — costo de ventas 3 hot dogs (3 × ₡1,500 = ₡4,500)${NC}\n"
echo "" >> "$OUTPUT_FILE"
echo "  Asiento COGS-FV" >> "$OUTPUT_FILE"

COGS_COUNT=$(jq '[.[] | select(.numberEntry | startswith("COGS-FV-"))] | length' "$TEMP_ENTRIES")
if [[ "${COGS_COUNT:-0}" -ge 1 ]]; then
  log_ok "Asiento COGS-FV encontrado ($COGS_COUNT)"
  COGS_DR=$(jq '[.[] | select(.numberEntry | startswith("COGS-FV-")) | .lines[] | .debitAmount] | add // 0' "$TEMP_ENTRIES")
  COGS_CR=$(jq '[.[] | select(.numberEntry | startswith("COGS-FV-")) | .lines[] | .creditAmount] | add // 0' "$TEMP_ENTRIES")
  assert_float_eq "    COGS-FV DR 119 = ₡4,500" "4500" "$COGS_DR"
  assert_float_eq "    COGS-FV CR 109 = ₡4,500" "4500" "$COGS_CR"
  echo "    DR=$COGS_DR  CR=$COGS_CR" >> "$OUTPUT_FILE"

  # Ninguna línea COGS debe usar cuenta 0 (bug conocido: IdAccountCOGS ?? 0)
  COGS_ZERO=$(jq '[.[] | select(.numberEntry | startswith("COGS-FV-") or startswith("DEV-COGS-FV-")) | .lines[] | select(.idAccount == 0)] | length' "$TEMP_ENTRIES")
  if [[ "${COGS_ZERO:-0}" -eq 0 ]]; then
    log_ok "Ninguna línea COGS usa cuenta=0 (IdAccountCOGS configurado correctamente)"
    echo "    [OK] Ninguna línea COGS usa cuenta=0" >> "$OUTPUT_FILE"
  else
    log_fail "¡${COGS_ZERO} línea(s) de COGS con idAccount=0! Verificar IdAccountCOGS en tipo de factura."
    echo "    [FAIL] $COGS_ZERO línea(s) de COGS con cuenta=0" >> "$OUTPUT_FILE"
  fi
else
  log_fail "Asiento COGS-FV no encontrado"
  echo "    [FAIL] COGS-FV no encontrado" >> "$OUTPUT_FILE"
fi

# ── Verificar asiento DEV-COGS-FV (reversión COGS 1 hot dog) ─────────────────
printf "\n  ${BOLD}Asiento DEV-COGS-FV — reversión COGS devolución parcial (1 × ₡1,500 = ₡1,500)${NC}\n"
echo "" >> "$OUTPUT_FILE"
echo "  Asiento DEV-COGS-FV" >> "$OUTPUT_FILE"

DEV_COGS_COUNT=$(jq '[.[] | select(.numberEntry | startswith("DEV-COGS-FV-"))] | length' "$TEMP_ENTRIES")
if [[ "${DEV_COGS_COUNT:-0}" -ge 1 ]]; then
  log_ok "Asiento DEV-COGS-FV existe ($DEV_COGS_COUNT encontrado(s))"
  DEV_COGS_DR=$(jq '[.[] | select(.numberEntry | startswith("DEV-COGS-FV-")) | .lines[] | .debitAmount] | add // 0' "$TEMP_ENTRIES")
  DEV_COGS_CR=$(jq '[.[] | select(.numberEntry | startswith("DEV-COGS-FV-")) | .lines[] | .creditAmount] | add // 0' "$TEMP_ENTRIES")
  assert_float_eq "    DEV-COGS-FV DR total (inventario recuperado) = ₡1,500" "1500" "$DEV_COGS_DR"
  assert_float_eq "    DEV-COGS-FV CR total (reversa COGS) = ₡1,500"          "1500" "$DEV_COGS_CR"
  echo "    DR=$DEV_COGS_DR  CR=$DEV_COGS_CR" >> "$OUTPUT_FILE"
else
  log_fail "Asiento DEV-COGS-FV no encontrado — la devolución parcial no generó reversión de COGS"
  echo "    [FAIL] DEV-COGS-FV no encontrado" >> "$OUTPUT_FILE"
fi

# ── Verificar asiento DEV-ING-FV (reversión ingresos + IVA) ──────────────────
printf "\n  ${BOLD}Asiento DEV-ING-FV — reversión ingresos + IVA (1 hot dog devuelto)${NC}\n"
echo "" >> "$OUTPUT_FILE"
echo "  Asiento DEV-ING-FV" >> "$OUTPUT_FILE"

DEV_ING_COUNT=$(jq '[.[] | select(.numberEntry | startswith("DEV-ING-FV-"))] | length' "$TEMP_ENTRIES")
if [[ "${DEV_ING_COUNT:-0}" -ge 1 ]]; then
  DEV_ING_DR_117=$(jq '[.[] | select(.numberEntry | startswith("DEV-ING-FV-")) | .lines[] | select(.idAccount == 117) | .debitAmount] | add // 0' "$TEMP_ENTRIES")
  DEV_ING_DR_127=$(jq '[.[] | select(.numberEntry | startswith("DEV-ING-FV-")) | .lines[] | select(.idAccount == 127) | .debitAmount] | add // 0' "$TEMP_ENTRIES")
  DEV_ING_CR_106=$(jq '[.[] | select(.numberEntry | startswith("DEV-ING-FV-")) | .lines[] | select(.idAccount == 106) | .creditAmount] | add // 0' "$TEMP_ENTRIES")
  log_ok "DEV-ING-FV encontrado"
  assert_float_eq "    DR 117 Ingresos = ₡3,000 (1 × ₡3,000)"   "3000"  "$DEV_ING_DR_117"
  assert_float_eq "    DR 127 IVA = ₡390 (3,000 × 13%)"          "390"   "$DEV_ING_DR_127"
  assert_float_eq "    CR 106 Caja = ₡3,390"                      "3390"  "$DEV_ING_CR_106"
  echo "    DR_117=$DEV_ING_DR_117  DR_127=$DEV_ING_DR_127  CR_106=$DEV_ING_CR_106" >> "$OUTPUT_FILE"
else
  log_fail "DEV-ING-FV no encontrado — partial-return no generó reversión de ingresos"
  echo "    [FAIL] DEV-ING-FV no encontrado" >> "$OUTPUT_FILE"
fi

# ── Verificar asiento AJ-regalía (2 hot dogs sobre lote PT) ──────────────────
printf "\n  ${BOLD}Asiento AJ-regalía — 2 hot dogs regalados sobre lote PT (2 × ₡1,500 = ₡3,000)${NC}\n"
echo "" >> "$OUTPUT_FILE"
echo "  Asiento AJ-regalía" >> "$OUTPUT_FILE"

AJ_COUNT=$(jq '[.[] | select(.numberEntry | startswith("AJ-"))] | length' "$TEMP_ENTRIES")
if [[ "${AJ_COUNT:-0}" -ge 1 ]]; then
  log_ok "Asiento AJ encontrado ($AJ_COUNT)"
  AJ_DR_113=$(jq '[.[] | select(.numberEntry | startswith("AJ-")) | .lines[] | select(.idAccount == 113) | .debitAmount] | add // 0' "$TEMP_ENTRIES")
  AJ_CR_109=$(jq '[.[] | select(.numberEntry | startswith("AJ-")) | .lines[] | select(.idAccount == 109) | .creditAmount] | add // 0' "$TEMP_ENTRIES")
  assert_float_eq "    AJ DR 113 (Merma, regalía 2 × ₡1,500) = ₡3,000"  "3000" "$AJ_DR_113"
  assert_float_eq "    AJ CR 109 (Inventario) = ₡3,000"                  "3000" "$AJ_CR_109"
  echo "    AJ_COUNT=$AJ_COUNT  DR_113=$AJ_DR_113  CR_109=$AJ_CR_109" >> "$OUTPUT_FILE"
else
  log_fail "Asiento AJ no encontrado — la regalía no generó asiento de merma"
  echo "    [FAIL] AJ no encontrado" >> "$OUTPUT_FILE"
fi

# ── Verificación de partida doble ─────────────────────────────────────────────
printf "\n  ${BOLD}Partida doble (DR = CR)${NC}\n"
assert_float_eq "    ΣDR = ΣCR = 111,810" "111810" "$GRAND_DR"
assert_float_eq "    ΣDR = ΣCR"           "$GRAND_DR" "$GRAND_CR"
{
  echo ""
  echo "  Partida doble: ΣDR=$GRAND_DR  ΣCR=$GRAND_CR"
} >> "$OUTPUT_FILE"

# ── Nota contable (calculada desde asientos reales) ───────────────────────────
_SALDO_110=$(awk "BEGIN {printf \"%.0f\", ${REAL_DR[110]:-0} - ${REAL_CR[110]:-0}}")
_SALDO_111=$(awk "BEGIN {printf \"%.0f\", ${REAL_CR[111]:-0} - ${REAL_DR[111]:-0}}")
_SALDO_115=$(awk "BEGIN {printf \"%.0f\", ${REAL_DR[115]:-0} - ${REAL_CR[115]:-0}}")
_SALDO_109=$(awk "BEGIN {printf \"%.0f\", ${REAL_DR[109]:-0} - ${REAL_CR[109]:-0}}")
_SALDO_124=$(awk "BEGIN {printf \"%.0f\", ${REAL_DR[124]:-0} - ${REAL_CR[124]:-0}}")
_SALDO_127=$(awk "BEGIN {printf \"%.0f\", ${REAL_CR[127]:-0} - ${REAL_DR[127]:-0}}")
_IVA_NETO=$(awk "BEGIN {printf \"%.0f\", ($_SALDO_127) - ($_SALDO_124)}")
if awk "BEGIN {exit !(($_IVA_NETO + 0) < 0)}"; then
  _IVA_SIGNO="crédito a favor del negocio"
else
  _IVA_SIGNO="IVA a pagar al gobierno"
fi

printf "\n"
printf "  ${YELLOW}${BOLD}📋 Nota contable (desde asientos reales)${NC}\n"
printf "  ${YELLOW}Cuenta 110 Materias Primas:     saldo neto DR = ₡%s (MP comprada, acumulada)${NC}\n"   "$_SALDO_110"
printf "  ${YELLOW}Cuenta 111 Prod. en Proceso:    saldo neto CR = ₡%s (costo producción consumido)${NC}\n" "$_SALDO_111"
printf "  ${YELLOW}Cuenta 115 Costos Producción:   saldo neto DR = ₡%s (idem, lado débito)${NC}\n"        "$_SALDO_115"
printf "  ${YELLOW}Cuenta 109 Inventario:          saldo neto CR = ₡%s (salida neta por COGS/regalía)${NC}\n" "$(( -_SALDO_109 ))"
printf "  ${YELLOW}Cuenta 124 IVA Acreditable:     saldo neto DR = ₡%s (crédito fiscal de compras)${NC}\n" "$_SALDO_124"
printf "  ${YELLOW}Cuenta 127 IVA por Pagar:       saldo neto CR = ₡%s${NC}\n"                            "$_SALDO_127"
printf "  ${YELLOW}Posición IVA vs gobierno:        ₡%s − ₡%s = ₡%s (%s)${NC}\n" "$_SALDO_127" "$_SALDO_124" "$_IVA_NETO" "$_IVA_SIGNO"

{
  echo ""
  echo "# ── NOTA CONTABLE (calculada desde asientos reales) ──────────────"
  echo "#  - Cuenta 110 MP:               saldo neto DR = $_SALDO_110 (MP comprada, acumulada)"
  echo "#  - Cuenta 111 Prod. en Proceso: saldo neto CR = $_SALDO_111 (costo producción consumido)"
  echo "#  - Cuenta 115 Costos Producc.:  saldo neto DR = $_SALDO_115 (idem, lado débito)"
  echo "#  - Cuenta 109 Inventario:       saldo neto CR = $(( -_SALDO_109 )) (salida neta)"
  echo "#  - Cuenta 124 IVA Acreditable:  saldo neto DR = $_SALDO_124 (crédito fiscal de compras)"
  echo "#  - Cuenta 127 IVA por Pagar:    saldo neto CR = $_SALDO_127"
  echo "#  Posición IVA vs gobierno: $_SALDO_127 - $_SALDO_124 = $_IVA_NETO ($_IVA_SIGNO)"
  echo ""
  echo "# ── RESUMEN ────────────────────────────────────────────────────────"
  echo "  Verificaciones exitosas : $CHECKS_OK"
  echo "  Verificaciones fallidas : $CHECKS_FAIL"
} >> "$OUTPUT_FILE"

# ── Resumen ───────────────────────────────────────────────────────────────────
echo ""
printf "${BOLD}╔══════════════════════════════════════════════════════╗${NC}\n"
if [[ $CHECKS_FAIL -eq 0 ]]; then
  printf "${BOLD}${GREEN}║   ✅  TODOS LOS SALDOS CONTABLES CORRECTOS         ║${NC}\n"
else
  printf "${BOLD}${RED}║   ⚠   ALGUNOS SALDOS NO COINCIDEN                  ║${NC}\n"
fi
printf "${BOLD}╚══════════════════════════════════════════════════════╝${NC}\n"
printf "  ${GREEN}✅  Exitosas : %d${NC}\n" "$CHECKS_OK"
[[ $CHECKS_FAIL -gt 0 ]] && printf "  ${RED}❌  Fallidas : %d${NC}\n" "$CHECKS_FAIL"
printf "\n  Reporte guardado en: %s\n\n" "$OUTPUT_FILE"

rm -f "$TEMP_ENTRIES"
[[ $CHECKS_FAIL -eq 0 ]]
