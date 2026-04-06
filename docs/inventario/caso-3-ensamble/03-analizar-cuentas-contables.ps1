# ============================================================================
#  CASO 3 — ENSAMBLE EN VENTA (Hot Dog)
#  03-analizar-cuentas-contables.ps1 — Analisis de T-accounts y Saldos
#
#  Propósito:
#   Descarga todos los asientos del periodo, construye los T-accounts por
#   cuenta, calcula saldos netos y los compara contra los valores esperados.
#   Genera cuentas_caso3_*.txt con el reporte completo.
#
#  Asientos esperados del Caso 3:
#   FC-*     : DR 110=75000  DR 124=9750   CR 106=84750
#   PROD-OP  : DR 115=4500   CR 110=4500   (4 asientos, 3 hot dogs)
#   PROD-CAP : DR 109=4500   CR 115=4500   (IAS 2.12)
#   FV-*     : DR 106=10170  CR 117=9000   CR 127=1170
#   COGS-FV  : DR 119=4500   CR 109=4500   (3 x 1500)
#   DEV-COGS : DR 109=1500   CR 119=1500   (1 x 1500)
#   DEV-ING  : DR 117=3000   DR 127=390    CR 106=3390
#   AJ-*     : DR 130=3000   CR 109=3000   (2 x 1500, IAS 2.16)
#
#  Uso:
#   pwsh docs/inventario/caso-3-ensamble/03-analizar-cuentas-contables.ps1
# ============================================================================

# ── Rutas y estado ────────────────────────────────────────────────────────────
$SCRIPT_DIR  = $PSScriptRoot
$HOST_URL    = 'https://localhost:8000/api/v1'
$EMAIL       = 'ezekiell1988@hotmail.com'
$CREDS_FILE  = Join-Path $SCRIPT_DIR '../../../credentials/db.txt'

$script:CHECKS_OK   = 0
$script:CHECKS_FAIL = 0
$script:REAL_DR     = @{}
$script:REAL_CR     = @{}

# ── Helpers ───────────────────────────────────────────────────────────────────

function Write-Ok([string]$msg) {
    Write-Host "  ✅  $msg" -ForegroundColor Green
    $script:CHECKS_OK++
}

function Write-Fail([string]$msg) {
    Write-Host "  ❌  $msg" -ForegroundColor Red
    $script:CHECKS_FAIL++
}

function Write-Info([string]$msg) { Write-Host "  $msg" -ForegroundColor DarkGray }

function Assert-FloatEqual([string]$label, $expected, $actual) {
    try {
        $diff = [Math]::Abs([double]"$actual" - [double]"$expected")
        if ($diff -lt 0.01) {
            Write-Ok "${label}: $actual"
        } else {
            Write-Fail "${label}: esperado=$expected  real=$actual"
        }
    } catch {
        Write-Fail "${label}: error al comparar (expected=$expected, actual=$actual)"
    }
}

# ── Verificar dependencias ────────────────────────────────────────────────────
if (-not (Get-Command 'sqlcmd' -ErrorAction SilentlyContinue)) {
    Write-Host 'FALTA dependencia: sqlcmd' -ForegroundColor Red; exit 1
}

# ── Leer credenciales BD ──────────────────────────────────────────────────────
$credLines = Get-Content $CREDS_FILE
$DB_HOST   = ($credLines | Select-String '^HOST:').ToString().Split()[1].Trim()
$DB_PORT   = ($credLines | Select-String '^PORT:').ToString().Split()[1].Trim()
$DB_USER   = ($credLines | Select-String '^USER:').ToString().Split()[1].Trim()
$DB_PASS   = ($credLines | Select-String '^PASSWORD:').ToString().Split()[1].Trim()

