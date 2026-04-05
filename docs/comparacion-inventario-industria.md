# Comparación de Manejo de Inventario — family-account vs. Industria

> Fecha: abril 2026  
> Base: análisis del modelo EF Core actual (`analisis-inventario-ef.md`) comparado contra prácticas documentadas de ERPs líderes (SAP B1, Odoo 17, QuickBooks Commerce, Cin7, Shopify + WMS, NetSuite).

---

## 1. Mapa de capacidades: estado actual

| Capacidad | family-account (hoy) | Nivel |
|-----------|---------------------|-------|
| Costo promedio ponderado (WAC) | ✅ `Product.AverageCost`, recalculado al confirmar compras/ajustes/producciones | Estándar |
| Trazabilidad por lote (`InventoryLot`) | ✅ Completo — sourceType, costo unitario, vencimiento, stock disponible | Avanzado |
| FEFO / FIFO sugerido en venta | ✅ `GetSuggestedLotAsync` — FEFO primero, FIFO como respaldo | Avanzado |
| Restricción de stock nunca negativo | ✅ CHECK constraint en BD | Buena práctica |
| Unidades de presentación con EAN | ✅ `ProductUnit` — barcode, factor de conversión, precio de venta | Estándar |
| BOM explosion en venta | ✅ `SalesInvoiceLineBomDetail` + `IdProductRecipe` en línea | Avanzado |
| Versioning de recetas (BOM) | ✅ `ProductRecipe.VersionNumber`, soft-delete, snapshot en producción | Avanzado |
| Combos con slots multiproducto | ✅ `ProductComboSlot / ProductComboSlotProduct` + explosión en `ConfirmAsync` | Avanzado |
| Órdenes de producción (MO) | ✅ `ProductionOrder` — Modalidad A (stock) y B (contra pedido) | Estándar |
| Snapshot de receta en producción | ✅ `ProductionSnapshot / ProductionSnapshotLine` — qty calculada vs real | Avanzado |
| Pedidos de venta (`SalesOrder`) | ✅ Con fulfillments, anticipos y estados | Estándar |
| Listas de precios | ✅ `PriceList / PriceListItem` con vigencia por fecha | Estándar |
| Distribución contable por producto | ✅ `ProductAccount` — validación de suma = 100% al confirmar | Específico |
| Concurrencia optimista en costo | ✅ `Product.RowVersion` | Buena práctica |
| Múltiples métodos de costeo | ❌ Solo WAC | Básico |
| Múltiples almacenes / ubicaciones | ❌ No existe `Warehouse` ni `Location` | Ausente |
| Control de series/números de serie | ❌ No existe `SerialNumber` | Ausente |
| Punto de reorden / stock de seguridad | ❌ No hay campos en `Product` | Ausente |
| Conteo cíclico (cycle count) | ❌ No hay flujo diferenciado de conteo | Ausente |
| Transferencias entre almacenes | ❌ No hay `InventoryTransfer` | Ausente |
| Estado de cuarentena / calidad | ❌ `InventoryLot` no tiene estado de calidad | Ausente |
| Reserva de stock contra pedido | ❌ Solo fulfillment, sin reserva real en lote | Parcial |
| Análisis ABC / XYZ | ❌ No hay clasificación automática | Ausente |
| Previsión de demanda | ❌ No hay módulo de forecasting | Ausente |
| Backflushing automático | Parcial — BOM explosion FEFO al confirmar venta | Parcial |
| Opciones configurables con impacto de stock | ❌ `ProductOptionItem.PriceDelta` existe, pero sin `IdProduct` ni qty | Ausente |

---

## 2. Comparativa por dimensión

### 2.1 Métodos de costeo

| ERP / Sistema | Métodos soportados |
|---|---|
| **SAP Business One** | WAC, FIFO, Costo Estándar (Standard Cost), Precio de venta, Precio de la última compra |
| **Odoo 17** | WAC (AVCO), FIFO, Costo estándar |
| **NetSuite** | WAC, FIFO, LIFO (en EEUU), Costo estándar, Costo específico por lote |
| **QuickBooks Commerce** | WAC exclusivamente |
| **Cin7 Core** | WAC, FIFO |
| **family-account (hoy)** | WAC exclusivamente |

**Brecha identificada**: family-account está alineado con QuickBooks Commerce (solo WAC), que es adecuado para PYME. Sin embargo, si se manejan productos importados de alto valor, FIFO o costo específico por lote (Specific Identification) es más preciso fiscalmente.

> **Mejora potencial M1**: agregar `CostingMethod ENUM('WAC','FIFO','SpecificLot')` en `Product` o `ProductType`. Al ser por tipo, todos los productos del mismo tipo usarían el mismo método — consistencia sin complejidad por producto.

---

### 2.2 Múltiples almacenes y ubicaciones

