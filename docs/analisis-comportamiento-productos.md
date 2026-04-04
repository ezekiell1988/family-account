# Análisis de Comportamiento Actual — Sistema de Productos

> Fecha: 4 de abril de 2026  
> Rama: `main`  
> Alcance: Entidades, servicios y endpoints relacionados con el flujo de productos, incluyendo productos configurables y combos.

---

## 1. Inventario de entidades y estado

| Entidad | Módulo API | CRUD completo | Observaciones |
|---|---|---|---|
| `Product` | `/products` | ✅ | CRUD expuesto a Admin. Incluye `HasOptions` e `IsCombo` |
| `ProductUnit` | `/product-units` | ✅ | Filter by product + búsqueda por barcode. Incluye `SalePrice` |
| `ProductCategory` | `/product-categories` | ✅ | Incluye asociación M:N con producto |
| `ProductProductCategory` | — | Implícito | Gestionado desde ProductCategories |
| `ProductAccount` | `/product-accounts` | ✅ | CRUD + filter by product |
| `ProductRecipe` | `/product-recipes` | ✅ | CRUD + filter by output |
| `ProductRecipeLine` | — | Implícito | Se crea/destruye con la receta |
| `ProductType` | `/product-types` | Solo lectura | Catálogo de sistema, sin CRUD |
| `InventoryLot` | `/inventory-lots` | Solo lectura | Se crea desde factura/ajuste (ver §3) |
| `InventoryAdjustment` | `/inventory-adjustments` | ✅ | Flujo Borrador → Confirmado → Anulado |
| `ProductOptionGroup` | `/product-option-groups` | ✅ | CRUD completo. Items implícitos (se crean/destruyen con el grupo) |
| `ProductOptionItem` | — | Implícito | Se crea/destruye con el grupo |
| `ProductComboSlot` | `/product-combo-slots` | ✅ | CRUD completo. Productos del slot implícitos |
| `ProductComboSlotProduct` | — | Implícito | Productos elegibles por slot |

---

## 2. Comportamiento actual documentado

### 2.1 `Product` — CRUD base

- Los campos `CodeProduct` y `NameProduct` son editables sin restricción de estado.
- `AverageCost` **nunca** se expone en `CreateProductRequest` ni `UpdateProductRequest`. Se inicializa en `0` y su actualización queda totalmente delegada a los procesos de confirmación de ajustes de inventario (§3.2).
- `IdProductParent` permite variantes hasta 1 nivel de profundidad, pero el servicio **no valida** que el padre referenciado no tenga a su vez un padre (la restricción de "máximo 1 nivel" existe solo como comentario en la configuración EF).
- `DeleteAsync` usa `ExecuteDeleteAsync` directo. Si hay lotes (`inventoryLot`), ajustes o líneas de factura vinculados, la FK de la BD lanzará una excepción genérica sin mensaje de negocio útil.
- `HasOptions` (`bool`, default `false`): indica que el producto tiene grupos de opciones configurables por el cliente. El modelo de datos ya existe pero el servicio **no valida** que, si `HasOptions=true`, el producto tenga al menos un `ProductOptionGroup` asociado.
- `IsCombo` (`bool`, default `false`): indica que el producto es un combo con slots. El servicio **no valida** que, si `IsCombo=true`, el producto tenga al menos un `ProductComboSlot` asociado.
- Un producto puede tener `HasOptions=true` e `IsCombo=false` (pizza configurable), o `IsCombo=true` con slots que apunten a productos con `HasOptions=true`. La combinación `HasOptions=true` y `IsCombo=true` en el mismo producto **no está validada** en el servicio.

### 2.2 `ProductUnit` — Presentaciones con factor de conversión

