# Plan — Módulo Obligaciones Financieras (family-account)

Fecha: 2026-04-12 (rev. 7 — flujo end-to-end validado en BD limpia)

## Objetivo

Gestionar préstamos bancarios (COOPEALIANZA y otros) con tabla de amortización, sincronización automática desde Excel, conciliación contra movimientos BAC y reclasificación contable largo plazo → corto plazo.

El ciclo completo se dispara **subiendo el Excel del banco**: el sistema compara el estado del auxiliar contra la BD, detecta cuotas pagadas, busca el movimiento BAC correspondiente y genera los asientos del período sin intervención manual.

---

## Análisis del Excel (COOPEALIANZA-Tabla-Pagos-202603-CRC.xlsx)

| Columna | Campo BD | Notas |
|---|---|---|
| Número de transacción | `numberInstallment` | 1 … 36, clave natural de upsert |
| Fecha de vencimiento | `dueDate` | Fecha esperada del pago |
| Saldo | `balanceAfter` | Saldo luego del pago — base para reclasificación |
| Capital | `amountCapital` | Porción que amortiza el principal |
| Interés | `amountInterest` | Gasto financiero del período |
| Mora | `amountLateFee` | Penalidad por pago tardío |
| Otros | `amountOther` | Cargos adicionales |
| Total | `amountTotal` | Capital + Interés + Mora + Otros |
| Estado | `statusInstallment` | `Pendiente` \| `Vigente` \| `Pagada` \| `Vencida` |

36 cuotas (2024-03 → 2027). Estado actual: algunas `Pagada`, una `Vigente`, resto `Pendiente`.

---

## Análisis del XLS — BAC Financiamientos (Tasa Cero)

Los archivos `BAC-XXXX-Financiamientos.xls` exportados desde el portal BAC no contienen una tabla de amortización histórica. Presentan un **listado de planes activos** con el estado actual a la fecha de descarga. Cada fila = un plan de financiamiento Tasa cero activo en esa tarjeta.

### Estructura del archivo (7 columnas, datos desde fila 8)

| Col | Nombre en XLS | Campo BD / Derivado | Notas |
|---|---|---|---|
| B | Fecha | `startDate` | Fecha de compra/inicio del plan |
| C | Concepto | `nameObligation` / `matchKeyword` | Nombre del comercio o "TRASLADO SALDO REVOLUTIVO" |
| D | Cuotas | `currentInstallment` / `termMonths` | Formato `"009/012"` → pagada=9, total=12 |
| E | Monto de cuota | `amountCapital` (= `amountTotal`) | Incluye moneda al final: `"29,472.00 CRC"` o `"9.20 USD"` |
| F | Saldo inicial | `originalAmount` | Monto total del financiamiento |
| G | Saldo faltante | — | Verifica: `remainingInstallments × montoCuota ≈ saldoFaltante` |

> Las filas con `Concepto = "Total"` son filas de subtotales por moneda — se ignoran en el parser.

### Datos reales (extracto 08/04/2026)

**Tarjeta 5466-37\*\*-\*\*\*\*-8608**

| Fecha | Concepto | Cuotas | Monto cuota | Saldo inicial | Saldo faltante |
|---|---|---|---|---|---|
| 15/07/2025 | AUTOPITS DESAMPARADOS | 009/012 | ₡29,472.00 | ₡353,664.79 | ₡88,416.79 |
| 09/02/2026 | VERDUGO 406 TC 24M | 003/024 | ₡4,995.80 | ₡119,899.99 | ₡104,912.59 |
| 14/02/2026 | IMPORT.MONGE EXPRESSO DES TC24 | 002/024 | ₡2,495.80 | ₡59,900.00 | ₡54,908.40 |
| 16/03/2026 | CACHOS MULTICENTRO DESAMPARADO | 001/003 | ₡16,300.00 | ₡48,900.00 | ₡32,600.00 |
| 17/06/2025 | ICON CC RETAIL CT | 010/018 | $9.20 | $167.35 | $75.35 |

**Tarjeta 5491-94\*\*-\*\*\*\*-6515**

| Fecha | Concepto | Cuotas | Monto cuota | Saldo inicial | Saldo faltante |
|---|---|---|---|---|---|
| 21/11/2022 | TRASLADO SALDO REVOLUTIVO | 040/060 | ₡17,875.80 | ₡409,883.04 | ₡275,966.36 |
| 09/08/2023 | TRASLADO SALDO REVOLUTIVO | 031/060 | ₡72,206.60 | ₡2,096,154.99 | ₡1,464,278.23 |
| 16/02/2026 | CLINICA ITA | 002/003 | ₡56,666.60 | ₡170,000.00 | ₡56,666.80 |

### Reconstrucción de la tabla de amortización

Como el XLS solo entrega el estado actual (no el histórico cuota a cuota), la tabla se reconstruye programáticamente:

```
cuotaActual = int("009")   # de "009/012"
termMonths  = int("012")

para n = 1..termMonths:
  dueDate[n]      = startDate.AddMonths(n)
  amountCapital   = montoCuota       # Tasa cero → capital = cuota completa
  amountInterest  = 0
  amountTotal     = montoCuota
  balanceAfter[n] = max(0, originalAmount - n × montoCuota)
  status[n] = 'Pagada'    si n < cuotaActual
            | 'Vigente'   si n == cuotaActual
            | 'Pendiente' si n > cuotaActual
```

