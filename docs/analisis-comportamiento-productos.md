# Análisis de Comportamiento Actual — Sistema de Productos

> Fecha: 4 de abril de 2026 (actualizado: 5 de abril de 2026 — IsNonProductLine en SalesInvoiceLine)  
> Rama: `main`  
> Alcance: Entidades, servicios y endpoints relacionados con el flujo de productos, inventario, facturas de compra y el nuevo módulo de facturas de venta.

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
| `InventoryLot` | `/inventory-lots` | Solo lectura | Se crea al confirmar `PurchaseInvoice` o `InventoryAdjustment` (ver §2.7) |
| `InventoryAdjustment` | `/inventory-adjustments` | ✅ | Flujo Borrador → Confirmado → Anulado. Genera asiento contable al confirmar si el tipo tiene cuentas configuradas |
| `InventoryAdjustmentType` | `/inventory-adjustment-types` | ✅ | Catálogo CRUD. Define cuentas contables: inventario, contrapartida entrada y contrapartida salida. Seed con 3 tipos (CONTEO, PRODUCCION, AJUSTE_COSTO) con cuentas preconfiguradas |
| `InventoryAdjustmentEntry` | — | Implícito | Tabla pivot N:M entre `inventoryAdjustment` y `accountingEntry`. Se crea automáticamente al confirmar si el tipo tiene cuentas configuradas |
| `ProductOptionGroup` | `/product-option-groups` | ✅ | CRUD completo. Items implícitos (se crean/destruyen con el grupo) |
| `ProductOptionItem` | — | Implícito | Se crea/destruye con el grupo |
| `ProductComboSlot` | `/product-combo-slots` | ✅ | CRUD completo. Productos del slot implícitos |
| `ProductComboSlotProduct` | — | Implícito | Productos elegibles por slot |
| `SalesInvoiceType` | `/sales-invoice-types` | ✅ | Catálogo CRUD. Define contrapartida, cuentas contables de ingresos y COGS. Seed con 4 tipos, todos con cuentas preconfiguradas |
| `SalesInvoice` | `/sales-invoices` | ✅ | Flujo Borrador → Confirmado → Anulado. Genera asiento contable + COGS al confirmar |
| `SalesInvoiceLine` | — | Implícito | Se crea/destruye con la factura. `IsNonProductLine` controla si requiere lote. Snapshots UnitCost y QuantityBase al confirmar |
| `SalesInvoiceEntry` | — | Implícito | Pivot N:M entre `salesInvoice` y `accountingEntry` |
| `SalesInvoiceLineEntry` | — | Implícito | Pivot N:M entre `salesInvoiceLine` y `accountingEntryLine` |

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

✅ **Validaciones implementadas (5-abr-2026):**

- `IsBase = true` → el servicio **verifica** que no exista otra unidad base para el mismo producto (V2).  
- `IsBase = true` → el servicio **exige** `ConversionFactor = 1.0` y que `IdUnit == product.IdUnit` (V3).  
- `IsBase = false` → el servicio **exige** `ConversionFactor > 0`.  
- **`SalePrice < 0`** rechazado con `InvalidOperationException` (V8).  
- Los errores de validación devuelven `422 ValidationProblem` en lugar de excepción de BD.

Nota: el índice único `UQ_productUnit_idProduct_idUnit` en BD sigue siendo la última barrera para duplicados; sigue llegando como `409 Conflict` desde el módulo.

### 2.3 `ProductRecipe` — BOM (Bill of Materials)

✅ **Validaciones implementadas (5-abr-2026):**

- El servicio **valida** que `IdProductOutput` sea solo de tipo "Producto en Proceso" (id=2) o "Producto Terminado" (id=3). Tipos "Materia Prima" (id=1) o "Reventa" (id=4) son rechazados con `422 ValidationProblem` (V4).
- El servicio **valida** que ninguna `ProductRecipeLine` tenga `IdProductInput == IdProductOutput` (V5).
- No hay detección de ciclos (producto A requiere B, producto B requiere A) — gap L5 pendiente.
- `UpdateAsync` reemplaza todas las líneas con las nuevas (`RemoveRange` + nuevo listado), lo que es correcto en comportamiento.

