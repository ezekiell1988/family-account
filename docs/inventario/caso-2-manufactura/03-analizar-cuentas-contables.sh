#!/usr/bin/env bash
# ============================================================================
#  CASO 2 — MANUFACTURA (Chile Embotellado Marca X)
#  03-analizar-cuentas-contables.sh — Análisis de T-accounts y Saldos
#
#  Propósito:
#   Descarga todos los asientos del período, construye los T-accounts por
#   cuenta, calcula saldos netos y los compara contra los valores esperados
#   del Caso 2. Genera cuentas_caso2_*.txt con el reporte completo.
#
#  Asientos generados en el flujo:
#
#   FC-XXXXXX (Factura Compra MP, con ProductAccount → cta 110):
#     DR 110 ₡52,600  DR 124 ₡6,838   CR 106 ₡59,438
#
#   PROD-OP-XXXX × 4 asientos (uno por cada MP consumido en producción):
#     DR 115 ₡20,000  CR 110 ₡20,000  (Chile Seco  20 KG × ₡1,000)
#     DR 115 ₡ 2,500  CR 110 ₡ 2,500  (Vinagre     5 LTR × ₡500)
#     DR 115 ₡   100  CR 110 ₡   100  (Sal         0.5 KG × ₡200)
#     DR 115 ₡30,000  CR 110 ₡30,000  (Frasco      100 UNI × ₡300)
#     Acumulado: DR 115 ₡52,600 / CR 110 ₡52,600
#
#   FV-XXXXXXXX-XXX (Factura Venta 30 frascos):
#     DR 106 ₡50,850  CR 117 ₡45,000  CR 127 ₡5,850
#
#   COGS-FV-XXXXXX (COGS automático al confirmar FV):
#     DR 119 ₡15,780  CR 109 ₡15,780  (30 × ₡526)
#
#   DEV-COGS-FV-XXX (reversión COGS al devolver 5 frascos):
#     DR 109 ₡2,630   CR 119 ₡2,630   (5 × ₡526)
#
#   DEV-ING-FV-XXX (reversión ingresos + IVA al devolver):
#     DR 117 ₡7,500   DR 127 ₡975     CR 106 ₡8,475
#
#   AJ-XXXXXX (ajuste regalía 2 frascos):
#     DR 113 ₡1,052   CR 109 ₡1,052   (2 × ₡526)
#
#  T-accounts esperados:
#   106: DR=50,850   CR=67,913   Net=CR 17,063
#   109: DR= 2,630   CR=16,832   Net=CR 14,202
#   110: DR=52,600   CR=52,600   Net=    0     (comprado y consumido íntegramente)
#   111: (eliminado — CR ahora va directamente a 110)
#   113: DR= 1,052   CR=     0   Net=DR  1,052
#   115: DR=52,600   CR=     0   Net=DR 52,600
#   117: DR= 7,500   CR=45,000   Net=CR 37,500
#   119: DR=15,780   CR= 2,630   Net=DR 13,150
#   124: DR= 6,838   CR=     0   Net=DR  6,838
#   127: DR=   975   CR= 5,850   Net=CR  4,875
#   ΣDR = ΣCR = 190,825 ✓
#
#  Uso:
#   bash docs/inventario/caso-2-manufactura/03-analizar-cuentas-contables.sh
# ============================================================================

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TEMP_ENTRIES="/tmp/fa_caso2_entries.json"
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

# ── Auth ───────────────────────────────────────────────────────────────────────
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
ACC_NORMAL[111]="cr"   # Activo (WIP — saldo CR indica costo acumulado en producción)
ACC_NORMAL[113]="dr"   # Gasto
ACC_NORMAL[115]="dr"   # Gasto (Costos de Producción)
ACC_NORMAL[117]="cr"   # Ingreso
ACC_NORMAL[119]="dr"   # Gasto
ACC_NORMAL[124]="dr"   # Activo (crédito fiscal)
ACC_NORMAL[127]="cr"   # Pasivo

