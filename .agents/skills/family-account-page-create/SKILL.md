---
name: family-account-page-create
description: >
  Guía completa para crear una página nueva en family-account: estructura de carpetas,
  page coordinador, sub-componentes web (Color Admin) y mobile (Ionic), barrel de exports,
  registro en rutas (app.routes.ts) y registro en menú (AppMenuService).
  Usar SIEMPRE que se cree una página nueva en este proyecto.
applyTo: "src/familyAccountWeb/**"
---

# Crear una Página Nueva en family-account

> **Skills de referencia obligatoria**
> - Versión **web** (Color Admin): leer el skill `color-admin-create-page` antes de implementar el sub-componente web.
> - Versión **mobile** (Ionic): leer el skill `ionic-design` antes de implementar el sub-componente mobile.

## 1. Estructura de carpetas

```
src/app/pages/
  index.ts                                  ← Barrel global de páginas (actualizar)
  <nombre-kebab>/
    <nombre-kebab>.ts                       ← Page coordinador
    <nombre-kebab>.html                     ← Template coordinador
    <nombre-kebab>.scss                     ← Vacío (estilos en sub-componentes)
    index.ts                                ← Barrel del page (actualizar)
    components/
      index.ts                              ← Barrel de componentes internos
      <nombre-kebab>-web/
        <nombre-kebab>-web.component.ts
        <nombre-kebab>-web.component.html
        <nombre-kebab>-web.component.scss
      <nombre-kebab>-mobile/
        <nombre-kebab>-mobile.component.ts
        <nombre-kebab>-mobile.component.html
        <nombre-kebab>-mobile.component.scss
```

---

## 2. Page coordinador (`<nombre-kebab>.ts`)

Responsabilidades: estado reactivo con signals + llamadas al servicio. **Sin lógica de presentación**.

```typescript
import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  computed,
  ChangeDetectionStrategy,
} from '@angular/core';
import { AppSettings, LoggerService } from '../../../service';
import { ResponsiveComponent } from '../../../shared';
import { MiPaginaWebComponent } from './components/mi-pagina-web/mi-pagina-web.component';
import { MiPaginaMobileComponent } from './components/mi-pagina-mobile/mi-pagina-mobile.component';

@Component({
  selector: 'app-mi-pagina',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [MiPaginaWebComponent, MiPaginaMobileComponent],
  templateUrl: './mi-pagina.html',
  styleUrls: ['./mi-pagina.scss'],
})
export class MiPaginaPage extends ResponsiveComponent implements OnInit, OnDestroy {
  private readonly logger = inject(LoggerService).getLogger('MiPaginaPage');

  // Estado reactivo
  loading      = signal(false);
  errorMessage = signal('');

  constructor(public appSettings: AppSettings) {
    super();
    // Ajustar layout de Color Admin si la página no usa sidebar
    // this.appSettings.appSidebarNone = true;
    // this.appSettings.appTopMenu     = true;
  }

  override ngOnDestroy(): void {
    // ⚠️ SIEMPRE restaurar AppSettings al salir
    // this.appSettings.appSidebarNone = false;
    // this.appSettings.appTopMenu     = false;
    super.ngOnDestroy();
  }

  ngOnInit(): void {
    this.logger.info('🚀 Cargando Mi Página');
    this.load();
  }

  load(): void {
    // this.svc.loadList().subscribe({ next: ..., error: ... });
  }
}
```

**Reglas del coordinador:**
- Extiende `ResponsiveComponent` → provee `isMobile()` e `isDesktop()` como signals computados.
- Estado como `signal()`, nunca propiedades planas mutables para estado reactivo.
- `ngOnDestroy()` **SIEMPRE** llama `super.ngOnDestroy()` y restaura `AppSettings` si se modificó en el constructor.
- Importa únicamente `MiPaginaWebComponent` y `MiPaginaMobileComponent` (ni Ionic ni Bootstrap directamente).
- Los helpers de display (`formatDate`, `getBadgeClass`) van en los sub-componentes, NO aquí.

---

## 3. Template coordinador (`<nombre-kebab>.html`)

```html
@if (isMobile()) {
  <app-mi-pagina-mobile
    [loading]="loading()"
    [errorMessage]="errorMessage()"
  />
} @else {
  <app-mi-pagina-web
    [loading]="loading()"
    [errorMessage]="errorMessage()"
  />
}
```

