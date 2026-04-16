# Casos de Uso de Inventario — Análisis y Brechas

> Fecha: abril 2026  
> Base: modelo EF Core actual + pruebas de flujo end-to-end por cada escenario

---

## Los 6 casos de uso

| # | Nombre | Ejemplo | Patrón |
|---|--------|---------|--------|
| C1 | Reventa | Coca-Cola — compro y vendo con ganancia | Compra → Lote → Venta |
| C2 | Manufactura | Chile embotellado — compro MP, produzco y vendo producto terminado | Compra MP → Producción → Lote terminado → Venta |
| C3 | Ensamble en venta | Hot dog — los ingredientes se descuentan hasta que se factura | Compra ingredientes → Venta con explosión BOM |
| C4 | Variantes | Ropa — camisa por talla y color; al facturar se selecciona la variante exacta | Compra por variante → Venta con selector de talla/color |
| C5 | Pedido configurado | Pizza — el cliente define tamaño, masa, sabor e ingredientes extra; pasa a producción y se entrega | Pedido + opciones → Producción → Entrega |
| C6 | Combo configurado multi-slot | "2 pizzas grandes + bebida" — el combo preselecciona tamaño y masa, el cliente elige el sabor de cada pizza y la bebida | Combo (N slots) → selección por slot + opciones → N producciones → Entrega → Factura jerárquica |

---

## C1 — Reventa (Coca-Cola) ✅ Completo

### Flujo esperado

```
PurchaseInvoice (confirmar)
  └──► InventoryLot  [SourceType='Compra', QuantityAvailable, UnitCost]
         └──► SalesInvoiceLine.IdInventoryLot (operador asigna lote)
                └──► SalesInvoice.ConfirmAsync → DeductLotAsync
                       └──► lot.QuantityAvailable -= qty
                            SalesInvoiceLine.UnitCost = snapshot costo
                            COGS accounting entry
```

### Estado

| Paso | Estado |
|---|---|
| `PurchaseInvoiceService.ConfirmAsync` crea `InventoryLot` con `SourceType="Compra"` | ✅ |
| Calcula `quantityBase = Qty × ConversionFactor` y guarda en lote | ✅ |
| Recalcula `Product.AverageCost` (WAC) al confirmar compra | ✅ |
| `SalesInvoice.ConfirmAsync` valida que la línea tenga `IdInventoryLot` asignado | ✅ |
| `DeductLotAsync` descuenta `QuantityAvailable`, snapshot de costo, genera COGS | ✅ |
| Libera `QuantityReserved` si el lote estaba reservado desde un `SalesOrder` | ✅ |
| `CancelAsync` devuelve `QuantityAvailable` al lote | ✅ |

### Pendiente / Fricción

- El operador debe seleccionar el lote manualmente en la factura. No hay FEFO automático en reventa directa (sí existe en C3 y C2). Si hay muchos lotes abiertos, la UX es tediosa.
- No hay endpoint `GET /inventory-lots/suggest/{id}?idWarehouse=` visible en C1 — el frontend debe usarlo explícitamente para sugerir al operador cuál lote elegir.

---

## C2 — Manufactura (Chile embotellado) ✅ Completo

### Flujo esperado

```
PurchaseInvoice (confirmar MP)
  └──► InventoryLot MP  [QuantityAvailable para cada materia prima]

ProductionOrder (Modalidad A — stock)
  ├── ProductionOrderLine → insumos requeridos
  └──► status: Borrador → Pendiente → EnProceso → Completado
         └──► [AL COMPLETAR]
                InventoryAdjustment tipo PRODUCCION
                  ├── líneas SALIDA: consume lotes de MP (FEFO)
                  ├── línea ENTRADA: crea lote del producto terminado
                  └── ProductionSnapshot (snapshot de receta usada vs real)

SalesInvoiceLine → lote del producto terminado → DeductLotAsync
```

### Estado

| Paso | Estado |
|---|---|
| `ProductionOrder` con `ProductionOrderLine` y status workflow | ✅ |
| `ProductionOrder.IdSalesOrder?` — vínculo opcional con pedido (Modalidad B) | ✅ |
| `ProductionSnapshot / ProductionSnapshotLine` — entidades en BD | ✅ Schema |
| `InventoryAdjustment.IdProductionOrder?` — FK vinculante | ✅ Schema |
| `ProductionOrderService.UpdateStatusAsync` ejecuta lógica de inventario al `Completado` | ✅ |
| Consumir lotes de MP con FEFO al completar producción | ✅ |
| Crear lote del producto terminado al completar producción | ✅ |
| Crear `ProductionSnapshot` con cantidades calculadas vs reales | ✅ |

### Detalles de implementación (abril 2026)

- `ProductionOrder` tiene nuevo campo `IdWarehouse` (nullable, override opcional al completar).
- `PATCH /production-orders/{id}/status` con `StatusProductionOrder: "Completado"` acepta `idWarehouse?` (override) y `lines: [{idProductionOrderLine, quantityProduced}]?` (cantidades reales).
- Stock insuficiente de MP: se permite completar; el lote queda en negativo y la respuesta incluye `warnings[]`.
- Costo unitario del PT = Σ(qty_MP × cost_MP) / qty_producida → WAC recalculado.
- `InventoryAdjustment` tipo `PRODUCCION` (ID=2) se crea automáticamente con asiento contable (DR Costos de Producción / CR Inventario MP).
- `ProductionSnapshot + ProductionSnapshotLine` creados con cantidades teóricas y reales.

### Brecha crítica

~~El `ProductionOrderService.UpdateStatusAsync` **solo cambia el estatus**. Al marcar `Completado` no ocurre ningún movimiento de inventario.~~

### Trabajo necesario para completar C2

~~Completado en abril 2026.~~
   - Crear un `InventoryAdjustment` tipo `PRODUCCION` con:
     - Una línea `SALIDA` por cada insumo (consumo de MP, FEFO por almacén).
     - Una línea `ENTRADA` para el lote del producto terminado.
   - Crear `ProductionSnapshot` con `QuantityCalculated` (de la receta) vs `QuantityReal` (ingresada por el operador).
   - Confirmar el ajuste automáticamente (`autoConfirm = true`).

---

## C3 — Ensamble en venta (Hot dog) ✅ Completo

### Flujo esperado

```
PurchaseInvoice (confirmar ingredientes: pan, salchicha, salsas)
  └──► InventoryLots por ingrediente

SalesInvoiceLine (producto = "Hot Dog", idInventoryLot = NULL)
  └──► SalesInvoice.ConfirmAsync
         └──► DetectaBOM: producto tiene ProductRecipe activa
                └──► ExplodeBomAsync
                       ├── Por cada ProductRecipeLine (pan, salchicha, salsa):
                       │     GetFefoLotAsync (FEFO automático, filtra Disponibles, no vencidos)
                       │     DeductLotAsync (descuenta QuantityAvailable, snapshot UnitCost)
                       │     SalesInvoiceLineBomDetail (registra lote, qty, costo por insumo)
                       └── SalesInvoiceLine.IdProductRecipe = snapshot de receta usada
```

### Estado

| Paso | Estado |
|---|---|
| `ProductRecipe / ProductRecipeLine` — receta con insumos y cantidades | ✅ |
| `ProductRecipe.IsActive` — una sola activa por producto | ✅ |
| `ExplodeBomAsync` en `ConfirmAsync` — detección automática si tiene receta activa | ✅ |
| `GetFefoLotAsync` — FEFO automático, filtra `StatusLot='Disponible'`, `QuantityAvailable > QuantityReserved` | ✅ |
| `SalesInvoiceLineBomDetail` — registro de cada insumo con lote, qty y costo | ✅ |
| `SalesInvoiceLine.IdProductRecipe` — snapshot de versión de receta usada | ✅ |
| `CancelAsync` — revierte todos los `BomDetails` devolviendo `QuantityAvailable` | ✅ |
| Combos con slots que incluyen recetas anidadas (`ExplodeComboAsync`) | ✅ |

### Observación — posible bug en escala de receta

`ExplodeBomAsync` calcula `qtyToConsume = recipeLine.QuantityInput × lineQtyBase`. Esto asume que `QuantityInput` está definido **por unidad base del producto final**. Si la receta define ingredientes para producir `QuantityOutput` unidades (ej: receta para 10 hot dogs) pero `QuantityInput` es el total para esas 10 unidades, el cálculo sobreconsumiría × 10.

> **Verificar**: ¿`ProductRecipeLine.QuantityInput` es "insumo por 1 unidad de output" o "insumo total para `ProductRecipe.QuantityOutput` unidades"?

---

## C4 — Variantes/Tallas/Color (Ropa) � 80% implementado (backend completo, frontend pendiente)

### Flujo esperado

```
Catálogo:
  Producto padre: "Camisa Oxford"  (IsVariantParent = true, sin stock propio)
    ├── Variante: "Camisa Oxford Talla S Azul"  (IdProductParent → padre)
    ├── Variante: "Camisa Oxford Talla M Azul"
    └── Variante: "Camisa Oxford Talla L Rojo"

PurchaseInvoice (confirmar por variante específica)
  └──► InventoryLot por variante  [Camisa Oxford Talla M Azul → X unidades]

SalesInvoice:
  UI: operador busca "Camisa Oxford" → se despliega selector de talla/color
       → elige "Talla M Azul" → se agrega SalesInvoiceLine con IdProduct = variante específica
  └──► ConfirmAsync → DeductLotAsync sobre el lote de la variante exacta
```