### 2.4 `ProductAccount` — Distribución contable

✅ **Validación implementada (5-abr-2026):** El servicio **valida** que la suma acumulada de `PercentageAccount` por producto no supere 100 al crear o actualizar. Una distribución incompleta (ej: 40+30=70) se permite para habilitar ingreso incremental, pero superar 100 lanza `422 ValidationProblem` (V6).

- Cuando la factura se confirma, el asiento SÍ usa estos porcentajes: `rawAmount = TotalLineAmount * PercentageAccount / 100m`. Si la suma es menor que 100, el CR del asiento no cubre el total de la línea (comportamiento permitido pero documentado).
- Los porcentajes negativos están permitidos en el modelo (crean líneas DR internas de contrapartida).

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

### 2.6.b `InventoryAdjustmentType` — Tipos de ajuste con configuración contable

Nuevo catálogo creado el 4-abr-2026. Reemplaza el campo `TypeAdjustment` (string libre con CHECK constraint) que tenía `InventoryAdjustment`.

**`InventoryAdjustmentType`**:
- Cada tipo define 3 cuentas contables opcionales: `IdAccountInventoryDefault` (activo de inventario), `IdAccountCounterpartEntry` (contrapartida entrada) y `IdAccountCounterpartExit` (contrapartida salida).
- Si `IdAccountInventoryDefault` es `null`, el servicio **omite** la generación del asiento al confirmar (comportamiento legacy compatible).
- El servicio **valida** en `ConfirmAsync` que si la cuenta de inventario está configurada, las contrapartidas requeridas según el tipo de línea también lo estén; si falta alguna, lanza `InvalidOperationException` con mensaje descriptivo.
- CRUD completo expuesto en `/inventory-adjustment-types` con endpoint adicional `GET /active.json`.

**Lógica del asiento al confirmar un ajuste** (nueva en `InventoryAdjustmentService.ConfirmAsync`):

| Caso de línea | DR | CR |
|---|---|---|
| `QuantityDelta > 0` (entrada) | `IdAccountInventoryDefault` × (qty × costo) | `IdAccountCounterpartEntry` |
| `QuantityDelta < 0` (salida) | `IdAccountCounterpartExit` × (qty × costo) | `IdAccountInventoryDefault` |
| `QuantityDelta = 0` + `UnitCostNew` al alza | `IdAccountInventoryDefault` × diff | `IdAccountCounterpartEntry` |
| `QuantityDelta = 0` + `UnitCostNew` a la baja | `IdAccountCounterpartExit` × \|diff\| | `IdAccountInventoryDefault` |
| `QuantityDelta = 0` + `UnitCostNew` sin cambio | sin movimiento contable | — |

El asiento queda vinculado al ajuste mediante `InventoryAdjustmentEntry` (misma estructura que `PurchaseInvoiceEntry`). `OriginModule = "InventoryAdjustment"`.

**Seed inicial** (IdAccounts configuradas desde la migración):

| Id | Código | Nombre | Inventario | Contrapartida Entrada | Contrapartida Salida |
|---|---|---|---|---|---|
| 1 | CONTEO | Conteo Físico | 109 Inventario Mercadería | 114 Sobrantes | 113 Faltantes/Merma |
| 2 | PRODUCCION | Producción | 111 Productos en Proceso | 115 Costos de Producción | 115 Costos de Producción |
| 3 | AJUSTE_COSTO | Ajuste de Costo | 109 Inventario Mercadería | 114 Sobrantes | 113 Faltantes/Merma |

**Nuevas cuentas contables agregadas al plan** (seed en `AccountConfiguration`):

| IdAccount | Código | Nombre | Tipo |
|---|---|---|---|
| 108 | `1.1.07` | Inventario | Activo (agrupadora) |
| 109 | `1.1.07.01` | Inventario de Mercadería | Activo |
| 110 | `1.1.07.02` | Materias Primas | Activo |
| 111 | `1.1.07.03` | Productos en Proceso | Activo |
| 112 | `5.14` | Ajustes de Inventario | Gasto (agrupadora) |
| 113 | `5.14.01` | Faltantes de Inventario (Merma) | Gasto |
| 114 | `5.14.02` | Sobrantes de Inventario | Gasto |
| 115 | `5.14.03` | Costos de Producción | Gasto |

