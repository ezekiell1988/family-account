# Plan — Productos Configurables y Combos

> Fecha: 4 de abril de 2026
> Rama: `main`
> Contexto: Extensión del sistema de productos para soportar configuración por el cliente (opciones de masa/tamaño) y combos con slots variables (2 pizzas + bebida).

---

## 1. Objetivo

Extender el modelo de `Product` sin romper el flujo actual (CRUD, facturas, inventario) para soportar dos nuevos comportamientos:

| Comportamiento | Ejemplo | Entidad central |
|---|---|---|
| **Producto configurable** | Pizza con masa y tamaño a elección | `ProductOptionGroup` + `ProductOptionItem` |
| **Combo con slots** | 2 pizzas a elegir + 1 bebida a elegir | `ProductComboSlot` + `ProductComboSlotProduct` |

---

## 2. Cambios al modelo existente

### 2.1 `Product` — 2 campos nuevos

| Campo | Tipo | Default | Descripción |
|---|---|---|---|
| `HasOptions` | `bool` | `false` | El producto tiene grupos de opciones configurables por el cliente |
| `IsCombo` | `bool` | `false` | El producto es un combo compuesto de slots |

> Un producto puede tener `HasOptions = true` e `IsCombo = false` (pizza configurable), o `IsCombo = true` con slots que a su vez apunten a productos con `HasOptions = true` (combo de pizzas configurables).

### 2.2 `ProductUnit` — 1 campo nuevo

| Campo | Tipo | Default | Descripción |
|---|---|---|---|
| `SalePrice` | `decimal(18,4)` | `0` | Precio base de venta para esta presentación. El precio final en combos/opciones se calcula sumando deltas. |

---

## 3. Nuevas entidades

### 3.1 `ProductOptionGroup`

**Tabla:** `productOptionGroup`

Agrupa opciones de un producto configurable. Un producto puede tener varios grupos (ej: "Elige tu masa", "Elige tu tamaño", "Adicionales", "Sabor").

| Campo | Tipo | Descripción |
|---|---|---|
| `IdProductOptionGroup` | `int` PK | Autoincremental |
| `IdProduct` | `int` FK | Producto al que pertenece el grupo |
| `NameGroup` | `varchar(200)` | Nombre visible ("Elige tu tamaño") |
| `IsRequired` | `bool` | Si el cliente debe elegir obligatoriamente. `false` → opciones opcionales (adicionales). |
| `MinSelections` | `int` | Mínimo de items a elegir. `0` para grupos opcionales. |
| `MaxSelections` | `int` | Máximo de items a elegir. `1` para exclusivo, `N` para múltiple. |
| `AllowSplit` | `bool` | Cuando `true`, en modo mitad/mitad el cliente asigna cada selección a una mitad (`half1 \| half2 \| whole`). Aplica a grupos de **sabor** y **adicionales**. |
| `SortOrder` | `int` | Orden de presentación al cliente |

**Índice:** ninguno único (un producto puede tener N grupos con el mismo nombre si se desea).

**FK:** `IdProduct` → `product` CASCADE.

---

### 3.2 `ProductOptionItem`

**Tabla:** `productOptionItem`

Cada opción dentro de un grupo (ej: "Delgada", "Gruesa", "Rellena").

| Campo | Tipo | Descripción |
|---|---|---|
| `IdProductOptionItem` | `int` PK | Autoincremental |
| `IdProductOptionGroup` | `int` FK | Grupo al que pertenece |
| `NameItem` | `varchar(200)` | Nombre visible ("Masa Delgada") |
| `PriceDelta` | `decimal(18,4)` | Ajuste de precio sobre el base (+2.00, 0, -1.00) |
| `IsDefault` | `bool` | Opción marcada por defecto al abrir el selector |
| `SortOrder` | `int` | Orden dentro del grupo |

**FK:** `IdProductOptionGroup` → `productOptionGroup` CASCADE.

---