**Reglas:**
- Los `input()` del componente hijo se pasan con `()` porque son signals: `[isLoading]="loading()"`.
- No hay breadcrumb, `<h1>` ni contenido presentacional aquí; todo va en el sub-componente web.

---

## 4. Sub-componente Web (`<nombre-kebab>-web.component.ts`)

> **Referencia:** consultar el skill `color-admin-create-page` para patrones detallados de `PanelComponent`, `ngx-datatable`, row-detail, formularios de creación/edición y confirmación de borrado.

Color Admin (Bootstrap 5 + Font Awesome) con `PanelComponent` y `ngx-datatable`.

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
import { NgxDatatableModule, ColumnMode } from '@swimlane/ngx-datatable';
import { PanelComponent } from '../../../../../components';

@Component({
  selector: 'app-mi-pagina-web',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [CommonModule, FormsModule, NgxDatatableModule, PanelComponent],
  templateUrl: './mi-pagina-web.component.html',
  styleUrls: ['./mi-pagina-web.component.scss'],
})
export class MiPaginaWebComponent {
  // Inputs del coordinador
  loading      = input(false);
  errorMessage = input('');

  // Outputs hacia el coordinador
  refresh = output<void>();

  ColumnMode = ColumnMode;
}
```

### Template web (`<nombre-kebab>-web.component.html`)

```html
<!-- BEGIN breadcrumb -->
<ol class="breadcrumb float-xl-end">
  <li class="breadcrumb-item"><a href="javascript:;">Home</a></li>
  <li class="breadcrumb-item active">Mi Página</li>
</ol>
<!-- END breadcrumb -->

<h1 class="page-header">
  Mi Página
  <small>Descripción breve de la sección</small>
</h1>

<!-- Error global -->
@if (errorMessage()) {
  <div class="alert alert-danger mb-3" role="alert">
    <i class="fa fa-exclamation-triangle me-2"></i>{{ errorMessage() }}
  </div>
}

<!-- Panel con tabla -->
<panel title="Mi Sección"
  variant="inverse"
  [showReloadButton]="true"
  [reload]="loading()"
  (onReload)="refresh.emit()">

  <ngx-datatable
    class="bootstrap"
    [rows]="[]"
    [columnMode]="ColumnMode.force"
    [headerHeight]="40"
    [footerHeight]="50"
    [rowHeight]="'auto'"
    [limit]="20"
    [loadingIndicator]="loading()">

    <ngx-datatable-column name="Columna" prop="campo" [sortable]="true">
      <ng-template let-value="value" ngx-datatable-cell-template>
        {{ value }}
      </ng-template>
    </ngx-datatable-column>

  </ngx-datatable>
</panel>
```

---

## 5. Sub-componente Mobile (`<nombre-kebab>-mobile.component.ts`)

> **Referencia:** consultar el skill `ionic-design` para el catálogo completo de componentes Ionic, patrones de listas, theming, dark mode y UX nativa iOS/Android.

Ionic con `IonContent`, `IonCard`, etc. Importar solo los componentes Ionic que se usen.

```typescript
import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { addIcons } from 'ionicons';
import { refreshOutline } from 'ionicons/icons';
import {
  IonContent,
  IonCard,
  IonCardHeader,
  IonCardTitle,
  IonCardContent,
  IonRefresher,
  IonRefresherContent,
  IonIcon,
  IonSpinner,
} from '@ionic/angular/standalone';

@Component({
  selector: 'app-mi-pagina-mobile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    IonContent,
    IonCard,
    IonCardHeader,
    IonCardTitle,
    IonCardContent,
    IonRefresher,
    IonRefresherContent,
    IonIcon,
    IonSpinner,
  ],
  templateUrl: './mi-pagina-mobile.component.html',
  styleUrls: ['./mi-pagina-mobile.component.scss'],
})
export class MiPaginaMobileComponent implements OnInit {
  // Inputs del coordinador
  loading      = input(false);
  errorMessage = input('');

  // Outputs hacia el coordinador
  refresh = output<void>();

  ngOnInit(): void {
    addIcons({ refreshOutline });
  }

