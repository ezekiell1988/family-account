# Plan — Frontend: Página de Productos con Configurables y Combos

> Fecha: 4 de abril de 2026
> Rama: `main`
> Contexto: Extender la página `maintenance/products` para gestionar los campos
> `hasOptions`, `isCombo`, presentaciones (`ProductUnit`) con `salePrice`, grupos
> de opciones y slots de combo. **No incluye lógica de ventas.**

---

## 1. Estado actual

### 1.1 Lo que existe hoy

| Pieza | Descripción |
|---|---|
| `products.ts` | Coordinador: carga lista, llama CRUD de producto y asociación de categorías |
| `products-web` | Tabla ngx-datatable + formulario inline + row-detail con categorías |
| `products-mobile` | ion-list expandible + FAB + formulario ion-card |
| `ProductService` | CRUD sobre `/api/v1/products` con `items` signal |
| `ProductDto` | `idProduct, codeProduct, nameProduct, idProductType, nameProductType, idUnit, codeUnit, idProductParent, averageCost` |
| `CreateProductRequest / UpdateProductRequest` | Mismos campos sin `hasOptions` ni `isCombo` |

### 1.2 Lo que NO existe en el frontend

- Servicio para `ProductUnit` (`/api/v1/product-units`)
- Servicio para `ProductOptionGroup` (`/api/v1/product-option-groups`)
- Servicio para `ProductComboSlot` (`/api/v1/product-combo-slots`)
- Modelos TypeScript para esas tres entidades
- UI para gestionar presentaciones, grupos de opciones y slots de combo

---

## 2. Gaps en el API

Antes de tocar el frontend, hay que cerrar estos gaps en el backend:

### 2.1 `ProductResponse` — falta exponer `HasOptions` e `IsCombo`

**Archivo:** `Features/Products/Dtos/ProductResponse.cs`

Agregar los dos campos al final del record:

```csharp
public sealed record ProductResponse(
    int      IdProduct,
    string   CodeProduct,
    string   NameProduct,
    int      IdProductType,
    string   NameProductType,
    int      IdUnit,
    string   CodeUnit,
    int?     IdProductParent,
    decimal  AverageCost,
    bool     HasOptions,   // ← nuevo
    bool     IsCombo);     // ← nuevo
```

**Archivo:** `Features/Products/ProductService.cs` — `ToResponse` debe mapear los dos nuevos campos:

```csharp
private static ProductResponse ToResponse(Product p) => new(
    p.IdProduct, p.CodeProduct, p.NameProduct,
    p.IdProductType, p.IdProductTypeNavigation.NameProductType,
    p.IdUnit, p.IdUnitNavigation.CodeUnit,
    p.IdProductParent, p.AverageCost,
    p.HasOptions, p.IsCombo);   // ← nuevo
```

### 2.2 `CreateProductRequest` / `UpdateProductRequest` — falta `HasOptions` e `IsCombo`

**Archivos:** `Features/Products/Dtos/CreateProductRequest.cs` y `UpdateProductRequest.cs`

Agregar en ambos:

```csharp
[Description("Indica que el producto tiene opciones configurables")]
public bool HasOptions { get; init; } = false;

[Description("Indica que el producto es un combo de slots")]
public bool IsCombo { get; init; } = false;
```

**Archivo:** `ProductService.cs` — en `CreateAsync` y `UpdateAsync` asignar los campos:

```csharp
HasOptions = request.HasOptions,
IsCombo    = request.IsCombo,
```

### 2.3 `ProductUnitResponse` — falta `SalePrice`

**Archivo:** `Features/ProductUnits/Dtos/ProductUnitResponse.cs`

Agregar `decimal SalePrice` al final:

```csharp
public sealed record ProductUnitResponse(
    int     IdProductUnit,
    ...
    string? BrandPresentation,
    decimal SalePrice);   // ← nuevo
```

El `ProductUnitService.ToResponse` debe mapear `pu.SalePrice`.

### 2.4 `CreateProductUnitRequest` / `UpdateProductUnitRequest` — falta `SalePrice`

Agregar en ambos:

```csharp
[Range(typeof(decimal), "0", "999999999999.9999", ...)]
[Description("Precio base de venta para esta presentación")]
public decimal SalePrice { get; init; } = 0m;
```

El servicio debe asignar `SalePrice = request.SalePrice` en Create y Update.

---

## 3. Cambios a modelos TypeScript (`product.models.ts`)