# ── Saldos esperados para el Caso 2 ──────────────────────────────────────────
#
#  FC-MP (con ProductAccount → 110):
#    DR 110=52600  DR 124=6838   CR 106=59438
#
#  PROD-OP (4 asientos, acumulado):
#    DR 115=52600  CR 110=52600  (ProductAccount → 110 para cada ingrediente)
#
#  FV-PT:
#    DR 106=50850  CR 117=45000  CR 127=5850
#
#  COGS-FV:
#    DR 119=15780  CR 109=15780
#
#  DEV-COGS-FV:
#    DR 109=2630   CR 119=2630
#
#  DEV-ING-FV:
#    DR 117=7500   DR 127=975    CR 106=8475
#
#  AJ-regalía:
#    DR 113=1052   CR 109=1052
#
EXP_DR[106]=50850;   EXP_CR[106]=67913;   EXP_NETO[106]=17063;  EXP_NETO_TIPO[106]="CR"
EXP_DR[109]=2630;    EXP_CR[109]=16832;   EXP_NETO[109]=14202;  EXP_NETO_TIPO[109]="CR"
EXP_DR[110]=52600;   EXP_CR[110]=52600;   EXP_NETO[110]=0;      EXP_NETO_TIPO[110]="ZERO"
EXP_DR[113]=1052;    EXP_CR[113]=0;       EXP_NETO[113]=1052;   EXP_NETO_TIPO[113]="DR"
EXP_DR[115]=52600;   EXP_CR[115]=0;       EXP_NETO[115]=52600;  EXP_NETO_TIPO[115]="DR"
EXP_DR[117]=7500;    EXP_CR[117]=45000;   EXP_NETO[117]=37500;  EXP_NETO_TIPO[117]="CR"
EXP_DR[119]=15780;   EXP_CR[119]=2630;    EXP_NETO[119]=13150;  EXP_NETO_TIPO[119]="DR"
EXP_DR[124]=6838;    EXP_CR[124]=0;       EXP_NETO[124]=6838;   EXP_NETO_TIPO[124]="DR"
EXP_DR[127]=975;     EXP_CR[127]=5850;    EXP_NETO[127]=4875;   EXP_NETO_TIPO[127]="CR"

# ── Preparar archivo de reporte ───────────────────────────────────────────────
RUN_TS="$(date '+%Y-%m-%d_%H-%M-%S')"
OUTPUT_FILE="$SCRIPT_DIR/cuentas_caso2_${RUN_TS}.txt"

{
  echo "# =================================================================="
  echo "#  CASO 2 — MANUFACTURA · Análisis de T-accounts"
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
printf "${CYAN}${BOLD}▶  CASO 2 — Análisis de T-accounts contables${NC}\n"
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
  local check_dr=$?
  assert_float_eq "    CR total" "$exp_cr" "$real_cr"
  local check_cr=$?

  local real_neto
  real_neto=$(awk "BEGIN {printf \"%.2f\", $real_dr - $real_cr}")
  local sign label
  if awk "BEGIN {exit !($real_neto >= 0)}"; then
    sign="DR"; abs_real="$real_neto"
  else
    sign="CR"; abs_real=$(awk "BEGIN {printf \"%.2f\", -($real_neto)}")
  fi
  label="${sign} ${abs_real}"

  local exp_label="${exp_tipo} ${exp_neto}"
  if [[ "$exp_tipo" == "ZERO" ]]; then
    if awk "BEGIN {exit !($abs_real < 0.01)}"; then
      log_ok  "    Saldo neto = 0 (esperado ZERO — DR = CR)"
      echo "    [OK] Saldo neto = 0 (ZERO)" >> "$OUTPUT_FILE"
    else
      log_fail "    Saldo neto = $label (esperado ZERO — DR = CR)"
      echo "    [FAIL] Saldo neto = $label (esperado ZERO)" >> "$OUTPUT_FILE"
    fi
  elif awk "BEGIN {a=$abs_real+0; e=$exp_neto+0; d=a-e; if(d<0)d=-d; exit !(d<0.01 && \"$sign\" == \"$exp_tipo\")}"; then
    log_ok  "    Saldo neto = $label (esperado $exp_label)"
    echo "    [OK] Saldo neto = $label" >> "$OUTPUT_FILE"
  else
    log_fail "    Saldo neto = $label (esperado $exp_label)"
    echo "    [FAIL] Saldo neto = $label (esperado $exp_label)" >> "$OUTPUT_FILE"
  fi
}

check_account_balance 106   # Caja CRC
check_account_balance 109   # Inventario de Mercadería (aparece en COGS/DEV)
check_account_balance 110   # Materias Primas (DR = CR = 52,600, saldo neto = 0)
check_account_balance 113   # Faltantes/Merma
check_account_balance 115   # Costos de Producción
check_account_balance 117   # Ingresos por Ventas
check_account_balance 119   # Costo de Ventas
check_account_balance 124   # IVA Acreditable
check_account_balance 127   # IVA por Pagar

# ── Verificar asiento de producción (originModule=ProductionOrder → DR 115, CR 110) ─────────────
printf "\n  ${BOLD}Asiento producción (originModule=ProductionOrder, DR 115 / CR 110)${NC}\n"
echo "" >> "$OUTPUT_FILE"
echo "  Asiento producción (originModule=ProductionOrder)" >> "$OUTPUT_FILE"

