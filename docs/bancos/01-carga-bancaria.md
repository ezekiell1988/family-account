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
| Seeds templates | ✅ | BCR (`BCR-HTML-XLS-V1`), BAC (`BAC-TXT-V1`), BNCR (`BNCR-CSV-V1`) |
| Seeds templates pendientes | ❌ | `COOPEAL-XLS-V1`, `DAVIV-XLS-V1` |
| `IBankStatementParser` | ✅ | Interfaz genérica + `ParsedTransaction` centralizado |
| `BcrXlsParser` | ✅ | HTML embebido en `.xls` del portal BCR |
| `BacTxtParser` | ✅ | Pipe-delimitado TXT del portal BAC (tarjetas crédito) |
| `BncrCsvParser` | ✅ | CSV punto-y-coma encoding Latin-1 del BNCR |
| `CoopealianzaXlsParser` | ❌ | Pendiente |
| `DaviviendaXlsParser` | ❌ | Pendiente |
| `BankStatementParserFactory` | ✅ | Dispatch por `CodeTemplate`, singleton |
| `KeywordClassifier` | ✅ | Auto-clasifica `IdBankMovementType` por keywords del template |
| `BankStatementImportJob` (Hangfire) | ✅ | Usa factory, parsea + clasifica + persiste transacciones |
| Upload endpoint | ✅ | `POST /bank-statement-imports/upload/{idBankAccount}/{idTemplate}` |
| Keywords BCR | ✅ | `MONEDERO SINPE MOVIL` (8), `TRANSFERENC BANCOBCR` (6), `DB AH TELEF`/`MOVISTAR`/`KOLBI`/`PG AH TIEMPO AIRE TD` (4) |
| Clasificación manual | ✅ | `POST /bank-statement-transactions/{id}/classify` |
| Clasificación masiva | ❌ | `POST /bank-statement-imports/{id}/classify-all` — pendiente |
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
  BacTxtParser.cs                  ✅
  BncrCsvParser.cs                 ✅
  CoopealianzaXlsParser.cs         ❌ pendiente
  DaviviendaXlsParser.cs           ❌ pendiente
  BankStatementParserFactory.cs    ✅
```

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
