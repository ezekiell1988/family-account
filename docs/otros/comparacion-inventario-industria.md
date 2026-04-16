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
| Múltiples almacenes / ubicaciones | ✅ `Warehouse` table — `InventoryLot.idWarehouse` FK | Estándar |
| Control de series/números de serie | ❌ No existe `SerialNumber` | Ausente |
| Punto de reorden / stock de seguridad | ✅ `Product.ReorderPoint`, `SafetyStock`, `ReorderQuantity`. Endpoint `GET /products/below-reorder-point.json` | Estándar |
| Conteo cíclico (cycle count) | ✅ `POST /inventory-adjustments/cycle-count` — el operador ingresa cantidad física por lote, el system calcula `quantityDelta = físico − libro` automáticamente. Preview sin persistir vía `POST /cycle-count/preview`. Usa tipo `CONTEO` (seed). | Estándar |
| Transferencias entre almacenes | ❌ No hay `InventoryTransfer` | Ausente |
| Estado de calidad / cuarentena | ✅ `InventoryLot.StatusLot` — Disponible | Cuarentena | Bloqueado | Vencido. Solo Disponibles van a FEFO | Avanzado |
| Reserva de stock contra pedido | ❌ Solo fulfillment, sin reserva real en lote | Parcial |
| Análisis ABC / XYZ | ✅ `Product.ClassificationAbc` — A/B/C. Job Hangfire semanal calcula sobre ventas confirmadas de los últimos 90 días | Estándar |
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

**Estado: ✅ Implementado (abril 2026 — M2)**

Cambios realizados:
- **`Warehouse`** — nueva entidad `{ IdWarehouse, NameWarehouse, IsDefault, IsActive }`. Seed: `IdWarehouse=1, NameWarehouse='Principal', IsDefault=true`.
- **`InventoryLot.IdWarehouse INT NOT NULL DEFAULT 1`** — todos los lotes existentes quedan asignados al almacén Principal.
- **`PurchaseInvoice.IdWarehouse INT? FK`** — opcional en la cabecera; si es nulo al confirmar, el sistema busca el almacén marcado como `IsDefault`. Si no hay ninguno, la confirmación falla con error descriptivo.
- **`InventoryAdjustmentConfiguration`** — FK `Restrict` (no se puede eliminar un almacén con lotes asociados).
- **`InventoryLotResponse`** — expone `IdWarehouse` y `NameWarehouse`.
- **`InventoryLotService.GetByProductAsync`** — acepta `int? idWarehouse` opcional para filtrar por almacén.
- **`InventoryLotService.GetSuggestedLotAsync`** — acepta `int? idWarehouse` opcional; el FEFO interno de `SalesInvoiceService` sigue sin filtro de almacén (backward compatible).
- **Endpoints `GET /inventory-lots/by-product/{id}.json?idWarehouse=`** y **`GET /inventory-lots/suggest/{id}.json?idWarehouse=`** actualizados.
- **`WarehouseService / WarehousesModule`** — CRUD completo en `GET|POST|PUT|DELETE /api/v1/warehouses`. Al establecer `IsDefault=true` en un almacén, los demás pierden esa condición automáticamente.
- **Transferencias entre almacenes**: se implementan creando un `InventoryAdjustmentType` de tipo transferencia y dos ajustes delta (-/+) en cada almacén respectivo (sin cambio de modelo adicional).
- **Migración**: `20260405180401_AddWarehouse`.

> **Mejora potencial M2**: agregar `Warehouse { IdWarehouse, NameWarehouse, IsDefault }` y `IdWarehouse INT FK` en `InventoryLot`. Prácticamente sin cambios en la lógica de confirmación: la selección de lote en FEFO ya filtra por `IdProduct`; agregar un filtro adicional `IdWarehouse = ?` es mínimo. Transferencias entre almacenes serían un nuevo `InventoryAdjustmentType` predefinido.

---

### 2.3 Reserva de stock (Soft Reservation)

