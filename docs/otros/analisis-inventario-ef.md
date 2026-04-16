# Análisis de Manejo de Inventario — EF Core Model

> Revisión basada en las configuraciones EF y entidades de dominio del módulo de inventario y productos.
> Fecha: abril 2026

---

## 1. Mapa del modelo de inventario

```
Product ──────────────┐
  ├── ProductType      │
  ├── UnitOfMeasure    │
  ├── ProductUnit      │ (presentaciones + EAN)
  ├── ProductAccount   │ (distribución contable %)
  ├── ProductCategory  │ (N:M vía ProductProductCategory)
  ├── ProductRecipe    │ (BOM / recetas)
  ├── ProductOptionGroup / ProductOptionItem (opciones configurables)
  └── ProductComboSlot / ProductComboSlotProduct (combos)

InventoryLot ──── stock en unidad base por lote
  ├── originado por: PurchaseInvoice  (sourceType = 'Compra')
  ├── originado por: InventoryAdjustment (sourceType = 'Ajuste' | 'Producción')
  └── consumido por: SalesInvoiceLine (FK IdInventoryLot → RESTRICT)

InventoryAdjustment
  └── InventoryAdjustmentLine ──── referencia InventoryLot (delta +/-)
  └── InventoryAdjustmentEntry (N:M con AccountingEntry)
```

---

## 2. Fortalezas identificadas

| # | Aspecto | Detalle |
|---|---------|---------|
| F1 | **Stock nunca negativo** | `CK_inventoryLot_quantityAvailable >= 0` en BD garantiza integridad. |
| F2 | **CHECK en sourceType** | `CK_inventoryLot_sourceType IN ('Compra','Producción','Ajuste')` — dominio cerrado. |
| F3 | **Máquina de estados** | `statusAdjustment` y `statusInvoice` con CHECK constraints correctamente definidas. |
| F4 | **InventoryAdjustmentEntry N:M** | Permite vincular múltiples asientos (confirmación + reversión) sin mutar asientos confirmados. Diseño de auditoría sólido. |
| F5 | **Snapshot de costo en venta** | `SalesInvoiceLine.UnitCost` captura el costo del lote al confirmar → COGS calculable históricamente. |
| F6 | **QuantityBase almacenada** | Guardar la cantidad en unidad base en la línea de factura evita recalcular si cambia el factor de conversión. |
| F7 | **ProductUnit con EAN** | Índice único filtrado en `codeBarcode` → escaneo de barcode correcto. |
| F8 | **Seed data en catálogos** | `ProductType` y `InventoryAdjustmentType` con `HasData` — entornos nuevos funcionales desde cero. |
| F9 | **Índices filtrados** | Uso de `HasFilter("[idX] IS NOT NULL")` en FKs opcionales → eficiencia correcta. |

---

## 3. Problemas y observaciones

### 🔴 Críticos (pueden corromper datos)

---

#### P1 — Suma de `ProductAccount.PercentageAccount` no validada en BD ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se agregó validación en `SalesInvoiceService.ConfirmAsync`. Antes de usar la distribución contable de un producto, se verifica que la suma de `PercentageAccount` sea exactamente 100. Si no, el método retorna error `(false, "La distribución contable del producto X suma Y% en lugar de 100%...", null)`.

- **Ubicación**: `ProductAccountConfiguration`, comentario del campo.
- **Problema**: La regla "la suma por `idProduct` debe ser 100.00" solo existe en el comentario SQL. No hay CHECK constraint ni trigger que lo imponga. Si la lógica de aplicación falla, los asientos contables se generarían mal (desequilibrados).
- **Opciones**:
  - CHECK constraint a nivel de BD (difícil con sumas entre filas — requiere trigger o procedimiento).
  - **Recomendado**: validar en el service antes de confirmar el ajuste/factura, con error explícito (`400 Bad Request`).
  - Agregar una columna calculada o vista que exponga el total por producto para facilitar la detección.

**❓ Pregunta**: ¿Un producto puede tener distribución contable incompleta (< 100%) y aún así usarse en facturas de venta? ¿O la regla del 100% solo aplica al confirmar?

---

#### P2 — `InventoryAdjustmentLine.UnitCostNew` requerido cuando `quantityDelta > 0` no está en BD ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se agregó `CK_inventoryAdjustmentLine_unitCostNew` en `InventoryAdjustmentLineConfiguration` y aplicado en migración `20260405011042_InventoryConstraints_P1P2P3`.

- **Ubicación**: `InventoryAdjustmentLineConfiguration`, comentario del campo.
- **Problema**: No existe `CHECK (quantityDelta <= 0 OR unitCostNew IS NOT NULL)`. Una línea de entrada sin costo dejaría lotes con `unitCost = 0`, corrompiendo el costo promedio ponderado.

---

#### P3 — `ProductUnit.IsBase = true` sin índice único parcial ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se agregó `UQ_productUnit_idProduct_isBase` (`WHERE isBase = 1`) en `ProductUnitConfiguration` y aplicado en migración `20260405011042_InventoryConstraints_P1P2P3`.

- **Ubicación**: `ProductUnitConfiguration`.
- **Problema**: El comentario dice "exactamente 1 registro por producto marca la unidad base", pero no existe un índice único filtrado `UNIQUE (idProduct) WHERE isBase = 1`. Podría existir más de una unidad base por producto.

---

### 🟠 Importantes (gaps de diseño o comportamiento indefinido)

---

