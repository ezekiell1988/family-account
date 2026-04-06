# Caso 3 — Ensamble en venta: Configuraciones del sistema

> Ver el flujo completo en [caso-3-ensamble-proceso.md](./caso-3-ensamble-proceso.md).

Este documento describe todas las configuraciones necesarias para el Caso 3. Los **productos y la receta** vienen del seed inicial; las **ProductUnits y ProductAccounts** se crean en tiempo de ejecución antes de registrar facturas.

---

## 1. Productos involucrados

### Ingredientes (ProductType = 1 — Materia Prima)

| idProduct | Código | Nombre | Unidad base (idUnit) |
|---|---|---|---|
| 7 | MP-PAN-HD-001 | Pan de Hot Dog | 1 (UNI) |
| 8 | MP-SALCHICHA-001 | Salchicha | 1 (UNI) |
| 9 | MP-MOSTAZA-001 | Mostaza | 6 (ML) |
| 10 | MP-CATSUP-001 | Catsup | 6 (ML) |

### Producto ensamblado (ProductType = 3 — Producto Terminado)

| idProduct | Código | Nombre | Unidad base (idUnit) | TrackInventory |
|---|---|---|---|---|
| 11 | PT-HOT-DOG-001 | Hot Dog | 1 (UNI) | **true** |

> **Diferencia clave con Manufactura:** el Hot Dog también es `ProductType = 3` y tiene una receta activa. El sistema distingue el modo de operación por la presencia de la receta al confirmar la venta, **no** por el tipo de producto.

---

## 2. Unidades de medida (UnitOfMeasure)

Seed en `UnitOfMeasureConfiguration`.

| idUnit | Código | Nombre |
|---|---|---|
| 1 | UNI | Unidad |
| 6 | ML | Mililitro |

---

## 3. Receta (ProductRecipe + ProductRecipeLine)

### Receta (cabecera) — seed en `ProductRecipeConfiguration`

| Campo | Valor |
|---|---|
| idProductRecipe | 2 |
| idProductOutput | 11 (Hot Dog) |
| nameRecipe | Receta Hot Dog |
| quantityOutput | 1.0000 (1 hot dog por corrida) |
| versionNumber | 1 |
| isActive | true |

> El sistema busca la receta activa del producto en el momento de **completar la orden de producción** (no al crear la factura). Si no existe receta activa para un producto con inventario, la línea de factura requiere `IdInventoryLot` obligatorio.

### Ingredientes (líneas) — seed en `ProductRecipeLineConfiguration`

| idProductRecipeLine | Ingrediente | Cantidad por corrida | Unidad |
|---|---|---|---|
| 5 | Pan de Hot Dog (id=7) | 1.0000 | UNI |
| 6 | Salchicha (id=8) | 1.0000 | UNI |
| 7 | Mostaza (id=9) | 15.0000 | ML |
| 8 | Catsup (id=10) | 20.0000 | ML |

**Cálculo de consumo para N hot dogs:**  
`factor = quantityProduced / recipe.QuantityOutput = N / 1 = N`  
→ Para 3 hot dogs: 3 panes, 3 salchichas, 45 ml mostaza, 60 ml catsup.

---

## 4. Unidades de compra/venta por producto (ProductUnit)

No vienen en el seed. **Deben crearse antes de registrar cualquier factura.**

```
POST /product-units  →  Ingrediente Pan
{ "idProduct": 7, "idUnit": 1, "conversionFactor": 1.0, "isBase": true,
  "usedForPurchase": true, "usedForSale": false, "namePresentation": "Unidad base" }

POST /product-units  →  Ingrediente Salchicha
{ "idProduct": 8, "idUnit": 1, "conversionFactor": 1.0, "isBase": true,
  "usedForPurchase": true, "usedForSale": false, "namePresentation": "Unidad base" }

POST /product-units  →  Ingrediente Mostaza
{ "idProduct": 9, "idUnit": 6, "conversionFactor": 1.0, "isBase": true,
  "usedForPurchase": true, "usedForSale": false, "namePresentation": "Mililitro base" }

POST /product-units  →  Ingrediente Catsup
{ "idProduct": 10, "idUnit": 6, "conversionFactor": 1.0, "isBase": true,
  "usedForPurchase": true, "usedForSale": false, "namePresentation": "Mililitro base" }

POST /product-units  →  Hot Dog (venta)
{ "idProduct": 11, "idUnit": 1, "conversionFactor": 1.0, "isBase": true,
  "usedForPurchase": false, "usedForSale": true, "namePresentation": "Unidad" }
```