| Sistema | Mecanismo |
|---|---|
| **SAP B1** | `Quantity Committed` — campo calculado por SO + MO confirmados |
| **Odoo 17** | `Reserved` (Demand) vs `Available` vs `On Hand` — tres layers |
| **NetSuite** | `Quantity Committed` por location |
| **Cin7** | `Allocated` por orden de venta o producción confirmada |
| **family-account (hoy)** | `InventoryLot.QuantityReserved` — se incrementa al asignar un `SalesOrderLineFulfillment` tipo `Stock` y se decrementa al confirmar la factura o eliminar el fulfillment. `QuantityAvailableNet = QuantityAvailable - QuantityReserved` expuesto en el DTO. |

**Estado: ✅ Implementado (abril 2026 — M3)**

Cambios realizados:
- **`InventoryLot`** — nuevo campo `QuantityReserved DECIMAL(18,6) DEFAULT 0` con CHECK constraint `>= 0`.
- **`InventoryLotResponse`** — expone `QuantityReserved` y `QuantityAvailableNet` (calculado).
- **`InventoryLotService.GetSuggestedLotAsync`** — filtra por `QuantityAvailable > QuantityReserved` (stock neto > 0).
- **`SalesInvoiceService.GetFefoLotAsync`** — igual filtro para FEFO en BOM y combos.
- **`SalesOrderService.AddFulfillmentAsync`** — valida stock neto antes de reservar e incrementa `QuantityReserved`.
- **`SalesOrderService.RemoveFulfillmentAsync`** — decrementa `QuantityReserved` al eliminar el fulfillment.
- **`SalesInvoiceService.DeductLotAsync`** — al confirmar factura, decrementa `QuantityAvailable` y libera `QuantityReserved` simultáneamente.
- **Migración**: `20260405172645_AddInventoryLotQuantityReserved`.

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

**Estado: ✅ Implementado (abril 2026 — M5)**

Cambios realizados:
- **`InventoryLot`** — nuevo campo `StatusLot VARCHAR(20) NOT NULL DEFAULT 'Disponible'` con CHECK constraint `IN ('Disponible', 'Cuarentena', 'Bloqueado', 'Vencido')`.
- **`InventoryLotResponse`** — expone `StatusLot`.
- **`InventoryLotService.GetSuggestedLotAsync`** — filtra por `StatusLot == "Disponible"` (solo lotes disponibles se sugieren en FEFO).
- **`SalesInvoiceService.GetFefoLotAsync`** — igual filtro para FEFO en BOM y combos.
- **`IInventoryLotService / InventoryLotService.UpdateStatusAsync`** — cambia el estado de un lote.
- **`PATCH /inventory-lots/{id}/status`** — endpoint protegido con rol Admin, valida valores permitidos.
- **Migración**: `20260405173654_AddInventoryLotStatusAndProductReorder`.

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

**Estado: ✅ Implementado (abril 2026 — M6)**

Sin cambios de modelo ni migraciones — el tipo `CONTEO` (id=1) ya existía en el seed de `InventoryAdjustmentType`.

Cambios realizados:
- **`CycleCountDtos.cs`** — `CycleCountLineRequest`, `CreateCycleCountRequest`, `CycleCountPreviewLineResponse`, `CycleCountPreviewResponse`.
- **`IInventoryAdjustmentService`** — `PreviewCycleCountAsync` + `CreateCycleCountAsync(request, autoConfirm)`.
- **`InventoryAdjustmentService`** — implementación de ambos métodos + validación `ValidateCycleCountLots` (lotes duplicados / inexistentes). El service carga `QuantityAvailable` de cada lote, calcula `delta = físico − libro` y crea un `CreateInventoryAdjustmentRequest` de tipo `CONTEO` solo con las líneas que tienen diferencia (delta ≠ 0).
- **`POST /inventory-adjustments/cycle-count/preview`** — vista previa (sin persistir): devuelve todas las líneas + subconjunto `linesWithDifference`.
- **`POST /inventory-adjustments/cycle-count?autoConfirm=false|true`** — crea el ajuste tipo `CONTEO`; con `autoConfirm=true` lo confirma en el mismo request.

Flujo del operador:
1. Llama a `preview` para ver diferencias antes de comprometerse.
2. Si está conforme, llama a `cycle-count?autoConfirm=true` (o crea en borrador y confirma luego).
3. El ajuste resultante es un `InventoryAdjustment` estándar consultable, anulable y auditable igual que cualquier otro ajuste.

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

**Estado: ✅ Implementado (abril 2026 — M7)**