### 3.1 `ProductDto` — agregar dos campos nuevos

```typescript
export interface ProductDto {
  idProduct:       number;
  codeProduct:     string;
  nameProduct:     string;
  idProductType:   number;
  nameProductType: string;
  idUnit:          number;
  codeUnit:        string;
  idProductParent: number | null;
  averageCost:     number;
  hasOptions:      boolean;   // ← nuevo
  isCombo:         boolean;   // ← nuevo
}
```

### 3.2 `CreateProductRequest` / `UpdateProductRequest` — agregar dos campos

```typescript
export interface CreateProductRequest {
  codeProduct:     string;
  nameProduct:     string;
  idProductType:   number;
  idUnit:          number;
  idProductParent: number | null;
  hasOptions:      boolean;   // ← nuevo
  isCombo:         boolean;   // ← nuevo
}

// UpdateProductRequest: mismo cambio
```

### 3.3 Nuevos modelos — `ProductUnit`

```typescript
export interface ProductUnitDto {
  idProductUnit:     number;
  idProduct:         number;
  idUnit:            number;
  codeUnit:          string;
  conversionFactor:  number;
  isBase:            boolean;
  usedForPurchase:   boolean;
  usedForSale:       boolean;
  codeBarcode:       string | null;
  namePresentation:  string | null;
  brandPresentation: string | null;
  salePrice:         number;
}

export interface CreateProductUnitRequest {
  idProduct:         number;
  idUnit:            number;
  conversionFactor:  number;
  isBase:            boolean;
  usedForPurchase:   boolean;
  usedForSale:       boolean;
  codeBarcode:       string | null;
  namePresentation:  string | null;
  brandPresentation: string | null;
  salePrice:         number;
}

export interface UpdateProductUnitRequest {
  conversionFactor:  number;
  isBase:            boolean;
  usedForPurchase:   boolean;
  usedForSale:       boolean;
  codeBarcode:       string | null;
  namePresentation:  string | null;
  brandPresentation: string | null;
  salePrice:         number;
}
```

### 3.4 Nuevos modelos — `ProductOptionGroup`

```typescript
export interface ProductOptionItemDto {
  idProductOptionItem: number;
  nameItem:            string;
  priceDelta:          number;
  isDefault:           boolean;
  sortOrder:           number;
}

export interface ProductOptionGroupDto {
  idProductOptionGroup: number;
  idProduct:            number;
  nameGroup:            string;
  isRequired:           boolean;
  minSelections:        number;
  maxSelections:        number;
  allowSplit:           boolean;
  sortOrder:            number;
  items:                ProductOptionItemDto[];
}

export interface ProductOptionItemRequest {
  nameItem:   string;
  priceDelta: number;
  isDefault:  boolean;
  sortOrder:  number;
}

export interface CreateProductOptionGroupRequest {
  idProduct:     number;
  nameGroup:     string;
  isRequired:    boolean;
  minSelections: number;
  maxSelections: number;
  allowSplit:    boolean;
  sortOrder:     number;
  items:         ProductOptionItemRequest[];
}

export interface UpdateProductOptionGroupRequest {
  nameGroup:     string;
  isRequired:    boolean;
  minSelections: number;
  maxSelections: number;
  allowSplit:    boolean;
  sortOrder:     number;
  items:         ProductOptionItemRequest[];
}
```

### 3.5 Nuevos modelos — `ProductComboSlot`

```typescript
export interface ProductComboSlotProductDto {
  idProductComboSlotProduct: number;
  idProduct:                 number;
  nameProduct:               string;
  priceAdjustment:           number;
  sortOrder:                 number;
}

export interface ProductComboSlotDto {
  idProductComboSlot: number;
  idProductCombo:     number;
  nameSlot:           string;
  quantity:           number;
  isRequired:         boolean;
  sortOrder:          number;
  products:           ProductComboSlotProductDto[];
}

export interface ProductComboSlotProductRequest {
  idProduct:       number;
  priceAdjustment: number;
  sortOrder:       number;
}

export interface CreateProductComboSlotRequest {
  idProductCombo: number;
  nameSlot:       string;
  quantity:       number;
  isRequired:     boolean;
  sortOrder:      number;
  products:       ProductComboSlotProductRequest[];
}

export interface UpdateProductComboSlotRequest {
  nameSlot:   string;
  quantity:   number;
  isRequired: boolean;
  sortOrder:  number;
  products:   ProductComboSlotProductRequest[];
}
```