---

### 2.7 `InventoryLot` — Lotes de inventario

Los lotes se crean por dos vías:

| Vía | ¿Crea lote? | Actualiza `AverageCost`? |
|---|---|---|
| Confirmación de `PurchaseInvoice` ✅ | ✅ **SÍ** (L1 resuelto 5-abr-2026) | ✅ SÍ (WACC) |
| Confirmación de `InventoryAdjustment` (delta > 0) | ✅ SÍ (modifica/crea lote según estado) | ✅ SÍ |
| Confirmación de `SalesInvoice` | ✅ Decrementa lote existente | ✅ SÍ (WACC) |

**Implementación L1 (5-abr-2026):** `PurchaseInvoiceService.ConfirmAsync` ahora, tras generar el asiento contable y actualizar el estado, itera cada línea con `IdProduct` e `IdUnit`, resuelve el `ConversionFactor` desde `ProductUnit`, crea un `InventoryLot` con:
- `SourceType = "Compra"`
- `QuantityAvailable = Quantity × ConversionFactor`
- `UnitCost = UnitPrice / ConversionFactor`
- `IdPurchaseInvoice` poblado (trazabilidad completa)

Luego recalcula `product.AverageCost` WACC con todos los lotes del producto.

`InventoryLot.SourceType` valores en uso: `"Compra"`, `"Ajuste"`, `"Venta"` (nuevo — al crear lote al confirmar venta se haría con este valor, actualmente solo se decrementa el lote pasado por `IdInventoryLot`).

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

✅ **Implementado (5-abr-2026, L2):** `MapLinesAsync` ahora resuelve el `ProductUnit` para el par `IdProduct + IdUnit`, calcula `QuantityBase = Math.Round(Quantity × ConversionFactor, 6)` y devuelve error si no existe la presentación. Si la línea no tiene producto/unidad, `QuantityBase` queda `null`. En `ConfirmAsync` se actualiza la línea trackeada con el valor final calculado.

### 2.10 Flujo de estados

#### Factura de Compra
```
Borrador ──(Confirmar)──► Confirmado
    │                         │
    └──(Eliminar: solo Borrador)   └──(Anular)──► Anulado
```
- Al confirmar: genera asiento contable + crea `InventoryLot` por línea + actualiza `AverageCost` WACC.  
- Al anular: se anulan los asientos contables vinculados. No revierte inventario (gap L4 — derivado de que se crea lote al confirmar, revertirlo requería ajuste negativo automático o bloqueo si ya hay salidas posteriores).

#### Ajuste de Inventario
```
Borrador ──(Confirmar)──► Confirmado
                              │
                         └──(Anular)──► Anulado
```
- Al confirmar: aplica `QuantityDelta` a cada lote y recalcula `AverageCost`. Genera asiento contable si el tipo tiene cuentas configuradas.  
- Al anular ✅ **(L3 resuelto 5-abr-2026):** Revierte `QuantityDelta` en cada lote, valida que ningún lote quedaría negativo, recalcula `AverageCost` WACC, anula el asiento contable. Retorna `409 Conflict` con mensaje si la reversión causaría stock negativo.

#### Factura de Venta (nuevo módulo — 5-abr-2026)
```
Borrador ──(Confirmar)──► Confirmado
    │                         │
    └──(Eliminar: solo Borrador)   └──(Anular)──► Anulado
```
- Al confirmar: valida que todas las líneas de producto (`IsNonProductLine = false`) tengan `IdInventoryLot` asignado (error descriptivo si falta); genera `NumberInvoice` auto (`FV-YYYYMMDD-NNN`), genera asiento de ingresos (DR=contrapartida, CR=cuentas de ingresos por ProductAccount o fallback), decrementa `lot.QuantityAvailable` por línea de producto, toma snapshot de `lot.UnitCost` en la línea, genera asiento COGS si hay cuentas configuradas en el tipo. Líneas con `IsNonProductLine = true` (flete, servicio, gasto) solo contribuyen al asiento de ingresos, no generan COGS ni decrementan inventario.