Cambios realizados:
- **`Product`** — nuevos campos opcionales `ReorderPoint DECIMAL(12,4) NULL`, `SafetyStock DECIMAL(12,4) NULL`, `ReorderQuantity DECIMAL(12,4) NULL`.
- **`ProductResponse`** — expone los tres campos.
- **`CreateProductRequest` / `UpdateProductRequest`** — aceptan los tres campos opcionales con validación `Range >= 0`.
- **`ProductService.GetBelowReorderPointAsync`** — devuelve productos con `StockTotal < ReorderPoint`.
- **`GET /products/below-reorder-point.json`** — endpoint para alertas de reabastecimiento.
- **Migración**: `20260405173654_AddInventoryLotStatusAndProductReorder`.

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

**Estado: ✅ Implementado (abril 2026 — M9)**

Cambios realizados:
- **`Product`** — nuevo campo `ClassificationAbc CHAR(1) NULL` con CHECK constraint `IN ('A','B','C')`.
- **`ProductResponse`** — expone `ClassificationAbc`.
- **`BackgroundJobs/InventoryAbcJobs.cs`** — recalcula semanalmente con ventana de **90 días** de facturas de venta con `StatusInvoice = 'Confirmado'`. Algoritmo Pareto: suma valor por producto, ordena descendente, asigna `A` hasta el 80% acumulado, `B` hasta el 95%, `C` el resto con ventas. Productos sin ventas en el período quedan en `NULL`.
- **`HangfireAppExtensions.cs`** — job `recalculate-inventory-abc` programado los **domingos a las 02:00 AM UTC** (`0 2 * * 0`).
- **Migración**: `AddProductClassificationAbc`.

---

## 3. Tabla resumen de brechas vs. industria

| # | Capacidad faltante | Complejidad de modelo | Impacto operativo | Prioridad sugerida |
|---|---|---|---|---|
| M1 | Métodos de costeo múltiples (FIFO, Specific) | Media | Alto si hay productos de alto valor | Media |
| M2 | Múltiples almacenes / bodegas | Alta | Crítico en cuanto haya 2+ locales | ✅ Implementado |
| M3 | Reserva de stock contra pedido | Baja | Alta — evita doble-asignación | ✅ Implementado |
| M4 | Números de serie | Media | Alto para equipos o garantías | Media |
| M5 | Estado de calidad / cuarentena en lote | Muy baja | Crítico para alimentos (HACCP) | ✅ Implementado |
| M6 | Conteo cíclico como flujo (no solo ajuste manual) | Ningún cambio de modelo | Media | ✅ Implementado |
| M7 | Punto de reorden y stock de seguridad | Muy baja (solo campos) | Alta — operaciones diarias | ✅ Implementado |
| M8 | Opciones configurables con impacto de stock | Media | Alta si hay "extras" vendibles | Media |
| M9 | Clasificación ABC automática | Baja | Baja — analítica | ✅ Implementado |

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

1. ~~**M3 — Reserva de stock**~~: ✅ Implementado (abril 2026). `QuantityReserved` en `InventoryLot` — migración `20260405172645_AddInventoryLotQuantityReserved`.
2. ~~**M5 — Estado de calidad en lote**~~: ✅ Implementado (abril 2026). `StatusLot` en `InventoryLot` + `PATCH /inventory-lots/{id}/status` — migración `20260405173654_AddInventoryLotStatusAndProductReorder`.
3. ~~**M7 — Punto de reorden**~~: ✅ Implementado (abril 2026). Tres campos opcionales en `Product` + `GET /products/below-reorder-point.json` — misma migración.

### Mediano plazo (requieren diseño nuevo)

4. ~~**M2 — Múltiples almacenes**~~: ✅ Implementado (abril 2026). `Warehouse` + `IdWarehouse` en `InventoryLot` y `PurchaseInvoice` — migración `20260405180401_AddWarehouse`.
5. **M8 — Opciones con stock**: ampliar `ProductOptionItem` + nueva tabla `SalesInvoiceLineOption`. Requiere colaboración con UI.

### Largo plazo / condicional

6. **M1 — Costeo FIFO/Specific**: solo si el negocio justifica mayor precisión contable (importaciones, equipos).
7. **M4 — Series**: solo si se venden productos con garantía o número de activo.
6. ~~**M6, M9**~~: mejoras analíticas, no operativas. M6 y M9 ya implementados.