### Estado

| Aspecto | Estado |
|---|---|
| `Product.IdProductParent` — FK padre-hijo | ✅ Existe en entidad y DTOs |
| Inventario por variante funciona (InventoryLots son por IdProduct) | ✅ Si se crean las variantes, el inventario es por variante |
| `Product.IsVariantParent` (bool) — marcar el producto padre | ✅ Implementado (migración `AddVariantAttributes` aplicada) |
| `GET /products/{id}/variants.json` — listar hijos con atributos expandidos | ✅ Implementado |
| `IsVariantParent` expuesto en `ProductResponse` y en `CreateProductRequest`/`UpdateProductRequest` | ✅ Implementado |
| Validación en `SalesInvoice.ConfirmAsync`: bloquear venta de producto padre | ✅ Implementado |
| Validación en `SalesOrder.ConfirmAsync`: bloquear confirmación de pedido con producto padre | ✅ Implementado |
| `ProductAttribute` / `AttributeValue` / `ProductVariantAttribute` — sistema de atributos (Opción B) | ✅ Entidades + Fluent API + migración aplicada |
| `GET /products/{id}/attributes/data.json` — lista atributos + valores del padre | ✅ Implementado |
| `POST/PUT/DELETE /products/{id}/attributes` — CRUD de atributos | ✅ Implementado |
| `POST/PUT/DELETE /products/{id}/attributes/{attrId}/values` — CRUD de valores | ✅ Implementado |
| `POST /products/{id}/variants/generate` — generación automática desde cartesiano de atributos | ✅ Implementado |
| UI Angular: al agregar producto al carrito, si es padre → abrir selector de variantes | ⏳ **Pendiente (frontend)** |

---

## C5 — Pedido configurado (Pizza) ✅ Completo

### Flujo esperado

```
SalesOrder:
  SalesOrderLine (Pizza Grande)
    └── SalesOrderLineOptions (opciones elegidas por el cliente):
          - Tamaño: Grande (+$2.00)
          - Masa: Delgada ($0)
          - Sabor: Pepperoni (+$1.50)
          - Extra: Doble Queso (+$0.75, consume 0.1kg de IdProductQueso)

SalesOrder.ConfirmAsync → status "Confirmado"

[Operador envía a producción]
POST /sales-orders/{id}/send-to-production
  └──► crea ProductionOrder (Modalidad B — contra pedido)
         └──► ProductionOrderLine con insumos del combo + extras de opciones
  └──► SalesOrder.StatusOrder = "EnProduccion"

ProductionOrder.Completado:
  └──► consume lotes de ingredientes (FEFO)
       └──► crea lote del producto terminado

[Entrega al cliente]
POST /sales-orders/{id}/complete
  └──► SalesOrder.StatusOrder = "Completado"
POST /sales-orders/{id}/invoice  (o crear SalesInvoice manual vinculada al pedido)
  └──► SalesInvoice factura el lote del producto terminado
```

### Estado

| Paso | Estado |
|---|---|
| `ProductOptionGroup / ProductOptionItem` — opciones del producto con `PriceDelta` | ✅ |
| `Product.HasOptions = true` — flag para activar opciones | ✅ |
| `SalesOrder` con status workflow (Borrador→Confirmado→…→Completado) | ✅ Parcial |
| `ProductionOrder.IdSalesOrder?` — vínculo OP ↔ Pedido | ✅ Schema |
| `SalesOrderLineFulfillment` tipo `"Produccion"` con `IdProductionOrder` | ✅ |
| `SalesOrderLine` almacena opciones seleccionadas por el cliente | ✅ `SalesOrderLineOption` (migración `AddC5Options`) |
| `ProductOptionItem.IdProductRecipe` — opción vincula a receta de insumos (reemplaza `IdProductExtra/QuantityExtra`) | ✅ Implementado |
| Opciones condicionales: `ProductOptionItemAvailability` — filtrar items por selección previa (ej: Masa según Tamaño) | ✅ Entidad + CRUD + validación en `SalesOrderService` |
| `POST /sales-orders/{id}/send-to-production` — crea OP automáticamente | ✅ Implementado |
| Transición automática de `SalesOrder` a `"EnProduccion"` | ✅ Implementado |
| `ProductionOrder.Completado` ejecuta movimiento de inventario | ✅ Ver C2 |
| `POST /sales-orders/{id}/complete` | ✅ Implementado |
| `POST /sales-orders/{id}/invoice` — generar factura desde pedido | ✅ Implementado |

### Diseño de opciones con impacto de stock

`ProductOptionItem` solo tiene `PriceDelta`. La opción "Extra Queso" sube el precio pero no descuenta ingredientes del inventario. El approach inicial propuesto era `IdProductExtra + QuantityExtra`, pero eso solo soporta **un ingrediente por opción**. "Formato Grande" necesita más harina, más agua, más salsa — múltiples ingredientes.

#### Diseño correcto: cada opción vincula a una `ProductRecipe` (M8 — revisado)

```sql
ProductOptionItem (cambio):
  -- ❌ Descartar: IdProductExtra INT? FK → Product
  -- ❌ Descartar: QuantityExtra  DECIMAL(12,4)
  IdProductRecipe   INT? FK → ProductRecipe   -- la "fórmula" de la opción (NULL = solo precio)

SalesOrderLineOption (nueva tabla):
  IdSalesOrderLineOption   INT PK
  IdSalesOrderLine         INT FK (CASCADE)
  IdProductOptionItem      INT FK
  Quantity                 DECIMAL(12,4) DEFAULT 1

SalesInvoiceLineOption (nueva tabla):
  IdSalesInvoiceLineOption  INT PK
  IdSalesInvoiceLine        INT FK (CASCADE)
  IdProductOptionItem       INT FK
  Quantity                  DECIMAL(12,4) DEFAULT 1
```

#### Cómo quedaría el ejemplo completo

```
ProductRecipes (catálogo de fórmulas):
  "Base Pizza"        → [harina 400g, agua 250ml, levadura 5g]
  "Formato Grande"    → [harina 150g, agua 80ml, salsa 30g]    ← delta aditivo
  "Masa Delgada"      → [aceite de oliva 15ml]
  "Sabor Pepperoni"   → [pepperoni 80g, salsa extra 20g]
  "Extra Queso"       → [queso mozzarella 50g]

ProductOptionItems:
  "Grande (+$2.00)"    → IdProductRecipe → "Formato Grande"
  "Masa Delgada ($0)"  → IdProductRecipe → "Masa Delgada"
  "Pepperoni (+$1.50)" → IdProductRecipe → "Sabor Pepperoni"
  "Extra Queso (+$0.75)" → IdProductRecipe → "Extra Queso"

Pedido del cliente → SalesOrderLineOptions:
  Grande, Masa Delgada, Pepperoni, Extra Queso

→ send-to-production combina receta base + todas las fórmulas de opciones:
  harina:      400 + 150 = 550g
  agua:        250 + 80  = 330ml
  levadura:    5g
  aceite:      15ml
  pepperoni:   80g
  salsa:       30 + 20   = 50g
  mozzarella:  50g

→ ProductionOrder.Completado:
  descuenta cada ingrediente del inventario (FEFO)
  crea lote del producto terminado

→ SalesInvoice (lo que ve el cliente):
  Pizza                | 1 | $10.00
  + Formato Grande     |   |  $2.00
  + Masa Delgada       |   |  $0.00
  + Pepperoni          |   |  $1.50
  + Extra Queso        |   |  $0.75
                 Total |   | $14.25
  (el inventario ya fue descontado en producción; aquí solo se cobra)
```

> **Nota sobre recetas de opciones — absolutas vs delta**: Las recetas de opciones son **absolutas** (cada opción contiene su composición completa, independiente de la base). Esto es más simple de implementar y de mantener para el operador. La lógica de `send-to-production` simplemente agrega todos los `ProductRecipeLines` de la receta base del producto más los de cada opción seleccionada, agrupando por `IdProduct` y sumando cantidades.

En `ConfirmAsync` (factura): si la línea tiene `SalesInvoiceLineOptions` cuyo `ProductOptionItem.IdProductRecipe IS NOT NULL`, generar `BomDetail` adicionales para cada ingrediente de esas recetas.

### Implementación completada (abril 2026)

| Tarea | Estado |
|---|---|
| Migración `AddC5Options`: `SalesOrderLineOption`, `SalesInvoiceLineOption`, `ProductOptionItemAvailability`, FK `ProductOptionItem.IdProductRecipe` | ✅ Aplicada |
| `ProductOptionItem.IdProductRecipe` — vincula opción a receta de insumos | ✅ En DTOs, service y validación |
| `SalesOrderLineOption` — almacena opciones por línea; `PopulateOptionsAsync` ajusta `UnitPrice += Σ(PriceDelta × Qty)` | ✅ |
| `ValidateOptionsAsync` — T9: ownership por grupo + unicidad grupo por línea; T10: reglas de disponibilidad | ✅ |
| `POST /availability-rules`, `DELETE /availability-rules/{id}`, `GET /available-by-product/{id}` | ✅ |
| `POST /sales-orders/{id}/send-to-production` — combina receta base + recetas de opciones, crea OP Modalidad B, transición `EnProduccion` | ✅ |
| `POST /sales-orders/{id}/complete` — valida que todas las OPs vinculadas estén `Completado` | ✅ |
| `POST /sales-orders/{id}/invoice` — crea `SalesInvoice` con `SalesInvoiceLineOptions` copiadas; 409 si ya existe | ✅ |
| `SalesInvoiceService.ConfirmAsync` (T19) — omite `ExplodeBomAsync` cuando la línea ya tiene `IdInventoryLot` pre-asignado desde producción | ✅ |