# ── Autenticacion ─────────────────────────────────────────────────────────────
$PIN = '12345'
& sqlcmd -S "${DB_HOST},${DB_PORT}" -U $DB_USER -P $DB_PASS -C -d dbfa `
    -Q "SET NOCOUNT ON; DELETE FROM dbo.userPin WHERE idUser = 1 AND pin = '$PIN'; INSERT INTO dbo.userPin (idUser, pin) VALUES (1, '$PIN');" 2>$null

$loginBody = @{ emailUser = $EMAIL; pin = $PIN } | ConvertTo-Json -Compress
$loginResp  = Invoke-RestMethod -Method POST -Uri "$HOST_URL/auth/login" `
    -Body ([System.Text.Encoding]::UTF8.GetBytes($loginBody)) `
    -ContentType 'application/json' -SkipCertificateCheck -StatusCodeVariable 'loginSc'
$TOKEN = $loginResp.accessToken

if (-not $TOKEN -or $TOKEN -eq 'null') {
    Write-Host 'No se pudo obtener token' -ForegroundColor Red; exit 1
}

# ── Descargar todos los asientos ──────────────────────────────────────────────
$entriesRaw = Invoke-RestMethod -Method GET -Uri "$HOST_URL/accounting-entries/data.json" `
    -Headers @{ Authorization = "Bearer $TOKEN" } -SkipCertificateCheck
$entries = @($entriesRaw)
$TOTAL   = $entries.Count

# ── Catalogos de cuentas ──────────────────────────────────────────────────────
$ACC_NAME = @{
    '106' = '1.1.06.01  Caja CRC'
    '109' = '1.1.07.01  Inventario de Mercaderia'
    '110' = '1.1.07.02  Materias Primas'
    '115' = '5.14.03    Costos de Produccion'
    '117' = '4.5.01     Ingresos por Ventas'
    '119' = '5.15.01    Costo de Ventas'
    '124' = '1.1.09.01  IVA Acreditable CRC'
    '127' = '2.1.04.01  IVA por Pagar CRC'
    '130' = '5.14.01.02 Merma Anormal (IAS 2.16)'
}

# Saldos esperados para el Caso 3
$EXP_DR   = @{ '106'=10170; '109'=6000;  '110'=75000; '115'=4500; '117'=3000; '119'=4500; '124'=9750; '127'=390;  '130'=3000 }
$EXP_CR   = @{ '106'=88140; '109'=7500;  '110'=4500;  '115'=4500; '117'=9000; '119'=1500; '124'=0;    '127'=1170; '130'=0    }
$EXP_NETO = @{ '106'=77970; '109'=1500;  '110'=70500; '115'=0;    '117'=6000; '119'=3000; '124'=9750; '127'=780;  '130'=3000 }
$EXP_TIPO = @{ '106'='CR';  '109'='CR';  '110'='DR';  '115'='ZERO'; '117'='CR'; '119'='DR'; '124'='DR'; '127'='CR'; '130'='DR' }

# ── Preparar reporte ──────────────────────────────────────────────────────────
$RUN_TS     = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$OUTPUT_FILE = Join-Path $SCRIPT_DIR "cuentas_caso3_${RUN_TS}.txt"
$separator  = '-' * 97

$reportHeader = @(
    '# =================================================================='
    "#  CASO 3 — ENSAMBLE EN VENTA - Analisis de T-accounts"
    "#  Generado: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    '# =================================================================='
    ''
    "  Total de asientos en el sistema: $TOTAL"
    ''
    '# -- T-accounts por cuenta ------------------------------------------'
    ("  {0,-10} {1,-44} {2,12} {3,12} {4,15}" -f 'CUENTA', 'NOMBRE', 'TOTAL DR', 'TOTAL CR', 'SALDO NETO')
    "  $separator"
)
$reportHeader | Set-Content $OUTPUT_FILE -Encoding UTF8

# ── Encabezado en pantalla ────────────────────────────────────────────────────
Write-Host ''
Write-Host ('=' * 54) -ForegroundColor Cyan
Write-Host '  CASO 3 — Analisis de T-accounts contables' -ForegroundColor Cyan
Write-Host ('=' * 54) -ForegroundColor Cyan
Write-Host ''
Write-Host "  Total asientos en el sistema: $TOTAL"
Write-Host ''
Write-Host ("  {0,-10} {1,-44} {2,12} {3,12} {4,15}" -f 'CUENTA', 'NOMBRE', 'TOTAL DR', 'TOTAL CR', 'SALDO NETO')
Write-Host "  $separator"

# ── Construir T-accounts con PowerShell ──────────────────────────────────────
$tAccData = @{}
foreach ($entry in $entries) {
    foreach ($line in @($entry.lines)) {
        $acc = "$($line.idAccount)"
        if (-not $tAccData[$acc]) { $tAccData[$acc] = @{ DR = 0.0; CR = 0.0 } }
        $tAccData[$acc].DR += [double]$line.debitAmount
        $tAccData[$acc].CR += [double]$line.creditAmount
    }
}