### 2.11 `SalesInvoice` — Nuevo módulo de ventas (5-abr-2026)

**`SalesInvoiceType`** (catálogo):
- Seed: **4 tipos** — todos con `IdAccountSalesRevenue=117`, `IdAccountCOGS=119`, `IdAccountInventory=109` preconfigurados.
- Campos de cuenta: `IdAccountCounterpartCRC`, `IdAccountCounterpartUSD`, `IdBankMovementType`, `IdAccountSalesRevenue` (fallback ingresos), `IdAccountCOGS`, `IdAccountInventory`.

| Id | Código | Nombre | Contrapartida |
|---|---|---|---|
| 1 | `CONTADO_CRC` | Venta Contado CRC (₡) | Caja CRC (id=106) — DR fijo |
| 2 | `CONTADO_USD` | Venta Contado USD ($) | Caja USD (id=107) — DR fijo |
| 3 | `CREDITO_CRC` | Venta a Crédito CRC (₡) | BankMovement `COBRO-CRC` (id=9) |
| 4 | `CREDITO_USD` | Venta a Crédito USD ($) | BankMovement `COBRO-USD` (id=10) |

**`SalesInvoice`** (flujo completo):
- `NumberInvoice` se genera automáticamente al confirmar: `FV-YYYYMMDD-NNN` (secuencial por día).
- Cada línea tiene el campo `IsNonProductLine` (`bit`, default `false`):
  - `false` = línea de producto con stock. **`IdInventoryLot` obligatorio** al confirmar (CHECK constraint en BD y validación en servicio).
  - `true` = línea de flete/servicio/gasto. `IdInventoryLot` puede ser `null`; no genera COGS ni decrementa inventario.
- Líneas con `IsNonProductLine = false` decrementan el lote al confirmar y hacen snapshot de `UnitCost`.
- `QuantityBase` en línea = `Quantity × ConversionFactor` de la `ProductUnit`.
- Al anular: revierte `lot.QuantityAvailable`, anula asientos contables (ingresos y COGS), anula el `BankMovement` auto-creado si aplica.

**Asiento contable al confirmar venta a crédito** (neto efectivo):
- `DR Cuentas por Cobrar` (121/122) ← via `BankMovement` auto-creado con tipo COBRO-CRC/USD
- `CR Ingresos por Ventas` (117) ← asiento de factura de venta
- `DR Costo de Ventas` (119) + `CR Inventario` (109) ← asiento COGS

**Cuentas en plan contable** (seed en migración `ModuloVentas`):

| IdAccount | Código | Nombre | Tipo |
|---|---|---|---|
| 116 | `4.5` | Ingresos por Ventas | Ingreso (agrupadora) |
| 117 | `4.5.01` | Ingresos por Ventas — Mercadería | Ingreso |
| 118 | `5.15` | Costo de Ventas | Gasto (agrupadora) |
| 119 | `5.15.01` | Costo de Ventas — Mercadería | Gasto |

**Cuentas en plan contable** (seed en migración `ConfiguracionCuentasVenta` — 5-abr-2026):

| IdAccount | Código | Nombre | Tipo |
|---|---|---|---|
| 120 | `1.1.08` | Cuentas por Cobrar | Activo (agrupadora) |
| 121 | `1.1.08.01` | Cuentas por Cobrar — Clientes CRC (₡) | Activo |
| 122 | `1.1.08.02` | Cuentas por Cobrar — Clientes USD ($) | Activo |

**BankMovementType** agregados (seed en migración `ConfiguracionCuentasVenta`):

| Id | Código | Nombre | Signo | Contrapartida |
|---|---|---|---|---|
| 9 | `COBRO-CRC` | Cobro de Venta a Crédito (₡) | Abono | 121 CxC CRC |
| 10 | `COBRO-USD` | Cobro de Venta a Crédito ($) | Abono | 122 CxC USD |

---

## 3. Gaps por categoría

### 3.1 Gaps de validación (sin restricción en servicio)

