#!/usr/bin/env pwsh
# ============================================================================
#  CASO 1 — REVENTA (Coca-Cola 355ml)
#  03-analizar-cuentas-contables.ps1 — Análisis de T-accounts y Saldos
#
#  Propósito:
#   Descarga todos los asientos del período, construye los T-accounts por
#   cuenta, calcula saldos netos y los compara contra los valores esperados
#   del Caso 1. Genera cuentas_caso1_*.txt con el reporte completo.
#
#  Uso:
#   pwsh docs/inventario/caso-1-reventa/03-analizar-cuentas-contables.ps1
# ============================================================================

$SCRIPT_DIR = $PSScriptRoot
$REPO_ROOT  = (Resolve-Path (Join-Path $SCRIPT_DIR "../../..")).Path
$CREDS_FILE = Join-Path $REPO_ROOT "credentials\db.txt"
$HOST_URL   = "https://localhost:8000/api/v1"
$EMAIL      = "ezekiell1988@hotmail.com"

$script:CHECKS_OK   = 0
$script:CHECKS_FAIL = 0

# ── Helpers ───────────────────────────────────────────────────────────────────
function Write-Ok($msg) {
    Write-Host "  OK  $msg" -ForegroundColor Green
    $script:CHECKS_OK++
}
function Write-Fail($msg) {
    Write-Host "  FAIL  $msg" -ForegroundColor Red
    $script:CHECKS_FAIL++
}
function Write-Info($msg) { Write-Host "  $msg" }

function Assert-FloatEq($label, $expected, $actual) {
    $e = try { [decimal]$expected } catch { 0 }
    $a = try { [decimal]$actual   } catch { 0 }
    $diff = [Math]::Abs($a - $e)
    if ($diff -lt 0.01) { Write-Ok "${label}: $actual" }
    else { Write-Fail "${label}: esperado=$expected  real='$actual'" }
}

# ── Leer credenciales ─────────────────────────────────────────────────────────
if (-not (Test-Path $CREDS_FILE)) {
    Write-Host "ERROR  No se encontro $CREDS_FILE" -ForegroundColor Red; exit 1
}
$creds   = Get-Content $CREDS_FILE
$DB_HOST = (($creds | Where-Object { $_ -match '^HOST:' })     -split '\s+')[1]
$DB_PORT = (($creds | Where-Object { $_ -match '^PORT:' })     -split '\s+')[1]
$DB_USER = (($creds | Where-Object { $_ -match '^USER:' })     -split '\s+')[1]
$DB_PASS = (($creds | Where-Object { $_ -match '^PASSWORD:' }) -split '\s+')[1]

# ── Auth ──────────────────────────────────────────────────────────────────────
$PIN  = "12345"
$sqlQ = "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');"
& sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa -Q $sqlQ *>$null

$loginResp = Invoke-RestMethod -Method POST -Uri "$HOST_URL/auth/login" `
    -ContentType "application/json" `
    -Body "{`"emailUser`":`"$EMAIL`",`"pin`":`"$PIN`"}" `
    -SkipCertificateCheck
$TOKEN = $loginResp.accessToken
if (-not $TOKEN -or $TOKEN -eq "null") {
    Write-Host "ERROR  No se pudo obtener token" -ForegroundColor Red; exit 1
}