| Sistema | Granularidad |
|---|---|
| **SAP B1** | Almacén → Ubicación (bin) — trazabilidad hasta bin individual |
| **Odoo 17** | Almacén → Área → Pasillo → Estante → Celda (Multi-Location) |
| **NetSuite** | Subsidiary → Location → Bin optativo |
| **Cin7 Core** | Almacén → Ubicación — sincronizado con e-commerce |
| **QuickBooks Commerce** | Almacén sin bins |
| **family-account (hoy)** | Sin almacenes — el stock es por lote a nivel de empresa |

**Brecha identificada**: todos los ERPs comparados, incluso los de entrada como QuickBooks Commerce, manejan al menos múltiples almacenes. Con una sola tienda esto no es crítico, pero en cuanto se agregue una segunda sucursal, bodega de producción separada o bodega de materia prima vs producto terminado, el modelo actual no lo soporta.

> **Mejora potencial M2**: agregar `Warehouse { IdWarehouse, NameWarehouse, IsDefault }` y `IdWarehouse INT FK` en `InventoryLot`. Prácticamente sin cambios en la lógica de confirmación: la selección de lote en FEFO ya filtra por `IdProduct`; agregar un filtro adicional `IdWarehouse = ?` es mínimo. Transferencias entre almacenes serían un nuevo `InventoryAdjustmentType` predefinido.

---

### 2.3 Reserva de stock (Soft Reservation)

| Sistema | Mecanismo |
|---|---|
| **SAP B1** | `Quantity Committed` — campo calculado por SO + MO confirmados |
| **Odoo 17** | `Reserved` (Demand) vs `Available` vs `On Hand` — tres layers |
| **NetSuite** | `Quantity Committed` por location |
| **Cin7** | `Allocated` por orden de venta o producción confirmada |
| **family-account (hoy)** | `SalesOrderLineFulfillment` con `IdInventoryLot` — "asignación" manual, no decrementa `QuantityAvailable` hasta factura confirmada |

**Brecha identificada**: family-account usa fulfillment como pseudo-reserva, pero `QuantityAvailable` en `InventoryLot` no se modifica hasta que se confirma la factura. Si el mismo lote se asigna a dos pedidos diferentes antes de confirmar cualquiera de los dos, el segundo podría quedarse sin stock en el momento de confirmar.

> **Mejora potencial M3**: agregar `QuantityReserved DECIMAL(18,4) DEFAULT 0` en `InventoryLot`. Al asignar un `SalesOrderLineFulfillment` con `FulfillmentType = 'Stock'`, incrementar `QuantityReserved` y exponer `QuantityAvailableNet = QuantityAvailable - QuantityReserved` en las consultas. Al confirmar la factura, decrementar `QuantityAvailable` y `QuantityReserved` simultáneamente (transacción atómica). El CHECK constraint existente de `>= 0` aplicaría a `QuantityAvailable - QuantityReserved`.

---

### 2.4 Números de serie

| Sistema | Soporte |
|---|---|
| **SAP B1** | Sí — cada unidad tiene número de serie propio (`SerialNumber`) vinculado a un lote o línea de factura |
| **Odoo 17** | Sí — los productos con tracking `serial` tienen un lote de cantidad 1 que actúa como número de serie |
| **NetSuite** | Sí — configurable por ítem |
| **Cin7** | Sí — tracking por serial o batch |
| **family-account (hoy)** | No — `InventoryLot` no tiene campo `IsSerial` ni restricción de `QuantityAvailable <= 1` |

**Brecha identificada**: para electrónica, equipos o cualquier activo de alto valor, los serial numbers son necesarios para garantías, reparaciones y auditorías.

> **Mejora potencial M4**: agregar `TrackingType ENUM('None','Lote','Serial')` en `Product` (o en `ProductType.TrackInventory`). Para `Serial`: `InventoryLot.QuantityAvailable <= 1` (CHECK constraint), y el UI expone un campo de número de serie al crear el lote. Sin impacto en el modelo de `InventoryLot` —solo un campo extra y una constraint.

---

### 2.5 Estado de calidad / Cuarentena

| Sistema | Soporte |
|---|---|
| **SAP B1** | `InventoryStatus` por lote: Available, Restricted, Quarantined |
| **Odoo 17** | `Locations` especiales de calidad (Cuarentena, Virtual) bloquean stock automáticamente |
| **NetSuite** | Estado de lote con control de disponibilidad |
| **family-account (hoy)** | `InventoryLot` sin estado de calidad — solo `QuantityAvailable` |

**Brecha identificada**: en industria alimentaria (que parece ser el contexto del proyecto — salsas, perros calientes, bebidas) la trazabilidad de lotes con estado de calidad es un requisito de cumplimiento (BRC, ISO 22000, HACCP). Un lote en cuarentena no debería ser seleccionable en FEFO sin aprobación explícita.

