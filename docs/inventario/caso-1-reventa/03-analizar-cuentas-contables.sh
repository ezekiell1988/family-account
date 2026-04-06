#!/usr/bin/env bash
# ============================================================================
#  CASO 1 — REVENTA (Coca-Cola 355ml)
#  03-analizar-cuentas-contables.sh — Análisis de T-accounts y Saldos
#
#  Propósito:
#   Descarga todos los asientos del período, construye los T-accounts por
#   cuenta, calcula saldos netos y los compara contra los valores esperados
#   del Caso 1. Genera cuentas_caso1_*.txt con el reporte completo.
#   La nota contable al final se calcula dinámicamente desde los asientos
#   reales, no tiene valores quemados.
#
#  Uso:
#   bash docs/inventario/caso-1-reventa/03-analizar-cuentas-contables.sh
# ============================================================================

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
TEMP_ENTRIES="/tmp/fa_caso1_entries.json"
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

# ── Descargar todos los asientos ──────────────────────────────────────────────
curl -k -s -H "Authorization: Bearer $TOKEN" \
  "${HOST}/accounting-entries/data.json" > "$TEMP_ENTRIES"

TOTAL=$(jq 'length' "$TEMP_ENTRIES")

# ── Construir T-accounts con jq ───────────────────────────────────────────────
# Produce líneas: idAccount  totalDR  totalCR
T_ACCOUNTS=$(jq -r '
  [ .[] | .numberEntry as $num | .lines[] |
    { account: .idAccount, dr: (.debitAmount // 0), cr: (.creditAmount // 0) }
  ]
  | group_by(.account)
  | map({
      account: .[0].account,
      totalDR: (map(.dr) | add),
      totalCR: (map(.cr) | add)
    })
  | .[]
  | "\(.account)\t\(.totalDR)\t\(.totalCR)"
' "$TEMP_ENTRIES")

# ── Nombres de cuentas (las que participan en el flujo) ───────────────────────
ACC_NAME[106]="1.1.06.01  Caja CRC (₡)"
ACC_NAME[109]="1.1.07.01  Inventario de Mercadería"
ACC_NAME[113]="5.14.01    Faltantes de Inventario (Merma)"
ACC_NAME[117]="4.5.01     Ingresos por Ventas — Mercadería"
ACC_NAME[119]="5.15.01    Costo de Ventas — Mercadería"
ACC_NAME[124]="1.1.09.01  IVA Acreditable CRC (₡)"
ACC_NAME[127]="2.1.04.01  IVA por Pagar CRC (₡)"
# Tipo contable (Activo → saldo normal DR; Ingreso/Gasto → según tipo)
ACC_NORMAL[106]="dr"   # Activo
ACC_NORMAL[109]="dr"   # Activo
ACC_NORMAL[113]="dr"   # Gasto
ACC_NORMAL[117]="cr"   # Ingreso
ACC_NORMAL[119]="dr"   # Gasto
ACC_NORMAL[124]="dr"   # Activo (crédito fiscal)
ACC_NORMAL[127]="cr"   # Pasivo

# Saldos esperados para el Caso 1 (basados en asientos confirmados en consultas.sh)
#
#  FC-000001:         DR 109=100000  DR 124=13000  CR 106=113000
#  FV-20260405-001:   DR 106=16950    CR 117=15000  CR 127=1950
#  COGS-FV-000001:    DR 119=10000    CR 109=10000
#  DEV-COGS-FV-:      DR 109=3000     CR 119=3000
#  REINTEGRO-FV-001:  DR 117=5085     CR 106=5085
#  AJ-000001:         DR 113=2000     CR 109=2000
#
#  106 Caja:       DR=16950   / CR=118085  → neto CR = 101135  (salida de caja)
#  109 Inventario: DR=103000  / CR=12000   → neto DR = 91000   (100000+3000 / 10000+2000)
#  113 Merma:      DR=2000    / CR=0       → neto DR = 2000
#  117 Ingresos:   DR=5085    / CR=15000   → neto CR = 9915    (subtotal sin IVA)
#  119 COGS:       DR=10000   / CR=3000    → neto DR = 7000
#  124 IVA Acred.: DR=13000   / CR=0       → neto DR = 13000
#  127 IVA x Pag.: DR=0       / CR=1950    → neto CR = 1950

EXP_DR[106]=16950;   EXP_CR[106]=118085;  EXP_NETO[106]=101135;  EXP_NETO_TIPO[106]="CR"
EXP_DR[109]=103000;  EXP_CR[109]=12000;   EXP_NETO[109]=91000;   EXP_NETO_TIPO[109]="DR"
EXP_DR[113]=2000;    EXP_CR[113]=0;       EXP_NETO[113]=2000;    EXP_NETO_TIPO[113]="DR"
EXP_DR[117]=5085;    EXP_CR[117]=15000;   EXP_NETO[117]=9915;    EXP_NETO_TIPO[117]="CR"
EXP_DR[119]=10000;   EXP_CR[119]=3000;    EXP_NETO[119]=7000;    EXP_NETO_TIPO[119]="DR"
EXP_DR[124]=13000;   EXP_CR[124]=0;       EXP_NETO[124]=13000;   EXP_NETO_TIPO[124]="DR"
EXP_DR[127]=0;       EXP_CR[127]=1950;    EXP_NETO[127]=1950;    EXP_NETO_TIPO[127]="CR"

# ── Nota contable: cuentas de IVA (124 y 127) no tienen valores esperados quemados;
#  se calculan dinámicamente al final del script desde los asientos reales.

# ── Preparar output .txt ──────────────────────────────────────────────────────
RUN_TS="$(date '+%Y-%m-%d_%H-%M-%S')"
OUTPUT_FILE="$SCRIPT_DIR/cuentas_caso1_${RUN_TS}.txt"

{
  echo "# =================================================================="
  echo "#  CASO 1 — REVENTA · Análisis de T-accounts"
  echo "#  Generado: $(date '+%Y-%m-%d %H:%M:%S')"
  echo "# =================================================================="
  echo ""
  echo "  Total de asientos en el sistema: $TOTAL"
  echo ""
  echo "# ── Detalle de asientos (número | cuenta | DR | CR) ──────────────"

  # Línea por línea de todos los asientos
  jq -r '
    .[] |
    .numberEntry as $num |
    .lines[] |
    "  " + $num + "  cta=" + (.idAccount | tostring) + "  DR=" + (.debitAmount | tostring) + "  CR=" + (.creditAmount | tostring)
  ' "$TEMP_ENTRIES"

  echo ""
  echo "# ── T-accounts por cuenta ────────────────────────────────────────"
  printf "  %-10s %-42s %12s %12s %15s\n" "CUENTA" "NOMBRE" "TOTAL DR" "TOTAL CR" "SALDO NETO"
  printf "  %s\n" "$(printf '%.0s─' {1..95})"
} > "$OUTPUT_FILE"

# ── Procesar cada cuenta ──────────────────────────────────────────────────────
printf "\n${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n"
printf "${CYAN}${BOLD}▶  CASO 1 — Análisis de T-accounts contables${NC}\n"
printf "${CYAN}${BOLD}══════════════════════════════════════════════════════${NC}\n\n"
printf "  %-10s %-42s %12s %12s %15s\n" "CUENTA" "NOMBRE" "TOTAL DR" "TOTAL CR" "SALDO NETO"
printf "  %s\n" "$(printf '%.0s─' {1..95})"

while IFS=$'\t' read -r acc dr cr; do
  REAL_DR[$acc]=$dr
  REAL_CR[$acc]=$cr

  name="${ACC_NAME[$acc]:-cuenta $acc}"
  net=$(awk "BEGIN {printf \"%.2f\", $dr - $cr}")
  if awk "BEGIN {exit !($net >= 0)}"; then
    net_label="DR $(printf '%,.2f' "$net" 2>/dev/null || echo "$net")"
  else
    abs_net=$(awk "BEGIN {printf \"%.2f\", -($net)}")
    net_label="CR $abs_net"
  fi
  printf "  %-10s %-42s %12.2f %12.2f %15s\n" "$acc" "$name" "$dr" "$cr" "$net_label"
  echo "  $(printf '%-10s' "$acc") $(printf '%-42s' "$name") $(printf '%12.2f' "$dr") $(printf '%12.2f' "$cr") $(printf '%15s' "$net_label")" >> "$OUTPUT_FILE"
done <<< "$T_ACCOUNTS"

printf "  %s\n\n" "$(printf '%.0s─' {1..95})"
echo "  $(printf '%.0s─' {1..95})" >> "$OUTPUT_FILE"
echo "" >> "$OUTPUT_FILE"

# ── TOTAL DR = TOTAL CR (partida doble) ───────────────────────────────────────
GRAND_DR=$(jq '[.[].lines[].debitAmount]  | add // 0' "$TEMP_ENTRIES")
GRAND_CR=$(jq '[.[].lines[].creditAmount] | add // 0' "$TEMP_ENTRIES")

printf "  %-54s %12.2f %12.2f\n\n" "TOTAL (partida doble)" "$GRAND_DR" "$GRAND_CR"
{
  printf "  %-54s %12.2f %12.2f\n\n" "TOTAL (partida doble)" "$GRAND_DR" "$GRAND_CR"
  echo "# ── Verificaciones de saldos esperados ──────────────────────────"
} >> "$OUTPUT_FILE"

# ── Verificaciones ────────────────────────────────────────────────────────────
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

  # DR total
  local dr_ok cr_ok
  assert_float_eq "    DR total" "$exp_dr" "$real_dr"
  dr_ok=$?
  assert_float_eq "    CR total" "$exp_cr" "$real_cr"
  cr_ok=$?

  # Saldo neto
  local real_neto
  real_neto=$(awk "BEGIN {printf \"%.2f\", $real_dr - $real_cr}")
  local exp_neto_signed
  if [[ "$exp_tipo" == "CR" ]]; then
    exp_neto_signed=$(awk "BEGIN {printf \"%.2f\", -${exp_neto}}")
  else
    exp_neto_signed="$exp_neto"
  fi
  assert_float_eq "    Saldo neto ${exp_tipo} ${exp_neto}" "$exp_neto_signed" "$real_neto"

  {
    echo "    DR total  → esperado=$exp_dr  real=$real_dr"
    echo "    CR total  → esperado=$exp_cr  real=$real_cr"
    echo "    Saldo neto ${exp_tipo} ${exp_neto} → real=$real_neto"
  } >> "$OUTPUT_FILE"
}

check_account_balance 106
check_account_balance 109
check_account_balance 113
check_account_balance 117
check_account_balance 119
check_account_balance 124
check_account_balance 127

# ── Verificación de partida doble ─────────────────────────────────────────────
printf "\n  ${BOLD}Partida doble (DR = CR)${NC}\n"
assert_float_eq "    ΣDR = ΣCR" "$GRAND_DR" "$GRAND_CR"
{
  echo ""
  echo "  Partida doble: ΣDR=$GRAND_DR  ΣCR=$GRAND_CR"
} >> "$OUTPUT_FILE"

# ── Nota contable (calculada desde los asientos reales) ───────────────────────
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
printf "  ${YELLOW}Cuenta 109 Inventario:      saldo neto DR = ₡%s${NC}\n"           "$_SALDO_109"
printf "  ${YELLOW}Cuenta 124 IVA Acreditable: saldo neto DR = ₡%s (crédito fiscal de compras)${NC}\n" "$_SALDO_124"
printf "  ${YELLOW}Cuenta 127 IVA por Pagar:   saldo neto CR = ₡%s${NC}\n"           "$_SALDO_127"
printf "  ${YELLOW}Posición IVA vs gobierno:   ₡%s − ₡%s = ₡%s (%s)${NC}\n" "$_SALDO_127" "$_SALDO_124" "$_IVA_NETO" "$_IVA_SIGNO"

{
  echo ""
  echo "# ── NOTA CONTABLE (calculada desde asientos reales) ──────────────"
  echo "#  - Cuenta 109 Inventario:      saldo neto DR = $_SALDO_109"
  echo "#  - Cuenta 124 IVA Acreditable: saldo neto DR = $_SALDO_124 (crédito fiscal de compras)"
  echo "#  - Cuenta 127 IVA por Pagar:   saldo neto CR = $_SALDO_127"
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
