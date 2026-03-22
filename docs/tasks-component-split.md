# Tareas: Separar componentes en versión Web y Mobile

> **Patrón de referencia**: `header` ya tiene la estructura correcta.  
> Cada componente tendrá un coordinador principal + subcarpeta `components/` con `<name>-web/` y `<name>-mobile/`.

---

## Estructura objetivo por componente

```
<component>/
  <component>.component.ts        ← coordinador (solo isMobile() switch)
  <component>.component.html      ← solo @if(isMobile()) routing
  components/
    <component>-web/
      <component>-web.component.ts
      <component>-web.component.html
    <component>-mobile/
      <component>-mobile.component.ts
      <component>-mobile.component.html
    index.ts                      ← barrel exports
```

---

## BLOQUE 1 — Sidebar

- [ ] **1.1** Crear `sidebar/components/sidebar-web/sidebar-web.component.ts`  
  Extraer lógica web (Color Admin): `ngOnInit`, `handleSidebarMenuToggle`, `expandCollapseSubmenu`, `floatSubMenu`, etc.
- [ ] **1.2** Crear `sidebar/components/sidebar-web/sidebar-web.component.html`  
  Extraer el bloque `@if (isDesktop())` del HTML actual.
- [ ] **1.3** Crear `sidebar/components/sidebar-mobile/sidebar-mobile.component.ts`  
  Extraer lógica mobile (Ionic): `closeMenu`, `toggleMobileSubmenu`, `expandedItems`, `currentUser`, `isAuthenticated`.
- [ ] **1.4** Crear `sidebar/components/sidebar-mobile/sidebar-mobile.component.html`  
  Extraer el bloque `@if (isMobile())` del HTML actual (ion-menu completo).
- [ ] **1.5** Crear `sidebar/components/index.ts` con barrel exports.
- [ ] **1.6** Refactorizar `sidebar.component.ts` → solo coordinador (importar los dos sub-componentes).
- [ ] **1.7** Refactorizar `sidebar.component.html` → solo `@if(isMobile()) <sidebar-mobile> @else <sidebar-web>`.

---

## BLOQUE 2 — Sidebar Right

- [ ] **2.1** Crear `sidebar-right/components/sidebar-right-web/sidebar-right-web.component.ts`  
  Mover toda la lógica actual (es solo desktop, no tiene lógica mobile).
- [ ] **2.2** Crear `sidebar-right/components/sidebar-right-web/sidebar-right-web.component.html`  
  Mover el template HTML actual completo.
- [ ] **2.3** Crear `sidebar-right/components/sidebar-right-mobile/sidebar-right-mobile.component.ts`  
  Stub vacío (o `ng-container` vacío, ya que sidebar-right no aplica en mobile).
- [ ] **2.4** Crear `sidebar-right/components/sidebar-right-mobile/sidebar-right-mobile.component.html`  
  Template vacío o `<!-- sidebar-right: not applicable on mobile -->`.
- [ ] **2.5** Crear `sidebar-right/components/index.ts` con barrel exports.
- [ ] **2.6** Refactorizar `sidebar-right.component.ts` → solo coordinador.
- [ ] **2.7** Refactorizar `sidebar-right.component.html` → routing web/mobile.

---

## BLOQUE 3 — Theme Panel

- [ ] **3.1** Crear `theme-panel/components/theme-panel-web/theme-panel-web.component.ts`  
  Extraer lógica web (bootstrap modal, Color Admin): `handleThemePanel`, `handleDarkMode`, Color pickers, etc.
- [ ] **3.2** Crear `theme-panel/components/theme-panel-web/theme-panel-web.component.html`  
  Extraer bloque `@if (isDesktop())` del HTML.
- [ ] **3.3** Crear `theme-panel/components/theme-panel-mobile/theme-panel-mobile.component.ts`  
  Extraer lógica mobile (Ionic ion-menu, `openMobileSettings`, `closeMobileSettings`, FAB, AlertController).
- [ ] **3.4** Crear `theme-panel/components/theme-panel-mobile/theme-panel-mobile.component.html`  
  Extraer bloque `@if (isMobile())` del HTML (ion-fab + ion-menu completo).
- [ ] **3.5** Crear `theme-panel/components/index.ts` con barrel exports.
- [ ] **3.6** Refactorizar `theme-panel.component.ts` → solo coordinador (propagar `@Output` hacia arriba).
- [ ] **3.7** Refactorizar `theme-panel.component.html` → routing web/mobile.

---

## BLOQUE 4 — Top Menu

- [ ] **4.1** Crear `top-menu/components/top-menu-web/top-menu-web.component.ts`  
  Mover toda la lógica actual (es solo desktop: `handleTopMenuSubMenu`, drag, `isActive`, `isChildActive`, etc.).
- [ ] **4.2** Crear `top-menu/components/top-menu-web/top-menu-web.component.html`  
  Mover el template HTML actual completo.
- [ ] **4.3** Crear `top-menu/components/top-menu-mobile/top-menu-mobile.component.ts`  
  Stub vacío (top-menu no existe en mobile, el menú mobile es el sidebar).
- [ ] **4.4** Crear `top-menu/components/top-menu-mobile/top-menu-mobile.component.html`  
  Template vacío `<!-- top-menu: not applicable on mobile -->`.
- [ ] **4.5** Crear `top-menu/components/index.ts` con barrel exports.
- [ ] **4.6** Refactorizar `top-menu.component.ts` → solo coordinador.
- [ ] **4.7** Refactorizar `top-menu.component.html` → routing web/mobile.

