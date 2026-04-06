#!/usr/bin/env pwsh
# ============================================================================
#  CASO 2 — MANUFACTURA (Chile Embotellado Marca X)
#  03-analizar-cuentas-contables.ps1 — Analisis de T-accounts y Saldos
#
#  T-accounts esperados (con PROD-CAP IAS 2.12 + Merma Anormal IAS 2.16):
#   106: DR=50,850   CR=67,913   Net=CR 17,063
#   109: DR=55,230   CR=16,832   Net=DR 38,398
#   110: DR=52,600   CR=52,600   Net=0
#   115: DR=52,600   CR=52,600   Net=0
#   117: DR= 7,500   CR=45,000   Net=CR 37,500
#   119: DR=15,780   CR= 2,630   Net=DR 13,150
#   124: DR= 6,838   CR=     0   Net=DR  6,838
#   127: DR=   975   CR= 5,850   Net=CR  4,875
#   130: DR= 1,052   CR=     0   Net=DR  1,052
#   SigmaDR = SigmaCR = 243,425
#
#  Uso:
#   pwsh docs/inventario/caso-2-manufactura/03-analizar-cuentas-contables.ps1
# ============================================================================

param()

$SCRIPT_DIR = $PSScriptRoot
$REPO_ROOT  = (Resolve-Path (Join-Path $SCRIPT_DIR "../../..")).Path
$CREDS_FILE = Join-Path $REPO_ROOT "credentials\db.txt"
$HOST_URL   = "https://localhost:8000/api/v1"
$EMAIL      = "ezekiell1988@hotmail.com"

$checksOk   = 0
$checksFail = 0

# ── Helpers ───────────────────────────────────────────────────────────────────
function Log-Ok([string]$msg)   { Write-Host "  ✅  $msg" -ForegroundColor Green;  $script:checksOk++ }
function Log-Fail([string]$msg) { Write-Host "  ❌  $msg" -ForegroundColor Red;    $script:checksFail++ }
function Log-Info([string]$msg) { Write-Host "  $msg" }

function Assert-FloatEq([string]$label, [double]$expected, $actual) {
    $a    = [double]$actual
    $diff = [Math]::Abs($a - $expected)
    if ($diff -lt 0.01) { Log-Ok "${label}: $a" }
    else { Log-Fail "${label}: esperado=$expected  real=$a" }
}

# ── Leer credenciales BD ──────────────────────────────────────────────────────
$credsContent = Get-Content $CREDS_FILE
$DB_HOST = ($credsContent | Select-String '^HOST:').ToString().Split()[1].Trim()
$DB_PORT = ($credsContent | Select-String '^PORT:').ToString().Split()[1].Trim()
$DB_USER = ($credsContent | Select-String '^USER:').ToString().Split()[1].Trim()
$DB_PASS = ($credsContent | Select-String '^PASSWORD:').ToString().Split()[1].Trim()

# ── Autenticación ─────────────────────────────────────────────────────────────
$PIN = "12345"
sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa `
    -Q "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');" 2>$null

$loginResp = Invoke-RestMethod -Method POST -Uri "$HOST_URL/auth/login" `
    -Body (@{emailUser=$EMAIL;pin=$PIN} | ConvertTo-Json -Compress) `
    -ContentType "application/json" -SkipCertificateCheck -ErrorAction Stop

$TOKEN = $loginResp.accessToken
if (-not $TOKEN -or $TOKEN -eq "null") {
    Write-Host "❌  No se pudo obtener token" -ForegroundColor Red; exit 1
}

# ── Descargar todos los asientos ──────────────────────────────────────────────
$allEntries = Invoke-RestMethod -Method GET -Uri "$HOST_URL/accounting-entries/data.json" `
    -Headers @{ "Authorization" = "Bearer $TOKEN" } -SkipCertificateCheck -ErrorAction Stop

$total = @($allEntries).Count

# ── Construir T-accounts ──────────────────────────────────────────────────────
$tAccounts = @{}   # account → @{dr; cr}