> **Nota:** `saldoFaltante / montoCuota ≈ (termMonths - cuotaActual)` — usar esta relación para validar coherencia.

### Diferencias clave vs COOPEALIANZA

| Aspecto | COOPEALIANZA (Tipo A) | BAC Tasa Cero (Tipo B) |
|---|---|---|
| Formato fuente | XLSX — tabla completa cuota a cuota | XLS — resumen por plan activo |
| Interés | 18.5% anual | **0%** (Tasa cero) |
| Mora | Sí (columna Excel) | No (Tasa cero) |
| Columnas de montos | Capital / Interés / Mora / Otros / Total | Solo monto de cuota uniforme |
| Tabla generada | Usa datos reales del Excel | **Se reconstruye** desde el resumen |
| Moneda | CRC | CRC y/o USD (mixto en misma tarjeta) |
| Pasivo | Largo plazo (2.2) | Corriente o largo plazo según plazo restante |
| Matching en extracto | Keyword `COOPEALIANZA` en cuenta débito | Keyword en extracto tarjeta de crédito |

### Keywords en extracto TC — ajuste pendiente (01a-keywords.md)

Las keywords `TRASLADO SALDO REVOLUTIVO` y `CUOTA:` en Templates 4 y 5 actualmente apuntan a cuenta **42** (Coopealianza). Deben ajustarse a las cuentas específicas de cada tarjeta una vez que se creen las cuentas contables de Tipo B.

---

## Cuentas contables involucradas

> ✅ Todas las cuentas están creadas en BD (migraciones `AddFinancialObligations` + `AddFinancialObligationAccounts`).

| Rol en la obligación | Cuenta | IdAccount | Código |
|---|---|---|---|
| Pasivo largo plazo (principal) | Coopealianza Préstamo CR05081302810003488995 (₡) | **42** | 2.2.01.01 |
| Pasivo corriente (porción corriente) | Coopealianza - Porción Corriente CR05... (₡) | **134** | 2.1.02.01 |
| Intereses por Pagar devengados (agrupadora) | Intereses por Pagar | **135** | 2.1.05 |
| Intereses por Pagar devengados (detalle) | Intereses por Pagar - Coopealianza (₡) | **136** | 2.1.05.01 |
| Gasto intereses | Intereses Coopealianza | **137** | 5.5.05 |
| Gasto mora | Mora Coopealianza | **138** | 5.5.06 |
| Banco de pago BAC (CR) | BAC - Cta. CR73010200009497305680 - Baltodano Cubillo Ezequiel | **27** | 1.1.02.01 |

> La configuración exacta de cuentas se almacena en `FinancialObligation` (FK por cuenta), no hardcodeada.

### IDs a usar al crear la obligación COOPEALIANZA (Tipo A)

| Campo `FinancialObligation` | IdAccount |
|---|---|
| `IdAccountLongTerm` | **42** |
| `IdAccountShortTerm` | **134** |
| `IdAccountInterest` | **137** |
| `IdAccountLateFee` | **138** |
| `IdAccountOther` | null (no aplica) |
| `IdBankAccountPayment` | id de la cuenta BAC CR73 en `bankAccount` |

### IDs a usar al crear obligaciones BAC Tasa Cero (Tipo B)

> ⚠️ Las cuentas contables específicas por tarjeta **aún no existen** en BD. Requieren una migración `AddBacCreditCardObligationAccounts` antes de poder crear estas obligaciones vía API.

| Campo `FinancialObligation` | Tarjeta 8608 (CRC) | Tarjeta 8608 (USD) | Tarjeta 6515 (CRC) | Notas |
|---|---|---|---|---|
| `IdAccountLongTerm` | ⏳ nueva cuenta 2.1.01.01 | ⏳ nueva cuenta 2.1.01.02 | ⏳ nueva cuenta 2.1.01.03 | Pasivo TC Tasa Cero |
| `IdAccountShortTerm` | ⏳ misma o cuenta porción corriente | ⏳ idem | ⏳ idem | Si plazo > 12m, separar |
| `IdAccountInterest` | N/A — usar 28 como placeholder | idem | idem | Tasa cero → no genera asiento |
| `IdAccountLateFee` | null | null | null | Tasa cero sin mora |
| `IdBankAccountPayment` | idBankAccount de la TC en `bankAccount` | idem | idem | Pago dentro del extracto TC |
| `InterestRate` | **0.01** \* | 0.01 \* | 0.01 \* | \*Workaround — validación API min=0.01 |

> **Pendiente backend:** Relajar validación `InterestRate` de `min=0.01` a `min=0` en `CreateFinancialObligationRequest` para soportar Tasa cero correctamente.

---

## Modelo de dominio