# ── Descargar todos los asientos ──────────────────────────────────────────────
$allEntries = Invoke-RestMethod -Method GET -Uri "$HOST_URL/accounting-entries/data.json" `
    -Headers @{ "Authorization" = "Bearer $TOKEN" } -SkipCertificateCheck
$TOTAL = @($allEntries).Count

# ── Construir T-accounts ──────────────────────────────────────────────────────
# Recopilar todas las líneas de todos los asientos
$allLines = $allEntries | ForEach-Object {
    $numEntry = $_.numberEntry
    foreach ($line in $_.lines) {
        [PSCustomObject]@{
            numberEntry   = $numEntry
            idAccount     = $line.idAccount
            debitAmount   = [decimal]($line.debitAmount  ?? 0)
            creditAmount  = [decimal]($line.creditAmount ?? 0)
        }
    }
}

# Agrupar por cuenta
$tAccounts = $allLines | Group-Object idAccount | ForEach-Object {
    [PSCustomObject]@{
        account = [int]$_.Name
        totalDR = ($_.Group | Measure-Object -Property debitAmount  -Sum).Sum
        totalCR = ($_.Group | Measure-Object -Property creditAmount -Sum).Sum
    }
} | Sort-Object account

# Diccionario de nombres de cuenta
$accName = @{
    106 = "1.1.06.01  Caja CRC"
    109 = "1.1.07.01  Inventario de Mercaderia"
    117 = "4.5.01     Ingresos por Ventas"
    119 = "5.15.01    Costo de Ventas"
    124 = "1.1.09.01  IVA Acreditable CRC"
    127 = "2.1.04.01  IVA por Pagar CRC"
    130 = "5.14.01.02  Merma Anormal (IAS 2.16)"
}

# Saldos esperados para el Caso 1
#  FC-000001:         DR 109=100000  DR 124=13000  CR 106=113000
#  FV-20260405-001:   DR 106=16950   CR 117=15000  CR 127=1950
#  COGS-FV-000001:    DR 119=10000   CR 109=10000
#  DEV-COGS-FV-:      DR 109=3000    CR 119=3000
#  DEV-ING-FV-:       DR 117=4500    DR 127=585   CR 106=5085
#  AJ-000001:         DR 130=2000    CR 109=2000  (Merma Anormal IAS 2.16)
$expDR   = @{ 106=16950;   109=103000;  117=4500;   119=10000;  124=13000;  127=585;    130=2000 }
$expCR   = @{ 106=118085;  109=12000;   117=15000;  119=3000;   124=0;      127=1950;   130=0    }
$expNeto = @{ 106=101135;  109=91000;   117=10500;  119=7000;   124=13000;  127=1365;   130=2000 }
$expTipo = @{ 106="CR";    109="DR";    117="CR";   119="DR";   124="DR";   127="CR";   130="DR" }

# ── Archivo de salida ─────────────────────────────────────────────────────────
$RUN_TS      = (Get-Date -Format "yyyy-MM-dd_HH-mm-ss")
$OUTPUT_FILE = Join-Path $SCRIPT_DIR "cuentas_caso1_$RUN_TS.txt"

$sb = [System.Text.StringBuilder]::new()
$null = $sb.AppendLine("# ==================================================================")
$null = $sb.AppendLine("#  CASO 1 — REVENTA · Analisis de T-accounts")
$null = $sb.AppendLine("#  Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$null = $sb.AppendLine("# ==================================================================")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("  Total de asientos en el sistema: $TOTAL")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("# ── Detalle de asientos (numero | cuenta | DR | CR) ──────────────")

foreach ($e in $allEntries) {
    foreach ($l in $e.lines) {
        $null = $sb.AppendLine("  $($e.numberEntry)  cta=$($l.idAccount)  DR=$($l.debitAmount)  CR=$($l.creditAmount)")
    }
}

$null = $sb.AppendLine("")
$null = $sb.AppendLine("# ── T-accounts por cuenta ────────────────────────────────────────")
$null = $sb.AppendLine(("  {0,-10} {1,-42} {2,12} {3,12} {4,15}" -f "CUENTA","NOMBRE","TOTAL DR","TOTAL CR","SALDO NETO"))
$null = $sb.AppendLine(("  {0}" -f ("-"*95)))

# ── Encabezado consola ────────────────────────────────────────────────────────
Write-Host ""
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "▶  CASO 1 — Analisis de T-accounts contables" -ForegroundColor Cyan
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host ("  {0,-10} {1,-42} {2,12} {3,12} {4,15}" -f "CUENTA","NOMBRE","TOTAL DR","TOTAL CR","SALDO NETO")
Write-Host ("  {0}" -f ("-"*95))

# Mapa para acceso rápido a los valores reales por cuenta
$realDR = @{}
$realCR = @{}

foreach ($ta in $tAccounts) {
    $acc  = $ta.account
    $dr   = $ta.totalDR
    $cr   = $ta.totalCR
    $realDR[$acc] = $dr
    $realCR[$acc] = $cr

    $name = if ($accName.ContainsKey($acc)) { $accName[$acc] } else { "cuenta $acc" }
    $net  = $dr - $cr
    $netLabel = if ($net -ge 0) { "DR {0:F2}" -f $net } else { "CR {0:F2}" -f (-$net) }

    Write-Host ("  {0,-10} {1,-42} {2,12:F2} {3,12:F2} {4,15}" -f $acc, $name, $dr, $cr, $netLabel)
    $null = $sb.AppendLine(("  {0,-10} {1,-42} {2,12:F2} {3,12:F2} {4,15}" -f $acc, $name, $dr, $cr, $netLabel))
}

Write-Host ("  {0}" -f ("-"*95))
$null = $sb.AppendLine(("  {0}" -f ("-"*95)))

# Totales globales
$grandDR = ($allLines | Measure-Object -Property debitAmount  -Sum).Sum
$grandCR = ($allLines | Measure-Object -Property creditAmount -Sum).Sum

Write-Host ("  {0,-54} {1,12:F2} {2,12:F2}" -f "TOTAL (partida doble)", $grandDR, $grandCR)
Write-Host ""
$null = $sb.AppendLine(("  {0,-54} {1,12:F2} {2,12:F2}" -f "TOTAL (partida doble)", $grandDR, $grandCR))
$null = $sb.AppendLine("")
$null = $sb.AppendLine("# ── Verificaciones de saldos esperados ──────────────────────────")

# ── Verificaciones ────────────────────────────────────────────────────────────
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "▶  Verificaciones de saldos esperados" -ForegroundColor Cyan
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan

function Check-AccountBalance($acc) {
    $name    = if ($accName.ContainsKey($acc)) { $accName[$acc] } else { "cuenta $acc" }
    $realD   = $realDR[$acc] ?? 0
    $realC   = $realCR[$acc] ?? 0
    $eD      = $expDR[$acc]
    $eC      = $expCR[$acc]
    $eNeto   = $expNeto[$acc]
    $eTipo   = $expTipo[$acc]

    Write-Host ""
    Write-Host "  Cuenta $acc — $name" -ForegroundColor White

    Assert-FloatEq "    DR total"   "$eD"     "$realD"
    Assert-FloatEq "    CR total"   "$eC"     "$realC"

    $realNet      = $realD - $realC
    $expNetSigned = if ($eTipo -eq "CR") { -$eNeto } else { $eNeto }
    Assert-FloatEq "    Saldo neto $eTipo $eNeto" "$expNetSigned" "$realNet"

    $null = $sb.AppendLine("")
    $null = $sb.AppendLine("  Cuenta $acc — $name")
    $null = $sb.AppendLine("    DR total  → esperado=$eD  real=$realD")
    $null = $sb.AppendLine("    CR total  → esperado=$eC  real=$realC")
    $null = $sb.AppendLine("    Saldo neto $eTipo ${eNeto} → real=$realNet")
}

Check-AccountBalance 106
Check-AccountBalance 109
Check-AccountBalance 130
Check-AccountBalance 117
Check-AccountBalance 119
Check-AccountBalance 124
Check-AccountBalance 127

# ── Asiento DEV-ING-FV ───────────────────────────────────────────────────────
Write-Host ""
Write-Host "  Asiento DEV-ING-FV — reversion ingresos + IVA devolucion parcial" -ForegroundColor White
$null = $sb.AppendLine("")
$null = $sb.AppendLine("  Asiento DEV-ING-FV")

$devIngEntries = $allEntries | Where-Object { $_.numberEntry -like "DEV-ING-FV-*" }
$devIngCount   = @($devIngEntries).Count

if ($devIngCount -ge 1) {
    Write-Ok "Asiento DEV-ING-FV existe ($devIngCount encontrado(s))"
    $null = $sb.AppendLine("    DEV-ING-FV existe: $devIngCount")

    $devIngLines = $devIngEntries | ForEach-Object { $_.lines } | ForEach-Object { $_ }
    $devIngDR    = ($devIngLines | Measure-Object -Property debitAmount  -Sum).Sum
    $devIngCR    = ($devIngLines | Measure-Object -Property creditAmount -Sum).Sum
    Assert-FloatEq "    DEV-ING-FV DR total (ingresos+IVA revertidos)" "5085" "$devIngDR"
    Assert-FloatEq "    DEV-ING-FV CR total (salida caja/banco)"       "5085" "$devIngCR"

    $devIngDR117 = ($devIngLines | Where-Object { $_.idAccount -eq 117 } | Measure-Object -Property debitAmount -Sum).Sum
    $devIngDR127 = ($devIngLines | Where-Object { $_.idAccount -eq 127 } | Measure-Object -Property debitAmount -Sum).Sum
    Assert-FloatEq "    DEV-ING-FV DR cta 117 (ingresos netos)" "4500" "$devIngDR117"
    Assert-FloatEq "    DEV-ING-FV DR cta 127 (IVA revertido)"  "585"  "$devIngDR127"

    $null = $sb.AppendLine("    DR_117=$devIngDR117  DR_127=$devIngDR127  CR=$devIngCR")
} else {
    Write-Fail "Asiento DEV-ING-FV no encontrado — partial-return no genero reversion de ingresos"
    $null = $sb.AppendLine("    [FAIL] DEV-ING-FV no encontrado")
}

# ── Asiento DEV-COGS-FV ───────────────────────────────────────────────────────
Write-Host ""
Write-Host "  Asiento DEV-COGS-FV — reversion COGS devolucion parcial" -ForegroundColor White
$null = $sb.AppendLine("")
$null = $sb.AppendLine("  Asiento DEV-COGS-FV")

$devCogsEntries = $allEntries | Where-Object { $_.numberEntry -like "DEV-COGS-FV-*" }
$devCogsCount   = @($devCogsEntries).Count

if ($devCogsCount -ge 1) {
    Write-Ok "Asiento DEV-COGS-FV existe ($devCogsCount encontrado(s))"
    $null = $sb.AppendLine("    DEV-COGS-FV existe: $devCogsCount")

    $devCogsLines = $devCogsEntries | ForEach-Object { $_.lines } | ForEach-Object { $_ }
    $devCogsDR    = ($devCogsLines | Measure-Object -Property debitAmount  -Sum).Sum
    $devCogsCR    = ($devCogsLines | Measure-Object -Property creditAmount -Sum).Sum
    Assert-FloatEq "    DEV-COGS-FV DR total (inventario recuperado)" "3000" "$devCogsDR"
    Assert-FloatEq "    DEV-COGS-FV CR total (reversa COGS)"         "3000" "$devCogsCR"
    $null = $sb.AppendLine("    DR=$devCogsDR  CR=$devCogsCR")
} else {
    Write-Fail "Asiento DEV-COGS-FV no encontrado — la devolucion parcial no genero reversion de COGS"
    $null = $sb.AppendLine("    [FAIL] DEV-COGS-FV no encontrado")
}

# Verificar que ninguna línea COGS usa cuenta 0
$cogsZeroCount = ($allEntries |
    Where-Object { $_.numberEntry -like "COGS-FV-*" -or $_.numberEntry -like "DEV-COGS-FV-*" } |
    ForEach-Object { $_.lines } |
    Where-Object { $_.idAccount -eq 0 }).Count

if ($cogsZeroCount -eq 0) {
    Write-Ok "Ninguna linea COGS/DEV-COGS usa cuenta=0 (IdAccountCOGS configurado correctamente)"
    $null = $sb.AppendLine("    [OK] Ninguna linea COGS usa cuenta=0")
} else {
    Write-Fail "!$cogsZeroCount linea(s) de COGS con idAccount=0! Verificar IdAccountCOGS en tipo de factura."
    $null = $sb.AppendLine("    [FAIL] $cogsZeroCount linea(s) de COGS con cuenta=0")
}

# ── Partida doble ─────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "  Partida doble (ΣDR = ΣCR)" -ForegroundColor White
Assert-FloatEq "    ΣDR = ΣCR" "$grandDR" "$grandCR"
$null = $sb.AppendLine("")
$null = $sb.AppendLine("  Partida doble: ΣDR=$grandDR  ΣCR=$grandCR")

# ── Nota contable desde asientos reales ──────────────────────────────────────
$saldo109 = [Math]::Round(($realDR[109] ?? 0) - ($realCR[109] ?? 0), 0)
$saldo124 = [Math]::Round(($realDR[124] ?? 0) - ($realCR[124] ?? 0), 0)
$saldo127 = [Math]::Round(($realCR[127] ?? 0) - ($realDR[127] ?? 0), 0)
$ivaNeto  = $saldo127 - $saldo124
$ivaSigno = if ($ivaNeto -lt 0) { "credito a favor del negocio" } else { "IVA a pagar al gobierno" }

Write-Host ""
Write-Host "  Nota contable (desde asientos reales)" -ForegroundColor Yellow
Write-Host "  Cuenta 109 Inventario:      saldo neto DR = $saldo109" -ForegroundColor Yellow
Write-Host "  Cuenta 124 IVA Acreditable: saldo neto DR = $saldo124 (credito fiscal de compras)" -ForegroundColor Yellow
Write-Host "  Cuenta 127 IVA por Pagar:   saldo neto CR = $saldo127" -ForegroundColor Yellow
Write-Host "  Posicion IVA vs gobierno:   $saldo127 - $saldo124 = $ivaNeto ($ivaSigno)" -ForegroundColor Yellow

$null = $sb.AppendLine("")
$null = $sb.AppendLine("# ── NOTA CONTABLE (calculada desde asientos reales) ──────────────")
$null = $sb.AppendLine("#  - Cuenta 109 Inventario:      saldo neto DR = $saldo109")
$null = $sb.AppendLine("#  - Cuenta 124 IVA Acreditable: saldo neto DR = $saldo124 (credito fiscal de compras)")
$null = $sb.AppendLine("#  - Cuenta 127 IVA por Pagar:   saldo neto CR = $saldo127")
$null = $sb.AppendLine("#  Posicion IVA vs gobierno: $saldo127 - $saldo124 = $ivaNeto ($ivaSigno)")
$null = $sb.AppendLine("")
$null = $sb.AppendLine("# ── RESUMEN ────────────────────────────────────────────────────────")
$null = $sb.AppendLine("  Verificaciones exitosas : $($script:CHECKS_OK)")
$null = $sb.AppendLine("  Verificaciones fallidas : $($script:CHECKS_FAIL)")

# ── Escribir archivo ──────────────────────────────────────────────────────────
$sb.ToString() | Set-Content $OUTPUT_FILE -Encoding UTF8

# ── Resumen final ─────────────────────────────────────────────────────────────
Write-Host ""
if ($script:CHECKS_FAIL -eq 0) {
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║   TODOS LOS SALDOS CONTABLES CORRECTOS              ║" -ForegroundColor Green
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Green
} else {
    Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║   ALGUNOS SALDOS NO COINCIDEN                       ║" -ForegroundColor Red
    Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Red
}
Write-Host "  Exitosas : $($script:CHECKS_OK)" -ForegroundColor Green
if ($script:CHECKS_FAIL -gt 0) {
    Write-Host "  Fallidas : $($script:CHECKS_FAIL)" -ForegroundColor Red
}
Write-Host ""
Write-Host "  Reporte guardado en: $OUTPUT_FILE" -ForegroundColor DarkGray
Write-Host ""

exit $script:CHECKS_FAIL