---

## 4. Nuevos servicios Angular

### 4.1 `ProductUnitService` (`service/product-unit.service.ts`)

```
GET  /product-units/by-product/{idProduct}.json  → ProductUnitDto[]
POST /product-units/                              → ProductUnitDto  (Admin)
PUT  /product-units/{id}                          → ProductUnitDto  (Admin)
DEL  /product-units/{id}                         (Admin)
```

- No necesita `items` signal global (los datos son por-producto, se pasan via input/output).
- El servicio expone métodos que retornan `Observable` y maneja `isLoading` / `error` con señales locales.
- Patrón igual al de `ProductTypeService` pero sin estado global de lista.

### 4.2 `ProductOptionGroupService` (`service/product-option-group.service.ts`)

```
GET  /product-option-groups/by-product/{idProduct}.json → ProductOptionGroupDto[]
POST /product-option-groups/                            → ProductOptionGroupDto  (Admin)
PUT  /product-option-groups/{id}                        → ProductOptionGroupDto  (Admin)
DEL  /product-option-groups/{id}                        (Admin)
```

### 4.3 `ProductComboSlotService` (`service/product-combo-slot.service.ts`)

```
GET  /product-combo-slots/by-combo/{idProductCombo}.json → ProductComboSlotDto[]
POST /product-combo-slots/                               → ProductComboSlotDto  (Admin)
PUT  /product-combo-slots/{id}                           → ProductComboSlotDto  (Admin)
DEL  /product-combo-slots/{id}                           (Admin)
```

### 4.4 Registrar en `service/index.ts`

Agregar exports de los tres servicios nuevos.

---

## 5. Cambios a `products.ts` (coordinador)

### 5.1 Nuevas inyecciones

```typescript
private readonly productUnitSvc       = inject(ProductUnitService);
private readonly productOptionGroupSvc = inject(ProductOptionGroupService);
private readonly productComboSlotSvc   = inject(ProductComboSlotService);
```

### 5.2 Señales de datos por-producto expandido

```typescript
// Datos cargados para el producto actualmente expandido en el row-detail
expandedProductUnits        = signal<ProductUnitDto[]>([]);
expandedProductOptionGroups = signal<ProductOptionGroupDto[]>([]);
expandedProductComboSlots   = signal<ProductComboSlotDto[]>([]);
loadingDetail               = signal(false);
```

### 5.3 Métodos de carga lazy (llamados desde el output `rowExpanded`)

```typescript
loadDetailFor(product: ProductDto): void {
  this.loadingDetail.set(true);
  this.expandedProductUnits.set([]);
  this.expandedProductOptionGroups.set([]);
  this.expandedProductComboSlots.set([]);

  forkJoin([
    this.productUnitSvc.getByProduct(product.idProduct),
    product.hasOptions
      ? this.productOptionGroupSvc.getByProduct(product.idProduct)
      : of([]),
    product.isCombo
      ? this.productComboSlotSvc.getByCombo(product.idProduct)
      : of([]),
  ]).pipe(finalize(() => this.loadingDetail.set(false)))
    .subscribe({
      next: ([units, groups, slots]) => {
        this.expandedProductUnits.set(units);
        this.expandedProductOptionGroups.set(groups);
        this.expandedProductComboSlots.set(slots);
      },
    });
}
```

### 5.4 Acciones CRUD para presentaciones

```typescript
createProductUnit(req: CreateProductUnitRequest): void { ... }
updateProductUnit(payload: UpdateProductUnitRequest & { id: number }): void { ... }
deleteProductUnit(id: number): void { ... }
```

### 5.5 Acciones CRUD para grupos de opciones

```typescript
createOptionGroup(req: CreateProductOptionGroupRequest): void { ... }
updateOptionGroup(payload: UpdateProductOptionGroupRequest & { id: number }): void { ... }
deleteOptionGroup(id: number): void { ... }
```

### 5.6 Acciones CRUD para slots de combo

```typescript
createComboSlot(req: CreateProductComboSlotRequest): void { ... }
updateComboSlot(payload: UpdateProductComboSlotRequest & { id: number }): void { ... }
deleteComboSlot(id: number): void { ... }
```

### 5.7 Nuevos bindings en `products.html`