foreach ($acc in ($tAccData.Keys | Sort-Object { [int]$_ })) {
    $dr = $tAccData[$acc].DR
    $cr = $tAccData[$acc].CR
    $script:REAL_DR[$acc] = $dr
    $script:REAL_CR[$acc] = $cr

    $name = $ACC_NAME[$acc] ? $ACC_NAME[$acc] : "cuenta $acc"
    $net  = $dr - $cr

    if ($net -ge 0) {
        $netLabel = "DR {0:F2}" -f $net
    } else {
        $netLabel = "CR {0:F2}" -f (-$net)
    }

    $lineStr = "  {0,-10} {1,-44} {2,12:F2} {3,12:F2} {4,15}" -f $acc, $name, $dr, $cr, $netLabel
    Write-Host $lineStr
    Add-Content $OUTPUT_FILE $lineStr
}

Write-Host "  $separator"
Write-Host ''
Add-Content $OUTPUT_FILE "  $separator"
Add-Content $OUTPUT_FILE ''

# ── Totales de partida doble ──────────────────────────────────────────────────
$allLines = $entries | ForEach-Object { $_.lines }
$GRAND_DR = ($allLines | Measure-Object debitAmount  -Sum).Sum
$GRAND_CR = ($allLines | Measure-Object creditAmount -Sum).Sum
if ($null -eq $GRAND_DR) { $GRAND_DR = 0.0 }
if ($null -eq $GRAND_CR) { $GRAND_CR = 0.0 }

$totalLine = "  {0,-56} {1,12} {2,12}" -f 'TOTAL (partida doble)', ([double]$GRAND_DR).ToString('F2'), ([double]$GRAND_CR).ToString('F2')
Write-Host $totalLine
Write-Host ''
Add-Content $OUTPUT_FILE $totalLine
Add-Content $OUTPUT_FILE ''
Add-Content $OUTPUT_FILE '# -- Verificaciones de saldos esperados ------------------'

# ════════════════════════════════════════════════════════════════════════════
# Verificaciones por cuenta
# ════════════════════════════════════════════════════════════════════════════
Write-Host ('=' * 54) -ForegroundColor Cyan
Write-Host '  Verificaciones de saldos esperados' -ForegroundColor Cyan
Write-Host ('=' * 54) -ForegroundColor Cyan

function Test-AccountBalance([string]$acc) {
    $name    = $ACC_NAME[$acc] ? $ACC_NAME[$acc] : $acc
    $realDr  = $script:REAL_DR[$acc] ? [double]$script:REAL_DR[$acc] : 0.0
    $realCr  = $script:REAL_CR[$acc] ? [double]$script:REAL_CR[$acc] : 0.0
    $expDr   = [double]$EXP_DR[$acc]
    $expCr   = [double]$EXP_CR[$acc]
    $expNeto = [double]$EXP_NETO[$acc]
    $expTipo = $EXP_TIPO[$acc]

    Write-Host ''
    Write-Host ("  Cuenta $acc — $name") -ForegroundColor White
    Add-Content $OUTPUT_FILE ''
    Add-Content $OUTPUT_FILE "  Cuenta $acc — $name"

    Assert-FloatEqual '    DR total' "$expDr" "$realDr"
    Assert-FloatEqual '    CR total' "$expCr" "$realCr"

    $realNeto = $realDr - $realCr
    if ($realNeto -ge 0) {
        $sign = 'DR'; $absReal = $realNeto
    } else {
        $sign = 'CR'; $absReal = -$realNeto
    }
    $label    = "$sign $( $absReal.ToString('F2') )"
    $expLabel = "$expTipo $expNeto"

    $diff = [Math]::Abs($absReal - $expNeto)
    if ($diff -lt 0.01 -and $sign -eq $expTipo) {
        Write-Ok  "    Saldo neto = $label (esperado $expLabel)"
        Add-Content $OUTPUT_FILE "    [OK] Saldo neto = $label"
    } elseif ($expTipo -eq 'ZERO' -and $diff -lt 0.01) {
        Write-Ok  "    Saldo neto = 0.00 (capitalizado — IAS 2.12)"
        Add-Content $OUTPUT_FILE "    [OK] Saldo neto = 0.00"
    } else {
        Write-Fail "    Saldo neto = $label (esperado $expLabel)"
        Add-Content $OUTPUT_FILE "    [FAIL] Saldo neto = $label (esperado $expLabel)"
    }
}