---
## C6 — Combo configurado multi-slot (2 pizzas + bebida) 🟡 55% implementado

### Diferencia clave con C5

C5 es **un producto configurado individualmente**: una pizza con sus opciones → una producción.

C6 es un **agrupador** (`IsCombo = true`) que contiene N slots. Cada slot puede ser un producto a escoger de una lista y, si ese producto es configurable (tiene `HasOptions = true`), el cliente también elige sus opciones **dentro del contexto del slot**. Adicionalmente, el combo puede **prefijar opciones** en cada slot (ej: el slot "Pizza #1" ya tiene Tamaño=Grande y Masa=Delgada fijados), dejando sólo Sabor y Extras libres para el cliente.

### Flujo esperado

```
Catálogo:
  Producto: "Combo 2 Pizzas + Bebida"  (IsCombo = true, precio base $25.00)
    Slot 1: "Pizza #1"  (productos permitidos: [Pizza Grande])  quantity=1
      PresetOptions: [Tamaño → Grande, Masa → Delgada]   ← fijados, no editables por el cliente
      OpenOptionGroups: [Sabor (requerido), Extras (opcional)]  ← el cliente elige
    Slot 2: "Pizza #2"  (productos permitidos: [Pizza Grande])  quantity=1
      PresetOptions: [Tamaño → Grande, Masa → Delgada]
      OpenOptionGroups: [Sabor (requerido), Extras (opcional)]
    Slot 3: "Bebida"    (productos permitidos: [Coca-Cola, Sprite, Agua Mineral])  quantity=1
      PresetOptions: []     ← sin opciones fijas
      OpenOptionGroups: []  ← el producto de bebida no tiene grupos de opciones

SalesOrder (pedido del cliente):
  SalesOrderLine (Combo 2 Pizzas + Bebida)  → $25.00 base
    SalesOrderLineComboSlotSelection  [slot=Pizza #1, producto=Pizza Grande]
      SalesOrderLineSlotOption: Tamaño → Grande    (isPreset=true)
      SalesOrderLineSlotOption: Masa   → Delgada   (isPreset=true)
      SalesOrderLineSlotOption: Sabor  → Pepperoni (+$1.50)  ← cliente eligió
      SalesOrderLineSlotOption: Extra  → Doble Queso (+$0.75) ← cliente eligió
    SalesOrderLineComboSlotSelection  [slot=Pizza #2, producto=Pizza Grande]
      SalesOrderLineSlotOption: Tamaño → Grande    (isPreset=true)
      SalesOrderLineSlotOption: Masa   → Delgada   (isPreset=true)
      SalesOrderLineSlotOption: Sabor  → Hawaiian  (+$1.00)
    SalesOrderLineComboSlotSelection  [slot=Bebida, producto=Coca-Cola]
      (sin opciones)

SalesOrder.ConfirmAsync → status "Confirmado"

POST /sales-orders/{id}/send-to-production
  → Por cada slot con receta (Pizza #1, Pizza #2):
      Crea ProductionOrder (Modalidad B) con insumos:
        receta base del producto del slot + recetas de cada opción del slot
        agrupando ingredientes y sumando cantidades (igual que C5)
  → Slot Bebida: no envía a producción (es reventa directa)
  → SalesOrder.StatusOrder = "EnProduccion"

ProductionOrder #1 (Pizza #1 — Grande + Delgada + Pepperoni + Doble Queso):
  insumos: harina 550g, agua 330ml, levadura 5g, aceite 15ml,
           pepperoni 80g, salsa extra 20g, mozzarella 50g

ProductionOrder #2 (Pizza #2 — Grande + Delgada + Hawaiian):
  insumos: harina 550g, agua 330ml, levadura 5g, aceite 15ml,
           piña 60g, jamón 80g

ProductionOrders.Completado (por cada una):
  → consume lotes de ingredientes (FEFO)
  → crea lote del producto terminado (Pizza #1 terminada, Pizza #2 terminada)

POST /sales-orders/{id}/complete
  → SalesOrder.StatusOrder = "Completado"

POST /sales-orders/{id}/invoice  →  SalesInvoice vinculada al pedido
  Factura visible al cliente:
  ─────────────────────────────────────────────────────────────────
  Combo 2 Pizzas + Bebida          1 ×   $25.00  =  $25.00
    Pizza #1                       1 ×    $0.00  [incluida en combo]
      + Tamaño: Grande                    $0.00  [incluido]
      + Masa: Delgada                     $0.00  [incluido]
      + Sabor: Pepperoni                  $1.50
      + Extra: Doble Queso                $0.75
    Pizza #2                       1 ×    $0.00  [incluida en combo]
      + Tamaño: Grande                    $0.00  [incluido]
      + Masa: Delgada                     $0.00  [incluido]
      + Sabor: Hawaiian                   $1.00
    Bebida: Coca-Cola               1 ×    $0.00  [incluida en combo]
                                         ───────
                                Total:  $28.25
  ─────────────────────────────────────────────────────────────────
  (el movimiento de inventario ya ocurrió en producción;
   en la factura solo se cobra — igual que C5)
```

### Estado — comparación con el modelo existente

| Aspecto | Estado | Observación |
|---|---|---|
| `Product.IsCombo = true` — estructura del combo | ✅ Existe | Misma entidad `Product` |
| `ProductComboSlot` — slots con nombre, cantidad y productos permitidos | ✅ Existe | Entidad completa con configuración Fluent API |
| `ProductComboSlotProduct` — lista de productos permitidos por slot | ✅ Existe | `PriceAdjustment` + `SortOrder` |
| `ExplodeComboAsync` — explosión al confirmar `SalesInvoice` | ✅ Parcial | **Crítico**: toma `slot.ProductComboSlotProducts.FirstOrDefault()` — no hay selección del cliente |
| `SalesInvoiceLineBomDetail.IdProductComboSlot` — trazabilidad por slot | ✅ Existe | Funciona para el caso actual (sin selección) |
| `ProductOptionGroup / ProductOptionItem` — opciones con `PriceDelta` | ✅ Existe | Base para opciones dentro del slot |
| `Product.HasOptions = true` | ✅ Existe | Necesario para saber si el producto del slot tiene opciones |
| **`ProductComboSlotPresetOption`** — opciones preseleccionadas por slot, no editables por el cliente | ❌ **No existe** | Nueva tabla requerida |
| **Selección de producto por slot en pedido** (`SalesOrderLineComboSlotSelection`) | ❌ **No existe** | Nueva tabla requerida |
| **Opciones elegidas dentro de cada slot** (`SalesOrderLineSlotOption`) | ❌ **No existe** | Nueva tabla requerida |
| **`ExplodeComboAsync` respeta la selección del cliente por slot** | ❌ **No implementado** | Hoy siempre usa el primer producto del slot |
| **`ExplodeBomWithOptionsAsync`** — receta base del producto del slot + recetas de sus opciones | ❌ **No existe** | Extensión de `ExplodeBomAsync` para opciones del slot |
| **`send-to-production` para combo**: N OPs, una por slot con receta | ❌ **No existe** | Extensión del `send-to-production` de C5 (que tampoco existe aún) |
| **`SalesInvoiceLineComboSlotSelection`** — snapshot de selecciones por slot en factura | ❌ **No existe** | Nueva tabla requerida |
| **`SalesInvoiceLineSlotOption`** — opciones del slot en factura (con precio aplicado) | ❌ **No existe** | Nueva tabla requerida |
| **Jerarquía visual en factura**: agrupador → slot → opciones → ingredientes | ❌ **No existe** | Los `BomDetail` existen pero son planos; falta capa de slots/opciones |

### Brecha crítica — `ExplodeComboAsync` ignora la selección del cliente

El combo actual hardcodea `slot.ProductComboSlotProducts.FirstOrDefault()` (línea ~803 en `SalesInvoiceService.cs`). Para C6 esta línea debe sustituirse por la selección real del cliente guardada en `SalesInvoiceLineComboSlotSelection`. Si no existe esa selección, el combo cobra pero descuenta el stock incorrecto.

### Nuevas entidades necesarias