```html
<app-products-web
  ...
  [expandedUnits]="expandedProductUnits()"
  [expandedOptionGroups]="expandedProductOptionGroups()"
  [expandedComboSlots]="expandedProductComboSlots()"
  [loadingDetail]="loadingDetail()"
  [allProducts]="products()"
  [allUnits]="units()"
  (rowExpanded)="loadDetailFor($event)"
  (createUnit)="createProductUnit($event)"
  (editUnit)="updateProductUnit($event)"
  (removeUnit)="deleteProductUnit($event)"
  (createOptionGroup)="createOptionGroup($event)"
  (editOptionGroup)="updateOptionGroup($event)"
  (removeOptionGroup)="deleteOptionGroup($event)"
  (createComboSlot)="createComboSlot($event)"
  (editComboSlot)="updateComboSlot($event)"
  (removeComboSlot)="deleteComboSlot($event)"
/>
```

---

## 6. Cambios a `products-web.component`

### 6.1 Formulario crear/editar producto — nuevos campos

Agregar dos checkboxes después del campo `Producto padre`:

```html
<div class="col-sm-auto d-flex align-items-center gap-3 pt-4">
  <div class="form-check">
    <input id="hasOptions" type="checkbox" class="form-check-input"
      [checked]="formHasOptions()"
      (change)="formHasOptions.set($any($event.target).checked)">
    <label for="hasOptions" class="form-check-label">Tiene opciones</label>
  </div>
  <div class="form-check">
    <input id="isCombo" type="checkbox" class="form-check-input"
      [checked]="formIsCombo()"
      (change)="formIsCombo.set($any($event.target).checked)">
    <label for="isCombo" class="form-check-label">Es combo</label>
  </div>
</div>
```

Agregar señales:

```typescript
formHasOptions = signal(false);
formIsCombo    = signal(false);
```

Actualizar `openEdit()` / `openCreate()` / `submitForm()` para incluir los campos.

### 6.2 Columnas de la tabla — nuevos badges

Agregar columna `Comportamiento` (width: 120) entre Unidad y Acciones:

```html
<ngx-datatable-column name="Comportamiento" [width]="120" [sortable]="false">
  <ng-template let-row="row" ngx-datatable-cell-template>
    @if (row.hasOptions) {
      <span class="badge bg-warning text-dark me-1" title="Producto configurable">
        <i class="fa fa-sliders me-1"></i>Opciones
      </span>
    }
    @if (row.isCombo) {
      <span class="badge bg-purple me-1" title="Combo">
        <i class="fa fa-layer-group me-1"></i>Combo
      </span>
    }
  </ng-template>
</ngx-datatable-column>
```

### 6.3 Row-detail — estructura de tabs (Color Admin)

El row-detail actual solo muestra categorías. Con los cambios queda con **4 tabs**:

```
┌─────────────────────────────────────────────────────────────────┐
│ [Presentaciones] [Categorías] [Opciones*] [Combo*]              │
│  * visible solo si hasOptions / isCombo                         │
├─────────────────────────────────────────────────────────────────┤
│ Contenido del tab activo                                        │
└─────────────────────────────────────────────────────────────────┘
```

Estado local para controlar tab activo:

```typescript
activeDetailTab = signal<'units' | 'categories' | 'options' | 'combo'>('units');
```

Se resetea a `'units'` en `toggleRowDetail()`.

#### Tab 1 — Presentaciones

Lista de `ProductUnitDto[]` para el producto expandido (input `expandedUnits`):

- Columnas: Nombre, Unidad, Factor conversión, Precio venta, Base, Compra, Venta, Barcode, Acciones
- Botón `+ Nueva presentación` abre formulario inline debajo de la tabla
- **Formulario de presentación** contiene:
  - `idUnit` (select de `allUnits`), `conversionFactor`, `isBase` (checkbox), `usedForPurchase`, `usedForSale`, `salePrice`, `codeBarcode`, `namePresentation`, `brandPresentation`
- Editar: precarga el formulario con los datos actuales
- Eliminar: confirmación inline
- La fila base (`isBase = true`) se indica con badge dorado y no se puede eliminar

#### Tab 2 — Categorías

Mismo contenido que existe hoy (selector + botón Asociar, lista de categorías asociadas con botón Desasociar).

#### Tab 3 — Opciones *(solo si `row.hasOptions`)*

Lista de `ProductOptionGroupDto[]` (input `expandedOptionGroups`):

- Muestra grupos como cards colapsables
  - Header del card: `nameGroup` · badges `isRequired` · `min/max` · `allowSplit`
  - Body del card: tabla de items (nameItem, priceDelta, isDefault, sortOrder)
  - Acciones del card: Editar grupo, Eliminar grupo
