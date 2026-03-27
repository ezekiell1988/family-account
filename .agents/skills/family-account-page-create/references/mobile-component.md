# Reference: Mobile Component (Ionic — ion-list expandible + FAB)

TypeScript y HTML del sub-componente móvil: lista Ionic expandible con pull-to-refresh,
formulario inline en ion-card, confirmación de borrado y FAB de nueva entrada.

> **Regla de tecnología:** todo es Ionic (`@ionic/angular/standalone`). Cero Bootstrap,
> cero Font Awesome, cero clases CSS custom. Los formularios usan `ion-card` con
> `ion-input fill="outline"` standalone. La lista usa `ion-list + ion-item`,
> NO `ion-card` por ítem. El botón de nueva entrada es un `ion-fab` fijo.

---

## TypeScript

```typescript
import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { addIcons } from 'ionicons';
import {
  addOutline, pencilOutline, trashOutline,
  chevronDownOutline, chevronForwardOutline,
  albumsOutline, warningOutline, closeOutline, saveOutline,
} from 'ionicons/icons';
import {
  IonContent,
  IonRefresher,
  IonRefresherContent,
  IonSpinner,
  IonText,
  IonList,
  IonItem,
  IonLabel,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonBadge,
  IonButton,
  IonIcon,
  IonInput,
  IonToggle,     // incluir si hay toggles en el formulario
  IonSelect,     // incluir si hay selects en el formulario
  IonSelectOption,
  IonGrid,
  IonRow,
  IonCol,
  IonFab,
  IonFabButton,
} from '@ionic/angular/standalone';
import { HeaderComponent, FooterComponent } from '../../../../../components';
import { MiItemDto } from '../../../../../shared/models';

@Component({
  selector: 'app-mi-pagina-mobile',
  host: { class: 'ion-page' },                // ← OBLIGATORIO
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslatePipe,
    HeaderComponent,
    FooterComponent,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonSpinner,
    IonText,
    IonList,
    IonItem,
    IonLabel,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonBadge,
    IonButton,
    IonIcon,
    IonInput,
    IonGrid,
    IonRow,
    IonCol,
    IonFab,
    IonFabButton,
  ],
  templateUrl: './mi-pagina-mobile.component.html',
})
export class MiPaginaMobileComponent {
  items        = input<MiItemDto[]>([]);
  isLoading    = input(false);
  deletingId   = input<number | null>(null);
  errorMessage = input('');

  refresh    = output<void>();
  create     = output<unknown>(); // reemplazar con CreatePayload tipado
  editSave   = output<unknown>(); // reemplazar con UpdatePayload & { id: number }
  remove     = output<number>();
  clearError = output<void>();

  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formField       = signal('');
  confirmDeleteId = signal<number | null>(null);

  isEditing   = computed(() => this.editingId() !== null);
  isFormValid = computed(() => this.formField().trim().length > 0);

  constructor() {
    addIcons({
      addOutline, pencilOutline, trashOutline,
      chevronDownOutline, chevronForwardOutline,
      albumsOutline, warningOutline, closeOutline, saveOutline,
    });
  }

  toggleExpand(id: number): void {
    this.expandedId.update(v => v === id ? null : id);
  }

  // ⚠️ Debe ser async — el refresher necesita esperar antes de cerrarse
  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

  openCreate(): void         { /* inicializar signals del form; showForm.set(true) */ }
  openEdit(row: MiItemDto): void { /* cargar row en signals del form */ }
  cancelForm(): void         { this.showForm.set(false); this.editingId.set(null); }
  submitForm(): void         { /* emit create o editSave, luego cancelForm() */ }
  askDelete(id: number): void    { this.confirmDeleteId.set(id); }
  cancelDelete(): void           { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }
}
```

**Diferencias clave respecto al web component:**
- `CUSTOM_ELEMENTS_SCHEMA` — **NO usar**. Importar cada componente Ionic individualmente.
- **Sin** `@ViewChild`, **sin** `ChangeDetectorRef` — el expand es un `@if` reactivo sobre un signal.
- `handleRefresh(event)` debe ser `async` para que `.complete()` se llame en el momento correcto.
- `addIcons()` va en el **constructor** (no en `ngOnInit`) — los iconos deben estar disponibles antes del primer render.

