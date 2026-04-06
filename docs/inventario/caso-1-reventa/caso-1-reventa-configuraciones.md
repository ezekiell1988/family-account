# Caso 1 — Reventa: Configuraciones del sistema

> Ver el flujo completo en [caso-1-reventa-proceso.md](./caso-1-reventa-proceso.md).

Este documento describe todas las configuraciones que deben existir en el sistema para que el Caso 1 funcione correctamente. La mayoría viene del **seed inicial** (datos precargados en migraciones); algunas se crean en tiempo de ejecución.

---

## 1. Producto

El producto base del caso. Configurado con `TrackInventory = true` para que el sistema descuente y registre COGS automáticamente.

| Campo | Valor |
|---|---|
| idProduct | 1 |
| nameProduct | Coca-Cola 355ml |
| idProductType | 4 (Reventa) |
| isCombo | false |
| isVariantParent | false |

**Tipo de producto (ProductType):**

| Campo | Valor |
|---|---|
| idProductType | 4 |
| nameProductType | Reventa |
| trackInventory | **true** → habilita descuento de stock y generación de COGS al vender |

---

## 2. Unidad de medida y presentación (ProductUnit)

La API valida en `MapLinesAsync` que exista un registro `ProductUnit` para cada par `(idProduct, idUnit)` en las líneas de factura antes de guardar. Sin este registro, la creación falla.

| Campo | Valor |
|---|---|
| idProduct | 1 |
| idUnit | 1 (Unidad) |
| conversionFactor | 1.0 |
| isBase | true |
| usedForPurchase | true |
| usedForSale | true |

**Endpoint para crearlo (si no existe):**
```
POST /product-units
{
  "idProduct": 1, "idUnit": 1, "conversionFactor": 1.0,
  "isBase": true, "usedForPurchase": true, "usedForSale": true,
  "namePresentation": "Unidad base"
}
```

El `conversionFactor` determina cómo se convierte la unidad de compra/venta a la unidad base del lote. Con factor 1, `unitCost = unitPrice / conversionFactor = unitPrice`.

---

## 3. Tipo de factura de compra (PurchaseInvoiceType)

Seed en `PurchaseInvoiceTypeConfiguration`. Determina la cuenta CR del asiento de compra y si se auto-crea un movimiento bancario.

| idPurchaseInvoiceType | Código | Nombre | CounterpartFromBankMovement | CR CRC | CR USD | Default inventario | Default gasto (fallback) |
|---|---|---|---|---|---|---|---|
| **1** | **EFECTIVO** | Pago en Efectivo | **false** | 106 (Caja CRC) | 107 (Caja USD) | 109 (Inventario) | 75 (5.12.01) |
| 2 | DEBITO | Tarjeta Débito / Transferencia | true | — | — | 109 (Inventario) | 75 (5.12.01) |
| 3 | TC | Tarjeta de Crédito | true | — | — | 109 (Inventario) | 75 (5.12.01) |

**Para el Caso 1 se usa `idPurchaseInvoiceType = 1` (EFECTIVO).** La contraparte CR sale directo de caja sin pasar por movimiento bancario.

**Cómo funciona el asiento FC (con IVA separado):**

```
ConfirmAsync:
  1. Resuelve crAccountId = 106 (Caja CRC, porque moneda = CRC y CounterpartFromBankMovement = false)
  2. Por cada línea con idProduct:
       a. rawAmount = quantity × unitPrice (sin IVA)  ← monto neto
       b. Si el producto tiene ProductAccount → DR = pa.IdAccount por proporcional  (override explícito)
       c. Si no → DR = IdDefaultInventoryAccount (109) por rawAmount              ← default del tipo de factura
       d. Si no hay IdDefaultInventoryAccount → DR = IdDefaultExpenseAccount (75)  ← fallback para líneas sin producto
  3. Agrega línea DR IVA Acreditable:
       DR 124 (IVA Acreditable CRC) = invoice.TaxAmount
  4. Agrega línea CR:
       CR crAccountId (106) = totalDR (suma neto + IVA = totalAmount)
```

---

## 4. Tipo de factura de venta (SalesInvoiceType)

Seed en `SalesInvoiceTypeConfiguration`. Determina la cuenta DR del asiento de ingresos, la cuenta de fallback de ingresos y las cuentas COGS/Inventario.