```
FinancialObligation (cabecera del préstamo)
  ├── IdAccountLongTerm  → 2.2.01.01
  ├── IdAccountShortTerm → 2.1.02.01
  ├── IdAccountInterest  → 5.5.05
  ├── IdAccountLateFee   → 5.5.06
  ├── IdBankAccountPayment → bankAccount BAC débito
  ├── FinancialObligationInstallment[] (tabla de amortización — 1 por cuota)
  │     ├── SyncedAt                        (última sincronización desde Excel)
  │     ├── FinancialObligationPayment?     (datos del pago — 0 ó 1 por cuota)
  │     ├── FinancialObligationBankMovement[]   ← movimientos bancarios vinculados (1:N)
  │     └── FinancialObligationAccountingEntry[] ← asientos de pago (1:N, typeEntry=Pago)
  ├── FinancialObligationBankMovement[]     (nivel obligación, sin cuota — reservado)
  └── FinancialObligationAccountingEntry[]  (reclasificación, ajuste saldo inicial — 1:N)
```

> Las relaciones con `bankMovement` y `accountingEntry` son siempre **1:N** a través de las tablas de relación propias. Un pago puede tener 0 ó 1 movimientos bancarios vinculados; una obligación puede acumular N asientos contables de distintos tipos a lo largo de su vida.

---

## Entidades

### 1. `FinancialObligation`

| Propiedad | Tipo | Notas |
|---|---|---|
| `IdFinancialObligation` | `int` | PK autoincremental |
| `NameObligation` | `string` | Ej. "Préstamo COOPEALIANZA CRC" |
| `IdCurrency` | `int` | FK → `currency` |
| `OriginalAmount` | `decimal` | Monto original del préstamo |
| `InterestRate` | `decimal` | Tasa anual (ej. 18.50) |
| `StartDate` | `DateOnly` | Fecha de primer desembolso |
| `TermMonths` | `int` | Plazo total en meses |
| `IdBankAccountPayment` | `int?` | FK → `bankAccount` (BAC débito) |
| `IdAccountLongTerm` | `int` | FK → `account` (Pasivo no corriente) |
| `IdAccountShortTerm` | `int` | FK → `account` (Pasivo corriente — porción) |
| `IdAccountInterest` | `int` | FK → `account` (Gasto intereses) |
| `IdAccountLateFee` | `int?` | FK → `account` (Gasto mora, opcional) |
| `IdAccountOther` | `int?` | FK → `account` (Gasto otros, opcional) |
| `StatusObligation` | `string` | CHECK: `Activo` \| `Liquidado` |
| `Notes` | `string?` | Observaciones |

### 2. `FinancialObligationInstallment`

| Propiedad | Tipo | Notas |
|---|---|---|
| `IdFinancialObligationInstallment` | `int` | PK |
| `IdFinancialObligation` | `int` | FK |
| `NumberInstallment` | `int` | Clave de upsert del Excel |
| `DueDate` | `DateOnly` | Fecha de vencimiento |
| `BalanceAfter` | `decimal` | Saldo luego del pago |
| `AmountCapital` | `decimal` | Capital |
| `AmountInterest` | `decimal` | Interés |
| `AmountLateFee` | `decimal` | Mora (default 0) |
| `AmountOther` | `decimal` | Otros (default 0) |
| `AmountTotal` | `decimal` | Capital + Interés + Mora + Otros |
| `StatusInstallment` | `string` | CHECK: `Pendiente` \| `Vigente` \| `Pagada` \| `Vencida` |
| `SyncedAt` | `DateTime?` | Última vez que el Excel actualizó esta fila |

Índice único: `UQ_financialObligationInstallment_idObligation_number`

### 3. `FinancialObligationPayment`

Registro del pago a nivel de cuota. Existe si la cuota fue pagada (aunque no tenga movimiento bancario vinculado). Ya **no** contiene FKs directos a `bankMovement` ni a `accountingEntry` — esas relaciones se gestionan a través de las tablas 1:N propias.

| Propiedad | Tipo | Notas |
|---|---|---|
| `IdFinancialObligationPayment` | `int` | PK |
| `IdFinancialObligationInstallment` | `int` | FK único (1:1 con cuota) |
| `DatePayment` | `DateOnly` | Fecha real del pago |
| `AmountPaid` | `decimal` | Total efectivamente pagado |
| `AmountCapitalPaid` | `decimal` | Capital (tomado del Excel) |
| `AmountInterestPaid` | `decimal` | Interés (tomado del Excel) |
| `AmountLatePaid` | `decimal` | Mora (tomado del Excel) |
| `AmountOtherPaid` | `decimal` | Otros (tomado del Excel) |
| `IsAutoProcessed` | `bool` | true = generado automáticamente por el sync |
| `Notes` | `string?` | |

### 4. `FinancialObligationBankMovement` *(nueva)*

Tabla de relación 1:N entre la obligación (o una cuota específica) y los movimientos bancarios. Permite vincular múltiples movimientos a una cuota o a la obligación en general.

| Propiedad | Tipo | Notas |
|---|---|---|
| `IdFinancialObligationBankMovement` | `int` | PK |
| `IdFinancialObligation` | `int` | FK → `financialObligation` |
| `IdFinancialObligationInstallment` | `int?` | FK nullable → cuota específica (`null` = nivel obligación) |
| `IdBankMovement` | `int` | FK → `bankMovement` |
| `TypeMovement` | `string` | CHECK: `Pago` (extensible) |
| `Notes` | `string?` | |