- El servicio **no valida** la invariante principal del modelo: exactamente 1 `IsBase = true` por producto.
- El servicio **no valida** que cuando `IsBase = true`, `ConversionFactor = 1.0` y `IdUnit = product.IdUnit`.
- `UpdateAsync` permite cambiar `IsBase` y `ConversionFactor` sin ninguna verificación. Esto puede dejar un producto sin unidad base o con dos unidades base simultáneas.
- Las restricciones solo existen a nivel de BD (índice único `UQ_productUnit_idProduct_idUnit`), por lo que el error de violación llegaría como excepción genérica de SQL, no como validación semántica.
- `SalePrice` (`decimal(18,4)`, default `0`): precio base de venta para esta presentación. El precio final en combos y productos configurables se calcula sumando los `PriceDelta` de los `ProductOptionItem` seleccionados. El servicio actualmente **no valida** que `SalePrice >= 0`.

### 2.3 `ProductRecipe` — BOM (Bill of Materials)

- El servicio **no valida** que `IdProductOutput` sea de tipo "Producto en Proceso" o "Producto Terminado". Una receta puede crearse con un output de tipo "Materia Prima" o "Reventa", contradiciendo las reglas de negocio del modelo.
- El servicio **no valida** que ninguna `ProductRecipeLine` apunte al mismo producto que `IdProductOutput` (ingrediente = output es un error de datos).
- No hay detección de ciclos (producto A requiere B, producto B requiere A).
- `UpdateAsync` reemplaza todas las líneas con las nuevas (`RemoveRange` + nuevo listado), lo que es correcto en comportamiento pero puede ser pesado para recetas largas.

### 2.4 `ProductAccount` — Distribución contable

- El servicio **no valida** que la suma de `PercentageAccount` por producto sea exactamente 100. Es posible configurar una distribución incompleta (ej: 40% + 30% = 70%) y luego confirmar una factura con ese producto.
- Cuando la factura se confirma, el asiento SÍ usa estos porcentajes: `rawAmount = TotalLineAmount * PercentageAccount / 100m`. Si la suma no es 100, el DR del asiento no coincide con el total de la línea.
- Los porcentajes negativos están permitidos en el modelo (crean líneas CR internas), lo que es correcto para casos de contra-asientos, pero no está documentado en los DTOs.

### 2.5 `ProductOptionGroup` y `ProductOptionItem` — Opciones configurables

Entidades con configuración EF, módulo de endpoints, servicio e interfaces completamente implementados en `/product-option-groups`.

**`ProductOptionGroup`**:
- Agrupa opciones de un producto con `HasOptions=true`. Un producto puede tener N grupos (ej: "Elige tu masa", "Elige tu tamaño").
- `IsRequired` + `MinSelections` + `MaxSelections` definen la cardinalidad de la elección. El servicio **valida** `MaxSelections >= MinSelections` y que `MinSelections = 0` cuando `IsRequired = false`.
- `AllowSplit`: cuando `true`, en modo mitad/mitad el cliente puede asignar cada selección a `half1`, `half2` o `whole`. Aplica a grupos de sabor y adicionales.
- La FK hacia `product` usa `CASCADE`. El servicio **valida** que el producto padre tenga `HasOptions=true`.
- El servicio **valida** que el grupo tenga al menos 1 ítem.

**`ProductOptionItem`**:
- Cada fila es una opción dentro de un grupo (ej: "Masa Delgada", "Masa Gruesa").
- `PriceDelta` ajusta el precio base de la `ProductUnit` seleccionada. Puede ser positivo, negativo o cero.
- `IsDefault`: marca la opción pre-seleccionada al abrir el selector. El servicio **valida** que el conteo de ítems con `IsDefault=true` no exceda `MaxSelections` del grupo.
- La FK hacia `productOptionGroup` usa `CASCADE`.
- Create/Update son **atómicos**: el grupo y sus ítems se gestionan en un solo request (replace-all en Update).

### 2.6 `ProductComboSlot` y `ProductComboSlotProduct` — Combos con slots

Entidades con configuración EF, módulo de endpoints, servicio e interfaces completamente implementados en `/product-combo-slots`.

**`ProductComboSlot`**:
- Define un "hueco" dentro de un combo (`IsCombo=true`). Un combo "2 Pizzas + Bebida" tiene 3 slots.
- `Quantity` es la cantidad de ese slot dentro del combo (ej: 1 pizza, 1 bebida).
- `IsRequired` indica si el cliente debe llenar ese slot.
- La FK hacia `product` usa `CASCADE`. El servicio **valida** que el producto padre tenga `IsCombo=true`.
- El servicio **valida** que el slot tenga al menos 1 producto permitido.