  doRefresh(event: CustomEvent): void {
    this.refresh.emit();
    // Detener el refresher después de que el coordinador cargue:
    // event.detail.complete() se llama cuando loading() vuelve a false
    setTimeout(() => (event.target as HTMLIonRefresherElement).complete(), 1500);
  }
}
```

### Template mobile (`<nombre-kebab>-mobile.component.html`)

```html
<ion-content>

  <!-- Pull-to-refresh -->
  <ion-refresher slot="fixed" (ionRefresh)="doRefresh($any($event))">
    <ion-refresher-content></ion-refresher-content>
  </ion-refresher>

  <!-- Error -->
  @if (errorMessage()) {
    <div class="ion-padding">
      <div class="alert-error">
        {{ errorMessage() }}
      </div>
    </div>
  }

  <!-- Loading -->
  @if (loading()) {
    <div class="ion-padding ion-text-center">
      <ion-spinner name="crescent"></ion-spinner>
    </div>
  }

  <!-- Contenido -->
  <div class="ion-padding">
    <ion-card>
      <ion-card-header>
        <ion-card-title>Mi Página</ion-card-title>
      </ion-card-header>
      <ion-card-content>
        <!-- contenido aquí -->
      </ion-card-content>
    </ion-card>
  </div>

</ion-content>
```

---

## 6. Barrel de componentes (`components/index.ts`)

```typescript
export { MiPaginaWebComponent } from './mi-pagina-web/mi-pagina-web.component';
export { MiPaginaMobileComponent } from './mi-pagina-mobile/mi-pagina-mobile.component';
```

---

## 7. Barrel del page (`index.ts`)

```typescript
export { MiPaginaPage } from './mi-pagina';
```

---

## 8. Barrel global de páginas (`src/app/pages/index.ts`)

Agregar el export del nuevo page al archivo existente:

```typescript
// ANTES:
export { HomePage } from './home';
export { ErrorPage } from './error';
export { LoginPage } from './login';

// DESPUÉS (agregar la línea del nuevo page):
export { HomePage } from './home';
export { ErrorPage } from './error';
export { LoginPage } from './login';
export { MiPaginaPage } from './mi-pagina';
```

---

## 9. Rutas (`src/app/app.routes.ts`)

Agregar la ruta al array `routes` en el archivo existente:

```typescript
// app.routes.ts — solo agregar la nueva entrada al array routes:
{
  path: 'mi-pagina',
  component: MiPaginaPage,
  data: { title: 'Mi Página' },
  canActivate: [AuthGuard],   // ← incluir siempre si requiere autenticación
},
```

Importar el componente en el mismo archivo:
```typescript
import { MiPaginaPage } from './pages';
```

**Orden de rutas:**
1. `''` → redirect a `/home`
2. `'login'` → sin guard
3. Páginas autenticadas (orden alfabético o por sección)
4. `'**'` → `ErrorPage` (siempre al final)

---

## 10. Menú (`src/app/service/app-menus.service.ts`)

Agregar el item al array `menuConfig` dentro de `AppMenuService`:

```typescript
// En app-menus.service.ts, agregar dentro de menuConfig:
{
  icon: 'fa fa-<icono>',          // Font Awesome para desktop
  iconMobile: '<nombre>-outline', // Ionicon para mobile
  title: 'Mi Página',
  url: '/mi-pagina',
  roles: [1, 2],                  // IDs de rol que pueden ver el item
                                  // omitir `roles` si es visible para todos
},
```

**Roles disponibles:**
| ID | Nombre  | Acceso |
|----|---------|--------|
| 1  | DEV     | Total  |
| 2  | ADMIN   | Todo excepto herramientas internas |
| 3  | LOCAL   | Solo Inicio + secciones asignadas |
| 4  | SUPPORT | Solo Inicio |

Si el item **no** lleva `roles` (o el array está vacío), es visible para **todos** los roles autenticados.

---

## 11. Checklist completo

```
[ ] Crear carpeta src/app/pages/<nombre-kebab>/
[ ] Crear <nombre-kebab>.ts       (coordinador)
[ ] Crear <nombre-kebab>.html     (template coordinador)
[ ] Crear <nombre-kebab>.scss     (vacío)
[ ] Crear index.ts                (barrel del page)
[ ] Crear components/index.ts     (barrel de sub-componentes)
[ ] Crear components/<nombre>-web/   (3 archivos: .ts, .html, .scss)
[ ] Crear components/<nombre>-mobile/  (3 archivos: .ts, .html, .scss)
[ ] Actualizar src/app/pages/index.ts  (agregar export)
[ ] Actualizar app.routes.ts           (agregar ruta)
[ ] Actualizar app-menus.service.ts    (agregar item al menú)
[ ] Ejecutar build para verificar errores
```