#### P4 — Producción (`InventoryAdjustment` tipo PRODUCCION) no referencia la `ProductRecipe` ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se crearon las entidades `ProductionSnapshot` y `ProductionSnapshotLine` con migración `20260405012446_ProductionSnapshot`. La producción siempre debe referenciar una receta. El snapshot guarda una copia inmutable de la receta usada al confirmar, con cantidad calculada (teórica) y real tanto en la cabecera (producto final) como en cada línea de insumo. Los insumos extra no previstos en la receta se registran con `idProductRecipeLine = NULL`.

- **Problema**: Al confirmar una producción no había FK de `InventoryAdjustment` → `ProductRecipe`. No se podía saber qué receta se usó, con qué versión, ni auditar discrepancias entre cantidades teóricas (receta) y reales (líneas del ajuste).
- **Solución aplicada**:
  - `ProductionSnapshot` (1:1 con `InventoryAdjustment`): guarda `IdProductRecipe`, `QuantityCalculated` (copia de `ProductRecipe.QuantityOutput`) y `QuantityReal` (producción física).
  - `ProductionSnapshotLine` (por insumo): `IdProductRecipeLine` (nullable para insumos extra), `IdProductInput` (snapshot directo), `QuantityCalculated` y `QuantityReal`.

---

#### P5 — Selección de lote en venta es completamente manual ✅ IMPLEMENTADO

> **Resuelto** (abril 2026):
> - **Sugerencia FEFO**: nuevo método `GetSuggestedLotAsync` en `InventoryLotService`. Filtra lotes con `QuantityAvailable > 0` y `ExpirationDate >= referenceDate` (o sin fecha); ordena primero por vencimiento ASC (FEFO), luego por `IdInventoryLot` (FIFO de respaldo). Expuesto en `GET /inventory-lots/suggest/{idProduct}.json?date=yyyy-MM-dd`.
> - **Validación en confirmación**: en `SalesInvoiceService.ConfirmAsync`, antes de descontar el lote se verifica `lot.ExpirationDate < invoice.DateInvoice`. Si el lote está vencido se retorna error `400` con mensaje explícito.
> - **Selección manual**: el operador puede asignar cualquier lote válido en `SalesInvoiceLine.IdInventoryLot`; la sugerencia es solo orientativa.

- **Situación**: `SalesInvoiceLine.IdInventoryLot` lo fija el usuario/app. No hay lógica FEFO (First Expiry First Out) ni FIFO automático en el modelo.
- **Riesgo**: Se pueden seleccionar lotes vencidos o ignorar lotes próximos a vencer.
- **Opciones**:
  - El service de venta puede sugerir/auto-seleccionar el lote con `ExpirationDate` más cercano.
  - Agregar una restricción de negocio que no permita seleccionar un lote con `ExpirationDate < DateInvoice`.

**❓ Pregunta**: ¿La selección de lote es siempre manual por el operador, o se espera que el sistema sugiera / auto-asigne el lote FEFO?

---

#### P6 — `SalesInvoiceLine.IdInventoryLot` nullable cuando `IdProduct IS NOT NULL` ✅ IMPLEMENTADO

> **Resuelto** (5-abr-2026): Se agregó el campo `IsNonProductLine` (`bit NOT NULL DEFAULT 0`) en `SalesInvoiceLine`.
> - `IsNonProductLine = false` (default): línea de producto con stock. `IdInventoryLot` es **obligatorio** — garantizado por CHECK constraint `CK_salesInvoiceLine_lot_required (isNonProductLine = 1 OR idInventoryLot IS NOT NULL)` en BD y por validación explícita en `SalesInvoiceService.ConfirmAsync` con mensaje descriptivo.
> - `IsNonProductLine = true`: línea de flete/servicio/gasto. `IdInventoryLot` puede ser NULL; no genera COGS ni decrementa inventario al confirmar.
> - Migración aplicada: `AddSalesInvoiceLineIsNonProductLine`.

---

#### P7 — No hay campo `IsInventoried` / `TracksInventory` en `Product` ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se agregó `TrackInventory` (`bit NOT NULL DEFAULT 1`) directamente en `ProductType` en lugar de en `Product`. Todos los productos heredan el indicador a través de su tipo. Se añadió además el tipo **"Servicios"** (id=5) con `TrackInventory = false` para cubrir servicios, mano de obra y conceptos sin stock físico. Migración aplicada: `20260405015721_AddProductTypeTrackInventory`.
>
> - **Tipos inventariables** (`TrackInventory = true`): Materia Prima, Producto en Proceso, Producto Terminado, Reventa.
> - **Tipos no inventariables** (`TrackInventory = false`): Servicios.
> - Para saber si un producto lleva stock: `product.ProductType.TrackInventory`.

- **Problema**: Actualmente no hay indicador explícito de si un producto lleva stock. Se infiere por `ProductType` (¿Materia Prima/Terminado = sí, Reventa = sí, Servicio = no?). Pero no existe tipo "Servicio" en el catálogo, y la lógica de "si tiene producto → debe tener lote" en venta es ambigua.
- **Opciones**:
  - Agregar `TrackInventory BOOL NOT NULL DEFAULT true` en `Product`.
  - O documentar formalmente qué `ProductType` lleva stock y cuál no.

**❓ Pregunta**: ¿Puede existir un producto sin inventario (e.g., servicio de entrega, mano de obra)? ¿O todos los productos del catálogo llevan stock?

---