Índice único: `UQ_finObligBankMovement_obligation_bankMovement`

### 5. `FinancialObligationAccountingEntry` *(nueva)*

Tabla de relación 1:N entre la obligación (o una cuota específica) y los asientos contables. Centraliza todos los asientos generados por la obligación: pagos, reclasificaciones y ajustes de saldo inicial.

| Propiedad | Tipo | Notas |
|---|---|---|
| `IdFinancialObligationAccountingEntry` | `int` | PK |
| `IdFinancialObligation` | `int` | FK → `financialObligation` |
| `IdFinancialObligationInstallment` | `int?` | FK nullable → cuota específica (`null` = nivel obligación: RCLS, AjusteSaldo) |
| `IdAccountingEntry` | `int` | FK → `accountingEntry` |
| `TypeEntry` | `string` | CHECK: `Pago` \| `Reclasificacion` \| `AjusteSaldoInicial` |
| `Notes` | `string?` | |

Índice único: `UQ_finObligAccountingEntry_obligation_entry`

---

## Endpoint de importación y flujo automático

### `POST /financial-obligations/{id}/sync-excel`

Recibe el archivo `.xlsx` (multipart) y opcionalmente un `idBankMovement` que el usuario puede proveer como evidencia del pago del período. Ejecuta el siguiente algoritmo:

```
1. DETECTAR PERIODO FISCAL
   · Leer dueDate de la cuota con status 'Vigente' (o la de menor dueDate futura)
   · Buscar FiscalPeriod donde StartDate ≤ dueDate ≤ EndDate
   · Usar ese idFiscalPeriod para todos los asientos generados en este sync

2. PARSEAR el Excel → lista de InstallmentRow { number, dueDate, balanceAfter,
                        capital, interest, lateFee, other, total, status }

3. VERIFICAR AJUSTE DE SALDO INICIAL
   · Comparar OriginalAmount almacenado vs saldo reconstruido desde la primera cuota del Excel
   · Si difieren Y no existe ya un registro en FinancialObligationAccountingEntry (type=AjusteSaldoInicial):
       → GENERAR asiento de ajuste (Borrador, idFiscalPeriod del paso 1)
       → INSERTAR en FinancialObligationAccountingEntry
           { idFinancialObligation, idFinancialObligationInstallment=null, typeEntry='AjusteSaldoInicial' }

4. Para cada InstallmentRow:
   a. UPSERT en financialObligationInstallment
      - Si existe (por numberInstallment + idFinancialObligation):
          · Actualizar todos los montos, dueDate, balanceAfter, status, SyncedAt
      - Si no existe:
          · Insertar fila nueva, status = valor del Excel

   b. Si status == 'Pagada' Y no existe FinancialObligationPayment para esta cuota:
      → CREAR FinancialObligationPayment (montos del Excel, IsAutoProcessed=true)

      → BUSCAR movimiento BAC:
           - Si el usuario proveyó idBankMovement y corresponde a esta cuota (monto ≈ amountTotal):
               usar ese idBankMovement directamente
           - Si no: buscar automáticamente en bankMovement (ver §Matching)
      → Si movimiento encontrado:
           INSERTAR en FinancialObligationBankMovement
               { idFinancialObligation, idFinancialObligationInstallment, idBankMovement, typeMovement='Pago' }
      → Si no encontrado: paymentsSkipped++

      → GENERAR asiento contable de pago (Borrador, idFiscalPeriod del paso 1)
      → INSERTAR en FinancialObligationAccountingEntry
           { idFinancialObligation, idFinancialObligationInstallment, idAccountingEntry, typeEntry='Pago' }

5. RECLASIFICACIÓN AUTOMÁTICA del período:
   · Calcular porción corriente = suma de AmountCapital de cuotas con
     DueDate entre hoy y hoy+12m y status IN ('Pendiente','Vigente')
   · Si porción cambió respecto al último asiento de reclasificación:
       → GENERAR asiento de reclasificación (Borrador, idFiscalPeriod del paso 1)
       → INSERTAR en FinancialObligationAccountingEntry
           { idFinancialObligation, idFinancialObligationInstallment=null, idAccountingEntry, typeEntry='Reclasificacion' }

6. RETORNAR SyncResult {
     fiscalPeriodId, fiscalPeriodName,
     installmentsUpserted, paymentsCreated, paymentsSkipped,
     initialBalanceAdjustmentEntryId (id o null),
     reclassificationEntryId (id o null),
     warnings[]
   }
```

### Matching automático de movimiento BAC

Criterio de búsqueda en `bankMovement` / `bankStatementTransaction`:

```
· IdBankAccount = FinancialObligation.IdBankAccountPayment
· Amount ≈ installment.AmountTotal  (±1% tolerancia por redondeo)
· DateMovement entre dueDate-10 días y dueDate+10 días
· DescriptionMovement ILIKE '%COOPEALIANZA%' (o keyword configurable en FinancialObligation)
· No vinculado a otro FinancialObligationPayment
```

Si se encuentran 0 candidatos → cuota queda sin pago registrado (`paymentsSkipped`).  
Si se encuentran 2+ candidatos → se elige el de fecha más cercana a `dueDate`; si empatan → warning al usuario.  
Si se encuentra 1 candidato → vinculación automática.