**`ProductComboSlotProduct`**:
- Lista de productos permitidos para un slot. El cliente elige uno de esta lista.
- Índice único `UQ_productComboSlotProduct_idSlot_idProduct`: un producto no puede repetirse en el mismo slot (validado también en servicio antes del insert).
- `PriceAdjustment`: ajuste adicional al precio del combo por elegir ese producto en el slot.
- FK hacia `product` usa `RESTRICT` (no se puede eliminar un producto que está asignado a un slot de combo).
- El servicio **valida** que ningún producto del slot tenga `IsCombo=true` (sin combos anidados).
- Create/Update son **atómicos**: el slot y sus productos se gestionan en un solo request (replace-all en Update).

---

### 2.7 `InventoryLot` — Lotes de inventario

Los lotes se crean por dos vías actualmente:

| Vía | ¿Crea lote? | Actualiza `AverageCost`? |
|---|---|---|
| Confirmación de `PurchaseInvoice` | ❌ **NO** | ❌ NO |
| Confirmación de `InventoryAdjustment` (delta > 0) | ❌ NO (modifica lote existente) | ✅ SÍ |

**Gap crítico**: La confirmación de una factura de compra (`PurchaseInvoiceService.ConfirmAsync`) genera el asiento contable y crea el movimiento bancario, pero **no materializa el inventario**. Los campos `LotNumber` y `ExpirationDate` que vienen en `PurchaseInvoiceLine` se guardan en la línea pero nunca se copian a un `InventoryLot`.

El flujo actual obliga al usuario a crear manualmente un `InventoryAdjustment` para ingresar el stock, lo que:
- Rompe la trazabilidad (`inventoryLot.IdPurchaseInvoice` quedará siempre NULL en ese flujo).
- Duplica trabajo operativo.
- Deja el system en un estado inconsistente: la factura está "Confirmada" pero el inventario cero.

### 2.8 Cálculo de `AverageCost` (Costo Promedio Ponderado)

El recálculo ocurre en `InventoryAdjustmentService.ConfirmAsync` cuando:
- `QuantityDelta > 0` (entrada), o
- `UnitCostNew` tiene valor (corrección de costo).

Algoritmo:
```
totalQty  = SUM(quantityAvailable) de TODOS los lotes del producto
totalCost = SUM(quantityAvailable * unitCost) de TODOS los lotes del producto
product.AverageCost = totalQty > 0 ? totalCost / totalQty : 0
```

**Observación**: La fórmula lee los lotes **incluyendo el lote recién actualizado** en memoria (antes del `SaveChanges`). Esto es correcto porque el contexto EF tiene los valores actualizados en memoria. Sin embargo, lee **todos los lotes del producto desde la BD** pero usa los valores en memoria para el lote siendo ajustado; los demás lotes se leen con sus valores actuales en BD. Este comportamiento es correcto para WACC pero depende del orden de operaciones del `DbContext`.

**Ausencia**: Cuando la salida (`QuantityDelta < 0`) reduce a cero el stock total, `AverageCost` queda en el último valor calculado, no en 0. Esto puede ser intencionado (mantener referencia de costo) pero no está documentado.

### 2.9 `QuantityBase` en `PurchaseInvoiceLine`

La entidad `PurchaseInvoiceLine` tiene dos campos:
- `Quantity`: cantidad en la unidad de la presentación comprada (ej: 2 CAJAS).
- `QuantityBase`: cantidad en la unidad base del producto (ej: 48 unidades).

**Gap**: En `MapLine()`, `QuantityBase = l.Quantity` — se asigna siempre el mismo valor que `Quantity` sin aplicar el `ConversionFactor` de la `ProductUnit` correspondiente. La conversión nunca ocurre en el API; `QuantityBase` es actualmente redundante y contiene el mismo dato.

### 2.10 Flujo de estados