### 3.3 `ProductComboSlot`

**Tabla:** `productComboSlot`

Define un "hueco" dentro de un combo. El combo "2 Pizzas + Bebida" tiene 3 slots.

| Campo | Tipo | Descripción |
|---|---|---|
| `IdProductComboSlot` | `int` PK | Autoincremental |
| `IdProductCombo` | `int` FK | Producto padre con `IsCombo = true` |
| `NameSlot` | `varchar(200)` | Nombre visible ("Pizza #1", "Bebida") |
| `Quantity` | `decimal(12,4)` | Cantidad de ese slot dentro del combo |
| `IsRequired` | `bool` | Si el cliente debe llenar el slot obligatoriamente |
| `SortOrder` | `int` | Orden de presentación |

**FK:** `IdProductCombo` → `product` CASCADE.

---

### 3.4 `ProductComboSlotProduct`

**Tabla:** `productComboSlotProduct`

Lista de productos permitidos para un slot específico. El cliente elige uno (o varios si `MaxSelections > 1`) de esta lista.

| Campo | Tipo | Descripción |
|---|---|---|
| `IdProductComboSlotProduct` | `int` PK | Autoincremental |
| `IdProductComboSlot` | `int` FK | Slot al que pertenece |
| `IdProduct` | `int` FK | Producto permitido en este slot |
| `PriceAdjustment` | `decimal(18,4)` | Ajuste adicional al precio del combo por elegir este producto |
| `SortOrder` | `int` | Orden dentro del slot |

**Índice único:** `UQ_productComboSlotProduct_idSlot_idProduct`

**FK:** `IdProductComboSlot` → `productComboSlot` CASCADE, `IdProduct` → `product` RESTRICT.

---

## 4. Diagrama de relaciones

```
product
  ├─[HasOptions=true]─► productOptionGroup (1:N por idProduct)
  │                          └─► productOptionItem (1:N por idProductOptionGroup)
  │
  └─[IsCombo=true]────► productComboSlot (1:N por idProductCombo)
                             └─► productComboSlotProduct (1:N por idProductComboSlot)
                                      └─► product (el producto permitido)
                                               └─[HasOptions=true]─► productOptionGroup ...
```

---

## 5. Ejemplos de datos

### 5.1 Pizza configurable completa (tamaño + masa + modo + sabor + adicionales)