Test-AccountBalance '106'    # Caja CRC
Test-AccountBalance '109'    # Inventario Mercaderia
Test-AccountBalance '110'    # Materias Primas
Test-AccountBalance '130'    # Merma Anormal (IAS 2.16)
Test-AccountBalance '115'    # Costos de Produccion
Test-AccountBalance '117'    # Ingresos por Ventas
Test-AccountBalance '119'    # Costo de Ventas
Test-AccountBalance '124'    # IVA Acreditable
Test-AccountBalance '127'    # IVA por Pagar

# ════════════════════════════════════════════════════════════════════════════
# Verificar asientos de produccion PROD-OP (DR 115 / CR 110)
# ════════════════════════════════════════════════════════════════════════════
Write-Host ''
Write-Host '  Asientos PROD-OP — produccion automatica (DR 115 / CR 110)' -ForegroundColor White
Add-Content $OUTPUT_FILE ''
Add-Content $OUTPUT_FILE '  Asientos PROD-OP (originModule=ProductionOrder)'

$prodEntries = @($entries | Where-Object { $_.originModule -eq 'ProductionOrder' -and $_.numberEntry -like 'AJ-*' })
$prodCount   = $prodEntries.Count
if ($prodCount -ge 1) {
    Write-Ok "Asientos de produccion encontrados: $prodCount (esperado=4, uno por ingrediente)"
    $prodLines = $prodEntries | ForEach-Object { $_.lines }
    $prodDr115 = ($prodLines | Where-Object { $_.idAccount -eq 115 } | Measure-Object debitAmount  -Sum).Sum ?? 0
    $prodCr110 = ($prodLines | Where-Object { $_.idAccount -eq 110 } | Measure-Object creditAmount -Sum).Sum ?? 0
    Assert-FloatEqual '    Produccion: DR 115 (Costos Produccion) = 4500' '4500' $prodDr115
    Assert-FloatEqual '    Produccion: CR 110 (Materias Primas) = 4500'   '4500' $prodCr110
    Assert-FloatEqual '    Produccion: DR 115 = CR 110 (partida doble)'   $prodDr115 $prodCr110
    Add-Content $OUTPUT_FILE "    asientos=$prodCount  DR115=$prodDr115  CR110=$prodCr110"
} else {
    Write-Fail 'No se encontraron asientos de produccion (originModule=ProductionOrder)'
    Add-Content $OUTPUT_FILE '    [FAIL] No se encontraron asientos de produccion'
}

# ════════════════════════════════════════════════════════════════════════════
# Verificar asiento PROD-CAP (IAS 2.12 — capitalizacion MP→PT)
# ════════════════════════════════════════════════════════════════════════════
Write-Host ''
Write-Host '  Asiento PROD-CAP (IAS 2.12) — DR 109 Inventario PT / CR 115 Costos' -ForegroundColor White
Add-Content $OUTPUT_FILE ''
Add-Content $OUTPUT_FILE '  Asiento PROD-CAP (IAS 2.12 — capitalizacion MP→PT)'

$capDr109 = 0.0; $capCr115 = 0.0; $capCount = 0
foreach ($e in $entries) {
    if ($e.numberEntry -like 'PROD-CAP-*') {
        $capCount++
        foreach ($l in @($e.lines)) {
            if ($l.idAccount -eq 109) { $capDr109 += [double]($l.debitAmount  ?? 0) }
            if ($l.idAccount -eq 115) { $capCr115 += [double]($l.creditAmount ?? 0) }
        }
    }
}
if ($capCount -ge 1) {
    Write-Ok "PROD-CAP encontrado ($capCount asiento(s))"
    Assert-FloatEqual '    DR 109 Inventario PT = 4500 (3 x 1500)' '4500' $capDr109
    Assert-FloatEqual '    CR 115 Costos Produccion = 4500'          '4500' $capCr115
    Assert-FloatEqual '    DR 109 = CR 115 (IAS 2.12 partida doble)' $capDr109 $capCr115
    Add-Content $OUTPUT_FILE "    CAP_COUNT=$capCount  DR_109=$capDr109  CR_115=$capCr115"
} else {
    Write-Fail 'PROD-CAP no encontrado — IAS 2.12 no aplicado'
    Add-Content $OUTPUT_FILE '    [FAIL] PROD-CAP no encontrado'
}