foreach ($entry in $allEntries) {
    foreach ($line in $entry.lines) {
        $acc = [string]$line.idAccount
        if (-not $tAccounts.ContainsKey($acc)) {
            $tAccounts[$acc] = @{ dr = 0.0; cr = 0.0 }
        }
        $tAccounts[$acc].dr += [double]$line.debitAmount
        $tAccounts[$acc].cr += [double]$line.creditAmount
    }
}

# ── Metadatos de cuentas ──────────────────────────────────────────────────────
$accName = @{
    "106" = "1.1.06.01  Caja CRC"
    "109" = "1.1.07.01  Inventario de Mercaderia"
    "110" = "1.1.07.02  Materias Primas"
    "115" = "5.14.03    Costos de Produccion"
    "117" = "4.5.01     Ingresos por Ventas"
    "119" = "5.15.01    Costo de Ventas"
    "124" = "1.1.09.01  IVA Acreditable CRC"
    "127" = "2.1.04.01  IVA por Pagar CRC"
    "130" = "5.14.01.02  Merma Anormal (IAS 2.16)"
}

# ── Saldos esperados Caso 2 ───────────────────────────────────────────────────
#  FC-MP:            DR 110=52600  DR 124=6838   CR 106=59438
#  PROD-OP (×4):     DR 115=52600  CR 110=52600
#  PROD-CAP IAS2.12: DR 109=52600  CR 115=52600  → 115 neto=0
#  FV-PT:            DR 106=50850  CR 117=45000  CR 127=5850
#  COGS-FV:          DR 119=15780  CR 109=15780
#  DEV-COGS-FV:      DR 109=2630   CR 119=2630
#  DEV-ING-FV:       DR 117=7500   DR 127=975    CR 106=8475
#  AJ-regalia:       DR 130=1052   CR 109=1052
#
#  109 DR = 52600+2630=55230   109 CR = 15780+1052=16832
$expDR  = @{ "106"=50850.0; "109"=55230.0; "110"=52600.0; "115"=52600.0; "117"=7500.0;
             "119"=15780.0; "124"=6838.0;  "127"=975.0;   "130"=1052.0 }
$expCR  = @{ "106"=67913.0; "109"=16832.0; "110"=52600.0; "115"=52600.0; "117"=45000.0;
             "119"= 2630.0; "124"=0.0;     "127"=5850.0;  "130"=0.0 }
$expNet = @{ "106"=17063.0; "109"=38398.0; "110"=0.0;     "115"=0.0;     "117"=37500.0;
             "119"=13150.0; "124"=6838.0;  "127"=4875.0;  "130"=1052.0 }
$expNetTipo = @{ "106"="CR"; "109"="DR"; "110"="ZERO"; "115"="ZERO"; "117"="CR";
                 "119"="DR"; "124"="DR"; "127"="CR";   "130"="DR" }