```
product: { code: "PIZZA-BASE", name: "Pizza", hasOptions: true, isCombo: false }
  productUnit: { namePresentation: "Pizza", salePrice: 0.00 }  // precio armado por deltas

  // Grupo 1: Tamaño — requerido, exactamente 1, NO splittable
  productOptionGroup: { nameGroup: "Tamaño", isRequired: true, min: 1, max: 1,
                        allowSplit: false, sort: 1 }
    productOptionItem: { name: "Personal",  priceDelta:  8.00, isDefault: false, sort: 1 }
    productOptionItem: { name: "Mediana",   priceDelta: 12.00, isDefault: true,  sort: 2 }
    productOptionItem: { name: "Familiar",  priceDelta: 16.00, isDefault: false, sort: 3 }

  // Grupo 2: Masa — requerido, exactamente 1, NO splittable
  productOptionGroup: { nameGroup: "Masa", isRequired: true, min: 1, max: 1,
                        allowSplit: false, sort: 2 }
    productOptionItem: { name: "Delgada", priceDelta: 0.00, isDefault: true,  sort: 1 }
    productOptionItem: { name: "Gruesa",  priceDelta: 2.00, isDefault: false, sort: 2 }
    productOptionItem: { name: "Rellena", priceDelta: 4.00, isDefault: false, sort: 3 }

  // Grupo 3: Modo — requerido, exactamente 1, NO splittable (controla el modo mitad/mitad)
  productOptionGroup: { nameGroup: "Modo", isRequired: true, min: 1, max: 1,
                        allowSplit: false, sort: 3 }
    productOptionItem: { name: "Pizza completa",   priceDelta: 0.00, isDefault: true,  sort: 1 }
    productOptionItem: { name: "Mitad y mitad",    priceDelta: 0.00, isDefault: false, sort: 2 }

  // Grupo 4: Sabor — requerido, mínimo 1 máximo 4, SÍ splittable
  productOptionGroup: { nameGroup: "Sabor", isRequired: true, min: 1, max: 4,
                        allowSplit: true, sort: 4 }
    productOptionItem: { name: "Margarita",  priceDelta:  0.00, isDefault: true,  sort: 1 }
    productOptionItem: { name: "Pepperoni",  priceDelta:  2.00, isDefault: false, sort: 2 }
    productOptionItem: { name: "Hawaiana",   priceDelta:  2.00, isDefault: false, sort: 3 }
    productOptionItem: { name: "Especial",   priceDelta:  5.00, isDefault: false, sort: 4 }
    productOptionItem: { name: "BBQ Pollo",  priceDelta:  3.00, isDefault: false, sort: 5 }

  // Grupo 5: Adicionales — opcional, mínimo 0 máximo 3, SÍ splittable
  productOptionGroup: { nameGroup: "Adicionales", isRequired: false, min: 0, max: 3,
                        allowSplit: true, sort: 5 }
    productOptionItem: { name: "Extra queso",     priceDelta: 1.00, isDefault: false, sort: 1 }
    productOptionItem: { name: "Jamón",            priceDelta: 1.50, isDefault: false, sort: 2 }
    productOptionItem: { name: "Champiñones",      priceDelta: 1.00, isDefault: false, sort: 3 }
    productOptionItem: { name: "Jalapeños",        priceDelta: 0.50, isDefault: false, sort: 4 }
    productOptionItem: { name: "Aceitunas negras", priceDelta: 0.50, isDefault: false, sort: 5 }
```

**Precio final** = delta Tamaño + delta Masa + Σ deltas Sabor + Σ deltas Adicionales
→ "Familiar + Gruesa + Completa + Pepperoni + Extra queso" = $16 + $2 + $2 + $1 = **$21**

---

### 5.3 5.2 Patrón Mitad y Mitad — cómo lo interpreta el cliente (Angular)

El campo `AllowSplit = true` en un grupo le indica al frontend que, cuando el usuario eligió **"Mitad y mitad"** en el grupo **Modo**, cada selección de ese grupo debe llevar un `halfScope`:

| `halfScope` | Significado |
|---|---|
| `whole` | Aplica a la pizza entera |
| `half1` | Solo a la primera mitad |
| `half2` | Solo a la segunda mitad |

**Ejemplo de pedido guardado (no DB, lógica de ventas futura):**

```json
{
  "product": "PIZZA-BASE",
  "selections": [
    { "group": "Tamaño",      "item": "Familiar",     "halfScope": "whole" },
    { "group": "Masa",        "item": "Gruesa",       "halfScope": "whole" },
    { "group": "Modo",        "item": "Mitad y mitad", "halfScope": "whole" },
    { "group": "Sabor",       "item": "Pepperoni",    "halfScope": "half1" },
    { "group": "Sabor",       "item": "Margarita",    "halfScope": "half2" },
    { "group": "Adicionales", "item": "Extra queso",  "halfScope": "half1" },
    { "group": "Adicionales", "item": "Jalapeños",    "halfScope": "whole" }
  ]
}
```

> Tamaño y Masa tienen `allowSplit = false` → siempre `whole`, el UI no muestra selector de mitad.
> Sabor y Adicionales tienen `allowSplit = true` → el UI muestra "¿en qué mitad?" solo cuando Modo = "Mitad y mitad".

