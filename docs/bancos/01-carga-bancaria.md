# 01 — Módulo Carga Bancaria (family-account)

## Tablas involucradas

| Tabla | Rol |
|---|---|
| `bankStatementTemplate` | Configuración del formato por banco (parsers, keywords, columnas) |
| `bankStatementImport` | Cabecera de cada carga: cuenta, plantilla, archivo, estado del job |
| `bankStatementTransaction` | Filas del extracto bancario. Una por movimiento del banco |

---

## Estado actual

| Componente | Estado | Detalles |
|---|---|---|
| Entidades EF Core | ✅ | `BankStatementTemplate`, `BankStatementImport`, `BankStatementTransaction` |
| Seeds templates BCR | ✅ | `BCR-HTML-XLS-V1` |
| Seeds templates BAC | ✅ | `BAC-TXT-V1` (retrocompat), `BAC-TXT-CRC-V1`, `BAC-TXT-USD-V1`, `BAC-XLS-V1` |
| Seeds templates BNCR | ✅ | `BNCR-CSV-V1` |
| Seeds templates pendientes | ❌ | `COOPEAL-XLS-V1`, `DAVIV-XLS-V1` |
| `IBankStatementParser` | ✅ | Interfaz genérica + `ParsedTransaction` centralizado |
| `BcrXlsParser` | ✅ | HTML embebido en `.xls` del portal BCR |
| `BacTxtParser` | ✅ | Pipe-delimitado TXT; soporta `currency: CRC\|USD\|null` vía `ColumnMappings` |
| `BacXlsParser` | ✅ | XLS BIFF8 de cuentas de ahorro/débito BAC; columnas fijas (Débitos col 7, Créditos col 8) |
| `BncrCsvParser` | ✅ | CSV punto-y-coma encoding Latin-1 del BNCR |
| `CoopealianzaXlsParser` | ❌ | Pendiente |
| `DaviviendaXlsParser` | ❌ | Pendiente |
| `BankStatementParserFactory` | ✅ | Dispatch por `CodeTemplate` — cubre `BCR-HTML-XLS-V1`, `BAC-TXT-*`, `BAC-XLS-V1`, `BNCR-CSV-V1` |
| `KeywordClassifier` | ✅ | Auto-clasifica `IdBankMovementType` por keywords del template |
| `BankStatementImportJob` (Hangfire) | ✅ | Usa factory, parsea + clasifica + persiste transacciones |
| Upload endpoint | ✅ | `POST /bank-statement-imports/upload/{idBankAccount}/{idTemplate}` |
| Keywords BCR | ✅ | 8 reglas: salario, depósito, SINPE, compras, retiro, pago TC, préstamo, transferencia enviada |
| Keywords BAC CRC | ✅ | Pago recibido, transporte, digital/streaming, supermercados, farmacias, ferreterías, seguros, cuotas |
| Keywords BAC USD | ✅ | Pago recibido, transporte, suscripciones tech, seguros, cuotas |
| Keywords BNCR | ✅ | Salario, intereses, SINPE, retiros, pago TC, préstamo, débito SINPE; `PAGOSERVICIO` (sin espacios) |
| Clasificación manual | ✅ | `PATCH /bank-statement-transactions/{id}/classify` — tipo + cuenta + centro de costo (sin learnKeyword) |
| Clasificación masiva (`classify-batch`) | ✅ | `POST /bank-statement-imports/{id}/classify-batch` — clasifica todas + aprende keywords + centro de costo |
| Keywords con cuentas contables | ✅ | Migración `EnrichKeywordRulesWithAccounts` — todos los templates tienen `idAccountCounterpart` por keyword |
| Script prueba BCR | ✅ | `docs/bancos/BCR-carga-test.ps1` |
| Script prueba BAC TXT | ✅ | `docs/bancos/BAC-carga-test.ps1` — carga 6 archivos (3 CRC + 3 USD) |
| Script prueba BAC XLS | ✅ | `docs/bancos/BAC-XLS-carga-test.ps1` — cuenta ahorro/débito CR73 |
| Keywords BAC XLS | ✅ | DEP_ATM → DEP; TEF/SINPE → TRANSF-REC; COOPEALIANZA → PAGO-PREST; PAGO/SINPE MOVIL PAGO_TARJETA → PAGO-TC; DTR: → TRANSF-ENV |
| Script prueba BNCR | ✅ | `docs/bancos/BNCR-carga-test.ps1` — carga 2 archivos (1 CRC + 1 USD) |
| Frontend Angular | ✅ | Página `bancos/carga` implementada — web (Color Admin + ngx-datatable) + mobile (Ionic) |