```sql
-- Opciones preseleccionadas en el catálogo del slot (fijadas por el negocio, no editables por el cliente)
ProductComboSlotPresetOption (nueva):
  IdProductComboSlotPresetOption  INT PK AUTO
  IdProductComboSlot              INT FK → productComboSlot (CASCADE)
  IdProductOptionItem             INT FK → productOptionItem (RESTRICT)
  -- Índice único: un mismo item no puede repetirse en el mismo slot
  UQ (IdProductComboSlot, IdProductOptionItem)

-- Selección del cliente para cada slot al hacer el pedido
SalesOrderLineComboSlotSelection (nueva):
  IdSalesOrderLineComboSlotSelection  INT PK AUTO
  IdSalesOrderLine                    INT FK → salesOrderLine (CASCADE)
  IdProductComboSlot                  INT FK → productComboSlot (RESTRICT)
  IdProduct                           INT FK → product (RESTRICT)  ← producto elegido del slot
  SortOrder                           INT DEFAULT 0
  -- Índice único: un slot aparece una sola vez por línea de pedido
  UQ (IdSalesOrderLine, IdProductComboSlot)

-- Opciones elegidas dentro de cada selección de slot (incluye presets copiados + libres elegidos)
SalesOrderLineSlotOption (nueva):
  IdSalesOrderLineSlotOption           INT PK AUTO
  IdSalesOrderLineComboSlotSelection   INT FK (CASCADE)
  IdProductOptionItem                  INT FK → productOptionItem (RESTRICT)
  IsPreset                             BIT DEFAULT 0   ← copiado de ProductComboSlotPresetOption
  Quantity                             DECIMAL(12,4) DEFAULT 1

-- Contraparte en factura (snapshot inmutable al confirmar)
SalesInvoiceLineComboSlotSelection (nueva):
  IdSalesInvoiceLineComboSlotSelection INT PK AUTO
  IdSalesInvoiceLine                   INT FK → salesInvoiceLine (CASCADE)
  IdProductComboSlot                   INT FK → productComboSlot (RESTRICT)
  IdProduct                            INT FK → product (RESTRICT)
  SortOrder                            INT DEFAULT 0
  UQ (IdSalesInvoiceLine, IdProductComboSlot)

SalesInvoiceLineSlotOption (nueva):
  IdSalesInvoiceLineSlotOption         INT PK AUTO
  IdSalesInvoiceLineComboSlotSelection INT FK (CASCADE)
  IdProductOptionItem                  INT FK → productOptionItem (RESTRICT)
  IsPreset                             BIT DEFAULT 0
  PriceDeltaApplied                    DECIMAL(18,4) DEFAULT 0  ← snapshot del precio al momento de confirmar
  Quantity                             DECIMAL(12,4) DEFAULT 1
```

### Flujo de `ExplodeComboAsync` revisado para C6

```
ExplodeComboAsync (revisado):
  Cargar SalesInvoiceLineComboSlotSelections de la línea con sus SlotOptions

  Por cada SalesInvoiceLineComboSlotSelection:
    slotProduct  = selection.IdProduct       ← producto real elegido por el cliente
    options      = selection.SalesInvoiceLineSlotOptions
    recipeBase   = ProductRecipe activa de slotProduct (si existe)

    Si recipeBase existe:
      → ExplodeBomWithOptionsAsync(
            recipeBase,
            opcionesDelSlot = options WHERE IdProductOptionItem.IdProductRecipe IS NOT NULL,
            comboSlotId,  slotQty)
          ├── Por cada línea de recipeBase: consume insumo (FEFO) → BomDetail
          └── Por cada opción con IdProductRecipe: consume insumos de esa receta → BomDetail
    Si no tiene receta (reventa directa):
        → GetFefoLotAsync(slotProduct) → DeductLotAsync → BomDetail (IdProductComboSlot populado)
```

> **Relación con `SalesInvoiceLineBomDetail` existente**: No se modifica la tabla `SalesInvoiceLineBomDetail`. El campo `IdProductComboSlot` que ya tiene es suficiente para identificar a qué slot pertenece cada movimiento. La nueva tabla `SalesInvoiceLineComboSlotSelection` es sólo la capa de selección y opciones por encima.

### Modelo de precio del combo

```
Precio final = PrecioBase(combo)
             + Σ PriceDelta(opciones libres elegidas por slot)
             + Σ PriceAdjustment(producto elegido en slot, si es diferente del predeterminado)

Ejemplo:
  PrecioBase("Combo 2 Pizzas + Bebida") = $25.00
  Slot 1 — Pizza Pepperoni: +$1.50  (opción Sabor)
           Pizza Doble Queso: +$0.75  (opción Extra)
  Slot 2 — Pizza Hawaiian:   +$1.00  (opción Sabor)
  Slot 3 — Coca-Cola:        +$0.00  (sin ajuste)
  ─────────────────────────────────
  Total: $28.25
```

Las opciones `isPreset = true` tienen `PriceDelta = $0.00` porque ya están incluidas en el precio base del combo. Sólo las opciones libres pueden tener delta positivo.

---

### Diseño potencial: opciones condicionales (dependencias entre grupos)

> **Aplica a C5 y C6**. Si el cliente elige "Tamaño Grande" puede seleccionar cualquier masa (Delgada, Artesanal, Clásica, Rellena). Si elige "Tamaño Pequeño", solo puede elegir Delgada y Clásica — Artesanal y Rellena quedan bloqueadas porque físicamente no aplican a ese tamaño.

#### El problema con el modelo actual

`ProductOptionItem` es una entidad plana: `NameItem`, `PriceDelta`, `IsDefault`, `SortOrder`. No existe ningún concepto de "este item solo aparece cuando X está seleccionado". El grupo "Masa" muestra siempre los mismos 4 items sin importar qué se eligió en "Tamaño".

#### Diseño correcto: `ProductOptionItemAvailability` (lista de habilitadores)

Semántica **whitelist**:
- Un item **sin reglas** → siempre disponible (ej: "Masa Delgada", "Masa Clásica").
- Un item **con reglas** → solo disponible cuando al menos uno de sus items habilitadores está seleccionado.

```sql
ProductOptionItemAvailability (nueva tabla):
  IdProductOptionItemAvailability  INT PK AUTO
  IdRestrictedItem   INT FK → productOptionItem (RESTRICT)
                     ← el item que solo aparece bajo condición
                     ← ej: "Masa Artesanal"
  IdEnablingItem     INT FK → productOptionItem (RESTRICT)
                     ← el item de otro grupo cuya selección habilita al anterior
                     ← ej: "Tamaño Grande"
  UQ (IdRestrictedItem, IdEnablingItem)
  -- Ambos items deben pertenecer al mismo producto (validado en service)
  -- IdRestrictedItem y IdEnablingItem deben pertenecer a grupos distintos (validado en service)
```

#### Ejemplo completo

```
Grupos del producto "Pizza":
  Grupo "Tamaño"  (IsRequired=true, MaxSelections=1):
    item 1: "Individual"  → sin reglas → siempre visible
    item 2: "Pequeño"     → sin reglas → siempre visible
    item 3: "Mediano"     → sin reglas → siempre visible
    item 4: "Grande"      → sin reglas → siempre visible

  Grupo "Masa"  (IsRequired=true, MaxSelections=1):
    item 5: "Delgada"    → sin reglas en ProductOptionItemAvailability → siempre visible
    item 6: "Clásica"    → sin reglas → siempre visible
    item 7: "Artesanal"  → reglas: [IdEnablingItem = "Mediano", IdEnablingItem = "Grande"]
    item 8: "Rellena"    → reglas: [IdEnablingItem = "Grande"]

ProductOptionItemAvailability:
  (IdRestrictedItem=7, IdEnablingItem=3)   ← Artesanal disponible si Mediano
  (IdRestrictedItem=7, IdEnablingItem=4)   ← Artesanal disponible si Grande
  (IdRestrictedItem=8, IdEnablingItem=4)   ← Rellena disponible solo si Grande

→ Cliente elige "Pequeño":
    Masa disponibles: Delgada ✅, Clásica ✅, Artesanal ❌, Rellena ❌

→ Cliente elige "Mediano":
    Masa disponibles: Delgada ✅, Clásica ✅, Artesanal ✅, Rellena ❌

→ Cliente elige "Grande":
    Masa disponibles: Delgada ✅, Clásica ✅, Artesanal ✅, Rellena ✅
```

#### Comportamiento esperado en frontend

1. Cuando el cliente cambia la selección en un grupo controlador ("Tamaño"), re-evaluar todos los grupos dependientes.
2. Ocultar o deshabilitar visualmente los items que queden sin habilitador activo.
3. Si el cliente tenía "Artesanal" seleccionada y cambia a "Pequeño" → **auto-deseleccionar** "Artesanal" (no puede quedar una selección inválida).
4. Si el item auto-deseleccionado era el único elegido en un grupo `IsRequired` → mostrar alerta para que el cliente elija una opción válida.

#### Endpoint de apoyo para frontend

```
GET /products/{id}/option-groups/available?activeItems=3,7
  → devuelve todos los grupos con los items filtrados por disponibilidad
     (solo items sin reglas + items cuyo IdEnablingItem esté en activeItems)
  → el frontend llama esto cada vez que el cliente cambia una opción
```

Alternativamente, el backend puede devolver todos los items con un campo `isAvailable: bool` calculado en la respuesta, y el frontend decide si los oculta o los deshabilita.

#### Validación en backend (defensa en profundidad)

Tanto `SalesOrderService` (al guardar líneas con opciones) como `SalesInvoiceService.ConfirmAsync` (antes de procesar inventario) deben verificar:

```
Por cada item seleccionado en la línea:
  Si tiene reglas en ProductOptionItemAvailability:
    → Verificar que al menos uno de sus IdEnablingItem también está seleccionado en la misma línea
    → Si no → error: "La opción 'Artesanal' no está disponible para el tamaño 'Pequeño'."
```

#### Integración con C6 — presets de combo

Si el slot "Pizza #1" tiene `ProductComboSlotPresetOption` con "Tamaño → Grande", cuando la UI renderiza el grupo "Masa" para ese slot debe tratar "Tamaño Grande" como ya activo. Los items de Masa se filtran considerando ese preset. El endpoint `GET /products/{id}/option-groups/available?activeItems=4` devuelve Masa con los 4 items disponibles. El cliente solo elige entre esas opciones sin poder cambiar el Tamaño.