| id | Código | CounterpartFromBankMovement | DR CRC | DR USD | Ingresos (CR fallback) | COGS (DR) | Inventario (CR) |
|---|---|---|---|---|---|---|---|
| **1** | **CONTADO_CRC** | **false** | **106** | — | **117** | **119** | **109** |
| 2 | CONTADO_USD | false | — | 107 | 117 | 119 | 109 |
| 3 | CREDITO_CRC | true | — | — | 117 | 119 | 109 |
| 4 | CREDITO_USD | true | — | — | 117 | 119 | 109 |

**Para el Caso 1 se usa `idSalesInvoiceType = 1` (CONTADO_CRC).**

**Cómo funciona el asiento FV (con IVA separado):**

```
ConfirmAsync:
  1. Resuelve drAccountId = 106 (Caja CRC, CounterpartFromBankMovement = false)
  2. Como NO hay ProductAccount (fue eliminado en Paso 4):
       CR 117 (IdAccountSalesRevenue) = quantity × unitPrice  ← monto neto sin IVA
  3. Agrega línea IVA por Pagar:
       CR 127 (IVA por Pagar CRC) = invoice.TaxAmount
  4. Agrega línea DR:
       DR drAccountId (106) = totalCR (= net + IVA = totalAmount)
```

**Cómo funciona el asiento COGS:**

```
  5. Por cada línea de venta con producto y TrackInventory:
       quantityBase = quantity × conversionFactor = 10 × 1 = 10
       monto COGS = quantityBase × lot.unitCost = 10 × 1,000 = 10,000
       DR 119 (IdAccountCOGS)       = 10,000
       CR 109 (IdAccountInventory)  = 10,000
  6. Descuenta quantityBase del lote (QuantityAvailable -= 10)
```

---

## 5. ProductAccount — vínculo producto ↔ cuenta contable

Registro que indica a qué cuenta contable se carga el costo al confirmar la factura de compra.

| Campo | Valor |
|---|---|
| idProduct | 1 |
| idAccount | 109 (1.1.07.01 Inventario de Mercadería) |
| percentageAccount | 100.00 |

> **Nota:** Crear el ProductAccount apuntando a 109 es **opcional** en el Caso 1. El tipo de factura ya tiene `IdDefaultInventoryAccount = 109`, por lo que sin ProductAccount la compra igualmente genera DR 109. El paso es útil si se desea apuntar a una cuenta diferente de la predeterminada (override explícito).

**Ciclo de vida en el Caso 1 (si se usa el override):**

```
[OPCIONAL] ANTES de crear FC  →  POST /product-accounts  (crea vínculo 1 → 109)
                              →  POST /purchase-invoices  (crea borrador)
                              →  POST /purchase-invoices/{id}/confirm
                                   Si tiene ProductAccount  → DR pa.IdAccount (109)
                                   Si no                   → DR IdDefaultInventoryAccount (109)  ← mismo resultado
DESPUÉS de confirmar FC (si creaste el ProductAccount):
                              →  DELETE /product-accounts/{id}  (elimina el vínculo)
                              →  POST /sales-invoices  (sin ProductAccount → fallback a 117)
```

> ⚠️ Si el ProductAccount **no se elimina antes de crear la venta**, el SalesInvoice usará `pa.IdAccount` (109) como cuenta CR de ingresos en lugar de `IdAccountSalesRevenue` (117), generando un asiento incorrecto.

---

## 6. Tipo de ajuste de inventario (InventoryAdjustmentType)

Seed en `InventoryAdjustmentTypeConfiguration`. Define las cuentas contables para cada tipo de ajuste.

| id | Código | Nombre | Inventario | Contrapartida Salida (DR) | Contrapartida Entrada (CR) |
|---|---|---|---|---|---|
| **1** | **CONTEO** | **Conteo Físico** | **109** | **113** (Merma) | **114** (Sobrantes) |
| 2 | PRODUCCION | Producción | 111 (Prod. en Proceso) | 115 (Costos Producción) | 115 (Costos Producción) |
| 3 | AJUSTE_COSTO | Ajuste de Costo | 109 | 113 (Merma) | 114 (Sobrantes) |

**Para el Caso 1 se usa `idInventoryAdjustmentType = 1` (CONTEO — Conteo Físico).**

El asiento de regalía (delta negativo) usa `IdAccountCounterpartExit`:
```
DR 113 (Merma)               = |quantityDelta| × unitCost = 2 × 1,000 = 2,000
CR 109 (Inventario)          = 2,000
```

---

## 7. Cuentas contables involucradas

Seed en `AccountConfiguration`. Todas estas cuentas deben existir con `AllowsMovements = true` para poder recibir asientos.