---

## HTML

```html
<!-- Header (maneja ion-header, toolbar, menú y notificaciones internamente) -->
<header
  [pageTitle]="'MI_PAGINA.TITLE' | translate"
  color="theme"
  [translucent]="true">
</header>

<ion-content class="ion-padding">
  <!-- Pull-to-refresh -->
  <ion-refresher slot="fixed" (ionRefresh)="handleRefresh($event)">
    <ion-refresher-content></ion-refresher-content>
  </ion-refresher>

  <!-- Error -->
  @if (errorMessage()) {
    <ion-text color="danger">
      <ion-grid>
        <ion-row class="ion-align-items-center">
          <ion-col>
            <ion-icon name="warning-outline" aria-hidden="true"></ion-icon>
            {{ errorMessage() }}
          </ion-col>
          <ion-col size="auto">
            <ion-button fill="clear" size="small" color="danger" (click)="clearError.emit()">
              <ion-icon slot="icon-only" name="close-outline"></ion-icon>
            </ion-button>
          </ion-col>
        </ion-row>
      </ion-grid>
    </ion-text>
  }

  <!-- Spinner de carga -->
  @if (isLoading()) {
    <div class="ion-text-center ion-padding">
      <ion-spinner name="crescent"></ion-spinner>
    </div>
  }

  <!-- Formulario crear/editar (ion-card) -->
  @if (showForm()) {
    <ion-card>
      <ion-card-header>
        <ion-card-title>
          <ion-icon [name]="isEditing() ? 'pencil-outline' : 'add-outline'"></ion-icon>
          {{ isEditing() ? ('MI_PAGINA.FORM.EDIT_TITLE' | translate) : ('MI_PAGINA.FORM.NEW_TITLE' | translate) }}
        </ion-card-title>
      </ion-card-header>
      <ion-card-content>
        <!--
          ⚠️ ion-input con fill="outline" es STANDALONE — NO va dentro de <ion-item>.
          Dentro de <ion-item> se omite fill y labelPlacement.
        -->
        <ion-input
          fill="outline"
          labelPlacement="floating"
          [label]="'MI_PAGINA.FORM.CAMPO_LABEL' | translate"
          [value]="formField()"
          (ionInput)="formField.set($any($event.target).value)">
        </ion-input>
        <ion-grid class="ion-margin-top">
          <ion-row>
            <ion-col>
              <ion-button expand="block" (click)="submitForm()" [disabled]="!isFormValid()">
                <ion-icon slot="start" name="save-outline"></ion-icon>
                {{ isEditing() ? ('COMMON.SAVE' | translate) : ('MI_PAGINA.FORM.CREATE_BTN' | translate) }}
              </ion-button>
            </ion-col>
            <ion-col>
              <ion-button expand="block" fill="outline" color="medium" (click)="cancelForm()">
                <ion-icon slot="start" name="close-outline"></ion-icon>
                {{ 'COMMON.CANCEL' | translate }}
              </ion-button>
            </ion-col>
          </ion-row>
        </ion-grid>
      </ion-card-content>
    </ion-card>
  }

  <!-- Confirmación eliminar (ion-card color="warning") -->
  @if (confirmDeleteId() !== null) {
    <ion-card color="warning">
      <ion-card-content>
        <ion-text>
          <p>
            <ion-icon name="warning-outline"></ion-icon>
            {{ 'MI_PAGINA.CONFIRM_DELETE' | translate }}
          </p>
        </ion-text>
        <ion-grid>
          <ion-row>
            <ion-col>
              <ion-button expand="block" color="danger" (click)="confirmDelete()">
                <ion-icon slot="start" name="trash-outline"></ion-icon>
                {{ 'COMMON.DELETE' | translate }}
              </ion-button>
            </ion-col>
            <ion-col>
              <ion-button expand="block" fill="outline" color="medium" (click)="cancelDelete()">
                {{ 'COMMON.CANCEL' | translate }}
              </ion-button>
            </ion-col>
          </ion-row>
        </ion-grid>
      </ion-card-content>
    </ion-card>
  }

  <!-- Lista de ítems (ion-list + ion-item expandible) -->
  @if (items().length === 0 && !isLoading()) {
    <div class="ion-text-center ion-padding">
      <ion-icon name="albums-outline" size="large"></ion-icon>
      <ion-text color="medium">
        <p>{{ 'MI_PAGINA.EMPTY' | translate }}</p>
      </ion-text>
    </div>
  } @else {
    <ion-list>
      @for (item of items(); track item.idMiItem) {
        <!--
          [button]="true"  → efecto ripple/hover al hacer tap
          [detail]="false" → ocultar la flecha de detalle (>) de iOS
          El chevron se muestra manualmente en slot="start"
        -->
        <ion-item [button]="true" [detail]="false" (click)="toggleExpand(item.idMiItem)">
          <ion-icon
            slot="start"
            [name]="expandedId() === item.idMiItem ? 'chevron-down-outline' : 'chevron-forward-outline'"
            color="medium">
          </ion-icon>
          <ion-label>
            <h2>{{ item.nombre }}</h2>
            <p><!-- subtítulo si aplica --></p>
          </ion-label>
          <ion-badge slot="end" [color]="item.isActive ? 'success' : 'medium'">
            {{ item.isActive ? ('COMMON.YES' | translate) : ('COMMON.NO' | translate) }}
          </ion-badge>
        </ion-item>

        <!-- Detalle expandido: div con padding, NO un ion-item adicional -->
        @if (expandedId() === item.idMiItem) {
          <div class="ion-padding-horizontal ion-padding-bottom">
            <ion-text color="medium">
              <p><!-- campos extra del registro --></p>
            </ion-text>
            <ion-grid>
              <ion-row>
                <ion-col>
                  <ion-button size="small" color="primary" (click)="openEdit(item)"
                    [disabled]="showForm() || deletingId() === item.idMiItem">
                    <ion-icon slot="start" name="pencil-outline"></ion-icon>
                    {{ 'COMMON.EDIT' | translate }}
                  </ion-button>
                </ion-col>
                <ion-col>
                  <ion-button size="small" color="danger" (click)="askDelete(item.idMiItem)"
                    [disabled]="showForm() || deletingId() === item.idMiItem">
                    @if (deletingId() === item.idMiItem) {
                      <ion-spinner slot="icon-only" name="crescent"></ion-spinner>
                    } @else {
                      <ion-icon slot="start" name="trash-outline"></ion-icon>
                      {{ 'COMMON.DELETE' | translate }}
                    }
                  </ion-button>
                </ion-col>
              </ion-row>
            </ion-grid>
          </div>
        }
      }
    </ion-list>
  }

  <!-- FAB: nuevo registro (fijo abajo a la derecha, dentro de ion-content) -->
  <ion-fab slot="fixed" vertical="bottom" horizontal="end">
    <ion-fab-button
      color="success"
      (click)="openCreate()"
      [disabled]="showForm()"
      [attr.aria-label]="'MI_PAGINA.NEW_ITEM' | translate">
      <ion-icon name="add-outline"></ion-icon>
    </ion-fab-button>
  </ion-fab>
</ion-content>

<!-- Footer (maneja ion-footer internamente) -->
<app-footer></app-footer>
```