> **Mejora potencial M5**: agregar `StatusLot VARCHAR(20) NOT NULL DEFAULT 'Disponible'` con CHECK `IN ('Disponible','Cuarentena','Bloqueado','Vencido')` en `InventoryLot`. El método `GetSuggestedLotAsync` ya filtra por `ExpirationDate >= referenceDate`; agregar filtro `StatusLot = 'Disponible'` es una línea extra. Los ajustes de cuarentena serían operaciones de cambio de estado, no decremento de stock.

---

### 2.6 Conteo cíclico (Cycle Count)

| Sistema | Soporte |
|---|---|
| **SAP B1** | `Inventory Counting` — genera documento de conteo, compara vs libro, genera ajuste |
| **Odoo 17** | `Physical Inventory` — diferencia automática → `Adjustments` por línea |
| **NetSuite** | `Cycle Count` con programación por zona o ítem |
| **Cin7** | `Stock Take` — exporta PDF/CSV con cantidades en libro para contar físicamente |
| **family-account (hoy)** | `InventoryAdjustment` tipo `Ajuste` puede cumplir este rol manualmente, pero no hay flujo de "conteo vs libro" |

**Brecha identificada**: los `InventoryAdjustment` actuales son ajustes directos de delta. No existe el concepto de "contar físicamente, comparar con lo que dice el sistema y generar el ajuste por la diferencia". Esto es más un flujo de UX que un problema de modelo.

> **Mejora potencial M6**: crear un nuevo `InventoryAdjustmentType` predefinido tipo `'Conteo'` donde la UI muestra la cantidad en libro (`InventoryLot.QuantityAvailable`) y el operador ingresa la cantidad física contada. El service calcula el delta automáticamente: `quantityDelta = contadoFísico - cantidadLibro`. Sin cambios de modelo — solo lógica de presentación y un endpoint `POST /inventory-adjustments/cycle-count`.

---

### 2.7 Punto de reorden y stock de seguridad

| Sistema | Soporte |
|---|---|
| **SAP B1** | `MinimumInventory` (safety stock) + `ReorderPoint` + `MaximumInventory` en ficha de artículo |
| **Odoo 17** | `Reordering Rules` — mínimo, máximo, cantidad de compra fija, por demanda |
| **NetSuite** | `Preferred Stock Level`, `Reorder Point`, `Safety Stock` |
| **QuickBooks Commerce** | Reorder Level, Reorder Quantity por producto |
| **family-account (hoy)** | Ningún campo de este tipo en `Product` |

**Brecha identificada**: no hay señal de cuándo reabastecer. El operador debe revisar el stock manualmente y decidir cuándo comprar.

> **Mejora potencial M7**: agregar en `Product` tres campos opcionales:
> ```
> ReorderPoint      DECIMAL(12,4) NULL   ← stock mínimo para disparar compra
> SafetyStock       DECIMAL(12,4) NULL   ← stock de reserva que no se toca
> ReorderQuantity   DECIMAL(12,4) NULL   ← cantidad sugerida a pedir
> ```
> Con estos datos, un endpoint `GET /products/below-reorder-point` devuelve el listado de productos con `StockTotal < ReorderPoint`, que puede alimentar una pantalla de "alertas de reabastecimiento" en el dashboard o generar compras sugeridas automáticas.

---

### 2.8 Opciones configurables con impacto de stock

| Sistema | Soporte |
|---|---|
| **SAP B1** | No nativo — se resuelve con variantes de ítem o kits |
| **Odoo 17** | `Product Variants` con `Attribute + Value` — cada combinación puede tener su propia referencia y stock |
| **Shopify + WMS** | `Variants` (color, talla…) con stock independiente por variante |
| **family-account (hoy)** | `ProductOptionGroup / ProductOptionItem` — solo `PriceDelta`, sin `IdProduct` ni movimiento de stock |

**Brecha identificada** (confirmada en el análisis original como Q8-5): una opción como "con extra queso" o "agrandado" debería descontar stock del ingrediente adicional al confirmar la venta, pero actualmente `ProductOptionItem` solo tiene `PriceDelta`.

> **Mejora potencial M8**: agregar en `ProductOptionItem`:
> ```
> IdProductExtra     INT? FK → Product    ← insumo extra opcional
> QuantityExtra      DECIMAL(12,4) NULL   ← cantidad a consumir del insumo
> ```
> En `ConfirmAsync`, si la `SalesInvoiceLine` tiene opciones seleccionadas con `IdProductExtra IS NOT NULL`, generar `BomDetail` adicionales por cada opción. La selección de qué opciones eligió el cliente en cada línea requiere una tabla de vínculo `SalesInvoiceLineOption`.

---

### 2.9 Análisis ABC y clasificación de inventario