#### Validaciones adicionales al crear reglas

| Validación | Descripción |
|---|---|
| V-DEP-1: grupos distintos | `IdRestrictedItem.IdProductOptionGroup ≠ IdEnablingItem.IdProductOptionGroup` |
| V-DEP-2: mismo producto | Ambos items deben pertenecer al mismo `IdProduct` |
| V-DEP-3: sin ciclos directos | Si A habilita a B, B no puede habilitar a A (se valida en service) |
| V-DEP-4: no auto-referencia | `IdRestrictedItem ≠ IdEnablingItem` |

#### Estado actual vs requerido

| Aspecto | Estado |
|---|---|
| `ProductOptionItem` con campos de dependencia | ❌ **No existe** — entidad plana |
| `ProductOptionItemAvailability` tabla | ❌ **No existe** |
| Frontend filtra items por selección actual | ❌ **No implementado** |
| Backend valida compatibilidad de opciones al confirmar | ❌ **No implementado** |
| Endpoint `GET /option-groups/available?activeItems=` | ❌ **No existe** |
| Auto-deselección en frontend al cambiar grupo controlador | ❌ **No implementado** |

#### Trabajo necesario

| Tarea | Complejidad |
|---|---|
| `ProductOptionItemAvailability` entidad + Fluent API | Muy baja |
| Migración EF Core | Muy baja |
| CRUD endpoints para gestionar reglas (`POST / DELETE /availability-rules`) | Baja |
| `GET /products/{id}/option-groups/available?activeItems=` | Baja |
| Validación en `SalesOrderService` y `SalesInvoiceService.ConfirmAsync` | Media |
| UI Angular: re-evaluar grupos al cambiar selección + auto-deselección | Media |

**Estimación: ~1 día backend + 0.5 día frontend (independiente de C5/C6, se puede hacer en paralelo)**



#### El problema

`ProductOptionGroup` tiene `IdProduct` como FK obligatoria y CASCADE on delete. Un grupo pertenece a **exactamente un producto**. Si el combo tiene un slot "Pizza #1" con producto "Pizza Grande" y presets "Tamaño → Grande, Masa → Delgada", esas opciones están definidas en los grupos de "Pizza Grande".

Si más adelante agregas "Pizza Familiar" al slot como segunda opción permitida, tendrías que crear desde cero los grupos "Tamaño" y "Masa" en "Pizza Familiar" con exactamente los mismos items. Si tienes 20 variantes de pizza, son 20 recreaciones del mismo grupo.

#### Opciones de diseño

**Opción A — `POST /products/{idTarget}/option-groups/copy-from/{idSource}` (recomendada, sin cambio de schema)**

Copia todos los `ProductOptionGroup` + `ProductOptionItem` de un producto origen al producto destino. Después de copiar son independientes: si "Pizza Grande" cambia el precio de "Masa Delgada" a $0.30, eso no afecta a "Pizza Familiar".

```
POST /products/42/option-groups/copy-from/7
  → Crea copia de los grupos "Tamaño/Masa/Sabor/Extras" de producto 7
    en el producto 42, con los mismos items y PriceDelta
```

| Aspecto | Detalle |
|---|---|
| Cambio de schema | ❌ Ninguno |
| Implementación | 1 endpoint + 1 service method (~30 min) |
| Sincronización futura | ❌ No — son copias independientes. Cambiar el original no actualiza las copias |
| UX | Operador crea el grupo "maestro" en un producto y lo copia a los demás con 1 click |

**Opción B — `OptionGroupTemplate` + `OptionGroupTemplateItem` globales (más potente)**

Introduce tablas de plantillas desacopladas de productos. Un producto puede "asignar" una plantilla y sobreescribir el precio por item si lo necesita.

```sql
OptionGroupTemplate:
  IdOptionGroupTemplate  INT PK
  NameGroup              VARCHAR(200)   ← "Masa", "Tamaño", "Sabor"
  IsRequired             BIT
  MinSelections          INT
  MaxSelections          INT
  SortOrder              INT

OptionGroupTemplateItem:
  IdOptionGroupTemplateItem  INT PK
  IdOptionGroupTemplate      INT FK (CASCADE)
  NameItem                   VARCHAR(200)   ← "Delgada", "Gruesa", "Integral"
  DefaultPriceDelta          DECIMAL(18,4)  ← precio sugerido
  SortOrder                  INT

ProductOptionGroup (cambio):
  IdOptionGroupTemplate  INT? FK → OptionGroupTemplate (RESTRICT)
                                   ← NULL = grupo personalizado sin plantilla
ProductOptionItem (cambio):
  IdOptionGroupTemplateItem  INT? FK → OptionGroupTemplateItem (RESTRICT)
                                       ← NULL = item sin vínculo a plantilla
  PriceDelta                 DECIMAL   ← puede diferir del DefaultPriceDelta de la plantilla
```

Al asignar una plantilla a un producto, el sistema crea los `ProductOptionGroup` + `ProductOptionItem` desde la plantilla pero guardando el `IdOptionGroupTemplateItem` como referencia. Esto permite:
- `GET /option-group-templates` — lista de grupos reutilizables en el catálogo
- Saber qué productos usan cada plantilla (para auditoría o bulk-updates)
- Actualizar el `DefaultPriceDelta` de la plantilla y propagar (opcionalmente) a productos vinculados

| Aspecto | Detalle |
|---|---|
| Cambio de schema | 2 tablas nuevas + 2 columnas FK nullable en existentes + migración |
| Implementación | ~2–3 horas backend + UI para gestionar plantillas |
| Sincronización futura | ✅ Opcional — se puede propagar cambios de plantilla a productos vinculados |
| UX | Operador gestiona un catálogo central de grupos, luego los asigna a productos |

#### Recomendación

Para el estado actual del proyecto, **Opción A** es suficiente y tiene costo casi cero. Resuelve el 90% de la fricción (no repetir data entry) sin complejidad de sincronización.

**Opción B** vale la pena si el catálogo de productos crece a decenas de variantes similares (ej: 15 pizzas distintas que todas tienen Tamaño, Masa, Sabor) y el equipo necesita actualizar precios de opciones en masa desde un lugar central.

> **Relación directa con C6**: `ProductComboSlotPresetOption` referencia `IdProductOptionItem`, que pertenece al grupo de opciones del producto en el slot. Si el slot permite elegir entre "Pizza Grande" y "Pizza Familiar", ambos productos deben tener los mismos grupos y los mismos items para que los presets del slot sean consistentes. La Opción A (copy-from) es el mecanismo más simple para garantizarlo.



### Trabajo necesario para completar C6

| Tarea | Complejidad | Prerequisito |
|---|---|---|
| C2 completo (ProductionOrder.Completado → inventario) | Alta | — |
| C5 completo: `ProductOptionItem.IdProductRecipe`, `SalesOrderLineOption`, `send-to-production` | Alta | C2 |
| `ProductComboSlotPresetOption` entidad + configuración Fluent API + endpoints CRUD | Baja | — |
| `SalesOrderLineComboSlotSelection` + `SalesOrderLineSlotOption` entidades | Media | — |
| `SalesInvoiceLineComboSlotSelection` + `SalesInvoiceLineSlotOption` entidades | Media | — |
| `SalesOrderService`: al agregar línea de combo, copiar presets al `SalesOrderLineSlotOption.IsPreset=true` | Baja | tabla entities |
| `ExplodeComboAsync` revisado: usa `SalesInvoiceLineComboSlotSelection` en vez de `FirstOrDefault()` | Media | tablas entities |
| `ExplodeBomWithOptionsAsync`: combina receta base + recetas de opciones (reutiliza lógica de C5) | Media | C5 (`ProductOptionItem.IdProductRecipe`) |
| `send-to-production` para combo: N OPs por slots con receta (puede compartir código con C5) | Media-Alta | C2, C5 |
| Migración EF Core: 5 tablas nuevas | Baja | todas las entidades |
| UI Angular: al agregar combo al pedido, mostrar slots → producto permitido → opciones (presets deshabilitados, libres editables) | Alta | endpoints listos |
| Factura: jerarquía visual Agrupador → Slot → Opciones | Media | `SalesInvoiceLineComboSlotSelection` |
| `ProductOptionItemAvailability` + re-evaluación dinámica en UI (presets del slot activan filtro) | Media | — puede hacerse independiente |

**Estimación total: ~5–7 días backend + UI (bloqueado por C2 y C5)**

---

## Arquitectura transversal — Sesión de carrito dirigida por backend (Redis)

> Aplica a todos los casos (C1–C6). Propuesta de refactoring arquitectural.

### Motivación

El enfoque actual (especialmente en C5 y C6) implica que el frontend carga todos los grupos de opciones, filtra por `ProductOptionItemAvailability`, evalúa qué presets aplican por slot, decide cuándo una línea está completa y calcula el precio — lógica que se duplica en cada canal: app web, app móvil, bot de WhatsApp, bot de Telegram, Messenger, etc. Cualquier cambio en reglas de negocio requiere actualizar todos los clientes.

El backend ya tiene Redis instalado y en uso (`IDistributedCache` vía `StackExchange.Redis`). La solución es que el backend sea la fuente de verdad del flujo de configuración de cada línea del carrito. El frontend simplemente **muestra lo que el backend manda** y **envía lo que el usuario eligió** — sin lógica de validación ni filtrado local.