---

## 5. Vínculo producto ↔ cuenta contable (ProductAccount)

Opcional. Permite que la compra de ingredientes se debite a **1.1.07.02 Materias Primas** (cuenta 110) en lugar del default 109.

| idProduct | Nombre | idAccount | Código cuenta | percentageAccount |
|---|---|---|---|---|
| 7 | Pan de Hot Dog | 110 | 1.1.07.02 Materias Primas | 100.00 |
| 8 | Salchicha | 110 | 1.1.07.02 Materias Primas | 100.00 |
| 9 | Mostaza | 110 | 1.1.07.02 Materias Primas | 100.00 |
| 10 | Catsup | 110 | 1.1.07.02 Materias Primas | 100.00 |

> Para el Hot Dog (id=11) **no se necesita** ProductAccount. Al confirmar la venta el sistema usa los fallbacks de `SalesInvoiceType` para generar el asiento de COGS.

---

## 6. Tipo de factura de compra (PurchaseInvoiceType)

El Caso 3 usa `idPurchaseInvoiceType = 1` (EFECTIVO).

| idPurchaseInvoiceType | Código | Nombre | CR CRC | DR inventario (default) |
|---|---|---|---|---|
| **1** | **EFECTIVO** | **Pago en Efectivo** | **106 (Caja CRC)** | **109 (Inventario)** |

Con `ProductAccount` para los ingredientes (→ 110):

```
DR 110 (Materias Primas)     = rawAmount por cada ingrediente
DR 124 (IVA Acreditable CRC) = taxAmount
CR 106 (Caja CRC)            = totalAmount
```

---

## 7. Tipo de factura de venta (SalesInvoiceType)

Para vender el Hot Dog se usa `idSalesInvoiceType = 1` (CONTADO_CRC).

| id | Código | DR CRC | Ingresos (CR) | COGS (DR) | Inventario (CR) |
|---|---|---|---|---|---|
| **1** | **CONTADO_CRC** | **106** | **117** | **119** | **109** |

**Asiento FV:**
```
DR 106 (Caja CRC)                 = totalAmount (neto + IVA)
CR 117 (Ingresos por Ventas)      = quantity × unitPrice
CR 127 (IVA por Pagar CRC)        = taxAmount
```

**Asiento COGS (descuento directo del lote PT — generado automáticamente al confirmar):**
```
DR 119 (Costo de Ventas)          = qty × lote_PT.unitCost
CR 109 (Inventario de Mercadería) = mismo monto
Descuenta el lote PT generado por la orden de producción
```
> La explosión de ingredientes ocurrió en la OP; en la factura solo sale el PT.

> **La línea de FV lleva el `IdInventoryLot` del lote PT** creado al completar la orden de producción. El sistema descuenta ese lote directamente (no hace explosión BOM en la factura). El COGS se genera a partir del costo unitario del lote PT calculado en la OP.

---

## 8. Tipo de ajuste de inventario (InventoryAdjustmentType)

Usado en la **devolución parcial** y en la **regalía** del Caso 3.

| id | Código | Nombre | Inventario (IdAccountInventoryDefault) | Contrapartida salida (DR) / entrada (CR) |
|---|---|---|---|---|
| **1** | **CONTEO** | **Conteo Físico** | **109 (Inventario de Mercadería)** | **113 (Merma) / 114 (Sobrantes)** |

**Asiento de regalía (2 hot dogs = delta negativo por ingrediente):**
```
DR 113 (Faltantes / Merma)        = |quantityDelta| × lot.unitCost  (por cada ingrediente)
CR 109 (Inventario de Mercadería) = mismo monto
```

**Asiento de devolución parcial (1 hot dog devuelto = delta positivo por ingrediente):**
```
DR 109 (Inventario de Mercadería) = quantityDelta × lot.unitCost
CR 114 (Sobrantes de Inventario)  = mismo monto
```

> La devolución parcial de un ensamblado no pasa por `POST /sales-invoices/{id}/partial-return` (ese endpoint requiere lotes directos de `SalesInvoiceLine`). Se hace con ajustes manuales por cada ingrediente.

---

## 9. Cuentas contables involucradas