**Reglas de validación del frontend para mitad/mitad:**
- Si `Modo = Completa`: grupos con `allowSplit = true` se tratan normalmente (sin selector de mitad)
- Si `Modo = Mitad y mitad`: cada selección en grupos con `allowSplit = true` requiere un `halfScope`
- El grupo Sabor con `min:1 max:4` en modo mitad/mitad permite hasta 4 sabores distribuidos entre ambas mitades (ej: 2 en half1, 2 en half2)
- El grupo Adicionales con `min:0 max:3`: máximo 3 adicionales en total sumando los de ambas mitades y whole

---

### 5.3 Combo Familiar

```
product: { code: "COMBO-FAM", name: "Combo Familiar", hasOptions: false, isCombo: true }
  productUnit: { namePresentation: "Combo", salePrice: 30.00 }

  productComboSlot: { name: "Pizza #1", qty: 1, isRequired: true, sort: 1 }
    productComboSlotProduct: { idProduct: PIZZA-MAR, priceAdjustment: 0, sort: 1 }
    productComboSlotProduct: { idProduct: PIZZA-PEP, priceAdjustment: 2, sort: 2 }
    productComboSlotProduct: { idProduct: PIZZA-ESP, priceAdjustment: 5, sort: 3 }

  productComboSlot: { name: "Pizza #2", qty: 1, isRequired: true, sort: 2 }
    productComboSlotProduct: { idProduct: PIZZA-MAR, priceAdjustment: 0, sort: 1 }
    productComboSlotProduct: { idProduct: PIZZA-PEP, priceAdjustment: 2, sort: 2 }
    productComboSlotProduct: { idProduct: PIZZA-ESP, priceAdjustment: 5, sort: 3 }

  productComboSlot: { name: "Bebida", qty: 1, isRequired: true, sort: 3 }
    productComboSlotProduct: { idProduct: COCA-350,  priceAdjustment: 0, sort: 1 }
    productComboSlotProduct: { idProduct: AGUA-500,  priceAdjustment: 0, sort: 2 }
```

**Precio final** = `salePrice(30)` + ajustes de slots + deltas de opciones de cada pizza elegida.

---

## 6. Nuevas features (archivos a crear)

```
Features/
  ProductOptionGroups/
    Dtos/
      ProductOptionGroupResponse.cs
      ProductOptionItemResponse.cs
      ProductOptionItemRequest.cs
      CreateProductOptionGroupRequest.cs
      UpdateProductOptionGroupRequest.cs
    IProductOptionGroupService.cs
    ProductOptionGroupService.cs
    ProductOptionGroupsModule.cs

  ProductComboSlots/
    Dtos/
      ProductComboSlotResponse.cs
      ProductComboSlotProductResponse.cs
      ProductComboSlotProductRequest.cs
      CreateProductComboSlotRequest.cs
      UpdateProductComboSlotRequest.cs
    IProductComboSlotService.cs
    ProductComboSlotService.cs
    ProductComboSlotsModule.cs
```

---

## 7. Endpoints a exponer

### ProductOptionGroups — `/product-option-groups`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/by-product/{idProduct}.json` | Grupos de opciones de un producto (con items) |
| GET | `/{id}.json` | Grupo por ID |
| POST | `/` | Crear grupo con sus items (Admin) |
| PUT | `/{id}` | Reemplazar grupo e items (Admin) |
| DELETE | `/{id}` | Eliminar grupo y sus items (Admin) |

### ProductComboSlots — `/product-combo-slots`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/by-combo/{idProductCombo}.json` | Slots de un combo (con productos permitidos) |
| GET | `/{id}.json` | Slot por ID |
| POST | `/` | Crear slot con productos permitidos (Admin) |
| PUT | `/{id}` | Reemplazar slot y productos permitidos (Admin) |
| DELETE | `/{id}` | Eliminar slot (Admin) |

---

## 8. Cambios a archivos existentes

