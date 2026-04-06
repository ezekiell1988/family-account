# Caso 2 — Manufactura: Configuraciones del sistema

> Ver el flujo completo en [caso-2-manufactura-proceso.md](./caso-2-manufactura-proceso.md).

Este documento describe todas las configuraciones que deben existir en el sistema para que el Caso 2 funcione correctamente. Los **productos, recetas y cuentas contables** vienen del seed inicial; las **ProductUnits y ProductAccounts** se crean en tiempo de ejecución antes de registrar facturas.

---

## 1. Productos involucrados

### Materias Primas (ProductType = 1)

| idProduct | Código | Nombre | Unidad base (idUnit) |
|---|---|---|---|
| 2 | MP-CHILE-001 | Chile Seco | 3 (KG) |
| 3 | MP-VINAGRE-001 | Vinagre Blanco | 7 (LTR) |
| 4 | MP-SAL-001 | Sal | 3 (KG) |
| 5 | MP-FRASCO-001 | Frasco 250ml | 1 (UNI) |

### Producto Terminado (ProductType = 3)

| idProduct | Código | Nombre | Unidad base (idUnit) | TrackInventory |
|---|---|---|---|---|
| 6 | PT-CHILE-EMB-001 | Chile Embotellado Marca X | 1 (UNI) | **true** |

**Tipos de producto relevantes:**

| idProductType | Nombre | TrackInventory |
|---|---|---|
| 1 | Materia Prima | true — se descuenta al ser consumida en producción |
| 3 | Producto Terminado | true — habilita COGS al vender |

> El sistema valida en `POST /product-recipes` que el `IdProductOutput` no sea de tipo Materia Prima ni Reventa. Solo tipos 2 (Prod. en Proceso) o 3 (Prod. Terminado) pueden ser output de una receta.

---

## 2. Unidades de medida (UnitOfMeasure)

Seed en `UnitOfMeasureConfiguration`. Todas las unidades usadas en el caso:

| idUnit | Código | Nombre |
|---|---|---|
| 1 | UNI | Unidad |
| 3 | KG | Kilogramo |
| 6 | ML | Mililitro |
| 7 | LTR | Litro |

---

## 3. Receta (ProductRecipe + ProductRecipeLine)

### Receta (cabecera) — seed en `ProductRecipeConfiguration`

| Campo | Valor |
|---|---|
| idProductRecipe | 1 |
| idProductOutput | 6 (Chile Embotellado Marca X) |
| nameRecipe | Receta Chile Embotellado |
| quantityOutput | 1.0000 (1 frasco por corrida) |
| versionNumber | 1 |
| isActive | true |

> Si se necesita ajustar la receta, `PUT /product-recipes/{id}` crea una nueva versión (`versionNumber` incrementa, la versión anterior queda `IsActive = false`). Al completar una orden, el sistema busca la receta activa para el producto.

### Ingredientes (líneas) — seed en `ProductRecipeLineConfiguration`

| idProductRecipeLine | Ingrediente | Cantidad por corrida | Unidad |
|---|---|---|---|
| 1 | Chile Seco (id=2) | 0.2000 | KG |
| 2 | Vinagre Blanco (id=3) | 0.0500 | LTR |
| 3 | Sal (id=4) | 0.0050 | KG |
| 4 | Frasco 250ml (id=5) | 1.0000 | UNI |

**Cálculo de consumo para N frascos:**  
`factor = quantityProduced / recipe.QuantityOutput = N / 1 = N`  
→ Para 100 frascos: 20 KG chile, 5 LTR vinagre, 0.5 KG sal, 100 frascos.

---

## 4. Unidades de compra/venta por producto (ProductUnit)

No vienen en el seed. **Deben crearse antes de registrar cualquier factura de compra o venta.**

La API valida en `MapLinesAsync` que exista un `ProductUnit` para cada par `(idProduct, idUnit)` en las líneas. Sin este registro la creación falla.

**Endpoints para crearlos:**

```
POST /product-units  →  MP Chile Seco
{ "idProduct": 2, "idUnit": 3, "conversionFactor": 1.0, "isBase": true,
  "usedForPurchase": true, "usedForSale": false, "namePresentation": "Kilogramo base" }

POST /product-units  →  MP Vinagre Blanco
{ "idProduct": 3, "idUnit": 7, "conversionFactor": 1.0, "isBase": true,
  "usedForPurchase": true, "usedForSale": false, "namePresentation": "Litro base" }

POST /product-units  →  MP Sal
{ "idProduct": 4, "idUnit": 3, "conversionFactor": 1.0, "isBase": true,
  "usedForPurchase": true, "usedForSale": false, "namePresentation": "Kilogramo base" }

POST /product-units  →  MP Frasco 250ml
{ "idProduct": 5, "idUnit": 1, "conversionFactor": 1.0, "isBase": true,
  "usedForPurchase": true, "usedForSale": false, "namePresentation": "Unidad base" }

POST /product-units  →  PT Chile Embotellado
{ "idProduct": 6, "idUnit": 1, "conversionFactor": 1.0, "isBase": true,
  "usedForPurchase": false, "usedForSale": true, "namePresentation": "Frasco 250ml" }
```