| # | Contexto | Regla ausente | Estado |
|---|---|---|---|
| V1 | `ProductService` | `IdProductParent` no puede apuntar a un producto que ya tiene padre | ⏳ Pendiente |
| V2 | `ProductUnitService` | Exactamente 1 `IsBase=true` por producto | ✅ Resuelto 5-abr-2026 |
| V3 | `ProductUnitService` | `IsBase=true` requiere `ConversionFactor=1.0` y `IdUnit=product.IdUnit` | ✅ Resuelto 5-abr-2026 |
| V4 | `ProductRecipeService` | `IdProductOutput` no puede ser tipo "Materia Prima" o "Reventa" | ✅ Resuelto 5-abr-2026 |
| V5 | `ProductRecipeService` | `IdProductInput` no puede ser igual a `IdProductOutput` | ✅ Resuelto 5-abr-2026 |
| V6 | `ProductAccountService` | Suma de `PercentageAccount` por producto no debe superar 100 | ✅ Resuelto 5-abr-2026 |
| V7 | `ProductService Delete` | Mensaje claro si el producto tiene lotes o facturas asociadas | ✅ Resuelto 5-abr-2026 |
| V8 | `ProductUnitService` | `SalePrice >= 0` | ✅ Resuelto 5-abr-2026 |
| V9 | `ProductService` | Si `HasOptions=true`, debe existir al menos 1 `ProductOptionGroup` | ⏳ Pendiente |
| V10 | `ProductService` | Si `IsCombo=true`, debe existir al menos 1 `ProductComboSlot` | ⏳ Pendiente |
| V11 | `ProductService` | La combinación `HasOptions=true` e `IsCombo=true` en el mismo producto requiere política explícita | ⏳ Pendiente |

### 3.2 Gaps de lógica de negocio

| # | Contexto | Gap | Estado |
|---|---|---|---|
| L1 | `PurchaseInvoiceService.ConfirmAsync` | No crea `InventoryLot` ni actualiza `AverageCost` | ✅ Resuelto 5-abr-2026 |
| L2 | `PurchaseInvoiceLine.QuantityBase` | Siempre = `Quantity`; la conversión por `ConversionFactor` nunca se aplica | ✅ Resuelto 5-abr-2026 |
| L3 | `InventoryAdjustmentService.CancelAsync` | No revierte deltas de inventario ni `AverageCost` | ✅ Resuelto 5-abr-2026 |
| L4 | `PurchaseInvoiceService.CancelAsync` | No revierte inventario (derivado de L1) | ⏳ Pendiente — requiere política de bloqueo o ajuste automático |
| L5 | `ProductRecipeService` | Sin detección de ciclos en ingredientes | ⏳ Pendiente |
| L6 | `ProductOptionGroup` / `ProductComboSlot` | Precio final de un configurado/combo no calculado en ningún endpoint | ⏳ Pendiente |

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