# ── Preparar archivo de reporte ───────────────────────────────────────────────
$runTs      = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$outputFile = Join-Path $SCRIPT_DIR "cuentas_caso2_${runTs}.txt"

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# ==================================================================")
$lines.Add("#  CASO 2 — MANUFACTURA · Analisis de T-accounts")
$lines.Add("#  Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
$lines.Add("# ==================================================================")
$lines.Add("")
$lines.Add("  Total de asientos en el sistema: $total")
$lines.Add("")
$lines.Add("# ── Detalle de asientos (numero | cuenta | DR | CR) ──────────────")

foreach ($entry in $allEntries) {
    foreach ($ln in $entry.lines) {
        $lines.Add("  $($entry.numberEntry)  cta=$($ln.idAccount)  DR=$($ln.debitAmount)  CR=$($ln.creditAmount)")
    }
}

$lines.Add("")
$lines.Add("# ── T-accounts por cuenta ────────────────────────────────────────")
$lines.Add(("  {0,-10} {1,-44} {2,12} {3,12} {4,15}" -f "CUENTA","NOMBRE","TOTAL DR","TOTAL CR","SALDO NETO"))
$lines.Add("  " + "─"*97)

# ── Encabezado en pantalla ────────────────────────────────────────────────────
Write-Host ""
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "▶  CASO 2 — Analisis de T-accounts contables" -ForegroundColor Cyan
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Total asientos en el sistema: $total"
Write-Host ""
Write-Host ("  {0,-10} {1,-44} {2,12} {3,12} {4,15}" -f "CUENTA","NOMBRE","TOTAL DR","TOTAL CR","SALDO NETO")
Write-Host ("  " + "─"*97)

# ── Mostrar tabla ─────────────────────────────────────────────────────────────
$sortedAccs = $tAccounts.Keys | Sort-Object { [double]$_ }
foreach ($acc in $sortedAccs) {
    $dr   = $tAccounts[$acc].dr
    $cr   = $tAccounts[$acc].cr
    $net  = $dr - $cr
    $name = if ($accName.ContainsKey($acc)) { $accName[$acc] } else { "cuenta $acc" }
    if ($net -ge 0) { $netLabel = "DR $("{0:F2}" -f $net)" }
    else            { $netLabel = "CR $("{0:F2}" -f (-$net))" }
    $row = "  {0,-10} {1,-44} {2,12:F2} {3,12:F2} {4,15}" -f $acc,$name,$dr,$cr,$netLabel
    Write-Host $row
    $lines.Add($row)
}

Write-Host ("  " + "─"*97)
$lines.Add("  " + "─"*97)

# Totales globales
$grandDR = ($allEntries | ForEach-Object { $_.lines } | ForEach-Object { [double]$_.debitAmount }  | Measure-Object -Sum).Sum
$grandCR = ($allEntries | ForEach-Object { $_.lines } | ForEach-Object { [double]$_.creditAmount } | Measure-Object -Sum).Sum

Write-Host ""
Write-Host ("  {0,-56} {1,12:F2} {2,12:F2}" -f "TOTAL (partida doble)",$grandDR,$grandCR)
Write-Host ""
$lines.Add("")
$lines.Add(("  {0,-56} {1,12:F2} {2,12:F2}" -f "TOTAL (partida doble)",$grandDR,$grandCR))
$lines.Add("")
$lines.Add("# ── Verificaciones de saldos esperados ──────────────────────────")

# ── Verificaciones por cuenta ─────────────────────────────────────────────────
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "▶  Verificaciones de saldos esperados" -ForegroundColor Cyan
Write-Host "══════════════════════════════════════════════════════" -ForegroundColor Cyan

function Check-Account([string]$acc) {
    $name    = if ($accName.ContainsKey($acc)) { $accName[$acc] } else { $acc }
    $realDR  = if ($tAccounts.ContainsKey($acc)) { $tAccounts[$acc].dr } else { 0.0 }
    $realCR  = if ($tAccounts.ContainsKey($acc)) { $tAccounts[$acc].cr } else { 0.0 }
    $eDR     = $expDR[$acc]; $eCR = $expCR[$acc]; $eNet = $expNet[$acc]; $eTipo = $expNetTipo[$acc]

    Write-Host ""
    Write-Host "  Cuenta $acc — $name" -ForegroundColor White
    $script:lines.Add(""); $script:lines.Add("  Cuenta $acc — $name")

    Assert-FloatEq "    DR total" $eDR $realDR
    Assert-FloatEq "    CR total" $eCR $realCR

    $realNet = $realDR - $realCR
    if ($realNet -ge 0) { $sign = "DR"; $absReal = $realNet }
    else                { $sign = "CR"; $absReal = -$realNet }

    if ($eTipo -eq "ZERO") {
        if ($absReal -lt 0.01) { Log-Ok "    Saldo neto = 0 (ZERO — DR = CR)" }
        else { Log-Fail "    Saldo neto = $sign $("{0:F2}" -f $absReal) (esperado ZERO)" }
    } elseif ([Math]::Abs($absReal - $eNet) -lt 0.01 -and $sign -eq $eTipo) {
        Log-Ok "    Saldo neto = $sign $("{0:F2}" -f $absReal) (esperado $eTipo $eNet)"
    } else {
        Log-Fail "    Saldo neto = $sign $("{0:F2}" -f $absReal) (esperado $eTipo $eNet)"
    }
}

foreach ($acc in @("106","109","110","130","115","117","119","124","127")) {
    Check-Account $acc
}

# ── Asientos de producción (PROD-OP: DR 115 / CR 110) ────────────────────────
Write-Host ""
Write-Host "  Asiento produccion (originModule=ProductionOrder, DR 115 / CR 110)" -ForegroundColor White
$script:lines.Add(""); $script:lines.Add("  Asiento produccion (originModule=ProductionOrder)")

$prodOPEntries = @($allEntries | Where-Object { $_.originModule -eq "ProductionOrder" -and $_.numberEntry -like "AJ-*" })
if ($prodOPEntries.Count -ge 1) {
    Log-Ok "Asientos de produccion encontrados: $($prodOPEntries.Count)"
    $prodDR115 = ($prodOPEntries | ForEach-Object { $_.lines } | Where-Object { $_.idAccount -eq 115 } | ForEach-Object { [double]$_.debitAmount } | Measure-Object -Sum).Sum
    $prodCR110 = ($prodOPEntries | ForEach-Object { $_.lines } | Where-Object { $_.idAccount -eq 110 } | ForEach-Object { [double]$_.creditAmount } | Measure-Object -Sum).Sum
    Assert-FloatEq "    Produccion: SigmaDR 115 (Costos Produccion) = 52600" 52600 $prodDR115
    Assert-FloatEq "    Produccion: SigmaCR 110 (Materias Primas) = 52600"   52600 $prodCR110
    Assert-FloatEq "    Produccion: DR 115 = CR 110 (partida doble OK)"      $prodDR115 $prodCR110
    $script:lines.Add("    Produccion asientos=$($prodOPEntries.Count)  DR115=$prodDR115  CR110=$prodCR110")
} else {
    Log-Fail "No se encontraron asientos de produccion (originModule=ProductionOrder, prefijo AJ-)"
    $script:lines.Add("    [FAIL] No se encontraron asientos de produccion")
}

# ── Asiento PROD-CAP (IAS 2.12: DR 109 / CR 115) ─────────────────────────────
Write-Host ""
Write-Host "  Asiento PROD-CAP (IAS 2.12) — DR 109 Inventario PT / CR 115 Costos Produccion" -ForegroundColor White
$script:lines.Add(""); $script:lines.Add("  Asiento PROD-CAP (IAS 2.12)")

$prodCapEntries = @($allEntries | Where-Object { $_.numberEntry -like "PROD-CAP-*" })
if ($prodCapEntries.Count -ge 1) {
    Log-Ok "PROD-CAP encontrado ($($prodCapEntries.Count) asiento(s))"
    $capDR109 = ($prodCapEntries | ForEach-Object { $_.lines } | Where-Object { $_.idAccount -eq 109 } | ForEach-Object { [double]$_.debitAmount } | Measure-Object -Sum).Sum
    $capCR115 = ($prodCapEntries | ForEach-Object { $_.lines } | Where-Object { $_.idAccount -eq 115 } | ForEach-Object { [double]$_.creditAmount } | Measure-Object -Sum).Sum
    Assert-FloatEq "    DR 109 Inventario PT = 52600 (costo total produccion)" 52600 $capDR109
    Assert-FloatEq "    CR 115 Costos Produccion = 52600"                      52600 $capCR115
    Assert-FloatEq "    DR 109 = CR 115 (IAS 2.12 partida doble OK)"           $capDR109 $capCR115
    $script:lines.Add("    CAP_COUNT=$($prodCapEntries.Count)  DR_109=$capDR109  CR_115=$capCR115")
} else {
    Log-Fail "PROD-CAP no encontrado — IAS 2.12 no aplicado"
    $script:lines.Add("    [FAIL] PROD-CAP no encontrado")
}

# ── Asiento DEV-ING-FV ────────────────────────────────────────────────────────
Write-Host ""
Write-Host "  Asiento DEV-ING-FV — reversion ingresos + IVA (devolucion 5 frascos)" -ForegroundColor White
$script:lines.Add(""); $script:lines.Add("  Asiento DEV-ING-FV")

$devIngEntries = @($allEntries | Where-Object { $_.numberEntry -like "DEV-ING-FV-*" })
if ($devIngEntries.Count -ge 1) {
    Log-Ok "DEV-ING-FV encontrado"
    $devIngDR117 = ($devIngEntries | ForEach-Object { $_.lines } | Where-Object { $_.idAccount -eq 117 } | ForEach-Object { [double]$_.debitAmount } | Measure-Object -Sum).Sum
    $devIngDR127 = ($devIngEntries | ForEach-Object { $_.lines } | Where-Object { $_.idAccount -eq 127 } | ForEach-Object { [double]$_.debitAmount } | Measure-Object -Sum).Sum
    $devIngCR106 = ($devIngEntries | ForEach-Object { $_.lines } | Where-Object { $_.idAccount -eq 106 } | ForEach-Object { [double]$_.creditAmount } | Measure-Object -Sum).Sum
    Assert-FloatEq "    DR 117 Ingresos = 7500 (5 × 1500)"  7500  $devIngDR117
    Assert-FloatEq "    DR 127 IVA = 975 (7500 × 13%)"       975  $devIngDR127
    Assert-FloatEq "    CR 106 Caja = 8475"                 8475  $devIngCR106
    $script:lines.Add("    DR_117=$devIngDR117  DR_127=$devIngDR127  CR_106=$devIngCR106")
} else {
    Log-Fail "DEV-ING-FV no encontrado"
    $script:lines.Add("    [FAIL] DEV-ING-FV no encontrado")
}

# ── Asiento DEV-COGS-FV ───────────────────────────────────────────────────────
Write-Host ""
Write-Host "  Asiento DEV-COGS-FV — reversion COGS (5 × 526 = 2630)" -ForegroundColor White
$script:lines.Add(""); $script:lines.Add("  Asiento DEV-COGS-FV")

$devCogsEntries = @($allEntries | Where-Object { $_.numberEntry -like "DEV-COGS-FV-*" })
if ($devCogsEntries.Count -ge 1) {
    Log-Ok "DEV-COGS-FV encontrado"
    $devCogsDR109 = ($devCogsEntries | ForEach-Object { $_.lines } | Where-Object { $_.idAccount -eq 109 } | ForEach-Object { [double]$_.debitAmount } | Measure-Object -Sum).Sum
    $devCogsCR119 = ($devCogsEntries | ForEach-Object { $_.lines } | Where-Object { $_.idAccount -eq 119 } | ForEach-Object { [double]$_.creditAmount } | Measure-Object -Sum).Sum
    Assert-FloatEq "    DR 109 Inventario = 2630 (5 × 526)" 2630 $devCogsDR109
    Assert-FloatEq "    CR 119 COGS = 2630"                 2630 $devCogsCR119
    $script:lines.Add("    DR_109=$devCogsDR109  CR_119=$devCogsCR119")
} else {
    Log-Fail "DEV-COGS-FV no encontrado"
    $script:lines.Add("    [FAIL] DEV-COGS-FV no encontrado")
}

# ── Verificar que ninguna línea COGS usa cuenta 0 ────────────────────────────
$cogsZeroCount = @($allEntries |
    Where-Object { $_.numberEntry -like "COGS-*" -or $_.numberEntry -like "DEV-COGS-*" } |
    ForEach-Object { $_.lines } |
    Where-Object { $_.idAccount -eq 0 }).Count
if ($cogsZeroCount -eq 0) { Log-Ok "Ninguna linea COGS ni DEV-COGS usa cuenta=0 ✓" }
else { Log-Fail "$cogsZeroCount linea(s) de COGS con cuenta=0" }

# ── Partida doble global ──────────────────────────────────────────────────────
Write-Host ""
Write-Host "  Partida doble global (SigmaDR = SigmaCR)" -ForegroundColor White
Assert-FloatEq "    SigmaDR = SigmaCR = 243,425" $grandDR $grandCR
$script:lines.Add("")
$script:lines.Add("  Partida doble global: SigmaDR=$("{0:F2}" -f $grandDR)  SigmaCR=$("{0:F2}" -f $grandCR)")

# ── Nota contable ─────────────────────────────────────────────────────────────
$saldo110 = if ($tAccounts["110"]) { $tAccounts["110"].dr - $tAccounts["110"].cr } else { 0 }
$saldo115 = if ($tAccounts["115"]) { $tAccounts["115"].dr - $tAccounts["115"].cr } else { 0 }
$saldo109 = if ($tAccounts["109"]) { $tAccounts["109"].dr - $tAccounts["109"].cr } else { 0 }
$saldo124 = if ($tAccounts["124"]) { $tAccounts["124"].dr - $tAccounts["124"].cr } else { 0 }
$saldo127CR = if ($tAccounts["127"]) { $tAccounts["127"].cr - $tAccounts["127"].dr } else { 0 }
$ivaNeto  = $saldo127CR - $saldo124
$ivaSigno = if ($ivaNeto -lt 0) { "credito a favor del negocio" } else { "IVA a pagar al gobierno" }

Write-Host ""
Write-Host "  📋 Nota contable (desde asientos reales)" -ForegroundColor Yellow
Write-Host "  Cuenta 110 Mat. Primas:       saldo neto = ₡$("{0:N0}" -f [Math]::Abs($saldo110)) (MP comprada y consumida integramente)" -ForegroundColor Yellow
Write-Host "  Cuenta 115 Costos Produccion: saldo neto = ₡$("{0:N0}" -f [Math]::Abs($saldo115)) (IAS 2.12 — capitalizado integramente a inv. PT)" -ForegroundColor Yellow
Write-Host "  Cuenta 109 Inventario PT:     saldo neto DR = ₡$("{0:N0}" -f $saldo109) (inventario PT disponible)" -ForegroundColor Yellow
Write-Host "  Cuenta 124 IVA Acreditable:   saldo neto DR = ₡$("{0:N0}" -f $saldo124)" -ForegroundColor Yellow
Write-Host "  Cuenta 127 IVA por Pagar:     saldo neto CR = ₡$("{0:N0}" -f $saldo127CR)" -ForegroundColor Yellow
Write-Host "  Posicion IVA vs gobierno:     ₡$("{0:N0}" -f $saldo127CR) - ₡$("{0:N0}" -f $saldo124) = ₡$("{0:N0}" -f $ivaNeto) ($ivaSigno)" -ForegroundColor Yellow

$script:lines.AddRange([string[]]@(
    "",
    "  Nota contable:",
    "  Cuenta 110 Mat. Primas       neto = ₡$($saldo110) (MP comprada y consumida)",
    "  Cuenta 115 Costos Produccion neto = ₡$($saldo115) (IAS 2.12 — capitalizado a inv. PT)",
    "  Cuenta 109 Inventario PT     DR neto = ₡$($saldo109) (inventario disponible en bodega)",
    "  Cuenta 124 IVA Acreditable   DR neto = ₡$($saldo124)",
    "  Cuenta 127 IVA por Pagar     CR neto = ₡$($saldo127CR)",
    "  IVA neto (127-124)           = ₡$($ivaNeto) ($ivaSigno)",
    "",
    "# ── RESUMEN ────────────────────────────────────────────────────────",
    "  Verificaciones exitosas : $checksOk",
    "  Verificaciones fallidas : $checksFail"
))

$script:lines | Set-Content $outputFile -Encoding UTF8

# ── Resumen ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════╗"
if ($checksFail -eq 0) {
    Write-Host "║   ✅  TODOS LOS SALDOS CONTABLES CORRECTOS         ║" -ForegroundColor Green
} else {
    Write-Host "║   ⚠   ALGUNOS SALDOS NO COINCIDEN                  ║" -ForegroundColor Red
}
Write-Host "╚══════════════════════════════════════════════════════╝"
Write-Host "  ✅  Exitosas : $checksOk"  -ForegroundColor Green
if ($checksFail -gt 0) { Write-Host "  ❌  Fallidas : $checksFail" -ForegroundColor Red }
Write-Host ""
Write-Host "  Reporte guardado en: $outputFile"
Write-Host ""

exit ($checksFail -gt 0 ? 1 : 0)
