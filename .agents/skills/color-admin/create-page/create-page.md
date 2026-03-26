---
name: color-admin-create-page
description: >
  Guía completa para crear una página nueva de mantenimiento/listado en este proyecto:
  estructura de carpetas, page coordinador con ResponsiveComponent, sub-componente web
  (Color Admin + Panel + ngx-datatable con row-detail opcional) y sub-componente mobile
  (Ionic cards colapsables con pull-to-refresh e infinite scroll). Cubre también barrels,
  registro en rutas, registro en menú, i18n con ngx-translate, accesibilidad WCAG 2.1 AA
  y tabla de anti-patrones. Usar SIEMPRE que se cree una página nueva de mantenimiento/listado.
applyTo: "**/*.{html,ts}"
---

# Skill: Color Admin — Crear Página Nueva

## Propósito

Guía paso a paso para crear una página de mantenimiento/listado con arquitectura
coordinador + sub-componentes web/mobile.

**Disparar cuando:**
- Se pide crear una página nueva de mantenimiento, listado o CRUD
- Se necesita estructurar un page con componentes web y mobile separados
- Se implementa un panel con ngx-datatable y acciones CRUD
- Se agrega una ruta nueva o una entrada en el menú de navegación

## Referencias

[reference: references/page-coordinator.md]
> Estructura de carpetas, Page component (.ts + .html), archivos index.ts (barrels).
> Patrón ResponsiveComponent + AppSettings + LoggerService.

[reference: references/web-component.md]
> Sub-componente de escritorio: Panel + ngx-datatable + formulario inline crear/editar.
> Variante sin y con row-detail (columna expandible). Checklist Panel y ngx-datatable.

[reference: references/mobile-component.md]
> Sub-componente móvil: tarjetas Ionic colapsables, pull-to-refresh, infinite scroll,
> búsqueda con ion-searchbar. CUSTOM_ELEMENTS_SCHEMA. handleRefresh async.

[reference: references/registration-i18n.md]
> Registro en app.routes.ts con AuthGuard, registro en menú (app-menus.service.ts),
> claves @ngx-translate (es.json / en.json) y claves COMMON.* reutilizables.

[reference: references/accessibility-antipatterns.md]
> Reglas WCAG 2.1 AA: for/id, aria-label, btn-close, links con ngTemplateOutlet, alt en imágenes,
> estilos inline prohibidos. Tabla completa de anti-patrones a evitar.

## Flujo de creación recomendado

1. Crear carpeta `pages/<sección>/<nombre-kebab>/` con subcarpeta `components/`
2. Crear **Page coordinator** (`.ts` + `.html` + `.scss`) y su `index.ts`
3. Crear **Web component** (`-web.component.ts` + `.html`) y su `index.ts`
4. Crear **Mobile component** (`-mobile.component.ts` + `.html`) y su `index.ts`
5. Actualizar `components/index.ts` con los dos exports
6. Registrar la ruta en `app.routes.ts`
7. Agregar la entrada en `app-menus.service.ts`
8. Agregar claves i18n en `es.json` y `en.json`