- **CRUD de `Product`**: Creación, actualización y eliminación con validación de código único (409 por índice `UQ_product_codeProduct`). Delete ahora retorna `409 Conflict` con mensaje descriptivo si el producto tiene lotes activos, líneas de factura o es insumo en recetas (V7).
- **Validaciones `ProductUnit`**: exactamente 1 `IsBase=true` por producto, `ConversionFactor=1` para base, `SalePrice >= 0` (V2/V3/V8).
- **Validaciones `ProductRecipe`**: tipo de output válido (solo Proceso/Terminado), sin self-reference (V4/V5).
- **Validación `ProductAccount`**: suma acumulada de porcentajes no supera 100 (V6).
- **Búsqueda de `ProductUnit` por barcode**: `GET /product-units/barcode/{barcode}.json` funciona correctamente.
- **Confirmación de `PurchaseInvoice`**: Genera asiento contable, crea `InventoryLot` por línea con `UnitCost = UnitPrice/ConversionFactor` y `QuantityAvailable = Quantity×ConversionFactor`, actualiza `AverageCost` WACC (L1+L2 resueltos).
- **Auto-creación de movimiento bancario** al confirmar factura con `CounterpartFromBankMovement=true` y sin movimiento previo vinculado.
- **Confirmación de `InventoryAdjustment`**: Genera número secuencial `AJ-YYYYMMDD-NNN`, aplica deltas, recalcula `AverageCost` y genera asiento contable según `InventoryAdjustmentType`.
- **Anulación de `InventoryAdjustment`**: Revierte deltas de inventario, valida stock negativo, recalcula `AverageCost`, anula asientos (L3 resuelto).
- **Asociación M:N producto-categoría** vía `POST /product-categories/{id}/products/{idProduct}`.
- **`ProductRecipe` con líneas**: Creación atómica de receta + ingredientes en un solo request.
- **CRUD de `ProductOptionGroup` + `ProductOptionItem`**: Validaciones completas (HasOptions, MaxSelections, IsDefault count).
- **CRUD de `ProductComboSlot` + `ProductComboSlotProduct`**: Validaciones completas (IsCombo, sin combos anidados, sin duplicados).
- **CRUD de `SalesInvoiceType`**: Catálogo con 3 tipos preconfigurados (CONTADO_CRC, CONTADO_USD, CREDITO). Define cuentas de ingresos, COGS e inventario.
- **`SalesInvoice` completo**:
  - CRUD Borrador con validación de estado antes de modificar/eliminar.
  - `ConfirmAsync`: valida que líneas de producto (`IsNonProductLine=false`) tengan `IdInventoryLot`; genera `NumberInvoice = "FV-YYYYMMDD-NNN"`, asiento de ingresos (DR contrapartida / CR ingresos por ProductAccount o fallback), decrementa `lot.QuantityAvailable` solo en líneas de producto, snapshot `UnitCost` + `QuantityBase` en línea, genera asiento COGS si cuentas configuradas. Líneas `IsNonProductLine=true` solo aportan al asiento de ingresos.
  - `CancelAsync`: revierte lotes, anula asientos contables, anula `BankMovement` auto-creado si aplica.

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

El sistema tiene el modelo de datos correcto y bien estructurado. Los endpoints CRUD base funcionan. El flujo contable de facturas opera correctamente.

**Implementado el 5-abr-2026 (plan-cambios.md completo):**

- ✅ **L1 + L2**: `PurchaseInvoiceService.ConfirmAsync` crea `InventoryLot` por línea con costo y cantidad base correctos, y recalcula `AverageCost` WACC.
- ✅ **L3**: `InventoryAdjustmentService.CancelAsync` revierte stock, valida negativos, recalcula WACC y anula asientos.
- ✅ **V2/V3/V8**: `ProductUnitService` valida base única, factor=1 para base, y precio ≥ 0.
- ✅ **V4/V5**: `ProductRecipeService` valida tipo de output y auto-referencia.
- ✅ **V6**: `ProductAccountService` valida suma ≤ 100.
- ✅ **V7**: `ProductService.DeleteAsync` retorna `409 Conflict` con mensaje descriptivo.
- ✅ **Módulo de Ventas**: entidades `SalesInvoice*`, 5 configuraciones EF, seed de tipos y cuentas (116-119), feature completa con CRUD + confirm + cancel, migración `ModuloVentas` aplicada.

**Gaps pendientes (menor prioridad):**

- **L4**: anulación de factura de compra no revierte inventario. Requiere política explícita (bloqueo si hay consumos, o ajuste negativo automático).
- **L5**: detección de ciclos en recetas — complejidad algorítmica alta, bajo impacto operativo inmediato.
- **V1**: validación de profundidad de `IdProductParent`.
- **V9/V10/V11**: validaciones de `HasOptions`/`IsCombo` — baja prioridad hasta tener frontend de gestión.
- **L6**: endpoint de precio calculado para configurados y combos.
- **A1-A5**: paginación, filtros de búsqueda y respuestas enriquecidas.
- **Cuentas de `SalesInvoiceType`**: los tipos seed (CONTADO_CRC/USD, CREDITO) tienen `IdAccountSalesRevenue`, `IdAccountCOGS` e `IdAccountInventory` en `null`. Deben configurarse manualmente vía `PUT /sales-invoice-types/{id}` para que la confirmación genere los asientos correctamente.