#### Factura de Compra
```
Borrador ──(Confirmar)──► Confirmado
    │                         │
    └──(Eliminar: solo Borrador)   └──(Anular)──► Anulado
```
- Al anular: se anulan los asientos contables vinculados.  
- Al anular: **no hay rollback de inventario** (por el gap §2.5, el inventario nunca se creó desde la factura).

#### Ajuste de Inventario
```
Borrador ──(Confirmar)──► Confirmado
                              │
                         └──(Anular)──► Anulado
```
- Al confirmar: aplica `QuantityDelta` a cada lote y recalcula `AverageCost`.
- Al anular: **no revierte** los deltas de inventario (marca como Anulado pero no hay rollback).

---

## 3. Gaps por categoría

### 3.1 Gaps de validación (sin restricción en servicio)

| # | Contexto | Regla ausente |
|---|---|---|
| V1 | `ProductService` | `IdProductParent` no puede apuntar a un producto que ya tiene padre |
| V2 | `ProductUnitService` | Exactamente 1 `IsBase=true` por producto |
| V3 | `ProductUnitService` | `IsBase=true` requiere `ConversionFactor=1.0` y `IdUnit=product.IdUnit` |
| V4 | `ProductRecipeService` | `IdProductOutput` no puede ser tipo "Materia Prima" o "Reventa" |
| V5 | `ProductRecipeService` | `IdProductInput` no puede ser igual a `IdProductOutput` |
| V6 | `ProductAccountService` | Suma de `PercentageAccount` por producto debe ser 100 |
| V7 | `ProductService Delete` | Mensaje claro si el producto tiene lotes o facturas asociadas |
| V8 | `ProductUnitService` | `SalePrice >= 0` |
| V9 | `ProductService` | Si `HasOptions=true`, debe existir al menos 1 `ProductOptionGroup` |
| V10 | `ProductService` | Si `IsCombo=true`, debe existir al menos 1 `ProductComboSlot` |
| V11 | `ProductService` | La combinación `HasOptions=true` e `IsCombo=true` en el mismo producto requiere política explícita |

### 3.2 Gaps de lógica de negocio

| # | Contexto | Gap |
|---|---|---|
| L1 | `PurchaseInvoiceService.ConfirmAsync` | No crea `InventoryLot` ni actualiza `AverageCost` |
| L2 | `PurchaseInvoiceLine.QuantityBase` | Siempre = `Quantity`; la conversión por `ConversionFactor` nunca se aplica |
| L3 | `InventoryAdjustmentService.CancelAsync` | No revierte deltas de inventario ni `AverageCost` |
| L4 | `PurchaseInvoiceService.CancelAsync` | No revierte inventario (derivado de L1) |
| L5 | `ProductRecipeService` | Sin detección de ciclos en ingredientes |
| L6 | `ProductOptionGroup` / `ProductComboSlot` | Precio final de un configurado/combo no está calculado en ningún endpoint (`SalePrice + PriceDelta + PriceAdjustment`); no existe todavía un endpoint de "calcular precio" |

### 3.3 Gaps de API / respuesta

| # | Contexto | Gap |
|---|---|---|
| A1 | `GET /products/data.json` | Sin filtro/búsqueda por texto; sin paginación |
| A2 | `ProductResponse` | No incluye lista de categorías ni count de `ProductUnits` |
| A3 | `GET /inventory-lots` | No hay endpoint de stock total expuesto públicamente (`GetStockTotalAsync` existe en servicio pero no en módulo) |
| A4 | `ProductType` | Sin seed explícito en API; se asume que los 4 tipos se crean vía migración |
| A5 | `ProductResponse` | No incluye colecciones inline de `ProductOptionGroups` ni `ProductComboSlots` (se obtienen por endpoints separados) |

---

## 4. Comportamiento que sí funciona correctamente

