# Reference: Accesibilidad (a11y) y Anti-patrones

Reglas WCAG 2.1 AA que deben cumplirse en toda página nueva, más la tabla de anti-patrones
comunes a evitar.

---

## Accesibilidad (a11y)

### Controles con `<label>` visible — emparejar `for`/`id`

```html
<!-- ❌ Incorrecto: label y control sueltos -->
<label class="form-label">Código</label>
<input type="text" class="form-control" />

<!-- ✅ Correcto -->
<label class="form-label" for="fCode">Código <span class="text-danger">*</span></label>
<input id="fCode" type="text" class="form-control" />

<!-- ✅ Lo mismo para select -->
<label class="form-label" for="fType">Tipo</label>
<select id="fType" class="form-select">...</select>
```

### Controles sin `<label>` visible — agregar `aria-label`

```html
<!-- ✅ Select de filtro sin label visible -->
<select class="form-select form-select-sm filter-type-select"
  aria-label="Filtrar por tipo"
  [value]="filterType()"
  (change)="filterType.set($any($event.target).value)">
  ...
</select>

<!-- ✅ Input de búsqueda sin label visible -->
<input type="text" class="form-control form-control-sm filter-search-input"
  placeholder="Buscar…"
  aria-label="Buscar por código o nombre"
  [value]="filterSearch()"
  (input)="filterSearch.set($any($event.target).value)" />
```

> `placeholder` **no** cuenta como nombre accesible — siempre añadir `aria-label` además del placeholder.

### Botones de icono sin texto visible

```html
<!-- ✅ Botón de acción con solo icono -->
<button class="btn btn-xs btn-info" aria-label="Editar" title="Editar">
  <i class="fa fa-edit"></i>
</button>

<!-- ✅ btn-close de Bootstrap — siempre vacío por diseño -->
<button type="button" class="btn-close" aria-label="Cerrar" (click)="clearError()"></button>
```

### Enlaces deben tener texto discernible

```html
<!-- ❌ Incorrecto: solo icono, sin texto ni aria-label -->
<a href="javascript:;" (click)="toggle()">
  <i class="fa fa-angle-double-left"></i>
</a>

<!-- ✅ Correcto: aria-label dinámico según estado -->
<a href="javascript:;"
   [attr.aria-label]="isMinified ? 'Expandir menú lateral' : 'Contraer menú lateral'"
   (click)="toggle()">
  <i [class]="isMinified ? 'fa fa-angle-double-right' : 'fa fa-angle-double-left'"></i>
</a>

<!-- ✅ Enlace con contenido de ngTemplateOutlet — aria-label explícito -->
<a class="menu-link" [attr.aria-label]="menu.title" [routerLink]="menu.url">
  <ng-container *ngTemplateOutlet="sidebarMenuNav; context: {menu: menu}"></ng-container>
</a>
```

> axe **no puede** computar el nombre accesible de `<a>` cuyo contenido proviene de `ngTemplateOutlet`.
> Agregar siempre `[attr.aria-label]="menu.title"` directamente en el `<a>`.

### Imágenes con `alt`

```html
<!-- ✅ Con significado -->
<img src="{{ menu.img }}" alt="{{ menu.title || '' }}" />

<!-- ✅ Decorativa -->
<img src="assets/img/decorative-bg.png" alt="" />
```

> Omitir `alt` por completo es siempre un error (WCAG 2.1 AA, regla axe `image-alt`).

### Controles dentro de celdas de tabla (`@for` loop)

Los encabezados `<th>` **no** proveen nombre accesible a los controles de la fila — axe lo reporta como `axe/forms`.
Cada `<input>` o `<select>` dentro de `<td>` debe tener su propio `aria-label`.

```html
<!-- ❌ Incorrecto: el <th>Débito</th> no le da nombre accesible al input -->
<td>
  <input type="number" class="form-control form-control-sm"
    [value]="line.debitAmount" />
</td>

<!-- ✅ Correcto -->
<td>
  <input type="number" class="form-control form-control-sm"
    aria-label="Débito"
    [value]="line.debitAmount" />
</td>

<!-- ✅ Select en celda -->
<td>
  <select class="form-select form-select-sm"
    aria-label="Cuenta contable"
    [value]="line.idAccount">
    ...
  </select>
</td>
```