---

## Fase 1 — Corregir keywords BCR ✅ `(completado 2026-04-08)`

Migración `AddBankStatementTemplatesBacBncr` aplicada.

---

## Fase 2 — Parser factory + parsers faltantes ✅ `(completado 2026-04-08)`

```
Features/BankStatementImports/Parsers/
  IBankStatementParser.cs          ✅
  BcrXlsParser.cs                  ✅
  BacTxtParser.cs                  ✅  (soporta currency: CRC|USD|null)
  BacXlsParser.cs                  ✅  (XLS BIFF8 — cuentas ahorro/débito BAC)
  BncrCsvParser.cs                 ✅
  CoopealianzaXlsParser.cs         ❌ pendiente
  DaviviendaXlsParser.cs           ❌ pendiente
  BankStatementParserFactory.cs    ✅
```

---

## Fase 2b — Plantillas BAC por moneda ✅ `(completado 2026-04-12)`

Cada archivo BAC tiene dos columnas de monto (Local CRC y Dollars USD). Se crearon plantillas separadas para cargar cada columna de forma independiente.

**Plantillas nuevas:**

| ID | CodeTemplate | Columna usada | Cuentas seed |
|---|---|---|---|
| 4 | `BAC-TXT-CRC-V1` | Local (CRC) | 3, 4, 5, 6 |
| 5 | `BAC-TXT-USD-V1` | Dollars (USD) | 12, 13, 14 |

**`BacTxtParser`** — lee el campo `currency` del `ColumnMappings` JSON:
- `{"currency":"CRC"}` → sólo columna Local
- `{"currency":"USD"}` → sólo columna Dollars
- `{}` → retrocompat (prioriza local, cae en USD si es cero)

**Migraciones aplicadas:**
- `AddBacTemplatesCrcUsd` — seeds plantillas 4 y 5
- `AddBacTemplateKeywords` — enriquecimiento de keywords

**Keywords agregados (ambas plantillas):**
- Farmacias, ferreterías, clínicas → Gasto General
- `SEGURO PROTECCION`, `INS ` → Gasto General
- `GOOGLE`, `2CO.COM`, `NEOTHEK`, `GAMMA.APP`, `OPENAI` → Gasto General
- `SIMAN`, `ALMACENES` → Gasto General
- `ICON CC RETAIL` → Gasto General (USD)

**Script de prueba:** `docs/bancos/BAC-carga-test.ps1`
- Carga 6 archivos: 3 CRC (AMEX, MC-6515, MC-8608) + 3 USD
- Verifica plantillas y cuentas en BD antes de subir
- Polling de status Hangfire por archivo
- Consulta BD con totales, clasificadas y sin clasificar

**Resultado validado 2026-04-12:**

| Cuenta | Import | Tx | Clasificadas |
|---|---|---|---|
| BAC-CC-AMEX-8052-CRC | 2 | 20 | 20/20 |
| BAC-CC-MC-6515-CRC | 3 | 14 | 14/14 |
| BAC-CC-MC-8608-CRC | 4 | 37 | 37/37 |
| BAC-CC-AMEX-8052-USD | 5 | 0 | — (archivo sin montos USD) |
| BAC-CC-MC-6515-USD | 6 | 1 | 1/1 |
| BAC-CC-MC-8608-USD | 7 | 17 | 17/17 |