| idAccount | Código | Nombre | Tipo | Saldo normal |
|---|---|---|---|---|
| 106 | 1.1.06.01 | Caja CRC (₡) | Activo | DR |
| 109 | 1.1.07.01 | Inventario de Mercadería | Activo | DR |
| 110 | 1.1.07.02 | Materias Primas | Activo | DR |
| 113 | 5.14.01 | Faltantes de Inventario / Merma | Gasto | DR |
| 114 | 5.14.02 | Sobrantes de Inventario | Ingreso | CR |
| 117 | 4.5.01 | Ingresos por Ventas — Mercadería | Ingreso | CR |
| 119 | 5.15.01 | Costo de Ventas — Mercadería | Gasto | DR |
| 124 | 1.1.09.01 | IVA Acreditable CRC (₡) | Activo | DR |
| 127 | 2.1.04.01 | IVA por Pagar CRC (₡) | Pasivo | CR |

---

## 10. Período fiscal y almacén

- **Período fiscal:** usar `idFiscalPeriod = 4` (Abril 2026), `statusPeriod = "Abierto"`.
- **Almacén:** usar `idWarehouse = 1` (default). Se asigna implícitamente en los lotes creados al confirmar la factura de compra.

---

## Diferencias clave vs. Caso 2 (Manufactura)

| Aspecto | Caso 2 — Manufactura | Caso 3 — Ensamble |
|---|---|---|
| Orden de producción | Manual: Borrador → Pendiente → EnProceso → Completado | **Automática** al confirmar el pedido con `idWarehouse` |
| ¿Cuándo se consumen los MP? | Al completar la OP (manual) | **Al completar la OP** (automático dentro del confirm) |
| Lote del producto final | Sí, se crea al completar la OP | **Sí**, se crea igual al completar la OP |
| `IdInventoryLot` en FV | Sí (lote del PT) | **Sí** (lote del PT generado por la OP) |
| Devolución parcial | `POST /sales-invoices/{id}/partial-return` | `POST /sales-invoices/{id}/partial-return` (mismo flujo) |
| Cancelación total | `POST /sales-invoices/{id}/cancel` | `POST /sales-invoices/{id}/cancel` (mismo flujo) |
| Disparador del ciclo | Separado: pedido → OP manual → factura manual | **Un solo confirm** del pedido ejecuta todo |

---

## Diagrama de dependencias de configuración

```
ProductType (id=1, TrackInventory=true)  → ingredientes: 7, 8, 9, 10
ProductType (id=3, TrackInventory=true)  → ensamblado: 11

ProductRecipe (id=2, output=11, qty=1, active)
  ├── ProductRecipeLine: 1 UNI  Pan de Hot Dog (id=7)
  ├── ProductRecipeLine: 1 UNI  Salchicha (id=8)
  ├── ProductRecipeLine: 15 ML  Mostaza (id=9)
  └── ProductRecipeLine: 20 ML  Catsup (id=10)

ProductUnit  ← crear antes de facturas
  ├── (product=7,  unit=1, base=true, purchase=true)
  ├── (product=8,  unit=1, base=true, purchase=true)
  ├── (product=9,  unit=6, base=true, purchase=true)
  ├── (product=10, unit=6, base=true, purchase=true)
  └── (product=11, unit=1, base=true, sale=true)

ProductAccount ← opcional (para DR 110 en lugar de DR 109 al comprar)
  ├── (product=7,  account=110, 100%)
  ├── (product=8,  account=110, 100%)
  ├── (product=9,  account=110, 100%)
  └── (product=10, account=110, 100%)

PurchaseInvoiceType (id=1, EFECTIVO)
  ├── IdAccountCounterpartCRC   = 106  (Caja CRC)
  └── IdDefaultInventoryAccount = 109  (fallback si no hay ProductAccount)

SalesInvoiceType (id=1, CONTADO_CRC)         ← línea sin IdInventoryLot
  ├── IdAccountCounterpartCRC = 106
  ├── IdAccountSalesRevenue   = 117
  ├── IdAccountCOGS           = 119  (DR al explotar BOM)
  └── IdAccountInventory      = 109  (CR al explotar BOM)

InventoryAdjustmentType (id=1, CONTEO)       ← para regalía y devolución parcial
  ├── IdAccountInventoryDefault = 109
  ├── IdAccountCounterpartEntry = 114  (Sobrantes — CR en entradas)
  └── IdAccountCounterpartExit  = 113  (Merma — DR en salidas)

Account seeds requeridos:
  106, 109, 110, 113, 114, 117, 119, 124, 127
```