#### P8 — Combos y recetas sin impacto de inventario definido en venta ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se implementaron las opciones **2B + 3A**. Se agrega la tabla `SalesInvoiceLineBomDetail` y el campo `IdProductRecipe` en `SalesInvoiceLine`. `ConfirmAsync` detecta automáticamente si una línea de producto requiere explosión BOM o de combo, y genera los movimientos de inventario correspondientes vía FEFO. Migración aplicada: `20260405023535_AddSalesInvoiceLineBomDetail`.
>
> **Escenario 1 — Producto terminado con lote propio (ej: botella de chile):** sin cambios. `SalesInvoiceLine.IdInventoryLot` apunta al lote; se descuenta directamente al confirmar.
>
> **Escenario 2 — BOM explosion (Opción 2B, ej: perro caliente):** si el producto tiene `ProductRecipe` activa, `ConfirmAsync` explota la receta × `lineQtyBase`, asigna FEFO por insumo y crea un `SalesInvoiceLineBomDetail` por cada línea de receta consumida. `IdProductRecipe` en la línea de factura guarda snapshot de qué receta se usó.
>
> **Escenario 3 — Combo (Opción 3A, ej: 2 perros + bebida):** si el producto tiene `IsCombo = true`, `ConfirmAsync` itera `ProductComboSlots`. Por cada slot: si el producto del slot tiene receta → BOM explosion; si es reventa/terminado → FEFO directo. Todo el movimiento queda en `SalesInvoiceLineBomDetail` con `IdProductComboSlot` populado.
>
> **Anulación:** `CancelAsync` itera primero los `BomDetails` de cada línea y revierte cada lote allí referenciado, luego revierte el lote directo si existe (Escenario 1). `AverageCost` se recalcula en ambos casos.

Análisis detallado revisado en abril 2026 a partir de tres escenarios reales de operación.

---

##### Escenario 1 — Producto terminado con lote propio (ej: botella de chile)

**Flujo**: Compra MP → `InventoryAdjustment` tipo PRODUCCION (con `ProductionSnapshot`) → crea lote del producto terminado → venta descuenta ese lote.

**Estado actual**: **ya funciona completamente**. La `SalesInvoiceLine` apunta al lote de la botella, `QuantityAvailable` baja al confirmar. No requiere cambios de modelo.

---

##### Escenario 2 — Venta con explosión de receta en tiempo real (ej: perro caliente)

El producto no tiene lote propio porque se arma al vender. Se necesita descontar las MPs directamente desde la venta.

**Opción 2A — Pre-producción (modelo actual sin cambios)**

Antes de vender, un operador crea un `InventoryAdjustment` tipo PRODUCCION que consume las MPs y genera un lote del "perro caliente". Luego la venta descuenta ese lote normalmente.

| | |
|---|---|
| ✅ Ventajas | Sin cambios de modelo. Trazabilidad completa vía `ProductionSnapshot`. |
| ❌ Desventajas | Operativamente incómodo: hay que producir antes de vender. No aplica a locales que arman al pedido. |

**Opción 2B — BOM Explosion en `ConfirmAsync` (nuevo)**

Al confirmar la factura, si el producto tiene `ProductRecipe` activa, `ConfirmAsync` explota la receta y descuenta un lote por cada insumo en lugar de descontar un lote del producto terminado.

Cambios de modelo requeridos:

```
SalesInvoiceLine
  ├── + IdProductRecipe INT? FK   ← snapshot de qué receta se usó al confirmar
  └── SalesInvoiceLineBomDetail   ← [NUEVA TABLA]
        ├── IdSalesInvoiceLineBomDetail  PK autoincremental
        ├── IdSalesInvoiceLine           FK (RESTRICT)
        ├── IdProductRecipeLine INT?     FK nullable (NULL = insumo extra no previsto en receta)
        ├── IdProduct INT                FK (snapshot del insumo consumido)
        ├── IdInventoryLot INT           FK (lote específico descontado — RESTRICT)
        ├── QuantityConsumed DECIMAL     (cantidad real en unidad base)
        └── UnitCost DECIMAL             (snapshot del costo del lote al confirmar)
```

Lógica en `ConfirmAsync` (pseudocódigo):
1. Si `product` tiene `ProductRecipe` activa → no busca `IdInventoryLot` en la línea.
2. Calcula insumos = `recipeLine.QuantityInput × line.QuantityBase`.
3. Por cada insumo: llama a `GetSuggestedLotAsync` (FEFO ya implementado) para seleccionar lote.
4. Crea un `SalesInvoiceLineBomDetail` por insumo y descuenta `QuantityAvailable`.
5. Graba `IdProductRecipe` en la línea como snapshot.

| | |
|---|---|
| ✅ Ventajas | Operación fluida (vender sin pre-producir). BomDetail ofrece trazabilidad exacta por lote e insumo. Compatible con FEFO ya implementado. |
| ❌ Desventajas | Requiere nueva tabla + lógica en `ConfirmAsync`. El inventario físico fin de día es de reconciliación, no de producción. |

---

##### Escenario 3 — Combo con recetas anidadas (ej: 2 perros calientes + bebida)

El combo (`IsCombo = true`) tiene `ProductComboSlots`:
- Slot 1: "Perro caliente" × 2 → tiene `ProductRecipe` → BOM explosion por los 2 perros.
- Slot 2: "Bebida" × 1 → es reventa → tiene lote propio.