---

## Flujo contable

### A) Pago de cuota (generado por sync-excel o manualmente)

```
DR  Pasivo Corriente    (IdAccountShortTerm)    AmountCapitalPaid
DR  Gasto Intereses     (IdAccountInterest)     AmountInterestPaid
DR  Gasto Mora          (IdAccountLateFee)      AmountLatePaid       [si > 0]
DR  Gasto Otros         (IdAccountOther)        AmountOtherPaid      [si > 0]
  CR  Banco BAC         (BankAccount.IdAccount) AmountPaid
```

Estado del asiento: **Borrador** (el usuario confirma manualmente).

### B) Reclasificación Largo Plazo → Corto Plazo

Calculado automáticamente en cada sync. También disponible en endpoint manual.

```
DR  Pasivo No Corriente (IdAccountLongTerm)     porciónCorriente
  CR  Pasivo Corriente  (IdAccountShortTerm)    porciónCorriente
```

`porciónCorriente` = BalanceAfter de la cuota actual − BalanceAfter de la cuota +12 meses  
(dato disponible directamente en el Excel, sin cálculo adicional).

---

## Nuevas cuentas creadas en las migraciones

> ✅ Aplicadas en migración `20260412202904_AddFinancialObligationAccounts`.

| IdAccount | Código | Nombre | Tipo | Padre |
|---|---|---|---|---|
| **134** | 2.1.02.01 | Coopealianza - Porción Corriente CR05081302810003488995 (₡) | Pasivo | 40 (2.1.02) |
| **135** | 2.1.05 | Intereses por Pagar *(agrupadora)* | Pasivo Corriente | 9 (2.1) |
| **136** | 2.1.05.01 | Intereses por Pagar - Coopealianza (₡) | Pasivo Corriente | 135 (2.1.05) |
| **137** | 5.5.05 | Intereses Coopealianza | Gasto | 92 (5.5) |
| **138** | 5.5.06 | Mora Coopealianza | Gasto | 92 (5.5) |

---

## Estructura de archivos

```
Domain/
  Entities/
    FinancialObligation.cs
    FinancialObligationInstallment.cs
    FinancialObligationPayment.cs
    FinancialObligationBankMovement.cs      ← NUEVA (relación 1:N con bankMovement)
    FinancialObligationAccountingEntry.cs   ← NUEVA (relación 1:N con accountingEntry)

Infrastructure/
  Data/
    Configuration/
      FinancialObligationConfiguration.cs
      FinancialObligationInstallmentConfiguration.cs
      FinancialObligationPaymentConfiguration.cs
      FinancialObligationBankMovementConfiguration.cs      ← NUEVA
      FinancialObligationAccountingEntryConfiguration.cs   ← NUEVA
    AppDbContext.cs                                        ← 5 DbSets

Features/
  FinancialObligations/
    Dtos/
      FinancialObligationResponse.cs
      FinancialObligationSummaryResponse.cs
      FinancialObligationAuxiliaryResponse.cs    ← NUEVA (tabla auxiliar enriquecida)
      FinancialObligationInstallmentResponse.cs
      FinancialObligationPaymentResponse.cs
      CreateFinancialObligationRequest.cs
      UpdateFinancialObligationRequest.cs
      SyncExcelResult.cs                         ← incluye fiscalPeriodId/Name + initialBalanceAdjustmentEntryId
      RegisterPaymentRequest.cs
    Parsers/
      FinancialObligationExcelParser.cs
    IFinancialObligationService.cs
    FinancialObligationService.cs               ← lógica sync + periodo fiscal + ajuste saldo + 1:N
    FinancialObligationsModule.cs
```

---

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| `GET` | `/financial-obligations` | Lista todas las obligaciones |
| `POST` | `/financial-obligations` | Crea una obligación nueva |
| `GET` | `/financial-obligations/{id}` | Detalle + cuotas |
| `PUT` | `/financial-obligations/{id}` | Actualiza datos |
| `DELETE` | `/financial-obligations/{id}` | Elimina si sin pagos |
| `GET` | `/financial-obligations/{id}/summary` | Saldo, próxima cuota, porción corriente |
| **`GET`** | **`/financial-obligations/{id}/auxiliary.json`** | **Tabla auxiliar enriquecida: cuotas + pago + movimiento BAC + asiento** |
| **`POST`** | **`/financial-obligations/{id}/sync-excel`** | **Import Excel → upsert cuotas → pagos → asientos** |
| `PUT` | `/financial-obligations/{id}/installments/{iid}` | Edita cuota manualmente |
| `POST` | `/financial-obligations/{id}/installments/{iid}/payment` | Registra pago manual (sin Excel) |
| `POST` | `/financial-obligations/{id}/reclassify` | Reclasificación manual de período |

---

## Vista Auxiliar — Tabla de Amortización Enriquecida

El endpoint `GET /financial-obligations/{id}/auxiliary.json` devuelve la tabla de cuotas enriquecida con datos de pago y conciliación bancaria. Es el insumo principal del frontend para el auxiliar contable del préstamo.

### Estructura del response por cuota (`AuxiliaryInstallmentRow`)