PROD_COUNT=$(jq '[.[] | select(.originModule == "ProductionOrder")] | length' "$TEMP_ENTRIES")
if [[ "${PROD_COUNT:-0}" -ge 1 ]]; then
  log_ok "Asientos de producción encontrados: $PROD_COUNT"
  PROD_DR_115=$(jq '[.[] | select(.originModule == "ProductionOrder") | .lines[] | select(.idAccount == 115) | .debitAmount] | add // 0' "$TEMP_ENTRIES")
  PROD_CR_110=$(jq '[.[] | select(.originModule == "ProductionOrder") | .lines[] | select(.idAccount == 110) | .creditAmount] | add // 0' "$TEMP_ENTRIES")
  assert_float_eq "    Producción: ΣDR 115 (Costos Producción) = ₡52,600" "52600" "$PROD_DR_115"
  assert_float_eq "    Producción: ΣCR 110 (Materias Primas) = ₡52,600"   "52600" "$PROD_CR_110"
  assert_float_eq "    Producción: DR 115 = CR 110 (partida doble OK)"     "$PROD_DR_115" "$PROD_CR_110"
  echo "    Producción asientos=$PROD_COUNT  DR115=$PROD_DR_115  CR110=$PROD_CR_110" >> "$OUTPUT_FILE"
else
  log_fail "No se encontraron asientos de producción (originModule=ProductionOrder)"
  echo "    [FAIL] No se encontraron asientos de producción" >> "$OUTPUT_FILE"
fi

# ── Verificar asiento DEV-ING-FV ──────────────────────────────────────────────
printf "\n  ${BOLD}Asiento DEV-ING-FV — reversión ingresos + IVA (devolución 5 frascos)${NC}\n"
echo "" >> "$OUTPUT_FILE"
echo "  Asiento DEV-ING-FV" >> "$OUTPUT_FILE"

DEV_ING_COUNT=$(jq '[.[] | select(.numberEntry | startswith("DEV-ING-FV-"))] | length' "$TEMP_ENTRIES")
if [[ "${DEV_ING_COUNT:-0}" -ge 1 ]]; then
  DEV_ING_DR_117=$(jq '[.[] | select(.numberEntry | startswith("DEV-ING-FV-")) | .lines[] | select(.idAccount == 117) | .debitAmount] | add // 0' "$TEMP_ENTRIES")
  DEV_ING_DR_127=$(jq '[.[] | select(.numberEntry | startswith("DEV-ING-FV-")) | .lines[] | select(.idAccount == 127) | .debitAmount] | add // 0' "$TEMP_ENTRIES")
  DEV_ING_CR_106=$(jq '[.[] | select(.numberEntry | startswith("DEV-ING-FV-")) | .lines[] | select(.idAccount == 106) | .creditAmount] | add // 0' "$TEMP_ENTRIES")
  log_ok "DEV-ING-FV encontrado"
  assert_float_eq "    DR 117 Ingresos = ₡7,500 (5 × ₡1,500)"  "7500"  "$DEV_ING_DR_117"
  assert_float_eq "    DR 127 IVA = ₡975 (7500 × 13%)"         "975"   "$DEV_ING_DR_127"
  assert_float_eq "    CR 106 Caja = ₡8,475"                   "8475"  "$DEV_ING_CR_106"
  echo "    DR_117=$DEV_ING_DR_117  DR_127=$DEV_ING_DR_127  CR_106=$DEV_ING_CR_106" >> "$OUTPUT_FILE"
else
  log_fail "DEV-ING-FV no encontrado"
  echo "    [FAIL] DEV-ING-FV no encontrado" >> "$OUTPUT_FILE"
fi

# ── Verificar asiento DEV-COGS-FV ─────────────────────────────────────────────
printf "\n  ${BOLD}Asiento DEV-COGS-FV — reversión COGS (5 × ₡526 = ₡2,630)${NC}\n"
echo "" >> "$OUTPUT_FILE"
echo "  Asiento DEV-COGS-FV" >> "$OUTPUT_FILE"