| Archivo | Cambio |
|---|---|
| `Domain/Entities/Product.cs` | +`HasOptions`, +`IsCombo`, +colecciones de navegación |
| `Domain/Entities/ProductUnit.cs` | +`SalePrice` |
| `Domain/Entities/ProductOptionGroup.cs` | +`AllowSplit` |
| `Infrastructure/Data/Configuration/ProductConfiguration.cs` | Configurar 2 campos nuevos + nav props |
| `Infrastructure/Data/Configuration/ProductUnitConfiguration.cs` | Configurar `SalePrice` |
| `Infrastructure/Data/Configuration/ProductOptionGroupConfiguration.cs` | Configurar `AllowSplit` |
| `Infrastructure/Data/AppDbContext.cs` | +4 DbSet nuevos |
| `Infrastructure/Extensions/FeaturesExtensions.cs` | Registrar 2 módulos nuevos |

---

## 9. Migración EF Core

Una sola migración `AddProductConfigurableAndCombo`:
- Agrega columnas `hasOptions`, `isCombo` en `product`
- Agrega columna `salePrice` en `productUnit`
- Crea tablas `productOptionGroup`, `productOptionItem`, `productComboSlot`, `productComboSlotProduct`

---

## 10. Cálculo de precio final (lógica de negocio futura)

El sistema de ventas que consuma este catálogo debe:

```
PrecioFinal(producto configurable) =
  productUnit.SalePrice                                  // precio base
  + Σ optionItem.PriceDelta (items elegidos)             // ajustes por grupo
  // El halfScope (whole/half1/half2) NO afecta el delta —
  // un adicional cuesta lo mismo en cualquier mitad

PrecioFinal(combo) =
  productUnit.SalePrice                                  // precio base del combo
  + Σ comboSlotProduct.PriceAdjustment (producto elegido en cada slot)
  + Σ PrecioFinal(subProducto configurable, si aplica)
```

**Nota sobre precio en mitad y mitad:**
- Actualmente `PriceDelta` de cada `ProductOptionItem` es plano: aplica igual sin importar si va en half1, half2 o whole.
- Si en el futuro se quiere cobrar 50% por mitad, eso es lógica del módulo de ventas (dividir `PriceDelta / 2`), no requiere cambio de modelo.

> El API no calcula el precio en este plan — solo expone el catálogo de opciones. El cálculo queda en el cliente (Angular) o en el futuro módulo de ventas.

---

## 11. Validaciones de negocio a implementar en servicios

| # | Servicio | Regla |
|---|---|---|
| V1 | `ProductOptionGroupService.Create` | Solo crear grupos en productos con `HasOptions = true` |
| V2 | `ProductOptionGroupService.Create` | Al menos 1 item por grupo |
| V3 | `ProductOptionGroupService.Create` | `MinSelections <= MaxSelections` |
| V4 | `ProductOptionGroupService.Create` | Si `IsRequired = false` entonces `MinSelections` debe ser `0` |
| V5 | `ProductOptionGroupService.Create` | `AllowSplit = true` solo es válido si el producto también tiene un grupo de nombre "Modo" con los items de modo completa/mitad |
| V6 | `ProductComboSlotService.Create` | Solo crear slots en productos con `IsCombo = true` |
| V7 | `ProductComboSlotService.Create` | Al menos 1 producto permitido por slot |
| V8 | `ProductComboSlotService.Create` | No repetir el mismo `IdProduct` en el mismo slot |

---

## 12. Orden de implementación

1. Modificar entidades `Product` y `ProductUnit`
2. Crear entidades `ProductOptionGroup`, `ProductOptionItem`, `ProductComboSlot`, `ProductComboSlotProduct`
3. Crear configuraciones EF para las 4 entidades nuevas
4. Modificar configuraciones existentes (`ProductConfiguration`, `ProductUnitConfiguration`)
5. Actualizar `AppDbContext` con los 4 DbSet nuevos
6. Crear feature `ProductOptionGroups` (Dtos + service + module)
7. Crear feature `ProductComboSlots` (Dtos + service + module)
8. Registrar módulos en `FeaturesExtensions`
9. Generar migración `AddProductConfigurableAndCombo`
10. Aplicar migración