El modelo actual (`SalesInvoiceLine` con un único `IdInventoryLot`) no cubre esto.

**Opción 3A — Explosión total en `BomDetail` al confirmar (recomendada junto con 2B)**

El operador registra **una sola** `SalesInvoiceLine` del combo. Al confirmar, `ConfirmAsync`:
1. Detecta `product.IsCombo = true`.
2. Itera `ProductComboSlots × Quantity` del slot:
   - Slot con receta → BOM explosion → genera `SalesInvoiceLineBomDetail` por insumo.
   - Slot sin receta (reventa) → descuenta lote directo → genera `SalesInvoiceLineBomDetail` apuntando al lote de la bebida.
3. Todo el movimiento de stock queda en `BomDetail`. La línea del combo tiene `IdInventoryLot = NULL`.

```
SalesInvoiceLine (combo "2 perros + bebida", precio total)
  ├── IdInventoryLot = NULL
  ├── IdProductRecipe = NULL  (el combo en sí no tiene receta propia)
  └── SalesInvoiceLineBomDetail[]
        ├── {pan x2,      lote pan-001,      qty=2}
        ├── {salchicha x2, lote sal-003,     qty=2}
        ├── {queso x2,    lote que-010,      qty=0.2L}
        ├── {mayo x2,     lote may-002,      qty=0.02L}
        └── {bebida x1,   lote beb-007,      qty=1}
```

| | |
|---|---|
| ✅ Ventajas | UI simple: el operador ve 1 línea = 1 combo. Stock correcto sin trabajo manual. Compatible con 2B. |
| ❌ Desventajas | `BomDetail` mezcla insumos de receta y productos directos — distinguir ambos casos con `IdProductRecipeLine` nullable. |

**Opción 3B — Líneas hijas explícitas en la factura**

Agregar `IdSalesInvoiceLineParent INT? FK` en `SalesInvoiceLine`. Al capturar la factura (no al confirmar), la UI genera líneas hijas automáticas por cada slot, y cada línea hija sigue el flujo del Escenario 1 o 2B según corresponda.

```
SalesInvoiceLine  (combo — IsComboParent, precio total)
  ├── SalesInvoiceLine hijo  (perro caliente ×2 → BOM explosion via 2B)
  └── SalesInvoiceLine hijo  (bebida ×1 → lote directo)
```

| | |
|---|---|
| ✅ Ventajas | Cada línea hija es autónoma y sigue el mismo código de los Escenarios 1/2. Factura legible con detalle visible. |
| ❌ Desventajas | Requiere `IdSalesInvoiceLineParent` en el modelo y mayor complejidad en UI y `ConfirmAsync`. |

---

##### Tabla comparativa de opciones

| | 2A (pre-prod) | 2B (BOM en confirm) | 3A (BomDetail, recom.) | 3B (líneas hijas) |
|---|:---:|:---:|:---:|:---:|
| Cambios de modelo | Ninguno | `SalesInvoiceLineBomDetail` + FK receta en línea | Igual que 2B | `IdSalesInvoiceLineParent` + 2B |
| Complejidad operativa | Alta | Baja | Baja | Media |
| Trazabilidad por lote e insumo | Vía ProductionSnapshot | Excelente | Excelente | Excelente |
| Escenario 1 (botella) | ✅ | ✅ | ✅ | ✅ |
| Escenario 2 (perro caliente) | ✅ Con fricción | ✅ | ✅ | ✅ |
| Escenario 3 (combo anidado) | ❌ | Parcial | ✅ | ✅ |
| FEFO automático | Manual | ✅ Ya implementado | ✅ | ✅ |

**Recomendación preliminar**: implementar **2B + 3A** — una sola tabla nueva `SalesInvoiceLineBomDetail` cubre los tres escenarios con la menor complejidad operativa.

---

**❓ Preguntas para decidir el camino:**

| # | Pregunta | Impacto |
|---|----------|---------|
| Q8-1 | ¿Los productos con receta se pre-producen siempre antes de vender (Opción 2A), o se venden directamente descontando insumos en tiempo real (Opción 2B)? | Determina si se necesita `SalesInvoiceLineBomDetail` |
| Q8-2 | Para los combos, ¿el operador necesita ver el desglose de insumos/slots en la UI de la factura antes de confirmar, o es suficiente confirmar y que el sistema explote automáticamente? | Determina 3A vs 3B |
| Q8-3 | Cuando un slot de combo tiene receta (ej: perro caliente), ¿el sistema debe auto-asignar los lotes FEFO, o el operador debe poder elegir manualmente el lote de cada insumo? | Determina si se necesita pantalla intermedia de revisión de BomDetail |
| Q8-4 | ¿Puede un combo tener slots donde el producto del slot **también es un combo** (combos de combos), o la anidación máxima es combo → producto simple / producto con receta? | Determina si se necesita recursión en la explosión |
| Q8-5 | `ProductOptionItem` tiene `PriceDelta` pero no `IdProduct`. ¿Las opciones (ej: "con extra queso") deben mover stock de algún insumo, o solo afectan el precio? | Determina si `ProductOptionItem` necesita `IdProduct` + `QuantityExtra` |

---

