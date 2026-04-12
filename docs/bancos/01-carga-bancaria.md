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
| Seeds templates BAC | ✅ | `BAC-TXT-V1` (retrocompat), `BAC-TXT-CRC-V1`, `BAC-TXT-USD-V1` |
| Seeds templates BNCR | ✅ | `BNCR-CSV-V1` |
| Seeds templates pendientes | ❌ | `COOPEAL-XLS-V1`, `DAVIV-XLS-V1` |
| `IBankStatementParser` | ✅ | Interfaz genérica + `ParsedTransaction` centralizado |
| `BcrXlsParser` | ✅ | HTML embebido en `.xls` del portal BCR |
| `BacTxtParser` | ✅ | Pipe-delimitado TXT; soporta `currency: CRC\|USD\|null` vía `ColumnMappings` |
| `BncrCsvParser` | ✅ | CSV punto-y-coma encoding Latin-1 del BNCR |
| `CoopealianzaXlsParser` | ❌ | Pendiente |
| `DaviviendaXlsParser` | ❌ | Pendiente |
| `BankStatementParserFactory` | ✅ | Dispatch por `CodeTemplate` — cubre `BCR-HTML-XLS-V1`, `BAC-TXT-*`, `BNCR-CSV-V1` |
| `KeywordClassifier` | ✅ | Auto-clasifica `IdBankMovementType` por keywords del template |
| `BankStatementImportJob` (Hangfire) | ✅ | Usa factory, parsea + clasifica + persiste transacciones |
| Upload endpoint | ✅ | `POST /bank-statement-imports/upload/{idBankAccount}/{idTemplate}` |
| Keywords BCR | ✅ | 8 reglas: salario, depósito, SINPE, compras, retiro, pago TC, préstamo, transferencia enviada |
| Keywords BAC CRC | ✅ | Pago recibido, transporte, digital/streaming, supermercados, farmacias, ferreterías, seguros, cuotas |
| Keywords BAC USD | ✅ | Pago recibido, transporte, suscripciones tech, seguros, cuotas |
| Keywords BNCR | ✅ | Salario, intereses, SINPE, retiros, pago TC, préstamo, débito SINPE |
| Clasificación manual | ✅ | `POST /bank-statement-transactions/{id}/classify` |
| Clasificación masiva | ❌ | `POST /bank-statement-imports/{id}/classify-all` — pendiente |
| Script prueba BCR | ✅ | `docs/bancos/BCR-carga-test.ps1` |
| Script prueba BAC | ✅ | `docs/bancos/BAC-carga-test.ps1` — carga 6 archivos (3 CRC + 3 USD) |
| Frontend Angular | ❌ | No existe página de carga |

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

## Fase 3 — Clasificación masiva

**Endpoint:**
```
POST /bank-statement-imports/{id}/classify-all
```
Re-ejecuta el `KeywordClassifier` sobre todas las transacciones sin `IdBankMovementType` del import. Devuelve conteo clasificadas/sin clasificar.

**Nuevo método en `IBankStatementImportService`:**
```csharp
Task<BulkClassifyResult> ClassifyAllAsync(int importId, CancellationToken ct);
```

---

## Fase 4 — Frontend Angular

**Nueva página:** `bancos/carga`

**Vista desktop (Color Admin):**
- Selector de cuenta bancaria + plantilla + botón upload
- Tabla `ngx-datatable` de transacciones con columnas: fecha, descripción, débito, crédito, tipo movimiento (dropdown editable), estado clasificación
- Botón "Clasificar todo" (llama `classify-all`)
- Indicador de estado del job Hangfire (polling)

**Vista mobile (Ionic):**
- `ion-list` con transacciones agrupadas por fecha
- FAB para nuevo upload
- Card con resumen de la importación (total, clasificadas, sin clasificar)

**Servicios Angular:**
- `BankStatementImportService` — upload + polling de status del job
- `BankStatementTransactionService` — list by import, classify individual, classify-all