### Modelo mental: máquina de estados por sesión

```
CartSession (Redis, TTL configurable — default 60 min):
  estado: confirmedLines[] + pendingLine?
  
  pendingLine puede estar en uno de estos estados:
    → WaitingSlotProduct  (C6: cliente elige qué producto va en este slot)
    → WaitingOption       (C5/C6: cliente elige dentro de un grupo de opciones)
    → Complete            (todos los grupos requeridos resueltos → lista para agregar)
  
  El backend avanza el estado después de cada respuesta del cliente.
  Nunca el cliente calcula cuál es el siguiente paso.
```

### Clave Redis

```
cartsession:{sessionId}   →  JSON serializado (TTL sliding, 60 min default)
```

`sessionId` es un `Guid` generado al crear la sesión. Puede asociarse a un `idContact` o ser anónima.

### Estructura del estado en Redis

```json
{
  "sessionId": "550e8400-e29b-41d4-a716-446655440000",
  "idContact": 12,
  "channel": "web",
  "createdAt": "2026-04-05T10:00:00Z",
  "confirmedLines": [
    {
      "seq": 1,
      "idProduct": 8,
      "nameProduct": "Coca-Cola 600ml",
      "quantity": 2,
      "unitPrice": 15.00,
      "lineTotal": 30.00,
      "selectedOptions": [],
      "comboSlots": []
    }
  ],
  "pendingLine": {
    "idProduct": 5,
    "nameProduct": "Pizza",
    "quantity": 1,
    "basePrice": 10.00,
    "optionsDelta": 2.25,
    "selectedOptions": [
      { "idProductOptionItem": 3, "nameItem": "Grande", "priceDelta": 0.00 }
    ],
    "comboSlots": [],
    "currentStep": {
      "type": "WaitingOption",
      "idProductOptionGroup": 2,
      "nameGroup": "¿Qué masa prefieres?",
      "isRequired": true,
      "minSelections": 1,
      "maxSelections": 1,
      "availableItems": [
        { "idProductOptionItem": 5, "nameItem": "Delgada",   "priceDelta": 0.00, "isDefault": true },
        { "idProductOptionItem": 6, "nameItem": "Clásica",   "priceDelta": 0.00, "isDefault": false },
        { "idProductOptionItem": 7, "nameItem": "Artesanal", "priceDelta": 0.50, "isDefault": false }
      ]
    }
  },
  "cartTotal": 30.00
}
```

`availableItems` ya viene filtrado por `ProductOptionItemAvailability` considerando las opciones ya elegidas (`selectedOptions`). El frontend solo pinta la lista — sin filtrar nada.

### Endpoints de la sesión de carrito

```
POST   /cart-sessions
  body: { channel: "web|whatsapp|telegram|messenger|manual", idContact?: int }
  → { sessionId, expiresAt }

GET    /cart-sessions/{sessionId}
  → CartState completo (líneas confirmadas + pendingLine con currentStep si hay)

DELETE /cart-sessions/{sessionId}
  → 204 (abandono de sesión)

── Agregar producto al carrito ──────────────────────────────────────────────

POST   /cart-sessions/{sessionId}/lines/start
  body: { idProduct, quantity }
  → CartStep                              ← primer paso a resolver
    (Si el producto no tiene opciones ni es combo → CartStep.type = "LineComplete"
     directamente, la línea entra a confirmedLines sin pasar por WaitingX)

POST   /cart-sessions/{sessionId}/lines/respond
  body: CartStepResponse                  ← lo que el usuario eligió
  → CartStep                              ← siguiente paso o "LineComplete"

── Gestión de líneas confirmadas ────────────────────────────────────────────

DELETE /cart-sessions/{sessionId}/lines/{seq}
  → CartState actualizado

PUT    /cart-sessions/{sessionId}/lines/{seq}/quantity
  body: { quantity }
  → CartState actualizado

── Finalizar ────────────────────────────────────────────────────────────────

POST   /cart-sessions/{sessionId}/checkout
  body: { type: "SalesOrder" | "SalesInvoice", idWarehouse, ... }
  → SalesOrder o SalesInvoice creado en BD
  → sesión eliminada de Redis
```

### Tipos de `CartStep` que el backend puede devolver

```json
// 1 — Cliente elige qué producto va en un slot (C6)
{
  "type": "WaitingSlotProduct",
  "slot": { "idProductComboSlot": 1, "nameSlot": "Pizza #1", "quantity": 1 },
  "presetOptions": [
    { "idProductOptionItem": 3, "nameItem": "Grande" },
    { "idProductOptionItem": 7, "nameItem": "Delgada" }
  ],
  "allowedProducts": [
    { "idProduct": 5, "nameProduct": "Pizza Grande", "priceAdjustment": 0.00 }
  ]
}

// 2 — Cliente elige dentro de un grupo de opciones (C5 y C6)
{
  "type": "WaitingOption",
  "idProductOptionGroup": 2,
  "nameGroup": "¿Qué masa prefieres?",
  "isRequired": true,
  "minSelections": 1,
  "maxSelections": 1,
  "availableItems": [ ...filtrados por ProductOptionItemAvailability... ]
}

// 3 — Línea completada, entra al carrito
{
  "type": "LineComplete",
  "addedLine": { "seq": 2, "nameProduct": "Pizza", "lineTotal": 12.25, ... },
  "cart": { ...CartState actualizado... }
}
```

### `CartStepResponse` que el cliente envía

```json
// Respuesta a WaitingSlotProduct
{ "type": "SlotProductSelected", "idProductComboSlot": 1, "idProduct": 5 }

// Respuesta a WaitingOption
{ "type": "OptionSelected", "idProductOptionGroup": 2, "selectedItems": [5] }

// Saltar grupo opcional sin elegir
{ "type": "OptionSkipped", "idProductOptionGroup": 4 }
```

### Lógica en `CartSessionService` (backend)

```
start(idProduct, quantity):
  1. Cargar producto: IsCombo? HasOptions? TrackInventory?
  2. Si no requiere configuración → agregar a confirmedLines directamente → LineComplete
  3. Si IsCombo → primera pendingLine con primer slot → WaitingSlotProduct
  4. Si HasOptions → primera pendingLine → WaitingOption (primer grupo IsRequired)

respond(CartStepResponse):
  1. Validar respuesta según tipo de step actual
  2. Aplicar selección al pendingLine en Redis
  3. Evaluar siguiente paso:
     a. ¿Quedan grupos IsRequired sin resolver? → WaitingOption (siguiente grupo,
        con availableItems filtrados por ProductOptionItemAvailability y selecciones actuales)
     b. ¿Combo con slots sin configurar? → WaitingSlotProduct siguiente slot
     c. ¿Slot configurado con producto que HasOptions? → WaitingOption para ese slot
     d. ¿Todo resuelto? → mover pendingLine a confirmedLines → LineComplete
  4. Guardar nuevo estado en Redis con TTL renovado
  5. Retornar el nuevo CartStep

checkout(type, ...):
  1. Validar que no hay pendingLine activa
  2. Convertir confirmedLines a SalesOrder/SalesInvoice según type
  3. Ejecutar ConfirmAsync si es SalesInvoice directo
  4. Eliminar sesión de Redis
  5. Retornar documento creado
```

### Cómo esto aplica a cada caso

| Caso | Steps generados | Complejidad de flow |
|---|---|---|
| **C1 — Reventa** | `start` → `LineComplete` directo | 1 step |
| **C2 — Manufactura** | igual a C1 (la producción es interna, no del carrito) | 1 step |
| **C3 — Ensamble BOM** | igual a C1 (la explosión es interna) | 1 step |
| **C4 — Variante** | `start` → `WaitingSlotProduct` (slot de variante) → `LineComplete` | 2 steps |
| **C5 — Pizza configurada** | `start` → `WaitingOption × N grupos` → `LineComplete` | 2–5 steps |
| **C6 — Combo 2 pizzas + bebida** | `start` → `WaitingSlotProduct` × 3 slots → `WaitingOption × N por slot` → `LineComplete` | 5–10 steps |

> Para **C4** se puede modelar la selección de variante como un "slot" especial con un solo producto (`WaitingSlotProduct`) en lugar de una opción, reutilizando el mismo mecanismo de C6.

### Soporte multi-canal

Dado que el estado vive en Redis y el contrato es JSON puro via REST:

| Canal | Implementación |
|---|---|
| App web Angular | Llama endpoints REST directamente |
| App móvil Ionic | Llama endpoints REST directamente |
| Bot WhatsApp | Webhook → llama `/respond` con cada mensaje del usuario, devuelve texto formateado del CartStep |
| Bot Telegram | Igual — el step `WaitingOption` se mapea a un InlineKeyboard de Telegram |
| Bot Messenger | Igual — el step se mapea a Quick Replies de Messenger |
| POS manual (admin) | Interfaz simplificada que consume los mismos endpoints |

Cada canal tiene su propio **adaptador de presentación** pero comparte la misma sesión de Redis y la misma lógica de negocio. Si se cambia el precio de "Masa Artesanal" o se agrega una regla de `ProductOptionItemAvailability`, **todos los canales lo reflejan sin tocar código de frontend**.

### Estado actual vs requerido