---

## 5. Vínculo producto ↔ cuenta contable (ProductAccount)

Registro **opcional pero recomendado** para que cada compra de Materia Prima se acredite a la cuenta correcta (110 Materias Primas) en lugar del default del tipo de factura (109 Inventario de Mercadería).

| idProduct | Nombre | idAccount | Código cuenta | percentageAccount |
|---|---|---|---|---|
| 2 | Chile Seco | 110 | 1.1.07.02 Materias Primas | 100.00 |
| 3 | Vinagre Blanco | 110 | 1.1.07.02 Materias Primas | 100.00 |
| 4 | Sal | 110 | 1.1.07.02 Materias Primas | 100.00 |
| 5 | Frasco 250ml | 110 | 1.1.07.02 Materias Primas | 100.00 |

**Ciclo de vida per MP:**

```
ANTES de crear FC  →  POST /product-accounts  (DR 110 en lugar de DR 109 default)
                   →  POST /purchase-invoices / confirm
DESPUÉS de confirmar FC (si deseas mantenerlos activos):
    El ProductAccount puede quedar. La producción NO lo usa —
    el consumo de MP genera su propio asiento vía InventoryAdjustmentType PRODUCCION.
```

> ⚠️ Para el PT (id=6) **no se necesita** ProductAccount antes de vender. Al confirmar la factura de venta el sistema usa los fallbacks de `SalesInvoiceType`: DR 119 (COGS), CR 109 (Inventario).

---

## 6. Tipo de factura de compra (PurchaseInvoiceType)

El Caso 2 usa `idPurchaseInvoiceType = 1` (EFECTIVO), igual que el Caso 1.

| idPurchaseInvoiceType | Código | Nombre | CR CRC | DR inventario (default) |
|---|---|---|---|---|
| **1** | **EFECTIVO** | **Pago en Efectivo** | **106 (Caja CRC)** | **109 (Inventario)** |

Si se crean `ProductAccount` para los MP (→ 110), ese default queda sobreescrito:

```
ConfirmAsync:
  DR 110 (Materias Primas, por ProductAccount)   = rawAmount por cada MP
  DR 124 (IVA Acreditable CRC)                   = taxAmount
  CR 106 (Caja CRC)                              = totalAmount
```

Sin ProductAccount:
```
  DR 109 (Inventario de Mercadería, default)     = rawAmount por cada MP
  DR 124 (IVA Acreditable CRC)                   = taxAmount
  CR 106 (Caja CRC)                              = totalAmount
```

---

## 7. Tipo de ajuste de inventario (InventoryAdjustmentType)

El sistema crea ajustes automáticamente al completar la orden de producción. No se llaman directamente en el flujo del Caso 2.

| id | Código | Nombre | Inventario (IdAccountInventoryDefault) | Contrapartida (IdAccountCounterpartExit / Entry) |
|---|---|---|---|---|
| **2** | **PRODUCCION** | **Producción** | **111 (Productos en Proceso)** | **115 / 115 (Costos de Producción)** |

**Asiento de consumo de MP (generado por `CompleteProductionAsync`):**

```
Por cada ingrediente consumido (delta negativo):
  DR 115 (Costos de Producción)   = |quantityConsumed| × lot.unitCost
  CR 111 (Productos en Proceso)   = mismo monto
```

El costo total de los ingredientes (`totalMpCost`) se acumula y se divide entre las unidades producidas para obtener el `unitCostPt` del lote de PT:

```
unitCostPt = totalMpCost / quantityProduced
```

**Estrategia de consumo:** **FEFO** (First Expired, First Out). El sistema consume primero los lotes con fecha de vencimiento más próxima del mismo almacén. Si hay stock insuficiente en algún insumo, la orden completa de todos modos con un `warning` y el lote más antiguo queda en negativo (deuda).

---

## 8. Tipo de factura de venta (SalesInvoiceType)

Para vender el PT se usa `idSalesInvoiceType = 1` (CONTADO_CRC), igual que el Caso 1.

| id | Código | DR CRC | Ingresos (CR fallback) | COGS (DR) | Inventario (CR) |
|---|---|---|---|---|---|
| **1** | **CONTADO_CRC** | **106** | **117** | **119** | **109** |

**Asiento FV:**
```
DR 106 (Caja CRC)                 = totalAmount (neto + IVA)
CR 117 (Ingresos por Ventas)      = quantity × unitPrice
CR 127 (IVA por Pagar CRC)        = taxAmount
```