> Aplica a **todos** los controles repetidos en `@for` dentro de `<tbody>`.
> El índice `$index` nunca debe usarse en `aria-label` — el tipo de campo sí ("Débito", "Proveedor", etc.).

### Inputs `readonly` sin `id` cuando tienen `<label>`

```html
<!-- ❌ Incorrecto: la label no tiene for y el input no tiene id -->
<label class="form-label">Moneda</label>
<input type="text" class="form-control" readonly [value]="currency()" />

<!-- ✅ Correcto -->
<label class="form-label" for="efCurrency">Moneda</label>
<input id="efCurrency" type="text" class="form-control" readonly [value]="currency()" />
```

> Los inputs `readonly` **también** necesitan `id` si tienen `<label for>`. axe no hace excepciones por `readonly`.

---

### Estilos inline prohibidos

```html
<!-- ❌ Incorrecto -->
<select style="width: auto">...</select>

<!-- ✅ Correcto: clase en template + regla en .scss -->
<select class="filter-type-select">...</select>
```

```scss
// en el .component.scss
.filter-type-select  { width: auto; }
.filter-search-input { width: 220px; }
```

---

## Anti-patrones a evitar

| ❌ Incorrecto | ✅ Correcto |
|---|---|
| `@ViewChild('rowDetail')` | `@ViewChild(DatatableRowDetailDirective)` |
| Olvidar `cdr.markForCheck()` tras toggle | Siempre llamar después de `toggleExpandRow()` |
| Botón "Nuevo" dentro del body del panel | Usar slot `<ng-container panel-header>` |
| Panel sin `variant="inverse"` | Siempre `variant="inverse"` |
| `class="material"` en ngx-datatable | Usar `class="bootstrap"` |
| `[rowHeight]="50"` con badges/row-detail | Usar `[rowHeight]="'auto'"` |
| Badges inline sin contenedor | `<div class="d-flex flex-wrap gap-1 py-1">` |
| `ngOnDestroy()` sin llamar `super` | `override ngOnDestroy(): void { super.ngOnDestroy(); }` |
| `ngOnDestroy()` sin restaurar AppSettings | Siempre `appSidebarNone = false` + `appTopMenu = false` antes de `super.ngOnDestroy()` |
| Leer state del servicio en `subscribe` | Asignar directamente: `items = this.svc.items` |
| Mobile con `@ViewChild` para expand | Signal puro: `expandedId.update(v => v === id ? null : id)` |
| `event.target.value` en `ion-searchbar` | Usar `event.detail?.value` — Ionic usa `CustomEvent` con `.detail` |
| `handleRefresh` síncrono | Usar `async/await` con `setTimeout(800)` antes de `.complete()` |
| Helpers de display en el coordinador page | Los helpers (`formatDate`, `getStatusBadgeClass`) van en los **sub-components** |
| `.subscribe()` sin callbacks ni finalize | Usar `{ next, error }` + `finalize()` siempre |
| `<select>` / `<input>` sin `aria-label` cuando no hay label | Siempre `aria-label` cuando no hay `<label>` visible |
| `<label>` sin `for` / control sin `id` | Emparejar siempre: `<label for="fCode">` + `<input id="fCode">` |
| `placeholder` como único nombre accesible | `aria-label` **además** del placeholder |
| Botón de icono sin texto accesible | Agregar `aria-label` o `title` al botón |
| `<button class="btn-close">` sin `aria-label` | Siempre `aria-label="Cerrar"` — Bootstrap `btn-close` no tiene texto interno |
| `<input>`/`<select>` dentro de `<td>` en `@for` sin `aria-label` | El `<th>` NO da nombre accesible al control — agregar `aria-label` en cada control de tabla |
| `<input readonly>` sin `id` cuando tiene `<label for>` | Los readonly también necesitan `id` — axe no hace excepciones por `readonly` |
| Estilos `style="…"` inline en el template | CSS class en el template + regla en el `.component.scss` |
| `<a>` con solo icono sin `aria-label` | `[attr.aria-label]` dinámico o `aria-label` estático descriptivo |
| `<a>` vacío (backdrop/stretched-link) | `aria-label="Cerrar menú lateral"` u otro texto descriptivo |
| `<a>` con contenido de `ngTemplateOutlet` sin `aria-label` | Agregar `[attr.aria-label]="menu.title"` en el propio `<a>` |
| `<img>` sin `alt` | Siempre `alt="{{ title \|\| '' }}"` o `alt=""` para decorativas |
