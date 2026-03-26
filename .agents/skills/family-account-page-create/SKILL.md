---
name: family-account-page-create
description: >
  Guía completa para crear una página nueva en family-account: modelos TypeScript,
  service, estructura de carpetas, page coordinador, sub-componentes web (Color Admin)
  y mobile (Ionic), barrel de exports, registro en rutas (app.routes.ts) y registro en
  menú (AppMenuService). Usar SIEMPRE que se cree una página nueva en este proyecto.
applyTo: "src/familyAccountWeb/**"
---

# Crear una Página Nueva en family-account

> **Skills de referencia obligatoria**
> - Versión **web** (Color Admin): leer el skill `color-admin` (sección `create-page`) antes de implementar el sub-componente web.
> - Versión **mobile** (Ionic): leer el skill `ionic-design` antes de implementar el sub-componente mobile.

---

## 0. Modelos TypeScript (`src/app/shared/models/<nombre>.models.ts`)

Antes de crear el service o el page, definir los DTOs que corresponden a los DTOs del API (.NET).

```typescript
// src/app/shared/models/<nombre>.models.ts
export interface <Nombre>Dto {
  id<Nombre>: number;          // PK: id{Entidad} camelCase
  campo1: string;
  campo2: number | null;
  isActive: boolean;
}

export interface Create<Nombre>Request {
  campo1: string;
  campo2: number | null;
  isActive: boolean;
}

export interface Update<Nombre>Request {
  campo1: string;
  campo2: number | null;
  isActive: boolean;
}
```