**Asiento COGS (TrackInventory = true en ProductType 3):**
```
DR 119 (Costo de Ventas)          = quantityBase × lot.unitCost
CR 109 (Inventario de Mercadería) = mismo monto
Descuenta quantityBase del lote de PT
```

---

## 9. Orden de producción — estados

El ciclo de vida de `ProductionOrder` sigue esta máquina de estados controlada vía `PATCH /production-orders/{id}/status`:

```
Borrador
  │  PATCH status → "Pendiente"   (asigna número OP-YYYY-NNNN)
  ▼
Pendiente
  │  PATCH status → "EnProceso"
  ▼
EnProceso
  │  PATCH status → "Completado"  ← aquí se ejecuta CompleteProductionAsync
  ▼
Completado

Cualquier estado antes de Completado puede → "Anulado"
```

Al pasar a `Completado` se puede enviar en el body `lines[].quantityProduced` para reportar cantidades reales distintas a las requeridas. Si no se envía, se usa `quantityRequired`.

---

## 10. Cuentas contables involucradas

Seed en `AccountConfiguration`. Todas con `AllowsMovements = true`.

| idAccount | Código | Nombre | Tipo | Saldo normal |
|---|---|---|---|---|
| 106 | 1.1.06.01 | Caja CRC (₡) | Activo | DR |
| 109 | 1.1.07.01 | Inventario de Mercadería | Activo | DR |
| 110 | 1.1.07.02 | Materias Primas | Activo | DR |
| 111 | 1.1.07.03 | Productos en Proceso | Activo | DR |
| 115 | 5.14.03 | Costos de Producción | Gasto | DR |
| 117 | 4.5.01 | Ingresos por Ventas — Mercadería | Ingreso | CR |
| 119 | 5.15.01 | Costo de Ventas — Mercadería | Gasto | DR |
| 124 | 1.1.09.01 | IVA Acreditable CRC (₡) | Activo | DR |
| 127 | 2.1.04.01 | IVA por Pagar CRC (₡) | Pasivo | CR |

---

## 11. Período fiscal y almacén

- **Período fiscal:** usar `idFiscalPeriod = 4` (Abril 2026), `statusPeriod = "Abierto"`.
- **Almacén:** usar `idWarehouse = 1` (default). Se asigna en la `ProductionOrder`; si no se asigna en la orden se puede enviar en el `PATCH Completado`.

---

## Diagrama de dependencias de configuración

```
ProductType (id=1, TrackInventory=true)  → MP: productos 2, 3, 4, 5
ProductType (id=3, TrackInventory=true)  → PT: producto 6

ProductRecipe (id=1, output=6, qty=1, active)
  ├── ProductRecipeLine: 0.2 KG chile (id=2)
  ├── ProductRecipeLine: 0.05 LTR vinagre (id=3)
  ├── ProductRecipeLine: 0.005 KG sal (id=4)
  └── ProductRecipeLine: 1 UNI frasco (id=5)

ProductUnit  ← crear antes de facturas
  ├── (product=2, unit=3, base=true)
  ├── (product=3, unit=7, base=true)
  ├── (product=4, unit=3, base=true)
  ├── (product=5, unit=1, base=true)
  └── (product=6, unit=1, base=true)

ProductAccount ← crear antes de FC de MP (opcional para usar cuenta 110)
  ├── (product=2, account=110, 100%)
  ├── (product=3, account=110, 100%)
  ├── (product=4, account=110, 100%)
  └── (product=5, account=110, 100%)

PurchaseInvoiceType (id=1, EFECTIVO)
  ├── IdAccountCounterpartCRC   = 106  (Caja CRC)
  └── IdDefaultInventoryAccount = 109  (fallback si no hay ProductAccount)

InventoryAdjustmentType (id=2, PRODUCCION)   ← auto al completar orden
  ├── IdAccountInventoryDefault = 111  (Productos en Proceso — CR consumo MP)
  └── IdAccountCounterpartExit  = 115  (Costos de Producción — DR consumo MP)

SalesInvoiceType (id=1, CONTADO_CRC)
  ├── IdAccountCounterpartCRC = 106  (Caja CRC)
  ├── IdAccountSalesRevenue   = 117  (Ingresos por Ventas)
  ├── IdAccountCOGS           = 119  (Costo de Ventas)
  └── IdAccountInventory      = 109  (Inventario — CR del COGS para PT)

Account seeds requeridos:
  106, 109, 110, 111, 115, 117, 119, 124, 127
```

---

## Migraciones relevantes

| Migración | Contenido relevante para el Caso 2 |
|---|---|
| `InitialCreate` | Tablas, plan de cuentas (incluye 110, 111, 115), tipos de producto, tipos de factura, tipos de ajuste (`PRODUCCION`), recetas de demo (idProductRecipe=1), productos MP (2–5) y PT (6) |
| `AddIvaAccounts` | Cuentas 123–128 (IVA Acreditable y IVA por Pagar) |