#### P9 — `PurchaseInvoice.ProviderName` es texto libre, sin FK a `Contact` ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se agregó `IdContact INT?` en `PurchaseInvoice` con FK → `Contact` (RESTRICT). `ProviderName` se mantiene como snapshot inmutable del nombre al momento de la factura.
> - Si se envía `IdContact` → se toma el nombre del catálogo y se guarda en `ProviderName`.
> - Si no se envía `IdContact` pero sí `ProviderName` → se aplica `GetOrCreateAsync("PRO")` para crear o reutilizar un contacto de tipo Proveedor automáticamente.
> - Si no se envía ninguno → error `400` con mensaje descriptivo.
> - **Seed**: 1 contacto genérico `{ CodeContact="SIN_PRO_CLI", Name="Sin proveedor / Cliente" }` con ambos tipos (CLI + PRO) para usarse como valor neutro.
> - Migración aplicada: `20260405153226_AddPurchaseInvoiceIdContact`.

---

#### P10 — `Product.AverageCost` sin token de concurrencia optimista ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se agregó la propiedad `RowVersion byte[]` en `Product` y se configuró con `.IsRowVersion()` en `ProductConfiguration`. SQL Server gestiona automáticamente la columna `rowversion`; EF Core lanzará `DbUpdateConcurrencyException` si dos confirmaciones paralelas intentan modificar `AverageCost` del mismo producto simultáneamente. Migración aplicada: `20260405153714_AddProductRowVersion`.

- **Problema**: `AverageCost` se recalcula al confirmar compras y ajustes. Si dos transacciones se confirman en paralelo sobre el mismo producto, hay riesgo de race condition con el cálculo del promedio ponderado.
- **Recomendado**: Agregar `RowVersion` (tipo `byte[]`) como concurrency token en `Product`:
  ```csharp
  builder.Property(p => p.RowVersion)
      .IsRowVersion();
  ```

---

### 🟡 Observaciones menores

---

#### ~~O1 — `InventoryAdjustmentType` seed usa IDs de cuentas hardcodeados (109-115)~~ ✅ NO APLICA

> **No aplica con EF `HasData`**: EF ordena los inserts automáticamente (primero `Account`, luego `InventoryAdjustmentType`), eliminando el riesgo de violación de FK durante el seed. Cualquier cambio al seed de `Account` genera una migración visible que haría fallar `database update` de forma ruidosa vía FK RESTRICT. El escenario "silencioso" descrito solo aplica a scripts SQL manuales o seeds en proyectos separados — ninguno de los dos es el caso aquí (todo en un solo `AppDbContext`).

~~Los seeds referencian `IdAccount = 109, 110, 111, 113, 114, 115` directamente. Si el seed de cuentas cambia o se ejecuta en distinto orden, estos ajustes quedan con FKs inválidas silenciosamente. Considerar verificar en tests de integración que estas cuentas existan.~~

---

#### O2 — `ProductAccount.IdCostCenter` SetNull en cascade ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): `CostCenterService.DeleteAsync` ya no ejecuta un `DELETE` físico. Ahora hace soft-delete: busca la entidad y setea `IsActive = false`. El endpoint `DELETE /cost-centers/{id}` retorna `204 No Content` si el centro existía, o `404 Not Found` si ya no existe — sin riesgo de FK violation ni de nullificar `ProductAccount.IdCostCenter` silenciosamente. `IsActive` ya estaba en el modelo y en `CostCenterConfiguration` (`HasDefaultValue(true)`), por lo que **no se requirió migración**.

Si se elimina un centro de costo, los `ProductAccount` quedan con `idCostCenter = NULL` sin alerta. El asiento contable generado omitirá el centro de costo sin error visible. Considerar un soft-delete en `CostCenter` en lugar de permitir delete físico.

---

#### O3 — `ProductRecipe` sin versioning ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se agregó `VersionNumber INT NOT NULL DEFAULT 1` en `ProductRecipe` con índice único `UQ_productRecipe_idProductOutput_versionNumber`. El comportamiento de `UpdateAsync` cambió: en lugar de mutar la receta existente, marca la versión actual como `IsActive = false` y crea una nueva fila con `VersionNumber = prev + 1`. `DeleteAsync` pasó a soft-delete (`IsActive = false`) para no romper los `ProductionSnapshot` que referencian versiones anteriores. Migración aplicada: `20260405155554_AddProductRecipeVersionNumber`.
>
> - **Auditoría**: `ProductionSnapshot.IdProductRecipe` apunta a la fila exacta (versión específica), por lo que la trazabilidad receta→producción queda completa sin datos adicionales.
> - **Query receta aktiva**: `WHERE IdProductOutput = X AND IsActive = 1`.
> - **Historial**: todas las versiones anteriores permanecen en la tabla con `IsActive = false`; crece solo una fila por cada vez que se modifica la receta (muy inferior al snapshot que crece `modificaciones × ingredientes`).

---

#### O4 — `UnitOfMeasure.TypeUnit` sin CHECK constraint ni catálogo ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se creó la entidad `UnitType` (catálogo de sistema sin CRUD) con seed de 4 tipos: Unidad (1), Volumen (2), Masa (3), Longitud (4). `UnitOfMeasure.TypeUnit string` fue reemplazado por `IdUnitType int FK → UnitType (RESTRICT)`. Se eliminó el CHECK constraint y la columna `typeUnit`. Migración aplicada: `20260405160710_AddUnitType`.
>
> - **DTOs**: `TypeUnit string` → `IdUnitType int` en requests; response incluye `IdUnitType` + `NameUnitType` proyectados desde la navegación.
> - **Catálogo**: `GET /unit-types` no existe (catálogo de sistema); los valores se exponen en la respuesta de `unitOfMeasure`.