## Notas

- **`header` (selector del `HeaderComponent`)** va **antes** de `<ion-content>`, fuera de él.
- **`app-footer`** va **después** de `</ion-content>`, fuera de él.
- **`ion-fab slot="fixed"`** va **dentro** de `<ion-content>` — el `slot="fixed"` lo ancla sin hacer scroll.
- El formulario y la confirmación usan `ion-card` — los ítems de la lista usan `ion-list + ion-item`.
- `ion-input fill="outline"` es **standalone**: nunca dentro de `<ion-item>`. Dentro de un item se omite `fill`.
- Sin breadcrumb ni `<h1>` — son exclusivos del sub-componente web (Color Admin).
- Sin `@for` con `visibleItems()` — usar simplemente `items()` salvo que se necesite infinite scroll.


---

## TypeScript

```typescript
import {
  Component,
  ChangeDetectionStrategy,
  CUSTOM_ELEMENTS_SCHEMA,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MiItemDto } from '../../../../../shared/models';

@Component({
  selector: 'app-mi-pagina-mobile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],   // ← para los elementos <ion-*>
  templateUrl: './mi-pagina-mobile.component.html',
  styleUrls: ['./mi-pagina-mobile.component.scss'],
})
export class MiPaginaMobileComponent {
  items      = input<MiItemDto[]>([]);
  isLoading  = input(false);
  deletingId = input<number | null>(null);

  refresh  = output<void>();
  create   = output<CreatePayload>();
  editSave = output<UpdatePayload & { id: number }>();
  remove   = output<number>();

  // En mobile el expand es puro signal, sin ViewChild ni cdr
  expandedId      = signal<number | null>(null);
  showForm        = signal(false);
  editingId       = signal<number | null>(null);
  formField       = signal('');
  confirmDeleteId = signal<number | null>(null);

  isEditing = computed(() => this.editingId() !== null);

  toggleExpand(id: number): void {
    this.expandedId.update(v => v === id ? null : id);
  }

  // ⚠️ DEBE ser async — el refresher necesita esperar antes de cerrarse
  async handleRefresh(event: CustomEvent): Promise<void> {
    this.refresh.emit();
    await new Promise(r => setTimeout(r, 800));
    (event.target as HTMLIonRefresherElement).complete();
  }

  // ── Infinite scroll (alternativa a paginación en mobile) ────────
  visibleCount = signal(20);
  visibleItems = computed(() => this.items().slice(0, this.visibleCount()));
  hasMore      = computed(() => this.visibleCount() < this.items().length);

  loadMore(): void {
    this.visibleCount.update(n => n + 20);
  }

  // ── Búsqueda Ionic ──────────────────────────────────────────────
  // ⚠️ ion-searchbar emite CustomEvent con { detail: { value } }
  //    NO usar event.target.value — en Ionic no funciona
  onSearchInput(event: CustomEvent): void {
    // this.search.emit((event.detail?.value ?? '') as string);
  }

  openCreate(): void { /* inicializar signals del form, showForm.set(true) */ }
  openEdit(row: MiItemDto): void { /* cargar row en signals del form */ }
  cancelForm(): void { this.showForm.set(false); this.editingId.set(null); }
  submitForm(): void { /* emit create o editSave, luego cancelForm() */ }
  askDelete(id: number): void { this.confirmDeleteId.set(id); }
  cancelDelete(): void { this.confirmDeleteId.set(null); }
  confirmDelete(): void {
    const id = this.confirmDeleteId();
    if (id !== null) { this.remove.emit(id); this.confirmDeleteId.set(null); }
  }
}
```