| Campo | Origen | Descripción |
|---|---|---|
| `numberInstallment` | `financialObligationInstallment` | Número de cuota |
| `dueDate` | idem | Fecha de vencimiento |
| `balanceAfter` | idem | Saldo luego del pago |
| `amountCapital` | idem | Porción capital |
| `amountInterest` | idem | Intereses |
| `amountLateFee` | idem | Mora |
| `amountOther` | idem | Otros |
| `amountTotal` | idem | Total cuota |
| `statusInstallment` | idem | `Pendiente` \| `Vigente` \| `Pagada` \| `Vencida` |
| `syncedAt` | idem | Última actualización desde Excel |
| `payment.datePayment` | `financialObligationPayment` | Fecha real del pago (null si no pagada) |
| `payment.amountPaid` | idem | Monto efectivamente pagado |
| `payment.isAutoProcessed` | idem | Fue detectado automáticamente por el sync |
| `payment.diffAmount` | calculado | `amountTotal - amountPaid` — detecta redondeos o diferencias |
| `bankMovement.numberMovement` | `bankMovement` | Número del movimiento BAC vinculado |
| `bankMovement.dateMovement` | idem | Fecha del débito en la cuenta |
| `bankMovement.descriptionMovement` | idem | Descripción del extracto |
| `bankMovement.amount` | idem | Monto del débito BAC |
| `bankMovement.statusMovement` | idem | Estado del movimiento |
| `accountingEntry.idAccountingEntry` | `accountingEntry` | ID del asiento generado |
| `accountingEntry.numberEntry` | idem | Número contable (ej. `PAG-0001-202603`) |
| `accountingEntry.statusEntry` | idem | `Borrador` \| `Publicado` \| `Anulado` |
| `accountingEntry.dateEntry` | idem | Fecha del asiento |

### Valor del auxiliar

- **Detectar cuotas sin conciliación**: `statusInstallment = Pagada` pero `bankMovement = null` → requiere vinculación manual.
- **Detectar diferencias de monto**: `diffAmount ≠ 0` → posible mora o redondeo no capturado.
- **Auditar asientos**: ver si el asiento ya fue Publicado o sigue en Borrador sin confirmar.
- **Trazabilidad completa**: desde la cuota del Excel hasta el débito en la cuenta bancaria y el asiento contable.
- **Navegación cruzada**: el `numberMovement` y `numberEntry` son links directos a los módulos de movimientos y contabilidad.

### DTO propuesto (`FinancialObligationAuxiliaryResponse`)

```csharp
public record FinancialObligationAuxiliaryResponse(
    int IdFinancialObligation,
    string NameObligation,
    decimal OriginalAmount,
    decimal CurrentBalance,
    string StatusObligation,
    IReadOnlyList<AuxiliaryInstallmentRow> Installments,
    // Asientos a nivel obligación (no vinculados a una cuota específica)
    IReadOnlyList<AuxiliaryEntryInfo> ObligationEntries
);

public record AuxiliaryInstallmentRow(
    int NumberInstallment,
    DateOnly DueDate,
    decimal BalanceAfter,
    decimal AmountCapital,
    decimal AmountInterest,
    decimal AmountLateFee,
    decimal AmountOther,
    decimal AmountTotal,
    string StatusInstallment,
    DateTime? SyncedAt,
    AuxiliaryPaymentInfo? Payment,
    // 1:N — normalmente 0 ó 1 elemento, pero el modelo soporta N
    IReadOnlyList<AuxiliaryBankMovementInfo> BankMovements,
    IReadOnlyList<AuxiliaryEntryInfo> AccountingEntries
);

public record AuxiliaryPaymentInfo(
    DateOnly DatePayment,
    decimal AmountPaid,
    decimal DiffAmount,        // AmountTotal - AmountPaid
    bool IsAutoProcessed
);

public record AuxiliaryBankMovementInfo(
    int IdFinancialObligationBankMovement,
    int IdBankMovement,
    string NumberMovement,
    DateOnly DateMovement,
    string DescriptionMovement,
    decimal Amount,
    string TypeMovement           // "Pago"
);

public record AuxiliaryEntryInfo(
    int IdFinancialObligationAccountingEntry,
    int IdAccountingEntry,
    string NumberEntry,
    string StatusEntry,
    DateOnly DateEntry,
    string TypeEntry              // "Pago" | "Reclasificacion" | "AjusteSaldoInicial"
);
```

### Vista frontend (columnas del ngx-datatable)

| Columna | Contenido | Indicador visual |
|---|---|---|
| `#` | `numberInstallment` | — |
| Vencimiento | `dueDate` | Rojo si vencida y sin pago |
| Capital | `amountCapital` | — |
| Interés | `amountInterest` | Gris si = 0 (tasa cero) |
| Mora | `amountLateFee` | Amarillo si > 0 |
| Total | `amountTotal` | **Bold** si es cuota vigente |
| Estado | `statusInstallment` | Badge: verde=Pagada, azul=Vigente, gris=Pendiente, rojo=Vencida |
| Fecha pago | `payment.datePayment` | `—` si null |
| Movimiento BAC | `bankMovements[0].numberMovement` | Link clickeable \| ⚠ sin vincular si Pagada y vacío |
| Asiento pago | `accountingEntries[type=Pago].numberEntry` | Badge: gris=Borrador, verde=Publicado \| `—` si null |
| Diff | `payment.diffAmount` | Amarillo si ≠ 0 |

