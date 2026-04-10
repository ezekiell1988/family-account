# 02 — Módulo Conciliación Bancaria (family-account)

## Tablas involucradas

| Tabla | Rol |
|---|---|
| `bankStatementTransaction` | Lado banco: filas del extracto ya cargadas e importadas |
| `bankMovement` | Lado contabilidad: movimiento registrado en el ERP con su `accountingEntry` |
| `bankReconciliationLine` | **Puente** (nueva): relaciona 1 transacción bancaria con N movimientos contables |
| `accountingEntry` | Asiento contable generado al confirmar un `bankMovement` |
| `accountingEntryLine` | Líneas débito/crédito del asiento |

### Relación bankReconciliationLine

```
bankStatementTransaction (1)
  └─ bankReconciliationLine (N)
       │  IdBankStatementTransaction  FK
       │  IdBankMovement              FK
       │  AmountApplied               decimal  ← monto parcial aplicado
       │  Notes                       string?
       └─► bankMovement
```

Un extracto bancario puede estar cubierto por varios `bankMovement` (ajustes, diferencias de tipo de cambio, etc.).

---

## Estado actual

| Componente | Estado | Detalles |
|---|---|---|
| `BankMovement` | ✅ | Entidad existente con estados `Borrador → Confirmado → Anulado` |
| `BankMovement.ConfirmAsync` | ✅ | Genera `AccountingEntry` con líneas DB/CR |
| `BankMovement.CancelAsync` | ✅ | Borrador/Confirmado → Anulado |
| `CreateMovementFromTransactionAsync` | ✅ | Crea `bankMovement` desde una transacción |
| `bankReconciliationLine` | ❌ | Entidad no existe aún |
| Bulk create movements | ❌ | No existe endpoint masivo |
| Bulk confirm movements | ❌ | No existe endpoint masivo |
| Resumen de conciliación | ❌ | No existe endpoint de cuadre |
| Frontend Angular | ❌ | No existe página de conciliación |

---

## Fase 1 — Entidad bankReconciliationLine

**Nueva entidad EF Core** siguiendo convenciones del proyecto:

```csharp
// Domain/Entities/BankReconciliationLine.cs
public sealed class BankReconciliationLine
{
    public int      IdBankReconciliationLine   { get; set; }
    public int      IdBankStatementTransaction { get; set; }
    public int      IdBankMovement             { get; set; }
    public decimal  AmountApplied              { get; set; }
    public string?  Notes                      { get; set; }
    public DateTime CreatedAt                  { get; set; }

    public BankStatementTransaction IdBankStatementTransactionNavigation { get; set; } = null!;
    public BankMovement             IdBankMovementNavigation             { get; set; } = null!;
}
```

Nueva migración EF.

---

## Fase 2 — Bulk operations

**Endpoints en un nuevo `BankReconciliationModule`:**

```
POST /bank-reconciliation/imports/{id}/create-all-movements
  body: { idFiscalPeriod, exchangeRateValue }
```
Crea un `bankMovement` Borrador por cada `bankStatementTransaction` clasificada y sin línea de conciliación.

```
POST /bank-reconciliation/imports/{id}/confirm-all-movements
```
Confirma todos los `bankMovement` en Borrador vinculados al import → genera `accountingEntry` por cada uno.

```
POST /bank-reconciliation/reconcile
  body: { idBankStatementTransaction, idBankMovement, amountApplied, notes? }
```
Vincula manualmente una transacción bancaria con un movimiento contable existente (crea `bankReconciliationLine`).

---

## Fase 3 — Resumen de conciliación

```
GET /bank-reconciliation/summary?idBankAccount=1&idFiscalPeriod=3
```

**Response:**
```json
{
  "idBankAccount": 1,
  "codeBankAccount": "BCR-AHO-001",
  "periodName": "Marzo 2026",
  "bankBalance": 125000.00,
  "accountingBalance": 123500.00,
  "difference": 1500.00,
  "unreconciledTransactions": 3,
  "pendingMovements": 2
}
```

`bankBalance` = suma de créditos - débitos de `bankStatementTransaction` del período.
`accountingBalance` = suma de `accountingEntryLine` donde `idAccount` = cuenta del banco, mismo período.

---

## Fase 4 — Frontend Angular

**Nueva página:** `bancos/conciliacion`

**Vista desktop (Color Admin):**
- Selector de cuenta + período
- Panel izquierdo: tabla de `bankStatementTransaction` sin conciliar
- Panel derecho: tabla de `bankMovement` Confirmados sin vincular
- Acción: arrastrar / seleccionar para crear `bankReconciliationLine`
- Botones masivos: "Crear movimientos", "Confirmar todo"
- Card de cuadre: saldo banco / saldo contable / diferencia

**Vista mobile (Ionic):**
- `ion-list` de transacciones pendientes agrupadas por fecha
- Swipe para vincular al movimiento sugerido
- Card de resumen con indicador de cuadre (verde/rojo)

**Servicios Angular:**
- `BankReconciliationService` — summary, reconcile, bulk operations