- **CRUD de `Product`**: Creación, actualización y eliminación con validación de código único (409 por índice `UQ_product_codeProduct`).
- **Búsqueda de `ProductUnit` por barcode**: `GET /product-units/barcode/{barcode}.json` funciona correctamente.
- **Confirmación de `InventoryAdjustment`**: Genera número secuencial `AJ-YYYYMMDD-NNN`, aplica deltas y recalcula `AverageCost`.
- **Confirmación de `PurchaseInvoice`**: Genera asiento contable con distribución por `ProductAccount`, y el fallback a `IdDefaultExpenseAccount` del tipo de factura.
- **Auto-creación de movimiento bancario** al confirmar factura con `CounterpartFromBankMovement=true` y sin movimiento previo vinculado.
- **Asociación M:N producto-categoría** vía `POST /product-categories/{id}/products/{idProduct}`.
- **`ProductRecipe` con líneas**: Creación atómica de receta + ingredientes en un solo request.
- **CRUD de `ProductOptionGroup` + `ProductOptionItem`**: `GET /by-product/{id}`, `GET /{id}`, `POST`, `PUT`, `DELETE`. Creación/actualización atómica con ítems. Validaciones: producto con `HasOptions=true`, `MaxSelections >= MinSelections`, conteo de `IsDefault` <= `MaxSelections`.
- **CRUD de `ProductComboSlot` + `ProductComboSlotProduct`**: `GET /by-combo/{id}`, `GET /{id}`, `POST`, `PUT`, `DELETE`. Creación/actualización atómica. Validaciones: producto con `IsCombo=true`, sin combos anidados en slots, sin duplicados en slot.

---

## 5. Dependencias para cambios futuros

Antes de modificar cualquier parte del flujo conviene considerar:

1. **`InventoryLot.IdPurchaseInvoice`** está diseñado como FK nullable hacia `purchaseInvoice` para trazar el origen del lote. Si se implementa L1 (crear lote al confirmar factura), este campo se poblará.

2. **`InventoryLot.SourceType`** es un `VARCHAR(20)` libre: valores actuales en uso son `"Compra"` y `"Ajuste"`. Si se implementa producción (V2), se agregaría `"Producción"`.

3. **`InventoryLot.IdProductionBatch`** y la entidad `ProductionBatch` están reservados para V2. Actualmente la entidad `InventoryLot` no incluye esa FK.

4. **`PurchaseInvoiceLine.QuantityBase`** requiere que el cliente envíe la `ProductUnit` seleccionada para poder calcular la conversión en el API, o bien que el API resuelva la conversión internamente dado `IdProduct` + `IdUnit`.

5. **Anulación con rollback de inventario** (L3, L4) requiere decidir la política: ¿se crea un ajuste negativo automático, o se bloquea la anulación si hay consumos posteriores del lote?

6. **`ProductOptionGroup.AllowSplit`** requiere definir cómo se representa la asignación mitad/mitad a nivel de pedido. Esta lógica no existe todavía en ninguna entidad de línea de pedido/factura.

7. **Precio final en productos configurables**: el precio de venta de un configurado es `ProductUnit.SalePrice + SUM(ProductOptionItem.PriceDelta de los items seleccionados)`. Si un producto pertenece a un slot de combo, el precio del slot puede sobreescribir o sumarse al precio base; la política exacta aún no está definida en el servicio.

8. **Combos con `MaxSelections > 1` en un slot**: el modelo soporta que el cliente elija N productos en un mismo slot. Esto implica que el `Quantity` del slot se reparte entre los N productos elegidos, o se multiplica; esta semántica no está documentada.

---

## 6. Resumen ejecutivo

El sistema tiene el modelo de datos correcto y bien estructurado. Los endpoints CRUD base funcionan. El flujo contable de facturas opera correctamente. El modelo de productos configurables y combos está definido en BD pero aún sin endpoints. Los gaps principales son:

- **Inventario desconectado de facturas**: confirmar una compra no mueve el inventario.
- **Sin validaciones de dominio** en `ProductUnit`, `ProductRecipe` y `ProductAccount` para las reglas críticas de negocio.
- **`QuantityBase` inoperante**: la conversión de unidades nunca se ejecuta.
- **Anulaciones sin rollback de inventario**.
- **Sin endpoint de precio calculado**: La fórmula `SalePrice + ∑PriceDelta + PriceAdjustment` para configurados y combos no está centralizada en ningún endpoint.