**Sección inferior de la página** — Asientos a nivel obligación (`obligationEntries`):

| Tipo | Descripción | Estado |
|---|---|---|
| `Reclasificacion` | Largo plazo → Corto plazo del período | Badge Borrador/Publicado |
| `AjusteSaldoInicial` | Diferencia entre saldo configurado y saldo real del Excel | Badge Borrador/Publicado |

> El row-detail expandible de cada cuota muestra el desglose completo: descripción del movimiento BAC, líneas del asiento de pago y notas. Si hay más de un movimiento o asiento vinculado (caso excepcional), se listan todos.

---

## Fases de implementación

| # | Fase | Contenido | Estado |
|---|---|---|---|
| 1 | **Entidades + Config + Migración** | 3 entidades originales + **2 nuevas** (`FinancialObligationBankMovement`, `FinancialObligationAccountingEntry`) + configs Fluent API | ⚠ Requiere nueva migración para las 2 tablas 1:N |
| 2 | **Parser Excel + servicio sync** | `FinancialObligationExcelParser`, algoritmo upsert, detección periodo fiscal, ajuste saldo inicial, matching BAC, generación asientos con inserción en tablas 1:N | ⚠ Requiere actualización (tablas 1:N aún no implementadas) |
| 3 | **DTOs + Module + endpoints** | 7 DTOs + nuevo `FinancialObligationAuxiliaryResponse`, endpoint `sync-excel` con `idBankMovement` opcional | ⚠ Requiere actualización |
| 4 | **Reclasificación automática** | Cálculo porción corriente desde `BalanceAfter`, integrado en sync y endpoint manual | ✅ Completado |
| 5 | **Cuentas contables completas** | 5 cuentas nuevas: 134, 135, 136, 137, 138 — migración `AddFinancialObligationAccounts` | ✅ Completado |
| 6 | **Frontend Angular — Lista + Summary** | Página `/obligaciones`: tabla con nombre, moneda, monto original, estado, próxima cuota y badge de conciliación pendiente. Header de detalle con `summary` (saldo vigente, cuotas pagadas/pendientes, porción corriente). Botón „Subir Excel" dispara `sync-excel`. | ⏳ Pendiente |
| 6b | **Frontend Angular — Tabla Auxiliar** | Sub-componente `FinancialObligationAuxiliaryComponent`: consume `/auxiliary.json`, columnas enriquecidas (estado, movimiento BAC, asiento), row-detail expandible, badges de alerta para cuotas sin conciliación o asientos en Borrador, link directo a módulo bancario y contable. | ⏳ Pendiente |
| 7 | **Tipo B — BAC Tasa Cero** | `FinancialObligationBacFinanciamientosParser`, nuevo endpoint `sync-bac-financiamientos`, migración cuentas TC, relajar `InterestRate ≥ 0`, ajustar keywords 01a | ⏳ Pendiente |

### Última ejecución validada (2026-04-12 — BD `20260412212705_InitialCreate`)

| Paso | Resultado |
|---|---|
| Obligación creada (BD nueva) | id=1 ✅ |
| 36 cuotas upserted | ✅ |
| paymentsSkipped=16 | Esperado — sin extractos BAC |
| RCLS-0001-202604 generado | id=1, PreviousShortTermPortion=0 → New=₡3,059,168.65 ✅ |
| Paso 12 — Borrador → Publicado | `statusEntry = Publicado` ✅ (vía `syncResp.reclassificationEntryId`) |

### Migración activa (tras re-create)

> ✅ BD regenerada el 2026-04-12 con una sola migración limpia (incluye fix bug duplicados).

| Migración | Descripción |
|---|---|
| `20260412212705_InitialCreate` | Única migración vigente — incluye todas las tablas, seed de cuentas (42, 134, 135, 136, 137, 138) y 3 tablas de obligaciones financieras |

---

## Dependencias con módulos existentes

| Módulo | Relación |
|---|---|
| `BankMovement` / `BankStatementTransaction` | Matching por monto + fecha + descripción |
| `AccountingEntry` | Asientos generados en Borrador |
| `Account` | FKs configurables por obligación |
| `Currency` | FK en cabecera |
| `BankAccount` | FK cuenta de pago BAC |
| `FiscalPeriod` | FK para los asientos generados |

---

## Decisiones tomadas

| Decisión | Resolución |
|---|---|
| ¿Pago parcial? | No en v1. Pago = total de la cuota según Excel |
| ¿Pago siempre requiere movimiento bancario? | **No** — el registro de pago se crea aunque no haya movimiento BAC vinculado (`paymentsSkipped`). El usuario puede vincular manualmente después |
| ¿Asiento en Borrador o Confirmado? | **Borrador** — usuario revisa y confirma |
| ¿Mora y otros desde Excel o manual? | **Desde Excel** — ya vienen en las columnas |
| ¿Matching automático de banco? | **Sí** — por monto ±1%, fecha ±10 días, keyword. El usuario también puede proveer `idBankMovement` directamente en el request |
| ¿Reclasificación automática en sync? | **Sí** — incluida en el algoritmo del sync |
| ¿Cuentas configurables? | **Sí** — FK en `FinancialObligation`, no hardcoded |
| ¿Relación con bankMovement y accountingEntry? | **1:N** a través de `FinancialObligationBankMovement` y `FinancialObligationAccountingEntry`. No hay FKs directos en `Payment` |
| ¿Ajuste de saldo inicial? | Si el saldo configurado difiere del Excel, el sync genera un asiento `AjusteSaldoInicial` sin movimiento bancario, vinculado a nivel de obligación |
| ¿Periodo fiscal? | Se detecta automáticamente desde `dueDate` de la cuota Vigente. Se asigna a todos los asientos generados en el sync |

