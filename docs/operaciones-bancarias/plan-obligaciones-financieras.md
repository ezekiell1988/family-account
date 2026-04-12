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

### IDs a usar al crear la obligación COOPEALIANZA

| Campo `FinancialObligation` | IdAccount |
|---|---|
| `IdAccountLongTerm` | **42** |
| `IdAccountShortTerm` | **134** |
| `IdAccountInterest` | **137** |
| `IdAccountLateFee` | **138** |
| `IdAccountOther` | null (no aplica) |
| `IdBankAccountPayment` | id de la cuenta BAC CR73 en `bankAccount` |

---

## Modelo de dominio

```
FinancialObligation (cabecera del préstamo)
  ├── IdAccountLongTerm  → 2.2.01.01
  ├── IdAccountShortTerm → 2.1.02.01  (nueva)
  ├── IdAccountInterest  → 5.5.04
  ├── IdAccountLateFee   → 5.5.04 (o nuevo)
  ├── IdBankAccountPayment → bankAccount BAC débito
  └── FinancialObligationInstallment[] (tabla de amortización — 1 por cuota)
        ├── SyncedAt           (última sincronización desde Excel)
        └── FinancialObligationPayment? (pago real + asiento — 0 ó 1 por cuota)
```

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

| Propiedad | Tipo | Notas |
|---|---|---|
| `IdFinancialObligationPayment` | `int` | PK |
| `IdFinancialObligationInstallment` | `int` | FK único (1:1 con cuota) |
| `IdBankMovement` | `int?` | FK → `bankMovement` (auto-detectado o manual) |
| `DatePayment` | `DateOnly` | Fecha real del pago |
| `AmountPaid` | `decimal` | Total efectivamente pagado |
| `AmountCapitalPaid` | `decimal` | Capital (tomado del Excel) |
| `AmountInterestPaid` | `decimal` | Interés (tomado del Excel) |
| `AmountLatePaid` | `decimal` | Mora (tomado del Excel) |
| `AmountOtherPaid` | `decimal` | Otros (tomado del Excel) |
| `IdAccountingEntry` | `int?` | FK → `accountingEntry` generado |
| `IsAutoProcessed` | `bool` | true = generado automáticamente por el import |
| `Notes` | `string?` | |

---

## Endpoint de importación y flujo automático

### `POST /financial-obligations/{id}/sync-excel`

Recibe el archivo `.xlsx` (multipart). Ejecuta el siguiente algoritmo:

```
1. PARSEAR el Excel → lista de InstallmentRow { number, dueDate, balanceAfter,
                        capital, interest, lateFee, other, total, status }

2. Para cada InstallmentRow:
   a. UPSERT en financialObligationInstallment
      - Si existe (por numberInstallment + idFinancialObligation):
          · Actualizar todos los montos, dueDate, balanceAfter, status, SyncedAt
      - Si no existe:
          · Insertar fila nueva, status = valor del Excel

   b. Si status == 'Pagada' Y no existe FinancialObligationPayment para esta cuota:
      → BUSCAR movimiento BAC automáticamente (ver §Matching)
      → CREAR FinancialObligationPayment
      → GENERAR asiento contable en estado Borrador
      → ACTUALIZAR statusInstallment = 'Pagada'

3. RECLASIFICACIÓN AUTOMÁTICA del período:
   · Calcular porción corriente = suma de AmountCapital de cuotas con
     DueDate entre hoy y hoy+12m y status IN ('Pendiente','Vigente')
   · Si porción cambió respecto al último asiento de reclasificación:
       → GENERAR asiento de reclasificación (Borrador)

4. RETORNAR SyncResult {
     installmentsUpserted, paymentsCreated, paymentsSkipped (sin movimiento BAC),
     reclassificationEntry (id o null), warnings[]
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

Infrastructure/
  Data/
    Configuration/
      FinancialObligationConfiguration.cs          ← incluye seed cuentas nuevas
      FinancialObligationInstallmentConfiguration.cs
      FinancialObligationPaymentConfiguration.cs
    AppDbContext.cs                                 ← 3 DbSets nuevos

Features/
  FinancialObligations/
    Dtos/
      FinancialObligationResponse.cs
      FinancialObligationSummaryResponse.cs         ← saldo, próxima cuota, porción corriente
      FinancialObligationInstallmentResponse.cs
      FinancialObligationPaymentResponse.cs
      CreateFinancialObligationRequest.cs
      UpdateFinancialObligationRequest.cs
      SyncExcelResult.cs                            ← resultado del sync
      RegisterPaymentRequest.cs                     ← pago manual (sin Excel)
    Parsers/
      FinancialObligationExcelParser.cs             ← lee .xlsx columnas fijas
    IFinancialObligationService.cs
    FinancialObligationService.cs                   ← lógica sync + matching + asientos
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
| **`POST`** | **`/financial-obligations/{id}/sync-excel`** | **Import Excel → upsert cuotas → pagos → asientos** |
| `PUT` | `/financial-obligations/{id}/installments/{iid}` | Edita cuota manualmente |
| `POST` | `/financial-obligations/{id}/installments/{iid}/payment` | Registra pago manual (sin Excel) |
| `POST` | `/financial-obligations/{id}/reclassify` | Reclasificación manual de período |

---

## Fases de implementación

| # | Fase | Contenido | Estado |
|---|---|---|---|
| 1 | **Entidades + Config + Migración** | 3 entidades, 3 configs Fluent API, migración `AddFinancialObligations` | ✅ Completado |
| 2 | **Parser Excel + servicio sync** | `FinancialObligationExcelParser`, algoritmo upsert, matching BAC, generación asientos | ✅ Completado |
| 3 | **DTOs + Module + endpoints** | 7 DTOs, 8 endpoints incluyendo `sync-excel` (multipart) y `summary` | ✅ Completado |
| 4 | **Reclasificación automática** | Cálculo porción corriente desde `BalanceAfter`, integrado en sync y endpoint manual | ✅ Completado |
| 5 | **Cuentas contables completas** | 5 cuentas nuevas: 134, 135, 136, 137, 138 — migración `AddFinancialObligationAccounts` | ✅ Completado |
| 6 | **Frontend Angular** | Página de obligaciones | ⏳ Pendiente |

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
| ¿Asiento en Borrador o Confirmado? | **Borrador** — usuario revisa y confirma |
| ¿Mora y otros desde Excel o manual? | **Desde Excel** — ya vienen en las columnas |
| ¿Matching automático de banco? | **Sí** — por monto ±1%, fecha ±10 días, keyword |
| ¿Reclasificación automática en sync? | **Sí** — incluida en el algoritmo del sync |
| ¿Cuentas configurables? | **Sí** — FK en `FinancialObligation`, no hardcoded |

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