| Aspecto | Estado |
|---|---|
| `IDistributedCache` (Redis) configurado en el proyecto | ✅ Ya existe — usado en `BankStatementImportJob` |
| `StackExchange.Redis` instalado | ✅ Ya existe |
| `CartSession` entidad en Redis | ❌ **No existe** |
| `CartSessionService` con máquina de estados | ❌ **No existe** |
| Endpoints `/cart-sessions/**` | ❌ **No existen** |
| `ProductOptionItemAvailability` (filtrado dinámico dentro del service) | ❌ **No existe** (prerequisito) |
| Frontend consume CartStep en lugar de cargar grupos directamente | ❌ **No implementado** |

### Trabajo necesario para implementar la sesión de carrito

| Tarea | Complejidad | Prerequisito |
|---|---|---|
| `CartSessionState` modelo C# (record con `confirmedLines`, `pendingLine`, `currentStep`) | Baja | — |
| `CartSessionService` con `StartAsync` y `RespondAsync` (máquina de estados) | Alta | — |
| Casos simples (C1–C3): `start` → `LineComplete` directo | Baja | CartSessionService |
| C4 (variante): `WaitingSlotProduct` con lista de variantes | Baja | CartSessionService |
| C5 (opciones): `WaitingOption` con filtrado por `ProductOptionItemAvailability` | Media | `ProductOptionItemAvailability` schema |
| C6 (combo+opciones): combinación de `WaitingSlotProduct` + `WaitingOption` por slot | Alta | C5 funcionando |
| `POST /cart-sessions/**` endpoints (Minimal API module) | Baja | CartSessionService |
| `POST /cart-sessions/{id}/checkout` → `SalesOrder` o `SalesInvoice` | Media | CartSessionService |
| Adaptador Angular (reemplaza lógica de grupos en frontend) | Media | endpoints listos |
| Adaptador WhatsApp/Telegram (mapea CartStep a mensajes) | Media-Alta | endpoints listos |

**Estimación: ~3–4 días backend para C1–C5, +2 días para C6 completo**  
*(independiente de si se implementa C2 antes — el carrito puede crear `SalesOrder` sin confirmar producción)*

### Decisiones de diseño pendientes para esta arquitectura

- [ ] ¿El `checkout` para C5/C6 crea `SalesOrder` (que luego pasa a producción) o puede crear `SalesInvoice` directamente para casos donde no hay producción (C1–C3)?
- [ ] ¿La sesión de carrito puede tener múltiples `pendingLine` simultáneas o solo una a la vez? (recomendación: una a la vez, simplifica la máquina de estados)
- [ ] ¿Se soporta recuperar una sesión en otro dispositivo/canal con el mismo `sessionId`? (útil para handoff web→móvil)
- [ ] ¿TTL fijo (60 min) o configurable por canal (bots de WhatsApp pueden necesitar más tiempo)?

---
## Resumen de estado

| Caso | Estado | Complejidad restante |
|---|---|---|
| **C1 — Reventa** | ✅ **Completo** | — |
| **C2 — Manufactura** | 🟡 **50%** — falta `Completado` → inventario | Media (~1 día backend) |
| **C3 — Ensamble en venta (BOM)** | ✅ **Completo** | Revisar bug de escala `QuantityOutput` |
| **C4 — Variantes (talla/color)** | � **80%** — backend completo, frontend pendiente | UI Angular selector de variantes (~0.5 día) |
| **C5 — Pedido configurado (pizza)** | ✅ **Completo** | — |
| **C6 — Combo configurado multi-slot** | 🟡 **55%** — schema + CRUD presets + SalesOrder completo | Alta (~2–3 días restantes) |

---

## Dependencias entre casos

```
C2 (Manufactura completa)
  └──► es prerequisito de C5 (pizza pasa a producción y genera stock)
  └──► es prerequisito de C6 (cada pizza del combo pasa a producción)

C1 (Reventa)
  └──► es la base ya funcionando — referencia para C4 (variantes son reventa con IdProductParent)

C3 (BOM explosion)
  └──► es prerequisito de C5 (los extras de pizza con ingredientes usan BOM explosion en factura)
  └──► es prerequisito de C6 (cada slot con receta usa BOM explosion)

C5 (Pedido configurado)
  └──► es prerequisito de C6:
       - SalesOrderLineOption y ProductOptionItem.IdProductRecipe ya deben existir
       - send-to-production ya debe existir (C6 lo extiende a N producciones por slot)
```

---

## Decisiones pendientes

- [ ] **C2**: ¿La cantidad real producida la ingresa el operador en `ProductionOrder` o se toma `QuantityRequired`?
- [ ] **C2**: ¿Se confirma el ajuste de inventario automáticamente al `Completado`, o queda en Borrador para revisión?
- [ ] **C3**: Verificar si `ProductRecipeLine.QuantityInput` es absoluto o relativo a `ProductRecipe.QuantityOutput`.
- [ ] **C4**: ¿Cuántas variantes máximo por producto? ¿Se necesita generación automática desde grilla?
- [ ] **C4**: ¿Los atributos (Talla, Color) deben ser entidades o es suficiente texto en `NameProduct`?
- [ ] **C5**: ¿Las opciones elegidas en el pedido afectan el precio final de la factura automáticamente, o el operador ajusta manualmente?
- [x] **C5**: ¿La `ProductionOrder` creada desde un pedido de pizza incluye todos los insumos de receta + extras de opciones, o solo el producto base? → **Sí: combina receta base del producto + recetas absolutas de cada `ProductOptionItem` seleccionado, agrupando ingredientes y sumando cantidades.**
- [x] **C5**: ¿Cómo modela una opción que afecta múltiples ingredientes (`IdProductExtra` era insuficiente)? → **Cada `ProductOptionItem` tiene `IdProductRecipe?` en lugar de `IdProductExtra + QuantityExtra`. Resuelto en M8 revisado.**
- [ ] **C6**: ¿El slot de "Bebida" que es reventa directa — el operador asigna el lote manualmente o se usa FEFO automático? (recomendación: FEFO automático igual que el resto del combo)
- [ ] **C6**: ¿Un combo genera una `ProductionOrder` por slot con receta (N OPs) o una sola OP con líneas para todos los slots? (recomendación: N OPs, una por slot, para mantener trazabilidad independiente de cada pizza)
- [ ] **C6**: ¿Las opciones `isPreset = true` se muestran en la UI del cliente como informativas (visibles pero bloqueadas) o directamente se ocultan?
- [ ] **C6**: ¿El precio del combo base incluye ingredientes de las opciones preseleccionadas (Grande, Masa Delgada), o las opciones preseleccionadas también pueden tener `PriceDelta > 0`?
- [ ] **C6**: ¿Se permite que en un slot de "Bebida" con 3 productos permitidos el cliente pueda elegir `priceAdjustment > 0` al escoger una opción premium (ej: jugo natural +$1.00)? (el campo `PriceAdjustment` en `ProductComboSlotProduct` ya lo soporta, solo confirmar flujo de precio)
- [ ] **C6**: ¿Se admiten combos dentro de combos (slots donde el producto permitido también es `IsCombo = true`)? — la validación **V9** en `ProductComboSlotService.CreateAsync` actualmente lo **bloquea** explícitamente. Confirmar si se mantiene esa restricción.
- [ ] **C6 / general**: Grupos de opciones reutilizables — ¿alcanza con `POST /products/{idTarget}/option-groups/copy-from/{idSource}` (Opción A, sin schema change) o se necesita un catálogo de plantillas global `OptionGroupTemplate` (Opción B)? Ver análisis en sección "Diseño potencial: grupos de opciones reutilizables".
- [ ] **C5 / C6 / general**: Opciones condicionales — `ProductOptionItemAvailability` (whitelist de habilitadores): ¿se implementa para el MVP de C5 o es un refinamiento posterior? El MVP puede funcionar sin reglas (todos los items siempre visibles), pero la UX es confusa si "Rellena" aparece aunque el tamaño sea "Pequeño". Ver análisis en sección "Diseño potencial: opciones condicionales".
- [ ] **Arquitectura carrito**: ¿el `checkout` para C5/C6 crea `SalesOrder` (producción posterior) o puede crear `SalesInvoice` directa para C1–C3? (recomendación: `SalesOrder` para todo, `SalesInvoice` inmediata solo cuando `ProductType.TrackInventory=false` o producto sin receta ni combo)
- [ ] **Arquitectura carrito**: ¿una sola `pendingLine` activa a la vez, o permite múltiples simultáneas? (recomendación: una a la vez, máquina de estados más simple)
- [ ] **Arquitectura carrito**: ¿TTL de sesión fijo (60 min) o configurable por canal? (bots de mensajería pueden necesitar 24h)
- [ ] **Arquitectura carrito**: ¿se soporta recuperar sesión en otro dispositivo/canal con el mismo `sessionId`? (útil para handoff web → móvil)

---

## Control de avance — sesión de trabajo

> Última actualización: 5 de abril de 2026

### Orden de trabajo definido

| Orden | Caso | Motivo | Estado |
|---|---|---|---|
| 1 | **C2** — Manufactura | Prerequisito de C5 y C6. Brecha concreta en `UpdateStatusAsync` | ✅ Completo |
| 2 | **C4** — Variantes | Independiente de C2/C5. Trabajo mínimo, no bloquea nada | ⏳ Pendiente |
| 3 | **C5** — Pedido configurado | Depende de C2 completo | ✅ Completo |
| 4 | **C6** — Combo multi-slot | Depende de C2 + C5 completos | ⏳ Pendiente |

---

### C2 — Manufactura: tareas