- Botón `+ Nuevo grupo` en la parte superior

**Formulario de grupo** (inline o modal pequeño):

| Campo | Tipo | Notas |
|---|---|---|
| Nombre del grupo | text | max 200 |
| Requerido | checkbox | Si false → minSelections forzado a 0 |
| Mín. selecciones | number | 0..N |
| Máx. selecciones | number | 1..N |
| Permite mitad/mitad | checkbox | allowSplit |
| Orden | number | sortOrder |
| Items | tabla editable | Filas: nombre, priceDelta, isDefault, sortOrder. Mín. 1 fila. Botón `+ Item` |

Items se gestionan como un array local (`formItems = signal<ProductOptionItemRequest[]>([])`).

**Validaciones en el frontend** (espejando las del servicio):
- Items ≥ 1
- `minSelections ≤ maxSelections`
- Si `isRequired = false` → `minSelections = 0` (auto-forzar o mostrar error)

#### Tab 4 — Combo *(solo si `row.isCombo`)*

Lista de `ProductComboSlotDto[]` (input `expandedComboSlots`):

- Muestra slots como cards colapsables
  - Header: `nameSlot` · `qty` · `isRequired`
  - Body: tabla de productos permitidos (nombreProducto, priceAdjustment, sortOrder)
  - Acciones del card: Editar slot, Eliminar slot
- Botón `+ Nuevo slot` en la parte superior

**Formulario de slot** (inline):

| Campo | Tipo | Notas |
|---|---|---|
| Nombre del slot | text | max 200. Ej: "Pizza #1" |
| Cantidad | number | `qty`, decimal |
| Requerido | checkbox | |
| Orden | number | sortOrder |
| Productos permitidos | tabla editable | Filas: select de `allProducts`, priceAdjustment, sortOrder. Mín. 1 fila. |

**Validaciones en el frontend**:
- Productos ≥ 1
- Sin `idProduct` repetido en la misma lista

### 6.4 Nuevos inputs/outputs en `products-web.component.ts`

```typescript
// Nuevos inputs
expandedUnits        = input<ProductUnitDto[]>([]);
expandedOptionGroups = input<ProductOptionGroupDto[]>([]);
expandedComboSlots   = input<ProductComboSlotDto[]>([]);
loadingDetail        = input(false);
allUnits             = input<UnitOfMeasureDto[]>([]);   // ya existe como units(), renombrar
allProducts          = input<ProductDto[]>([]);         // para el selector de slot products

// Nuevos outputs
rowExpanded    = output<ProductDto>();
createUnit     = output<CreateProductUnitRequest>();
editUnit       = output<UpdateProductUnitRequest & { id: number }>();
removeUnit     = output<number>();
createOptionGroup = output<CreateProductOptionGroupRequest>();
editOptionGroup   = output<UpdateProductOptionGroupRequest & { id: number }>();
removeOptionGroup = output<number>();
createComboSlot   = output<CreateProductComboSlotRequest>();
editComboSlot     = output<UpdateProductComboSlotRequest & { id: number }>();
removeComboSlot   = output<number>();
```

---

## 7. Cambios a `products-mobile.component`

### 7.1 Formulario crear/editar — nuevos campos

Agregar dos `IonToggle` después del campo `Producto padre`:

```html
<ion-item>
  <ion-label>Tiene opciones configurables</ion-label>
  <ion-toggle slot="end" [checked]="formHasOptions()"
    (ionChange)="formHasOptions.set($event.detail.checked)" />
</ion-item>
<ion-item>
  <ion-label>Es combo</ion-label>
  <ion-toggle slot="end" [checked]="formIsCombo()"
    (ionChange)="formIsCombo.set($event.detail.checked)" />
</ion-item>
```

### 7.2 Lista de productos — badges

En el `ion-item` de cada producto, mostrar badges bajo el nombre:

```html
@if (product.hasOptions) {
  <ion-badge color="warning">Opciones</ion-badge>
}
@if (product.isCombo) {
  <ion-badge color="tertiary">Combo</ion-badge>
}
```

### 7.3 Card expandida — secciones adicionales

La card expandida actual muestra la info básica del producto. Las nuevas secciones se añaden como `ion-card` adicionales dentro de la misma expansión:

**Sección Presentaciones**: `ion-list` de presentaciones con precio de venta visible. No se permite crear/editar desde mobile (solo lectura) en esta versión. Próxima versión puede agregar modal.