# ════════════════════════════════════════════════════════════════════════════
# Verificar asiento COGS-FV (DR 119 / CR 109)
# ════════════════════════════════════════════════════════════════════════════
Write-Host ''
Write-Host '  Asiento COGS-FV — costo de ventas 3 hot dogs (3 x 1500 = 4500)' -ForegroundColor White
Add-Content $OUTPUT_FILE ''
Add-Content $OUTPUT_FILE '  Asiento COGS-FV'

$cogsCount = 0; $cogsDr = 0.0; $cogsCr = 0.0; $cogsZero = 0
foreach ($e in $entries) {
    if ($e.numberEntry -like 'COGS-FV-*') {
        $cogsCount++
        foreach ($l in @($e.lines)) {
            $cogsDr  += [double]($l.debitAmount  ?? 0)
            $cogsCr  += [double]($l.creditAmount ?? 0)
            if ($l.idAccount -eq 0) { $cogsZero++ }
        }
    }
    if ($e.numberEntry -like 'DEV-COGS-FV-*') {
        foreach ($l in @($e.lines)) {
            if ($l.idAccount -eq 0) { $cogsZero++ }
        }
    }
}
if ($cogsCount -ge 1) {
    Write-Ok "Asiento COGS-FV encontrado ($cogsCount)"
    Assert-FloatEqual '    COGS-FV DR 119 = 4500' '4500' $cogsDr
    Assert-FloatEqual '    COGS-FV CR 109 = 4500' '4500' $cogsCr
    Add-Content $OUTPUT_FILE "    DR=$cogsDr  CR=$cogsCr"

    if ($cogsZero -eq 0) {
        Write-Ok 'Ninguna linea COGS usa cuenta=0 (IdAccountCOGS configurado correctamente)'
        Add-Content $OUTPUT_FILE '    [OK] Ninguna linea COGS usa cuenta=0'
    } else {
        Write-Fail "${cogsZero} linea(s) de COGS con idAccount=0! Verificar IdAccountCOGS en tipo de factura."
        Add-Content $OUTPUT_FILE "    [FAIL] $cogsZero linea(s) con idAccount=0"
    }
} else {
    Write-Fail 'Asiento COGS-FV no encontrado'
    Add-Content $OUTPUT_FILE '    [FAIL] COGS-FV no encontrado'
}

# ════════════════════════════════════════════════════════════════════════════
# Verificar asiento DEV-COGS-FV (reversion COGS 1 hot dog)
# ════════════════════════════════════════════════════════════════════════════
Write-Host ''
Write-Host '  Asiento DEV-COGS-FV — reversion COGS devolucion parcial (1 x 1500)' -ForegroundColor White
Add-Content $OUTPUT_FILE ''
Add-Content $OUTPUT_FILE '  Asiento DEV-COGS-FV'

$devCogsCount = 0; $devCogsDr = 0.0; $devCogsCr = 0.0
foreach ($e in $entries) {
    if ($e.numberEntry -like 'DEV-COGS-FV-*') {
        $devCogsCount++
        foreach ($l in @($e.lines)) {
            $devCogsDr += [double]($l.debitAmount  ?? 0)
            $devCogsCr += [double]($l.creditAmount ?? 0)
        }
    }
}
if ($devCogsCount -ge 1) {
    Write-Ok "Asiento DEV-COGS-FV existe ($devCogsCount encontrado(s))"
    Assert-FloatEqual '    DEV-COGS-FV DR total (inventario recuperado) = 1500' '1500' $devCogsDr
    Assert-FloatEqual '    DEV-COGS-FV CR total (reversa COGS) = 1500'          '1500' $devCogsCr
    Add-Content $OUTPUT_FILE "    DR=$devCogsDr  CR=$devCogsCr"
} else {
    Write-Fail 'Asiento DEV-COGS-FV no encontrado — la devolucion parcial no genero reversion de COGS'
    Add-Content $OUTPUT_FILE '    [FAIL] DEV-COGS-FV no encontrado'
}