---

## BLOQUE 5 — Footer

- [ ] **5.1** Crear `footer/components/footer-web/footer-web.component.ts`  
  Extraer lógica web: `footerText`, `footerClass`, `hasCustomContent`. Usar `AppSettings`.
- [ ] **5.2** Crear `footer/components/footer-web/footer-web.component.html`  
  Extraer bloque `@if (isDesktop())` del HTML (div#footer + ng-content).
- [ ] **5.3** Crear `footer/components/footer-mobile/footer-mobile.component.ts`  
  Extraer lógica mobile: `footerText`, `color`, `translucent`, `hasCustomContent`.
- [ ] **5.4** Crear `footer/components/footer-mobile/footer-mobile.component.html`  
  Extraer bloque `@if (isMobile())` del HTML (ion-footer + ion-toolbar).
- [ ] **5.5** Crear `footer/components/index.ts` con barrel exports.
- [ ] **5.6** Refactorizar `footer.component.ts` → solo coordinador (pasar `@Input` a ambos sub-componentes).
- [ ] **5.7** Refactorizar `footer.component.html` → routing web/mobile.

---

## BLOQUE 6 — Float Sub Menu

- [ ] **6.1** Crear `float-sub-menu/components/float-sub-menu-web/float-sub-menu-web.component.ts`  
  Mover toda la lógica actual (es solo desktop: `expandCollapseSubmenu`, `remainMenu`, `hideMenu`, inputs de posición).
- [ ] **6.2** Crear `float-sub-menu/components/float-sub-menu-web/float-sub-menu-web.component.html`  
  Mover el template HTML actual completo.
- [ ] **6.3** Crear `float-sub-menu/components/float-sub-menu-mobile/float-sub-menu-mobile.component.ts`  
  Stub vacío (float-sub-menu no existe en mobile).
- [ ] **6.4** Crear `float-sub-menu/components/float-sub-menu-mobile/float-sub-menu-mobile.component.html`  
  Template vacío.
- [ ] **6.5** Crear `float-sub-menu/components/index.ts` con barrel exports.
- [ ] **6.6** Refactorizar `float-sub-menu.component.ts` → solo coordinador (re-exponer todos los `@Input`/`@Output`).
- [ ] **6.7** Refactorizar `float-sub-menu.component.html` → routing web/mobile.

---

## BLOQUE 7 — Eliminar campaign-result-modal y export-result-modal

> Ambos se usan **solo** en `header-web`. Se inlinean ahí y se eliminan como componentes independientes.

- [ ] **7.1** Abrir `header/components/header-web/header-web.component.html`.  
  Reemplazar `<app-campaign-result-modal ...>` y `<app-export-result-modal ...>` con los templates inline (el HTML de los modales ya está en los componentes como template string).
- [ ] **7.2** Abrir `header/components/header-web/header-web.component.ts`.  
  Eliminar imports de `CampaignResultModalComponent` y `ExportResultModalComponent`.  
  Mantener los signals `showCampaignResultModal` y `showExportResultModal`.
- [ ] **7.3** Eliminar carpeta `components/campaign-result-modal/` completa.
- [ ] **7.4** Eliminar carpeta `components/export-result-modal/` completa.
- [ ] **7.5** Verificar que `components/index.ts` no exporta los modales eliminados (actualmente no los exporta, confirmar).

---

## BLOQUE 8 — Verificación final

- [ ] **8.1** Ejecutar `npm run build:dev` desde `src/familyAccountWeb/` y verificar 0 errores.
- [ ] **8.2** Revisar que el barrel `components/index.ts` exporta todos los coordinadores correctamente.
- [ ] **8.3** Verificar que ningún archivo externo importa los modales eliminados directamente.
- [ ] **8.4** Smoke test visual en modo desktop y mobile (Chrome DevTools device mode).

---

## Notas de implementación

### Coordinador típico (TS)
```typescript
@Component({
  selector: 'sidebar',
  templateUrl: './sidebar.component.html',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, SidebarWebComponent, SidebarMobileComponent],
})
export class SidebarComponent extends ResponsiveComponent {
  // Solo @Input/@Output que necesitan los hijos, pasados via binding
}
```

### Coordinador típico (HTML)
```html
@if (isMobile()) {
  <sidebar-mobile [inputs]="..." (outputs)="..."/>
} @else {
  <sidebar-web [inputs]="..." (outputs)="..."/>
}
```

### Para componentes que solo existen en web (sidebar-right, top-menu, float-sub-menu)
El archivo mobile puede ser un stub mínimo:
```typescript
@Component({ selector: 'sidebar-right-mobile', template: '', standalone: true })
export class SidebarRightMobileComponent {}
```

### Orden sugerido de ejecución
1. **Bloque 7** (eliminar modales) — menor riesgo, cambio acotado a header-web
2. **Bloque 5** (footer) — más simple, buen calentamiento
3. **Bloque 6** (float-sub-menu) — web-only, sin lógica mobile
4. **Bloque 4** (top-menu) — web-only, sin lógica mobile
5. **Bloque 2** (sidebar-right) — web-only, sin lógica mobile
6. **Bloque 3** (theme-panel) — tiene lógica mobile compleja
7. **Bloque 1** (sidebar) — el más complejo, dejar para el final
8. **Bloque 8** (verificación)