DEV_COGS_COUNT=$(jq '[.[] | select(.numberEntry | startswith("DEV-COGS-FV-"))] | length' "$TEMP_ENTRIES")
if [[ "${DEV_COGS_COUNT:-0}" -ge 1 ]]; then
  DEV_COGS_DR_109=$(jq '[.[] | select(.numberEntry | startswith("DEV-COGS-FV-")) | .lines[] | select(.idAccount == 109) | .debitAmount] | add // 0' "$TEMP_ENTRIES")
  DEV_COGS_CR_119=$(jq '[.[] | select(.numberEntry | startswith("DEV-COGS-FV-")) | .lines[] | select(.idAccount == 119) | .creditAmount] | add // 0' "$TEMP_ENTRIES")
  log_ok "DEV-COGS-FV encontrado"
  assert_float_eq "    DR 109 Inventario = ₡2,630 (5 × ₡526)" "2630" "$DEV_COGS_DR_109"
  assert_float_eq "    CR 119 COGS = ₡2,630"                  "2630" "$DEV_COGS_CR_119"
  echo "    DR_109=$DEV_COGS_DR_109  CR_119=$DEV_COGS_CR_119" >> "$OUTPUT_FILE"
else
  log_fail "DEV-COGS-FV no encontrado"
  echo "    [FAIL] DEV-COGS-FV no encontrado" >> "$OUTPUT_FILE"
fi

# Verificar que ninguna línea de COGS usa cuenta 0
COGS_ZERO_COUNT=$(jq '
  [.[] | select(.numberEntry | (startswith("COGS-") or startswith("DEV-COGS-")))
    | .lines[] | select(.idAccount == 0)] | length
' "$TEMP_ENTRIES")
if [[ "${COGS_ZERO_COUNT:-0}" -eq 0 ]]; then
  log_ok  "Ninguna línea COGS ni DEV-COGS usa cuenta=0 ✓"
  echo "    [OK] Ninguna línea COGS usa cuenta=0" >> "$OUTPUT_FILE"
else
  log_fail "$COGS_ZERO_COUNT línea(s) de COGS con cuenta=0"
  echo "    [FAIL] $COGS_ZERO_COUNT línea(s) de COGS con cuenta=0" >> "$OUTPUT_FILE"
fi

# ── Verificar partida doble global ────────────────────────────────────────────
printf "\n  ${BOLD}Partida doble global (ΣDR = ΣCR)${NC}\n"
assert_float_eq "    ΣDR = ΣCR = ₡190,825" "$GRAND_DR" "$GRAND_CR"
{
  echo ""
  echo "  Partida doble global: ΣDR=$GRAND_DR  ΣCR=$GRAND_CR"
} >> "$OUTPUT_FILE"

# ── Nota contable (calculada desde asientos reales) ───────────────────────────
_SALDO_110=$(awk "BEGIN {printf \"%.0f\", ${REAL_DR[110]:-0} - ${REAL_CR[110]:-0}}")
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
printf "  ${YELLOW}Cuenta 110 Mat. Primas:       saldo neto = ₡%s (MP comprada y consumida íntegramente)${NC}\n" "$_SALDO_110"
printf "  ${YELLOW}Cuenta 115 Costos Producción: saldo neto DR = ₡%s (costo total de los MP consumidos)${NC}\n" "$_SALDO_115"
printf "  ${YELLOW}Cuenta 109 Inventario:        saldo neto %s ₡%s (reflejado en COGS del PT)${NC}\n" \
  "$(awk "BEGIN {exit !(${REAL_DR[109]:-0} >= ${REAL_CR[109]:-0})}" && echo "DR =" || echo "CR =")" \
  "$(awk "BEGIN {n=${REAL_DR[109]:-0}-${REAL_CR[109]:-0}; if(n<0)n=-n; print int(n)}")"
printf "  ${YELLOW}Cuenta 124 IVA Acreditable:   saldo neto DR = ₡%s (crédito fiscal de compras)${NC}\n"        "$_SALDO_124"
printf "  ${YELLOW}Cuenta 127 IVA por Pagar:     saldo neto CR = ₡%s${NC}\n"                                   "$_SALDO_127"
printf "  ${YELLOW}Posición IVA vs gobierno:     ₡%s − ₡%s = ₡%s (%s)${NC}\n" \
  "$_SALDO_127" "$_SALDO_124" "$_IVA_NETO" "$_IVA_SIGNO"

{
  echo ""
  echo "  Nota contable:"
  echo "  Cuenta 110 Mat. Primas       neto = ₡$_SALDO_110 (MP comprada y consumida)"
  echo "  Cuenta 115 Costos Producción DR neto = ₡$_SALDO_115"
  echo "  Cuenta 124 IVA Acreditable   DR neto = ₡$_SALDO_124"
  echo "  Cuenta 127 IVA por Pagar     CR neto = ₡$_SALDO_127"
  echo "  IVA neto (127-124)           = ₡$_IVA_NETO ($_IVA_SIGNO)"
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