| Sistema | Soporte |
|---|---|
| **SAP B1** | `ABC Analysis` basado en valor de ventas o movimientos — reportes estándar |
| **Odoo 17** | No nativo — vía módulo de inventario avanzado o reportes BI |
| **NetSuite** | Reportes de rotación, días de inventario, análisis ABC configurable |
| **family-account (hoy)** | No existe ningún campo de clasificación en `Product` |

> **Mejora potencial M9** (baja prioridad): agregar `ClassificationAbc CHAR(1) NULL CHECK IN ('A','B','C')` en `Product`, calculado periódicamente desde un job de Hangfire según valor de ventas en los últimos N días. Útil para priorizar conteos cíclicos (M6) y políticas de reorden (M7) diferenciadas por clase.

---

## 3. Tabla resumen de brechas vs. industria

| # | Capacidad faltante | Complejidad de modelo | Impacto operativo | Prioridad sugerida |
|---|---|---|---|---|
| M1 | Métodos de costeo múltiples (FIFO, Specific) | Media | Alto si hay productos de alto valor | Media |
| M2 | Múltiples almacenes / bodegas | Alta | Crítico en cuanto haya 2+ locales | Alta |
| M3 | Reserva de stock contra pedido | Baja | Alta — evita doble-asignación | Alta |
| M4 | Números de serie | Media | Alto para equipos o garantías | Media |
| M5 | Estado de calidad / cuarentena en lote | Muy baja | Crítico para alimentos (HACCP) | Alta |
| M6 | Conteo cíclico como flujo (no solo ajuste manual) | Ningún cambio de modelo | Media | Baja |
| M7 | Punto de reorden y stock de seguridad | Muy baja (solo campos) | Alta — operaciones diarias | Media |
| M8 | Opciones configurables con impacto de stock | Media | Alta si hay "extras" vendibles | Media |
| M9 | Clasificación ABC automática | Baja | Baja — analítica | Baja |

---

## 4. Fortalezas de family-account sobre ERPs comparados

Estas capacidades están implementadas de forma más ro­bus­ta que en sistemas de entrada/medio rango:

| Ventaja | Detalle vs. industria |
|---|---|
| **Snapshot de receta en producción** | Odoo y QB Commerce no guardan una copia inmutable de la receta usada al producir. family-account guarda `ProductionSnapshot` con qty calculada vs real — auditoría superior. |
| **CHECK constraints explícitos en BD** | Muchos ERPs delegan toda validación a la capa de aplicación. family-account tiene `CK_inventoryLot_quantityAvailable >= 0`, `CK_inventoryLot_sourceType`, estado de ajuste, etc. — la BD rechaza datos inválidos independientemente de la app. |
| **BomDetail en venta con trazabilidad por lote** | SAP B1 hace backflushing pero no siempre guarda el lote específico consumido por ingrediente. family-account guarda `SalesInvoiceLineBomDetail` con `IdInventoryLot` y `UnitCost` snapshot — trazabilidad hacia atrás completa. |
| **Versioning de recetas con trazabilidad** | QB Commerce y Cin7 no manejan versiones de BOM. Odoo sí, pero no hace snapshot al producir. family-account liga `ProductionSnapshot.IdProductRecipe` a la versión exacta usada. |
| **Concurrencia optimista en AverageCost** | Muy pocos ERPs de PYME manejan race conditions en el recálculo del costo promedio. El `RowVersion` de EF Core ya previene corrupción silenciosa. |
| **Distribución contable % por producto** | Característica muy específica de contabilidad analítica que no existe en ERPs de entrada. Permite asientos de gasto desagregados por cuenta y centro de costo automáticamente. |

---

## 5. Recomendación de roadmap

### Corto plazo (impacto con mínimos cambios de modelo)

1. **M3 — Reserva de stock**: agregar `QuantityReserved` en `InventoryLot`. Sin migraciones de datos. Alta ganancia operativa.
2. **M5 — Estado de calidad en lote**: agregar `StatusLot` con CHECK constraint. Una migración simple. Crítico para alimentos.
3. **M7 — Punto de reorden**: tres campos opcionales en `Product`. No requiere cambios en lógica de confirmación.

### Mediano plazo (requieren diseño nuevo)

4. **M2 — Múltiples almacenes**: agregar `Warehouse` + `IdWarehouse` en `InventoryLot`. Requiere revisar todos los servicios que consultan stock.
5. **M8 — Opciones con stock**: ampliar `ProductOptionItem` + nueva tabla `SalesInvoiceLineOption`. Requiere colaboración con UI.

### Largo plazo / condicional

6. **M1 — Costeo FIFO/Specific**: solo si el negocio justifica mayor precisión contable (importaciones, equipos).
7. **M4 — Series**: solo si se venden productos con garantía o número de activo.
8. **M6, M9**: mejoras analíticas, no operativas.