---

#### O5 — Falta índice en `InventoryLot.IdInventoryAdjustment` ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se agregó `IX_inventoryLot_idInventoryAdjustment` (filtrado `WHERE idInventoryAdjustment IS NOT NULL`) en `InventoryLotConfiguration`. Migración aplicada: `20260405161040_AddInventoryLotIdInventoryAdjustmentIndex`.

`IdInventoryAdjustment` es FK nullable en `InventoryLot`, pero no tiene índice definido en `InventoryLotConfiguration`. Las consultas "todos los lotes creados por el ajuste X" harán full scan.

```csharp
builder.HasIndex(il => il.IdInventoryAdjustment)
    .HasFilter("[idInventoryAdjustment] IS NOT NULL")
    .HasDatabaseName("IX_inventoryLot_idInventoryAdjustment");
```

---

#### O6 — `InventoryAdjustmentLine.QuantityDelta = 0` (ajuste de costo puro) y su impacto en `AverageCost` ✅ IMPLEMENTADO

> **Resuelto** (abril 2026): Se implementaron dos tipos de ajuste de costo en una sola línea, discriminados por `idInventoryLot` vs `idProduct` (check constraint `CK_inventoryAdjustmentLine_target` garantiza que exactamente uno esté informado):
>
> - **Tipo 1 — Por lote** (`idInventoryLot`): `quantityDelta = 0`, `unitCostNew = nuevo costo unitario del lote`. Actualiza `InventoryLot.UnitCost` del lote específico y recalcula `Product.AverageCost` como promedio ponderado de todos sus lotes. Genera asiento contable por la diferencia `qty × (nuevo − viejo)`.
>
> - **Tipo 2 — Por producto** (`idProduct`): `quantityDelta = 0` (obligatorio), `unitCostNew = costo promedio objetivo`. Escala el `UnitCost` de **todos** los lotes con stock del producto proporcionalmente (conserva la distribución relativa de costos entre lotes). Establece `Product.AverageCost = unitCostNew` directamente. Genera asiento contable por la diferencia entre el valor total inventario nuevo y el viejo.
>
> - Nuevos check constraints: `CK_inventoryAdjustmentLine_target`, `CK_inventoryAdjustmentLine_productLevel`.
> - Migración aplicada: `20260405162306_AddInventoryAdjustmentLineProductLevel`.

---

#### O7 — Ausencia de `SalesOrder` y `ProductionOrder` como entidades de primer nivel ✅ IMPLEMENTADO

> **Resuelto** (5-abr-2026): Se implementaron las Modalidades A y B completas. Migración aplicada: `20260405165826_AddSalesOrdersProductionOrders`.
>
> **Entidades nuevas**: `PriceList`, `PriceListItem`, `SalesOrder`, `SalesOrderLine`, `SalesOrderLineFulfillment`, `SalesOrderAdvance`, `ProductionOrder`, `ProductionOrderLine`.
>
> **FK opcionales agregadas** (no rompen flujos existentes):
> - `InventoryAdjustment.IdProductionOrder INT?` — vincula cada corrida de producción a su orden.
> - `SalesInvoice.IdSalesOrder INT?` — vincula facturas a pedidos de origen.
> - `ProductionOrder.IdSalesOrder INT?` — NULL = Modalidad A; NOT NULL = Modalidad B.
>
> **Endpoints nuevos**:
> - `GET|POST|PUT|DELETE /api/v1/price-lists` + `GET /api/v1/price-lists/by-product/{id}`
> - `GET|POST|PUT|DELETE /api/v1/sales-orders` + `/confirm` + `/cancel` + `/fulfillments` + `/advances`
> - `GET|POST|PUT|DELETE /api/v1/production-orders` + `PATCH /{id}/status` + `GET /by-sales-order/{id}`

Existen **dos modalidades de producción** claramente diferenciadas:

---

##### Modalidad A — Producción para stock de tienda (flujo actual ✅)

El encargado de producción decide cuánto producir basándose en el inventario disponible y la demanda esperada. No hay pedido previo del cliente.

```
ProductRecipe
  └──► InventoryAdjustment (PRODUCCION)    ← ejecución directa
         └──► ProductionSnapshot           ← ya implementado ✅
         └──► InventoryLot (producto terminado)
                └──► SalesInvoice          ← venta en tienda
```

**Este flujo ya está cubierto completamente.** Para la Modalidad A se puede crear opcionalmente una `ProductionOrder` con `IdSalesOrder = NULL` para planificar y agrupar corridas parciales, pero no es obligatorio.

---

##### Modalidad B — Producción contra pedido de cliente (✅ IMPLEMENTADO)

Un cliente externo hace un pedido grande. El proceso es:

1. **Ingresa el pedido** (`SalesOrder`) con productos, cantidades y precio tomado de la lista de precios vigente.
2. **Se crea una orden de producción** (`ProductionOrder`) vinculada al pedido.
3. **El encargado define cómo cumplir cada línea**: puede usar stock existente, producir todo, o una combinación de ambos.
4. **Se produce en múltiples ejecuciones parciales** (cada una es un `InventoryAdjustment` tipo PRODUCCION con su `ProductionSnapshot`).
5. **Al completar**, se toma lo producido + lotes de stock asignados contra el pedido y se emite la `SalesInvoice`.
6. **Trazabilidad de margen**: precio de lista (snapshot al crear el pedido) vs costo real (MPs + lotes usados) → % de ganancia o pérdida documentado por pedido.