---

## Prueba de integración — Resultados (2026-04-12)

Ejecutado con `COOPEAL-sync-test.ps1` sobre BD nueva (`InitialCreate`).

### Resultados del sync

| Métrica | Valor | Observación |
|---|---|---|
| `installmentsUpserted` | 36 | Todas las cuotas del Excel cargadas |
| `paymentsCreated` | 0 | Sin movimientos BAC en BD — esperado |
| `paymentsSkipped` | 16 | 16 cuotas `Pagada` sin movimiento BAC vinculable |
| `reclassificationEntryId` | 1 | Asiento RCLS-0001-202604 generado en Borrador |
| `newShortTermPortion` | ₡3,059,168.65 | ✅ Correcto tras bug fix (cuotas 17-28) |

### Asiento de reclasificación generado

```
RCLS-0001-202604  |  Publicado  |  2026-04-12
DR  2.2.01.01  Coopealianza Préstamo (Pasivo No Corriente)  ₡3,059,168.65
  CR  2.1.02.01  Porción Corriente Coopealianza              ₡3,059,168.65
```

> ✅ Asiento confirmado por paso 12 del PS1 (`Borrador → Publicado`).

### Observaciones y pendientes

#### ✅ Obs #1 — Bug duplicados en colección de cuotas — CORREGIDO Y VERIFICADO

`db.FinancialObligationInstallment.Add(existing)` activaba el relationship fixup de EF Core que ya agrega automáticamente la entidad a `obligation.FinancialObligationInstallments`. La línea manual `obligation.FinancialObligationInstallments.Add(existing)` la insertaba por segunda vez, generando 72 entradas (36 × 2) y sumando el doble en `CalculateShortTermPortion`.

**Fix aplicado en `FinancialObligationService.cs`**: eliminada la línea `obligation.FinancialObligationInstallments.Add(existing)` del bloque de inserción nueva.

**Verificado en 2.ª ejecución post-fix**: `newShortTermPortion = ₡3,059,168.65` == `portionCurrentYear = ₡3,059,168.65` ✅

#### ℹ️ Obs #2 — `PaymentsSkipped = 16` (esperado en BD nueva)

Las 16 cuotas marcadas `Pagada` en el Excel no pueden vincularse a movimientos BAC porque la BD está vacía de extractos. Una vez que se carguen los extractos BAC de los meses anteriores (a través del módulo de carga bancaria), el próximo `sync-excel` los vinculará automáticamente por monto ±1%, fecha ±10 días y keyword `COOPEALIANZA`.

#### ℹ️ Obs #3 — `MatchKeyword` case-sensitive vs ILIKE

El servicio usa `DescriptionMovement.ToUpper().Contains(keyword)` en memoria. Pendiente validar que el keyword almacenado (`"COOPEALIANZA"`) coincida con el texto real de los movimientos BAC al momento de la carga.

#### ✅ Obs #4 — Script PS1 con 12 pasos — COMPLETAMENTE VALIDADO

`COOPEAL-sync-test.ps1` cubre el flujo completo:
- Pasos 1-5: validaciones previas (tablas BD, cuentas, login, obligación)
- Paso 6: `sync-excel` — upsert cuotas + pagos + asientos + reclasificación
- Pasos 7-11: verificaciones post-sync (summary, auxiliar BD, saldos, movimientos BAC)
- **Paso 12**: confirmar asiento de reclasificación `Borrador → Publicado` (GET entry + PUT con `statusEntry="Publicado"` + verify BD)
  - Si el sync genera un id nuevo: usa `syncResp.reclassificationEntryId`
  - Si no (re-run, `previousPortion == newPortion`): busca en BD cualquier RCLS Borrador de la misma obligación

**Verificado en BD (2026-04-12)**:
```
RCLS-0001-202604 | Publicado | FinancialObligationReclassify
DR  2.2.01.01  ₡3,059,168.65  CR  2.1.02.01  ₡3,059,168.65
```

#### ✅ Obs #5 — Bugs en PS1 corregidos

| Bug | Causa | Fix |
|---|---|---|
| `MethodException: ToString("1")` | `$entry.dateEntry` deserializa como tipo sin `.ToString(format)` | Conversión explícita: `if ($entry.dateEntry -is [datetime])...else ([string]).Substring(0,10)` |
| `GetResponseStream` no existe | `HttpResponseMessage` en PowerShell 7 no expone ese método | Rodeado en `try/catch` + `$script:HTTP_STATUS` seteado **antes** del intento de lectura del stream |