---

## Fase 2c — Parser BAC XLS (cuenta ahorro/débito) ✅ `(completado 2026-04-12)`

Soporte para el archivo `.xls` BIFF8 que exporta el portal BAC para cuentas de ahorro y débito.

**Paquete NuGet agregado:** `ExcelDataReader 3.7.0`

**Plantilla nueva:**

| ID | CodeTemplate | Cuenta seed | Moneda |
|---|---|---|---|
| 6 | `BAC-XLS-V1` | `BAC-AHO-001` (id=2, CR73010200009497305680) | CRC |

**`BacXlsParser`** — columnas fijas del XLS BAC:
- Col 0 = Fecha (acepta `DateTime` nativo o string `dd/MM/yyyy`)
- Col 1 = Referencia → `DocumentNumber`
- Col 4 = Descripción
- Col 7 = Débitos, Col 8 = Créditos, Col 9 = Balance

**Keywords:**
- `DEP_ATM`, `TATMFULL` → `DEP` (Depósito en Efectivo)
- `TEF DE:`, `DTR SINPE` → `TRANSF-REC` (SINPE recibido)
- `COOPEALIANZA`, `CAJA AHORRO` → `PAGO-PREST`
- `PAGO `, `SINPE MOVIL PAGO_TARJETA` → `PAGO-TC` (Pago Tarjeta)
- `DTR:`, `RETIRO ATM/CAJERO` → `TRANSF-ENV` (Transferencia/Retiro)

**Migraciones aplicadas:**
- `AddBacXlsTemplate` — seed plantilla id=6
- `UpdateBacXlsKeywords` — agrega `SINPE MOVIL PAGO_TARJETA` a `PAGO-TC`

**Script de prueba:** `docs/bancos/BAC-XLS-carga-test.ps1`

**Resultado validado 2026-04-12:**

| Cuenta | Import | Tx | Clasificadas | Débitos | Créditos |
|---|---|---|---|---|---|
| BAC-AHO-001 | 8 | 33 | 33/33 | ₡801,558.71 | ₡800,893.00 |

---

## Fase 2d — Scripts y keywords BNCR ✅ `(completado 2026-04-12)`

Soporte de carga y validación end-to-end para las dos cuentas del Banco Nacional de Costa Rica.

**Cuentas seed BNCR:**

| ID | CodeBankAccount | Número | Moneda |
|---|---|---|---|
| 7 | `BNCR-AHO-CRC-001` | CR86015100020019688637 | CRC |
| 8 | `BNCR-AHO-USD-001` | CR06015107220020012339 | USD |

**Plantilla:** `BNCR-CSV-V1` (id=3) — CSV punto-y-coma, codificación Latin-1, formato fecha `dd/MM/yyyy`.

**Fix de keyword — migración `UpdateBncrKeywords`:**

El BNCR exporta algunas descripciones sin espacios (`PAGOSERVICIOPROFESIONALSOFTWARE`). Se agregó `PAGOSERVICIO` a la regla `TRANSF-REC` para cubrir esta variante.

**Script de prueba:** `docs/bancos/BNCR-carga-test.ps1`
- Carga 2 archivos: 1 CRC + 1 USD
- Verifica plantilla (id=3) y cuentas (id=7, 8) en BD antes de subir
- Polling de status Hangfire por archivo
- Consulta BD con totales, clasificadas sin clasificar y detalle TOP 20

**Resultado validado 2026-04-12:**

| Cuenta | Import | Tx | Clasificadas | Débitos | Créditos |
|---|---|---|---|---|---|
| BNCR-AHO-CRC-001 | 11 | 38 | 25/38 | ₡1,486,490.00 | ₡1,510,449.95 |
| BNCR-AHO-USD-001 | 12 | 2 | 1/2 | 1,453.00 | 1,453.00 |