```
PriceList / PriceListItem                         ← lista de precios vigente
  └── IdProduct, UnitPrice, DateFrom, DateTo

SalesOrder                                        ← pedido del cliente
  ├── IdContact (cliente)
  ├── DateRequired (fecha prometida de entrega)
  ├── Status: Borrador→Confirmado→EnProduccion→Listo→Facturado→Cancelado
  ├── SalesOrderLine[]
  │     ├── IdProduct, Quantity
  │     ├── UnitPrice (snapshot de PriceListItem al crear el pedido)
  │     └── SalesOrderLineFulfillment[]            ← cómo se cumple cada línea
  │           ├── FulfillmentType: 'Stock' | 'Produccion'
  │           ├── QuantityFulfilled
  │           ├── IdInventoryLot FK? (si FulfillmentType = 'Stock')
  │           └── IdProductionOrderLine FK? (si FulfillmentType = 'Produccion')
  │
  ├── SalesOrderAdvance[]                          ← anticipos opcionales del cliente
  │     ├── IdSalesOrder INT NOT NULL FK    ← vínculo financiero obligatorio
  │     ├── IdProductionOrder INT? FK       ← referencia informativa opcional ("anticipo al arrancar orden #X")
  │     ├── Amount, DateAdvance
  │     └── IdAccountingEntry INT? FK       ← asiento contable del anticipo
  │
  ├── ProductionOrder[]                            ← planificación de producción
  │     ├── IdSalesOrder FK?  (NULL = Modalidad A, sin pedido)
  │     ├── Status: Pendiente→EnProceso→Completada→Cancelada
  │     ├── ProductionOrderLine[]
  │     │     └── IdProduct, QuantityRequired, QuantityProduced, IdProductRecipe
  │     └── InventoryAdjustment[]                  ← ejecuciones parciales
  │           └── ProductionSnapshot (ya implementado ✅)
  │
  └── SalesInvoice                                 ← se factura al cerrar el pedido
        ├── IdSalesOrder FK?
        └── (los anticipos se aplican como crédito al facturar)
```

**Entidades nuevas implicadas:**

| Entidad | Propósito | Nota |
|---------|-----------|------|
| `SalesOrder` | Cabecera del pedido | FK nullable desde `SalesInvoice` |
| `SalesOrderLine` | Línea por producto | Precio snapshot de lista vigente |
| `SalesOrderLineFulfillment` | Cómo se cumple cada línea (stock o producción) | Permite mezcla en misma línea |
| `PriceList` / `PriceListItem` | Lista de precios con vigencia por fechas | Cambia periódicamente, no por pedido |
| `ProductionOrder` | Orden de producción planificada | FK nullable `IdSalesOrder`; NULL = Modalidad A |
| `ProductionOrderLine` | Qué producir y cuánto | `QuantityRequired` vs `QuantityProduced` |
| `SalesOrderAdvance` | Anticipo/depósito del cliente | `IdSalesOrder NOT NULL` (vínculo financiero). `IdProductionOrder INT?` solo como contexto informativo de cuándo/por qué se recibió. Al facturar: `WHERE IdSalesOrder = X`. |

**Puntos de extensión limpios en el modelo actual** (todos FK nullable → no rompen Modalidad A ni ventas de tienda):
- `InventoryAdjustment.IdProductionOrder INT?`
- `SalesInvoice.IdSalesOrder INT?`
- `ProductionOrder.IdSalesOrder INT?`

**Valor de negocio para encargado de producción y ventas:**

| Necesidad | Cómo se resuelve |
|-----------|------------------|
| ¿Qué tengo pendiente de producir? | `ProductionOrder WHERE status IN ('Pendiente','EnProceso')` |
| ¿Para qué cliente produzco? | `ProductionOrder → SalesOrder → Contact` |
| ¿Cuánto llevo vs lo comprometido? | `QuantityRequired vs QuantityProduced` en `ProductionOrderLine` |
| Margen por pedido | `SalesOrderLine.UnitPrice × Qty − CostoReal` → % ganancia o pérdida |
| Costo real de producción | Suma `UnitCost × QuantityReal` de `ProductionSnapshotLine` vinculados |
| Costo de lotes de stock usados | Suma `InventoryLot.UnitCost × Qty` de `SalesOrderLineFulfillment` tipo Stock |
| Producción parcial en varios turnos | Múltiples `InventoryAdjustment` bajo la misma `ProductionOrder` |
| Anticipos al generar la factura | `SalesOrderAdvance WHERE IdSalesOrder = X` — simple y completo. El campo `IdProductionOrder` muestra contexto en UI ("anticipo al iniciar orden #2") sin afectar la lógica financiera. |

---

## 4. Resumen de preguntas abiertas