| IdAccount | Código | Nombre | Tipo | Saldo normal |
|---|---|---|---|---|
| 106 | 1.1.06.01 | Caja CRC (₡) | Activo | DR |
| 109 | 1.1.07.01 | Inventario de Mercadería | Activo | DR |
| 113 | 5.14.01 | Faltantes de Inventario (Merma) | Gasto | DR |
| 114 | 5.14.02 | Sobrantes de Inventario | Gasto | CR |
| 117 | 4.5.01 | Ingresos por Ventas — Mercadería | Ingreso | CR |
| 119 | 5.15.01 | Costo de Ventas — Mercadería | Gasto | DR |
| 124 | 1.1.09.01 | IVA Acreditable CRC (₡) | Activo | DR |
| 127 | 2.1.04.01 | IVA por Pagar CRC (₡) | Pasivo | CR |

**Jerarquía de las cuentas IVA:**

```
1 Activo
└── 1.1 Activo Corriente
    └── 1.1.09 IVA Acreditable (grupo)
        ├── 1.1.09.01 IVA Acreditable CRC (₡)  ← id=124
        └── 1.1.09.02 IVA Acreditable USD ($)  ← id=125

2 Pasivo
└── 2.1 Pasivo Corriente
    └── 2.1.04 IVA por Pagar (grupo)
        ├── 2.1.04.01 IVA por Pagar CRC (₡)   ← id=127
        └── 2.1.04.02 IVA por Pagar USD ($)   ← id=128
```

---

## 8. Lógica de selección de cuenta IVA por moneda

Tanto en compras como en ventas, el servicio detecta la moneda de la factura para usar la cuenta correcta:

```csharp
// PurchaseInvoiceService.ConfirmAsync
bool isUsdPurchase = invoice.IdCurrencyNavigation.CodeCurrency.Equals("USD", ...);
int ivaAcreditableId = isUsdPurchase ? 125 : 124;

// SalesInvoiceService.ConfirmAsync
bool isUsdSale = invoice.IdCurrencyNavigation.CodeCurrency.Equals("USD", ...);
int ivaPorPagarId = isUsdSale ? 128 : 127;
```

---

## 9. Período fiscal

El Caso 1 usa `idFiscalPeriod = 4` (Abril 2026). El período debe estar en estado `Abierto` para permitir la creación y confirmación de facturas y asientos.

`GET /fiscal-periods/4.json` → debe devolver `statusPeriod = "Abierto"`.

---

## 10. Almacén (Warehouse)

El Caso 1 usa `idWarehouse = 1`. Debe tener `isDefault = true` para que el fallback automático funcione cuando no se especifica almacén al confirmar.

---

## Diagrama de dependencias de configuración

```
ProductType (id=4, TrackInventory=true)
    └── Product (id=1)
            ├── ProductUnit (product=1, unit=1, convFactor=1)   [crear antes de FC]
            └── ProductAccount (product=1, account=109, 100%)   [OPCIONAL — ver sección 5]

PurchaseInvoiceType (id=1, EFECTIVO)
    ├── IdAccountCounterpartCRC     = 106  (Caja CRC)
    ├── IdDefaultInventoryAccount   = 109  (Inventario — DR por defecto para líneas con producto)
    └── IdDefaultExpenseAccount     = 75   (fallback para líneas sin producto)

SalesInvoiceType (id=1, CONTADO_CRC)
    ├── IdAccountCounterpartCRC = 106  (Caja CRC)
    ├── IdAccountSalesRevenue   = 117  (Ingresos — fallback si no hay ProductAccount)
    ├── IdAccountCOGS           = 119  (Costo de Ventas)
    └── IdAccountInventory      = 109  (Inventario — CR del COGS)

InventoryAdjustmentType (id=1, CONTEO — Conteo Físico)
    ├── IdAccountInventoryDefault   = 109  (Inventario)
    ├── IdAccountCounterpartExit    = 113  (Merma — para delta negativo)
    └── IdAccountCounterpartEntry   = 114  (Sobrantes — para delta positivo)

Account seeds requeridos:
    106, 109, 113, 114, 117, 119, 124, 127
```

---

## Migraciones relevantes

| Migración | Contenido |
|---|---|
| `InitialCreate` | Todas las tablas, seed del plan de cuentas 1–122, tipos de factura, tipos de ajuste |
| `AddIvaAccounts` | Agrega cuentas 123–128 (IVA Acreditable y IVA por Pagar, CRC y USD) |