> 13 transacciones CRC sin clasificar corresponden a descripciones personales (`DINERO PAPA`, `MAMI PLATA`, etc.) — se clasifican manualmente.

---

## Fase 3 — Keywords con cuentas contables + Clasificación masiva ✅ `(completado 2026-04-12)`

### Keywords enriquecidos con `idAccountCounterpart`

Migración `EnrichKeywordRulesWithAccounts` aplicada. Todos los templates tienen `idAccountCounterpart` en cada `KeywordRule`:

| Keyword(s) | Tipo | Cuenta contable |
|---|---|---|
| `SALARIO`, `ITQS` | SALARIO (1) | 44 — ITQS Salario |
| `DEP EFECTIVO`, `DEP_ATM` | DEP (2) | 106 — Caja CRC |
| `UBER`, `LYFT`, `BOLT` | GASTO (4) | 83 — Transporte Actividades |
| `NETFLIX.COM` | GASTO (4) | 69 — Netflix |
| `APPLE.COM` | GASTO (4) | 72 — Apple iCloud |
| `OPENAI`, `CHATGPT` | GASTO (4) | 73 — ChatGPT |
| `GITHUB`, `MICROSOFT`, `GOOGLE`, `NEOTHEK` | GASTO (4) | 74 — Copilot/Suscripciones |
| `WALMART`, `MAXIPALI`, `AUTOMERCADO`, `PALI` | GASTO (4) | 61 — Alimentación |
| `FARMACIA`, `DROGUERIA`, `CLINICA`, `FERRETERIA`, `SEGURO`, `SIMAN` | GASTO (4) | 75 — Gastos en Pareja |
| `RETIRO ATM`, `CAJERO` | RETIRO (5) | 106 — Caja CRC |
| `COOPEALIANZA`, `CUOTA:` | PAGO-PREST (7) | 42 — Coopealianza Préstamo |
| `MOVISTAR`, `KOLBI` | GASTO (4) | 68 — Teléfono Celular |

### Endpoint `classify` (individual)

```
PATCH /bank-statement-transactions/{id}/classify
```

Clasifica una sola transacción. **No soporta `learnKeyword`** — no aprende keywords.

```json
{ "idBankMovementType": 4, "idAccountCounterpart": 68, "idCostCenter": 1 }
```

> `idAccountCounterpart` e `idCostCenter` son **opcionales**.

---

### Endpoint `classify-batch`

```
POST /bank-statement-imports/{id}/classify-batch
```

Clasifica una o más transacciones. Soporta `learnKeyword` por ítem. El frontend lo usa tanto para el botón ✓ por fila como para «Confirmar Todo».

1. Actualiza `IdBankMovementType` e `IdAccountCounterpart` en cada transacción.
2. Si `learnKeyword=true`, agrega la descripción como nueva `KeywordRule` al template (solo si no está ya cubierta).
3. **No modifica** transacciones que ya tienen `IdAccountingEntry` (ya contabilizadas).

```csharp
Task<BulkClassifyResult> ClassifyBatchAsync(int importId, BulkClassifyRequest request, CancellationToken ct);
// Devuelve: BulkClassifyResult(Classified, KeywordsAdded)
```

**DTOs:**
```csharp
record BulkClassifyItem {
    int    IdBankStatementTransaction;
    int    IdBankMovementType;
    int?   IdAccountCounterpart;  // opcional — null usa la cuenta del tipo de movimiento
    int?   IdCostCenter;          // opcional — centro de costo para el asiento contable
    bool   LearnKeyword;          // true = aprender descripción como keyword nuevo
}
record BulkClassifyRequest { IReadOnlyList<BulkClassifyItem> Items; }
record BulkClassifyResult(int Classified, int KeywordsAdded);
```

---

## Fase 4 — Frontend Angular ✅ `(completado 2026-04-12)`

**Página:** `bancos/carga` (`BankStatementImportsPage`)