# ════════════════════════════════════════════════════════════════════════════
# Verificar asiento DEV-ING-FV (reversion ingresos + IVA)
# ════════════════════════════════════════════════════════════════════════════
Write-Host ''
Write-Host '  Asiento DEV-ING-FV — reversion ingresos + IVA (1 hot dog devuelto)' -ForegroundColor White
Add-Content $OUTPUT_FILE ''
Add-Content $OUTPUT_FILE '  Asiento DEV-ING-FV'

$devIngCount = 0; $devIngDr117 = 0.0; $devIngDr127 = 0.0; $devIngCr106 = 0.0
foreach ($e in $entries) {
    if ($e.numberEntry -like 'DEV-ING-FV-*') {
        $devIngCount++
        foreach ($l in @($e.lines)) {
            if ($l.idAccount -eq 117) { $devIngDr117 += [double]($l.debitAmount  ?? 0) }
            if ($l.idAccount -eq 127) { $devIngDr127 += [double]($l.debitAmount  ?? 0) }
            if ($l.idAccount -eq 106) { $devIngCr106 += [double]($l.creditAmount ?? 0) }
        }
    }
}
if ($devIngCount -ge 1) {
    Write-Ok 'DEV-ING-FV encontrado'
    Assert-FloatEqual '    DR 117 Ingresos = 3000 (1 x 3000)'  '3000' $devIngDr117
    Assert-FloatEqual '    DR 127 IVA = 390 (3000 x 13%)'      '390'  $devIngDr127
    Assert-FloatEqual '    CR 106 Caja = 3390'                  '3390' $devIngCr106
    Add-Content $OUTPUT_FILE "    DR_117=$devIngDr117  DR_127=$devIngDr127  CR_106=$devIngCr106"
} else {
    Write-Fail 'DEV-ING-FV no encontrado — partial-return no genero reversion de ingresos'
    Add-Content $OUTPUT_FILE '    [FAIL] DEV-ING-FV no encontrado'
}

# ════════════════════════════════════════════════════════════════════════════
# Verificar asiento AJ-regalia (2 hot dogs sobre lote PT)
# ════════════════════════════════════════════════════════════════════════════
Write-Host ''
Write-Host '  Asiento AJ-regalia — 2 hot dogs regalados sobre lote PT (2 x 1500 = 3000)' -ForegroundColor White
Add-Content $OUTPUT_FILE ''
Add-Content $OUTPUT_FILE '  Asiento AJ-regalia'

$ajCount = 0; $ajDr130 = 0.0; $ajCr109 = 0.0
foreach ($e in $entries) {
    if ($e.numberEntry -like 'AJ-*') {
        $ajCount++
        foreach ($l in @($e.lines)) {
            if ($l.idAccount -eq 130) { $ajDr130 += [double]($l.debitAmount  ?? 0) }
            if ($l.idAccount -eq 109) { $ajCr109 += [double]($l.creditAmount ?? 0) }
        }
    }
}
if ($ajCount -ge 1) {
    Write-Ok "Asiento AJ encontrado ($ajCount)"
    Assert-FloatEqual '    AJ DR 130 (Merma Anormal — IAS 2.16, regalia 2 x 1500) = 3000' '3000' $ajDr130
    Assert-FloatEqual '    AJ CR 109 (Inventario) = 3000'                                  '3000' $ajCr109
    Add-Content $OUTPUT_FILE "    AJ_COUNT=$ajCount  DR_130=$ajDr130  CR_109=$ajCr109"
} else {
    Write-Fail 'Asiento AJ no encontrado — la regalia no genero asiento de merma'
    Add-Content $OUTPUT_FILE '    [FAIL] AJ no encontrado'
}

# ════════════════════════════════════════════════════════════════════════════
# Verificacion de partida doble
# ════════════════════════════════════════════════════════════════════════════
Write-Host ''
Write-Host '  Partida doble (DR = CR)' -ForegroundColor White
Assert-FloatEqual '    DR = CR = 116310' '116310' $GRAND_DR
Assert-FloatEqual '    DR = CR'           $GRAND_DR $GRAND_CR

Add-Content $OUTPUT_FILE ''
Add-Content $OUTPUT_FILE "  Partida doble: DR=$GRAND_DR  CR=$GRAND_CR"