**Sección Opciones** *(si `hasOptions`)*: Acordeón Ionic con un item por grupo. Cada grupo muestra sus items como chips. Solo lectura en mobile por ahora.

**Sección Combo** *(si `isCombo`)*: Lista de slots con sus productos permitidos. Solo lectura en mobile por ahora.

> **Decisión de diseño**: La creación y edición de opciones/combo se hace exclusivamente desde desktop en esta versión. Mobile muestra los datos en modo lectura para no sobrecargar la pantalla pequeña.

### 7.4 Carga lazy en mobile

Cuando el usuario expande una card, emitir `(rowExpanded)` al coordinador para que cargue el detalle.

---

## 8. Archivos a crear / modificar

### 8.1 Backend (API)

| Archivo | Acción |
|---|---|
| `Features/Products/Dtos/ProductResponse.cs` | Agregar `HasOptions`, `IsCombo` |
| `Features/Products/Dtos/CreateProductRequest.cs` | Agregar `HasOptions`, `IsCombo` |
| `Features/Products/Dtos/UpdateProductRequest.cs` | Agregar `HasOptions`, `IsCombo` |
| `Features/Products/ProductService.cs` | Mapear `HasOptions`, `IsCombo` en `ToResponse`, `CreateAsync`, `UpdateAsync` |
| `Features/ProductUnits/Dtos/ProductUnitResponse.cs` | Agregar `SalePrice` |
| `Features/ProductUnits/Dtos/CreateProductUnitRequest.cs` | Agregar `SalePrice` |
| `Features/ProductUnits/Dtos/UpdateProductUnitRequest.cs` | Agregar `SalePrice` |
| `Features/ProductUnits/ProductUnitService.cs` | Mapear `SalePrice` en ToResponse, Create, Update |

### 8.2 Frontend — Modelos

| Archivo | Acción |
|---|---|
| `shared/models/product.models.ts` | Agregar campos a `ProductDto`, requests, agregar `ProductUnitDto` + requests, `ProductOptionGroupDto` + requests, `ProductComboSlotDto` + requests |
| `shared/models/index.ts` | Exportar los nuevos tipos |

### 8.3 Frontend — Servicios

| Archivo | Acción |
|---|---|
| `service/product-unit.service.ts` | Crear nuevo |
| `service/product-option-group.service.ts` | Crear nuevo |
| `service/product-combo-slot.service.ts` | Crear nuevo |
| `service/index.ts` | Exportar los tres |

### 8.4 Frontend — Página

| Archivo | Acción |
|---|---|
| `pages/maintenance/products/products.ts` | Inyectar 3 servicios, agregar señales de detalle, métodos CRUD |
| `pages/maintenance/products/products.html` | Nuevos bindings para web |
| `components/products-web/products-web.component.ts` | Nuevos inputs/outputs, señales de form y tabs |
| `components/products-web/products-web.component.html` | Checkboxes en form, columna badges, row-detail con 4 tabs |
| `components/products-mobile/products-mobile.component.ts` | Nuevos inputs/outputs, `formHasOptions`, `formIsCombo` |
| `components/products-mobile/products-mobile.component.html` | Toggles en form, badges, secciones de detalle |

---

## 9. Orden de implementación

```
1. [API]    Gaps en ProductResponse, CreateProductRequest, UpdateProductRequest
2. [API]    Gaps en ProductUnitResponse, CreateProductUnitRequest, UpdateProductUnitRequest
3. [FE]     Actualizar product.models.ts con todos los modelos nuevos
4. [FE]     Crear product-unit.service.ts
5. [FE]     Crear product-option-group.service.ts
6. [FE]     Crear product-combo-slot.service.ts
7. [FE]     Exportar nuevos servicios en service/index.ts
8. [FE]     Actualizar products.ts (coordinator)
9. [FE]     Actualizar products-web: formulario + badges + row-detail tabs
10. [FE]    Actualizar products-mobile: form toggles + badges + secciones read-only
```

---

## 10. Lo que queda fuera de este plan

| Tema | Plan futuro |
|---|---|
| Cálculo de precio final (salePrice + deltas) | Plan de ventas / módulo de facturación |
| Selector de opciones en el flujo de pedido | Plan de ventas |
| Creación/edición de opciones/combo desde mobile | Plan mobile avanzado |
| Validación de `AllowSplit` sólo cuando existe grupo "Modo" (V5 del API) | Plan de validación avanzada |