**Arquitectura:**
- Page coordinador `BankStatementImportsPage` — extiende `ResponsiveComponent`, estado con signals, `forkJoin` para carga inicial.
- `BankStatementImportsWebComponent` — Color Admin + `PanelComponent` + `ngx-datatable`.
- `BankStatementImportsMobileComponent` — Ionic (FAB upload, `ion-list` de imports, expansión inline de transacciones).

**Estado reactivo del coordinador:**
- `pendingCount = computed(...)` — transacciones sin `idBankMovementType` y sin `idAccountingEntry`. Se pasa a ambos sub-componentes.
- `isLoadingCatalogs = computed(...)` — `true` mientras `BankAccountService` o `BankStatementTemplateService` siguen cargando. Se pasa a ambos sub-componentes para bloquear los selects del formulario de upload.

**Vista desktop (Color Admin):**
- Formulario de upload: selector de cuenta bancaria + selector de plantilla + input de archivo.
- Tabla de imports con botón «Ver Tx» por fila.
- Panel de transacciones del import seleccionado con:
  - Columna "Tipo / Cuenta / CC": tres `<select>` apilados — tipo de movimiento (auto-clasifica la cuenta del tipo al cambiar) + cuenta contrapartida (editable) + centro de costo (opcional).
  - Botón **✓** por fila: llama a `classify-batch` con un solo ítem. Al lado, botón **🏷️** (toggle): azul = guarda descripción como regla (`learnKeyword=true`); gris = no guarda. Default gris — el usuario decide explícitamente. `idAccountCounterpart` e `idCostCenter` son opcionales.
  - Botón **«Confirmar Todo»** al pie: envía todas las transacciones pendientes como un solo payload `classify-batch`, respetando el estado del 🏷️ y el CC por fila. Muestra badge **"N pendientes"** cuando hay transacciones sin clasificar.
  - Selects de cuenta bancaria y plantilla muestran spinner `fa-spin` y quedan `[disabled]` mientras `isLoadingCatalogs` es `true`.
- Polling Hangfire: cada 2s hasta `Completado` o `Error`.

**Vista mobile (Ionic):**
- FAB upload (inferior-derecha).
- FAB **«Confirmar Todo»** (inferior-izquierda) cuando hay transacciones cargadas. `aria-label` dinámico incluye el conteo de pendientes.
- Cuando hay pendientes: fila `ion-item` con badge de conteo al inicio de la lista de transacciones.
- Por cada transacción: `ion-select` para tipo + `ion-select` para cuenta + `ion-select` para centro de costo + botón 🏷️ toggle + botón ✓ confirmar.
- Selects de cuenta bancaria y plantilla muestran label dinámico `"Cargando…"` y quedan `[disabled]` mientras `isLoadingCatalogs` es `true`.

**Servicios Angular:**
- `BankStatementImportService` — upload, `pollById`, `loadTransactions`, `classifyTransaction` (individual, disponible para uso directo), **`classifyBatch`** (masivo — usado por el frontend para todo).
- `AccountService` — plan de cuentas; filtrado a `allowsMovements && isActive` en los selectores.
- `CostCenterService` — centros de costo activos; filtrado a `isActive` en el selector.
- `BankStatementTemplateService`, `BankAccountService`, `BankMovementTypeService` — catálogos de soporte.

**Análisis de endpoints — ¿en servicio y dos endpoints o uno?**

Se mantienen dos endpoints:

| Endpoint | Para quién | Soporta `learnKeyword` |
|---|---|---|
| `PATCH /bank-statement-transactions/{id}/classify` | Scripts, curl, integraciones directas | ❌ No |
| `POST /bank-statement-imports/{id}/classify-batch` | Frontend Angular (fila individual + confirmar todo) | ✅ Sí |

El frontend **siempre usa `classify-batch`** — tanto para el botón ✓ por fila como para «Confirmar Todo». La diferencia es el payload: un ítem o todos. No se creó un tercer endpoint porque `classify-batch` ya cubre ambos casos.
