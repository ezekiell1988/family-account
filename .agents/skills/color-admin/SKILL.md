---
name: color-admin
description: >
  Skill maestro del template Color Admin para Angular. Enruta a cinco secciones especializadas:
  page-options (configurar layout/sidebar/menú con AppSettings en TypeScript),
  ui-elements (botones, iconos, tabs, modales, badges, tablas y cualquier elemento HTML visual),
  form (inputs, selects, checkboxes, validación, wizards y plugins de formulario en HTML),
  helper (utility classes CSS de Color Admin: colores, espaciado en px, tamaños, tipografía),
  create-page (crear página nueva de mantenimiento/listado con web+mobile, rutas, menú e i18n).
  Usar SIEMPRE que se construya o modifique cualquier componente Angular que use el template
  Color Admin: layout de página, elementos de UI, formularios, clases CSS de utilidad o cuando
  se cree una página nueva de mantenimiento/listado.
applyTo: "**/*.{html,ts}"
---

# Skill: Color Admin (maestro)

## Propósito

Este skill es el punto de entrada raíz para todo el conocimiento de **Color Admin** en este
proyecto Angular. Recoge cuatro secciones especializadas, identifica para cada una qué tipo
de archivo cubre y cuándo debe activarse.

**No inventes clases ni estructura HTML.** Todo lo que necesitas está documentado en las
referencias de cada sección.

---

## Secciones disponibles

### 1. Page Options — configuración de layout (`.ts`)

Controla el layout de la página mediante `AppSettings`: sidebar, menús, boxed layout,
full-height, footer fijo, etc.

[reference: page-options/page-options.md]
> Todas las opciones de layout de Color Admin activadas desde TypeScript (AppSettings).
> Incluye patrón base constructor/ngOnDestroy y referencia completa de variantes.

---

### 2. UI Elements — elementos visuales HTML (`.html`)

Botones, alertas, tipografía, tabs, acordeones, modales, iconos, banderas, tablas, badges,
progress bars, media objects, social buttons y widget boxes.

[reference: ui-elements/ui-elements.md]
> Clases CSS y patrones HTML listos para copiar. CSS ya compilado en vendor.min.css.

---

### 3. Form — formularios HTML (`.html`)

Inputs, selects, checkboxes, radios, switches, grupos de entrada, validación visual,
layouts (vertical, horizontal, inline), wizards multi-paso y plugins (datepicker,
timepicker, tagify, editor rico, color picker).

[reference: form/form.md]
> Formularios completos sin CSS personalizado, usando solo clases de Color Admin y ng-bootstrap.

---

### 4. Helper CSS — utility classes (`.html`)

Spacing en px (`mt-10px`, `p-15px`), sizing (`w-200px`, `h-50px`), colores de fondo y texto
con variantes 100–900 (`bg-blue-300`, `text-red`), tipografía (`fs-14px`, `fw-600`),
flex, borders, display, position y shadows.

[reference: helper/helper.md]
> Referencia completa de utility classes propias de Color Admin que amplían Bootstrap 5.

---

### 5. Create Page — página nueva de mantenimiento/listado (`.html` + `.ts`)

Estructura completa para una página nueva: carpetas, page coordinador con `ResponsiveComponent`,
sub-componente web (Panel + ngx-datatable, con o sin row-detail), sub-componente mobile
(Ionic cards colapsables), barrels, rutas, menú, i18n y accesibilidad WCAG 2.1 AA.

[reference: create-page/create-page.md]
> Guía paso a paso para crear una página nueva de mantenimiento/listado con arquitectura
> coordinador + sub-componentes web/mobile. Incluye anti-patrones a evitar.

---

## Reglas globales

1. **Nunca escribir CSS personalizado** si ya existe una clase en `vendor.min.css`.
2. **Page Options** se activa/desactiva en `constructor`/`ngOnDestroy` — nunca dejar estado activo al navegar.
3. **UI Elements y Form** aplican a archivos `.html`; **Page Options** aplica a archivos `.ts`.
4. **Helper** aplica a `.html` para cualquier clase de utilidad que no sea un componente concreto.
5. Para iconos: Bootstrap Icons (`bi bi-*`), FontAwesome 6 (`fas fa-*`), Solar Duotone (`solar:*-bold-duotone`), Simple Line (`icon-*`), Flags (`fi fi-*`).
6. **Create Page** aplica cuando se crea una página nueva de mantenimiento/listado — cubre tanto `.ts` como `.html`.