| # | Pregunta | Área |
|---|----------|------|
| Q1 | ¿Se puede usar un producto en factura si su distribución contable no suma 100%? | ProductAccount |
| ~~Q2~~ | ~~¿La producción siempre debe referenciar una `ProductRecipe`, o se permiten ajustes libres?~~ ✅ Siempre debe haber receta. Resuelto con `ProductionSnapshot`. | Producción |
| ~~Q3~~ | ~~¿La selección de lote en venta es manual o el sistema debe sugerir/auto-asignar (FEFO)?~~ ✅ Ambas. El operador elige el lote manualmente; el sistema sugiere el FEFO vía `/inventory-lots/suggest/{idProduct}.json` y bloquea lotes vencidos al confirmar. | SalesInvoiceLine |
| ~~Q4~~ | ~~¿Puede existir un producto sin inventario (servicios, mano de obra)? ¿Cómo se distingue?~~ ✅ Sí. Se usa el tipo "Servicios" (`TrackInventory = false`). El campo `ProductType.TrackInventory` define si el tipo lleva stock. | Product |
| ~~Q8-1~~ | ~~¿Los productos con receta se pre-producen antes de vender (2A) o se descuentan insumos en tiempo real al confirmar (2B)?~~ ✅ Tiempo real (2B). `ConfirmAsync` detecta receta activa y explota automáticamente. | Combos / Recetas |
| ~~Q8-2~~ | ~~¿El operador necesita ver el desglose de slots/insumos en UI antes de confirmar, o la explosión es automática?~~ ✅ Automática (3A). La UI registra 1 línea = 1 combo; el sistema explota al confirmar. | Combos / Recetas |
| ~~Q8-3~~ | ~~¿El sistema auto-asigna lotes FEFO en la explosión, o el operador elige el lote de cada insumo manualmente?~~ ✅ Auto-FEFO. `GetFefoLotAsync` asigna el lote con menor vencimiento disponible. | Combos / Recetas |
| Q8-4 | ¿Puede existir un combo cuyos slots sean también combos (anidación combo→combo)? | Combos |
| Q8-5 | ¿`ProductOptionItem` debe mover stock de algún insumo (ej: extra queso), o solo afecta el precio? | Opciones |
| ~~Q6~~ | ~~¿`PurchaseInvoice.ProviderName` sin FK es intencional en V1, o se planea normalizar?~~ ✅ Resuelto con `IdContact` FK + get-or-create de proveedor. | Compras |
| ~~Q7~~ | ~~¿Un ajuste de costo puro (delta=0) debe recalcular `Product.AverageCost`?~~ ✅ Sí. Tipo 1 (por lote): recalcula el WACC del producto. Tipo 2 (por producto): ajusta directamente el `AverageCost` y escala todos los lotes proporcionalmente. | AverageCost |
| ~~Q9-1~~ | ~~¿Un pedido puede mezclar stock existente y productos a producir?~~ ✅ Sí. El encargado define por línea si usa stock, produce, o una combinación. Trazado en `SalesOrderLineFulfillment`. | SalesOrder |
| ~~Q9-2~~ | ~~¿El precio se fija al ingresar el pedido o puede ajustarse?~~ ✅ Los precios viven en una entidad `PriceList` / `PriceListItem` que cambia periódicamente. Al crear el pedido se hace snapshot del precio vigente en `SalesOrderLine.UnitPrice`. El margen = precio snapshot − costo real de producción/stock. | SalesOrder / Margen |
| ~~Q9-3~~ | ~~¿Se registran anticipos del cliente?~~ ✅ Opcional. Entidad `SalesOrderAdvance` con monto, fecha y asiento contable. Se aplica como crédito al emitir la `SalesInvoice`. | SalesOrder / Finanzas |

---

## 5. Cambios de bajo riesgo recomendados para aplicar pronto

Los siguientes son cambios de BD (nuevas migraciones) que no rompen código existente:

1. ~~`CK_inventoryAdjustmentLine_unitCostNew` — CHECK `quantityDelta <= 0 OR unitCostNew IS NOT NULL`~~ ✅ Aplicado en `20260405011042_InventoryConstraints_P1P2P3`
2. ~~`UQ_productUnit_idProduct_isBase` — índice único filtrado para `isBase = 1`~~ ✅ Aplicado en `20260405011042_InventoryConstraints_P1P2P3`
3. ~~`IX_inventoryLot_idInventoryAdjustment` — índice en FK nullable~~ ✅ Aplicado en `20260405161040_AddInventoryLotIdInventoryAdjustmentIndex`
5. ~~`ProductionSnapshot` + `ProductionSnapshotLine` — trazabilidad receta→producción con teórico vs real~~ ✅ Aplicado en `20260405012446_ProductionSnapshot`
4. ~~Validación en service: suma `PercentageAccount = 100` antes de confirmar documentos con `idProduct` que tenga distribución contable~~ ✅ Aplicado en `SalesInvoiceService.ConfirmAsync`
6. ~~**`PriceList` + `PriceListItem`** — lista de precios con vigencia por fechas. Base para snapshot en `SalesOrderLine.UnitPrice`. Prioridad alta.~~ ✅ Aplicado en `20260405165826_AddSalesOrdersProductionOrders`
7. ~~**`SalesOrder` + `SalesOrderLine` + `SalesOrderLineFulfillment`** — pedido del cliente con mezcla de stock/producción por línea. FK nullable `SalesInvoice.IdSalesOrder`. Prioridad alta.~~ ✅ Aplicado en `20260405165826_AddSalesOrdersProductionOrders`
8. ~~**`SalesOrderAdvance`** — anticipo opcional del cliente con asiento contable. Se aplica como crédito al facturar. Prioridad media.~~ ✅ Aplicado en `20260405165826_AddSalesOrdersProductionOrders`
9. ~~**`ProductionOrder` + `ProductionOrderLine`** — planificación de producción contra pedido. FK nullable `InventoryAdjustment.IdProductionOrder` y `ProductionOrder.IdSalesOrder`. Prioridad alta.~~ ✅ Aplicado en `20260405165826_AddSalesOrdersProductionOrders`