# ════════════════════════════════════════════════════════════════════════════
# Nota contable (calculada desde asientos reales)
# ════════════════════════════════════════════════════════════════════════════
$saldo110 = [int](($script:REAL_DR['110'] ?? 0) - ($script:REAL_CR['110'] ?? 0))
$saldo115 = [int](($script:REAL_DR['115'] ?? 0) - ($script:REAL_CR['115'] ?? 0))
$saldo109 = ($script:REAL_DR['109'] ?? 0) - ($script:REAL_CR['109'] ?? 0)
$saldo124 = [int](($script:REAL_DR['124'] ?? 0) - ($script:REAL_CR['124'] ?? 0))
$saldo127 = [int](($script:REAL_CR['127'] ?? 0) - ($script:REAL_DR['127'] ?? 0))
$ivaNeto  = $saldo127 - $saldo124
$ivaSigno = $ivaNeto -lt 0 ? 'credito a favor del negocio' : 'IVA a pagar al gobierno'
$saldo109Abs = [Math]::Abs($saldo109)
$saldo109Sign = $saldo109 -ge 0 ? 'DR' : 'CR'

Write-Host ''
Write-Host '  Nota contable (desde asientos reales)' -ForegroundColor Yellow
Write-Host "  Cuenta 110 Mat. Primas:  saldo neto DR = $saldo110 (MP restante no consumida)" -ForegroundColor Yellow
Write-Host "  Cuenta 115 Costos Prod.: saldo neto = $saldo115 (IAS 2.12 — capitalizado integramente)" -ForegroundColor Yellow
Write-Host "  Cuenta 109 Inventario:   saldo neto $saldo109Sign $saldo109Abs" -ForegroundColor Yellow
Write-Host "  Cuenta 124 IVA Acred.:   saldo neto DR = $saldo124 (credito fiscal de compras)" -ForegroundColor Yellow
Write-Host "  Cuenta 127 IVA Pagar:    saldo neto CR = $saldo127" -ForegroundColor Yellow
Write-Host "  Posicion IVA:             $saldo127 - $saldo124 = $ivaNeto ($ivaSigno)" -ForegroundColor Yellow

$notaLines = @(
    ''
    '# -- NOTA CONTABLE (calculada desde asientos reales) ---------------'
    "#  - Cuenta 110 MP:              saldo neto DR = $saldo110 (MP restante no consumida)"
    "#  - Cuenta 115 Costos Prod.:    saldo neto = $saldo115 (IAS 2.12 — capitalizado a inv. PT)"
    "#  - Cuenta 109 Inventario:      saldo neto $saldo109Sign $saldo109Abs"
    "#  - Cuenta 124 IVA Acreditable: saldo neto DR = $saldo124 (credito fiscal)"
    "#  - Cuenta 127 IVA por Pagar:   saldo neto CR = $saldo127"
    "#  Posicion IVA: $saldo127 - $saldo124 = $ivaNeto ($ivaSigno)"
    ''
    '# -- RESUMEN --------------------------------------------------------'
    "  Verificaciones exitosas : $($script:CHECKS_OK)"
    "  Verificaciones fallidas : $($script:CHECKS_FAIL)"
)
$notaLines | Add-Content $OUTPUT_FILE

# ── Resumen final ─────────────────────────────────────────────────────────────
Write-Host ''
if ($script:CHECKS_FAIL -eq 0) {
    Write-Host '╔══════════════════════════════════════════════════════╗' -ForegroundColor Green
    Write-Host '║   ✅  TODOS LOS SALDOS CONTABLES CORRECTOS         ║' -ForegroundColor Green
    Write-Host '╚══════════════════════════════════════════════════════╝' -ForegroundColor Green
} else {
    Write-Host '╔══════════════════════════════════════════════════════╗' -ForegroundColor Red
    Write-Host '║   ❌  ALGUNOS SALDOS NO COINCIDEN                  ║' -ForegroundColor Red
    Write-Host '╚══════════════════════════════════════════════════════╝' -ForegroundColor Red
}
Write-Host "  Exitosas : $($script:CHECKS_OK)" -ForegroundColor Green
if ($script:CHECKS_FAIL -gt 0) {
    Write-Host "  Fallidas : $($script:CHECKS_FAIL)" -ForegroundColor Red
}
Write-Host ''
Write-Host "  💾  Reporte guardado en: $OUTPUT_FILE" -ForegroundColor DarkGray
Write-Host ''

exit ($script:CHECKS_FAIL -eq 0 ? 0 : 1)