| # | Tarea | Estado |
|---|---|---|
| C2-1 | `ProductionOrderService.UpdateStatusAsync`: al marcar `Completado`, consumir lotes de MP con FEFO | ✅ |
| C2-2 | Crear lote del producto terminado (`InventoryLot` con `SourceType="Producción"`) | ✅ |
| C2-3 | Crear `ProductionSnapshot` con cantidades calculadas vs reales | ✅ |
| C2-4 | Crear `InventoryAdjustment` tipo PRODUCCION con asiento contable automático | ✅ |
| C2-5 | `ProductionOrder.IdWarehouse` — bodega de producción con override al completar | ✅ |
| C2-6 | WAC recalculado del producto terminado (costo = Σ MP / qty producida) | ✅ |
| C2-7 | Stock insuficiente de MP: permite negativo + devuelve `warnings[]` en la respuesta | ✅ |
| C2-8 | Migración `AddProductionOrderWarehouse` aplicada | ✅ |

### C4 — Variantes: tareas pendientes

| # | Tarea | Estado |
|---|---|---|
| C4-1 | `Product.IsVariantParent` + migración `AddVariantAttributes` | ✅ |
| C4-2 | `ProductAttribute`, `AttributeValue`, `ProductVariantAttribute` — entidades + Fluent API + migración | ✅ |
| C4-3 | `ProductAttributesModule` con 7 endpoints (CRUD atributos + valores) | ✅ |
| C4-4 | `POST /products/{id}/variants/generate` — cartesiano automático | ✅ |
| C4-5 | `GET /products/{id}/variants.json` — variantes con atributos expandidos | ✅ |
| C4-6 | `IsVariantParent` en `ProductResponse`, `CreateProductRequest`, `UpdateProductRequest` | ✅ |
| C4-7 | Validación en `SalesInvoice.ConfirmAsync` y `SalesOrder.ConfirmAsync` | ✅ |
| C4-8 | UI Angular: al agregar producto al carrito, si es padre → abrir selector de variantes | ⏳ |

### C5 — Pedido configurado: tareas pendientes

| # | Tarea | Estado |
|---|---|---|
| C5-1 | `ProductOptionItem.IdProductRecipe` — reemplaza `IdProductExtra/QuantityExtra` | ✅ |
| C5-2 | `SalesOrderLineOption` tabla + `SalesOrderLine` recibe opciones | ✅ |
| C5-3 | `POST /sales-orders/{id}/send-to-production` (combina receta base + recetas de opciones) | ✅ |
| C5-4 | `SalesOrder` transición a `"EnProduccion"` | ✅ |
| C5-5 | `POST /sales-orders/{id}/complete` | ✅ |
| C5-6 | `POST /sales-orders/{id}/invoice` | ✅ |
| C5-7 | `SalesInvoiceLineOption` + explosión en `ConfirmAsync` (T19: omite BOM si `IdInventoryLot` pre-asignado) | ✅ |

### C6 — Combo multi-slot: tareas

| # | Tarea | Estado |
|---|---|---|
| C6-1 | `ProductComboSlotPresetOption` entidad + Fluent API + endpoints CRUD | ✅ |
| C6-2 | `SalesOrderLineComboSlotSelection` + `SalesOrderLineSlotOption` entidades + Fluent API | ✅ |
| C6-3 | `SalesInvoiceLineComboSlotSelection` + `SalesInvoiceLineSlotOption` entidades + Fluent API | ✅ |
| C6-4 | `SalesOrderService.ValidateComboSlotSelectionsAsync` — V-COMBO-1 a V-COMBO-5 | ✅ |
| C6-5 | `SalesOrderService.PopulateComboSlotSelectionsAsync` — copia presets, opciones libres, ajuste precio | ✅ |
| C6-6 | `GET /sales-orders/{id}.json` incluye `comboSlotSelections[]` con `slotOptions[]` | ✅ |
| C6-7 | `IsPreset` en `SalesOrderLineSlotOption` y `SalesInvoiceLineSlotOption` | ✅ |
| C6-8 | `SalesOrderService.SendToProductionAsync` extendido: N OPs, una por slot con receta | ⏳ |
| C6-9 | `SalesOrderService.GenerateInvoiceAsync` extendido: copiar selecciones → `SalesInvoiceLineComboSlotSelection` | ⏳ |
| C6-10 | `ExplodeComboAsync` revisado: usar `SalesInvoiceLineComboSlotSelection` en lugar de `FirstOrDefault()` | ⏳ |
| C6-11 | Migr. `AddC6ComboSelections` + `AddC6SlotOptionIsPreset` aplicadas | ✅ |
| C6-12 | UI Angular: flujo de configuración de slots en pedido | ⏳ |
| C6-13 | Factura: respuesta anidada Agrupador → Slot → Opciones (`SalesInvoiceDtos` + query) | ⏳ |

---

### Sesión 5-abril-2026 — C6 Bloque 1 + Bloque 2

**Lo que se hizo:**

#### Bloque 1 — Schema y migración `AddC6ComboSelections` ✅
- Creadas 5 entidades nuevas: `ProductComboSlotPresetOption`, `SalesOrderLineComboSlotSelection`, `SalesOrderLineSlotOption`, `SalesInvoiceLineComboSlotSelection`, `SalesInvoiceLineSlotOption`
- Creadas 5 configuraciones Fluent API correspondientes (índices únicos, FK behaviors, comentarios)
- `ProductComboSlot` extendida con `ICollection<ProductComboSlotPresetOption> PresetOptions`
- `SalesOrderLine` extendida con `ICollection<SalesOrderLineComboSlotSelection> ComboSlotSelections`
- `SalesInvoiceLine` extendida con `ICollection<SalesInvoiceLineComboSlotSelection> ComboSlotSelections`
- `SalesInvoiceLineComboSlotSelection` incluye `IdInventoryLot?` para lotes PT pre-asignados desde producción
- Registradas 5 entidades en `AppDbContext`
- Migración `AddC6ComboSelections` generada y aplicada a la BD ✅

#### Bloque 2 — CRUD `ProductComboSlotPresetOption` ✅
- `POST /product-combo-slots/{slotId}/preset-options` — V-PRESET-1 (item pertenece a producto del slot) + V-PRESET-2 (sin duplicados)
- `DELETE /product-combo-slots/{slotId}/preset-options/{presetOptionId}`
- `GET` endpoints del slot ahora incluyen `presetOptions[]` con nombre e ítem
- `ProductComboSlotResponse` extendido con `IReadOnlyList<ProductComboSlotPresetOptionResponse> PresetOptions`

**Próximos pasos (Bloque 3):**
- `SalesOrderService`: agregar `comboSlotSelections[]` a `CreateSalesOrderLineRequest` / `UpdateSalesOrderLineRequest`
- Validaciones V-COMBO-1 a V-COMBO-5
- `PopulateComboSlotSelectionsAsync`: copiar presets automáticamente + calcular `PriceAdjustment` por producto elegido
- Persistir `SalesOrderLineComboSlotSelection` + `SalesOrderLineSlotOptions`
- `GET /sales-orders/{id}.json` incluir selecciones por línea

---

### Sesión 5-abril-2026 (continuación) — C6 Bloque 3

**Lo que se hizo:**

#### Bloque 3 — `SalesOrderLine` acepta selecciones de slot (T14–T22) ✅

**DTOs nuevos (`CreateSalesOrderRequest.cs`):**
- `SalesOrderLineComboSlotSelectionRequest` — `{idProductComboSlot, idProduct, options?:[]}`
- `SalesOrderLineSlotOptionRequest` — `{idProductOptionItem, quantity}`
- `ComboSlotSelections?` agregado a `SalesOrderLineRequest`

**Response DTOs (`SalesOrderResponse.cs`):**
- `SalesOrderLineComboSlotSelectionResponse` — `{id, idSlot, slotName, idProduct, productName, slotOptions[]}`
- `SalesOrderLineSlotOptionResponse` — `{id, idItem, nameItem, priceDelta, quantity, isPreset}`
- `SalesOrderLineResponse` extendido con `ComboSlotSelections`

**Service (`SalesOrderService.cs`):**
- `ValidateComboSlotSelectionsAsync` — implementa V-COMBO-1 a V-COMBO-5 + validación de opciones de slot
- `PopulateComboSlotSelectionsAsync` — copia `ProductComboSlotPresetOption` con `IsPreset=true`, agrega opciones libres con `IsPreset=false`, calcula `PriceAdjustment` del producto elegido + `PriceDelta` de opciones libres → suma al `UnitPrice` de la línea
- `ProjectOrder` y `MapLine` actualizados para proyectar `ComboSlotSelections → SlotOptions`
- `CreateAsync` y `UpdateAsync` llaman a los nuevos métodos

**Entidades + migración:**
- `IsPreset` agregado a `SalesOrderLineSlotOption` y `SalesInvoiceLineSlotOption`
- Migración `AddC6SlotOptionIsPreset` generada y aplicada ✅

**Próximos pasos (Bloque 4):**
- `SendToProductionAsync` extendido: detectar líneas combo + `ComboSlotSelections`, crear N OPs (una por slot con receta activa)
- `GenerateInvoiceAsync` extendido: copiar `SalesOrderLineComboSlotSelection` → `SalesInvoiceLineComboSlotSelection` con `SlotOptions`  
- `ExplodeComboAsync` refactorizado: usar selección real del cliente en lugar de `FirstOrDefault()`