**Diferencias clave respecto al web component:**
- `CUSTOM_ELEMENTS_SCHEMA` — requerido para los web components de Ionic (`<ion-refresher>`, etc.).
- **Sin** `@ViewChild`, **sin** `ChangeDetectorRef` — el expand en mobile es un `@if` reactivo sobre un signal, no hay datatable imperativo.
- `handleRefresh(event)` — llama a `.complete()` sobre el `ion-refresher` element y **debe ser `async`**.

---

## HTML

```html
<!-- Pull-to-refresh de Ionic -->
<ion-refresher slot="fixed" (ionRefresh)="handleRefresh($event)">
  <ion-refresher-content></ion-refresher-content>
</ion-refresher>

<!-- Spinner de carga -->
@if (isLoading()) {
  <div class="p-3 text-center text-muted">
    <i class="fa fa-spinner fa-spin me-2"></i>Cargando...
  </div>
}

<!-- Formulario crear/editar -->
@if (showForm()) {
  <div class="card mx-2 mb-3">
    <div class="card-header fw-bold">
      {{ isEditing() ? 'Editar' : 'Nuevo' }}
    </div>
    <div class="card-body">
      <div class="mb-2">
        <label class="form-label form-label-sm" for="fCampoMobile">
          Campo <span class="text-danger">*</span>
        </label>
        <input id="fCampoMobile" type="text" class="form-control form-control-sm"
          [value]="formField()"
          (input)="formField.set($any($event.target).value)" />
      </div>
      <div class="d-flex gap-2">
        <button class="btn btn-sm btn-primary" (click)="submitForm()" [disabled]="!formField()">
          <i class="fa fa-save me-1"></i>{{ isEditing() ? 'Guardar' : 'Crear' }}
        </button>
        <button class="btn btn-sm btn-secondary" (click)="cancelForm()">Cancelar</button>
      </div>
    </div>
  </div>
}

<!-- Confirmación eliminar -->
@if (confirmDeleteId() !== null) {
  <div class="alert alert-warning mx-2 mb-2 d-flex flex-column gap-2">
    <span><i class="fa fa-exclamation-triangle me-1"></i>¿Eliminar este registro?</span>
    <div class="d-flex gap-2">
      <button class="btn btn-sm btn-danger" (click)="confirmDelete()">Eliminar</button>
      <button class="btn btn-sm btn-secondary" (click)="cancelDelete()">Cancelar</button>
    </div>
  </div>
}

<!-- Botón nuevo -->
@if (!showForm()) {
  <div class="px-2 mb-3">
    <button class="btn btn-success btn-sm w-100" (click)="openCreate()">
      <i class="fa fa-plus me-1"></i>Nuevo
    </button>
  </div>
}

<!-- Lista de tarjetas colapsables -->
@for (item of visibleItems(); track item.idMiItem) {
  <div class="card mx-2 mb-2">
    <div class="card-header d-flex align-items-center"
      (click)="toggleExpand(item.idMiItem)"
      style="cursor:pointer">
      <span class="fw-semibold flex-grow-1">{{ item.nombre }}</span>
      @if (item.isActive) {
        <span class="badge bg-success me-2">Activa</span>
      } @else {
        <span class="badge bg-secondary me-2">Inactiva</span>
      }
      <i [class]="'fa fa-chevron-' + (expandedId() === item.idMiItem ? 'up' : 'down')"></i>
    </div>

    <!-- Detalle colapsable -->
    @if (expandedId() === item.idMiItem) {
      <div class="card-body">
        <div class="mb-2 text-muted small">
          Actualizado: {{ item.updateAt | date:'dd/MM/yyyy HH:mm' }}
        </div>
        <div class="d-flex gap-2">
          <button class="btn btn-sm btn-info" (click)="openEdit(item)"
            [disabled]="showForm() || deletingId() === item.idMiItem">
            <i class="fa fa-edit me-1"></i>Editar
          </button>
          <button class="btn btn-sm btn-danger"
            (click)="askDelete(item.idMiItem)"
            [disabled]="showForm() || deletingId() === item.idMiItem">
            @if (deletingId() === item.idMiItem) {
              <i class="fa fa-spinner fa-spin"></i>
            } @else {
              <i class="fa fa-trash me-1"></i>Eliminar
            }
          </button>
        </div>
      </div>
    }
  </div>
}

<!-- Estado vacío -->
@if (items().length === 0 && !isLoading()) {
  <div class="p-4 text-center text-muted">
    <i class="fa fa-inbox fa-2x mb-2 d-block"></i>
    No hay registros.
  </div>
}
```

## Notas

- El `@for` itera sobre `visibleItems()` (no `items()`) para soporte de infinite scroll.
- `(click)` en el header de la tarjeta activa el toggle — usar `style="cursor:pointer"`.
- Sin breadcrumb ni `<h1>` — eso es exclusivo del coordinador desktop.