**Convenciones de modelos:**
- El archivo va en `src/app/shared/models/<nombre>.models.ts` (kebab-case).
- Los interfaces reflejan **exactamente** los records del API (C#): campos en camelCase.
- La PK sigue el patrón `id{Entidad}` (ej: `idAccount`, `idBankAccount`).
- Exportar desde `src/app/shared/models/index.ts` agregando:
  ```typescript
  export * from './<nombre>.models';
  ```

---

## 1. Service (`src/app/service/<nombre>.service.ts`)

```typescript
import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { tap, catchError, finalize } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';
import { <Nombre>Dto, Create<Nombre>Request, Update<Nombre>Request } from '../shared/models';

@Injectable({ providedIn: 'root' })
export class <Nombre>Service {
  private readonly http   = inject(HttpClient);
  private readonly logger = inject(LoggerService).getLogger('<Nombre>Service');
  private readonly base   = `${environment.apiUrl}<ruta-api>`; // ej: 'accounts'

  // ── Estado ───────────────────────────────────────────────────────
  items      = signal<<Nombre>Dto[]>([]);
  totalCount = signal<number>(0);
  isLoading  = signal<boolean>(false);
  error      = signal<string | null>(null);

  clearError(): void { this.error.set(null); }

  private start(): void { this.isLoading.set(true); this.error.set(null); }
  private stop():  void { this.isLoading.set(false); }
  private fail(msg: string): void { this.error.set(msg); }

  // ── LISTAR ───────────────────────────────────────────────────────
  loadList(): Observable<<Nombre>Dto[]> {
    this.start();
    return this.http.get<<Nombre>Dto[]>(`${this.base}.json`).pipe(
      tap(res => {
        this.items.set(res ?? []);
        this.totalCount.set(res?.length ?? 0);
      }),
      catchError(err => { this.fail('Error al cargar'); return throwError(() => err); }),
      finalize(() => this.stop()),
    );
  }

  // ── CREAR ────────────────────────────────────────────────────────
  create(req: Create<Nombre>Request): Observable<<Nombre>Dto> {
    this.start();
    return this.http.post<<Nombre>Dto>(`${this.base}/`, req).pipe(
      tap(res => {
        this.items.update(ls => [...ls, res]);
        this.totalCount.update(n => n + 1);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al crear';
        this.fail(typeof msg === 'string' ? msg : 'Error al crear');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ACTUALIZAR ───────────────────────────────────────────────────
  update(id: number, req: Update<Nombre>Request): Observable<<Nombre>Dto> {
    this.start();
    return this.http.put<<Nombre>Dto>(`${this.base}/${id}`, req).pipe(
      tap(res => this.items.update(ls => ls.map(i => (i.id<Nombre> === id ? res : i)))),
      catchError(err => {
        const msg = err?.error ?? 'Error al actualizar';
        this.fail(typeof msg === 'string' ? msg : 'Error al actualizar');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }

  // ── ELIMINAR ─────────────────────────────────────────────────────
  delete(id: number): Observable<void> {
    this.start();
    return this.http.delete<void>(`${this.base}/${id}`).pipe(
      tap(() => {
        this.items.update(ls => ls.filter(i => i.id<Nombre> !== id));
        this.totalCount.update(n => n - 1);
      }),
      catchError(err => {
        const msg = err?.error ?? 'Error al eliminar';
        this.fail(typeof msg === 'string' ? msg : 'Error al eliminar');
        return throwError(() => err);
      }),
      finalize(() => this.stop()),
    );
  }
}
```

**Convenciones del service:**
- Un service por feature, en `src/app/service/<nombre>.service.ts`.
- Exportar desde `src/app/service/index.ts` agregando:
  ```typescript
  export { <Nombre>Service } from './<nombre>.service';
  ```
- El estado son **signals** (`signal()`) — nunca `BehaviorSubject`.
- `loadList()` llama a `<ruta>.json` (convención del API de este proyecto).
- `create()` llama a `<ruta>/` (con barra final), `update`/`delete` a `<ruta>/{id}`.
- Los mensajes de error se obtienen de `err?.error` primero; si no es string, usar mensaje genérico.
- No hay `PagedResult` para endpoints que devuelven array directo; usarlo solo cuando el API retorna `{ data, totalCount }`.

---

## 2. Estructura de carpetas

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

## 3. Page coordinador (`<nombre-kebab>.ts`)

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
import { MiPaginaWebComponent, MiPaginaMobileComponent } from './components';

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

## 4. Template coordinador (`<nombre-kebab>.html`)

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

## 5. Sub-componente Web (`<nombre-kebab>-web.component.ts`)

> **Referencia:** consultar el skill `color-admin` (sección `create-page`) para patrones detallados de `PanelComponent`, `ngx-datatable`, row-detail, formularios de creación/edición y confirmación de borrado.

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

## 6. Sub-componente Mobile (`<nombre-kebab>-mobile.component.ts`)

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

## 7. Barrel de componentes (`components/index.ts`)

```typescript
export { MiPaginaWebComponent } from './mi-pagina-web/mi-pagina-web.component';
export { MiPaginaMobileComponent } from './mi-pagina-mobile/mi-pagina-mobile.component';
```

---

## 8. Barrel del page (`index.ts`)

```typescript
export { MiPaginaPage } from './mi-pagina';
```

---

## 9. Barrel global de páginas (`src/app/pages/index.ts`)

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

## 10. Rutas (`src/app/app.routes.ts`)

Agregar la ruta al array `routes` en el archivo existente:

```typescript
// app.routes.ts — solo agregar la nueva entrada al array routes:
{
  path: 'maintenance/<nombre-kebab>',   // ← siempre bajo maintenance/
  component: MiPaginaPage,
  data: { title: 'Mi Página' },
  canActivate: [AuthGuard],             // ← incluir siempre si requiere autenticación
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

**Convención de rutas:**
- Las páginas de mantenimiento/catálogos van bajo el prefijo `maintenance/`.
- No usar rutas hijas (children) — el proyecto usa rutas planas (`maintenance/accounts`, no `{ path: 'maintenance', children: [...] }`).

---

## 11. Menú (`src/app/service/app-menus.service.ts`)

Las páginas de mantenimiento van dentro del submenu **Mantenimiento**. Agregar el item dentro de `submenu` del bloque `Mantenimiento` existente:

```typescript
// En app-menus.service.ts — agregar dentro del submenu del item Mantenimiento:
{
  icon: 'fa fa-<icono>',
  iconMobile: '<nombre>-outline',
  title: 'Mi Página',
  url: '/maintenance/<nombre-kebab>',
  roles: [1, 2],
},
```

Si aún no existe el bloque Mantenimiento, crearlo:

```typescript
{
  icon: 'fa fa-wrench',
  iconMobile: 'settings-outline',
  title: 'Mantenimiento',
  url: '/maintenance',
  caret: 'true',
  roles: [1, 2],
  submenu: [
    {
      icon: 'fa fa-<icono>',
      iconMobile: '<nombre>-outline',
      title: 'Mi Página',
      url: '/maintenance/<nombre-kebab>',
      roles: [1, 2],
    },
  ],
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

## 12. Checklist completo

```
[ ] Crear src/app/shared/models/<nombre>.models.ts
[ ] Actualizar src/app/shared/models/index.ts    (agregar export)
[ ] Crear src/app/service/<nombre>.service.ts
[ ] Actualizar src/app/service/index.ts          (agregar export)
[ ] Crear carpeta src/app/pages/<nombre-kebab>/
[ ] Crear <nombre-kebab>.ts       (coordinador)
[ ] Crear <nombre-kebab>.html     (template coordinador)
[ ] Crear <nombre-kebab>.scss     (vacío)
[ ] Crear index.ts                (barrel del page)
[ ] Crear components/index.ts     (barrel de sub-componentes)
[ ] Crear components/<nombre>-web/   (3 archivos: .ts, .html, .scss)
[ ] Crear components/<nombre>-mobile/  (3 archivos: .ts, .html, .scss)
[ ] Actualizar src/app/pages/index.ts            (agregar export)
[ ] Actualizar app.routes.ts                     (agregar ruta maintenance/<nombre>)
[ ] Actualizar app-menus.service.ts              (agregar al submenu Mantenimiento)
[ ] Ejecutar build para verificar errores
```
