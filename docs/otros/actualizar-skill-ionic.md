# Actualizar Skill Ionic

Guía para mantener y ampliar el skill `ionic` ubicado en `.agents/skills/ionic/`.

---

## Estructura del skill

```
.agents/skills/ionic/
├── SKILL.md                          ← Índice raíz (registra secciones)
├── layout/
│   ├── layout.md                     ← Índice de la sección layout
│   └── references/
│       ├── ion-header-footer.md
│       ├── ion-content.md
│       └── ion-grid.md
├── navigation/
│   ├── navigation.md
│   └── references/
│       ├── ion-tabs.md
│       ├── ion-menu.md
│       └── ion-breadcrumbs.md
├── ui-elements/
│   ├── ui-elements.md
│   └── references/
│       ├── ion-accordion.md
│       ├── ion-card.md
│       └── ion-list-item.md
├── overlays/
│   ├── overlays.md
│   └── references/
│       ├── ion-modal.md
│       ├── ion-alert.md
│       ├── ion-action-sheet.md
│       ├── ion-popover.md
│       └── ion-toast.md
└── forms/
    ├── forms.md
    └── references/
        ├── ion-button.md
        └── ion-input.md
```

---

## Qué cubre cada sección

| Sección | Componentes | Descripción |
|---|---|---|
| `layout` | ion-header, ion-toolbar, ion-title, ion-footer, ion-content, ion-grid, ion-row, ion-col | Esqueleto y estructura de páginas Ionic |
| `navigation` | ion-tabs, ion-tab-bar, ion-tab-button, ion-menu, ion-menu-button, ion-menu-toggle, ion-breadcrumbs | Navegación principal y secundaria |
| `ui-elements` | ion-accordion, ion-accordion-group, ion-card, ion-list, ion-item, ion-label | Elementos visuales de contenido |
| `overlays` | ion-modal, ion-alert, ion-action-sheet, ion-popover, ion-toast | Capas superpuestas e interacciones |
| `forms` | ion-button, ion-input | Campos de formulario y botones |

---

## Cómo agregar un nuevo componente a una sección existente

### 1. Crear el archivo de referencia

Crear en `references/` de la sección correspondiente:

```
.agents/skills/ionic/<seccion>/references/ion-nuevo-componente.md
```

**Estructura del archivo de referencia:**

```markdown
# Reference: ion-nuevo-componente

## Uso básico

\`\`\`html
<!-- Ejemplo real tomado de https://ionicframework.com/docs/api/nuevo-componente -->
<ion-nuevo-componente>
  Contenido
</ion-nuevo-componente>
\`\`\`

\`\`\`html
<!-- Segundo ejemplo con variante importante -->
<ion-nuevo-componente prop="valor">
  Otro patrón
</ion-nuevo-componente>
\`\`\`

## Component TS

\`\`\`typescript
import { IonNuevoComponente } from '@ionic/angular/standalone';

@Component({ imports: [IonNuevoComponente] })
export class MiPage {
  // uso del componente desde TypeScript
}
\`\`\`

## Notas

- Prop importante 1: descripción.
- Prop importante 2: descripción.
- **CSS vars**: `--background`, `--color`, etc.
- **Imports TS**: `IonNuevoComponente` | variante avanzada: `+ IonOtroComponente`
```

### 2. Registrar en el índice de sección

Abrir `.agents/skills/ionic/<seccion>/<seccion>.md` y agregar la referencia en la lista de componentes:

```markdown
## Componentes de esta sección

- [reference: references/ion-accordion.md]
- [reference: references/ion-card.md]
- [reference: references/ion-list-item.md]
- [reference: references/ion-nuevo-componente.md]   ← Agregar aquí
```

---

## Cómo agregar una nueva sección completa

### 1. Crear la carpeta y archivos

```
.agents/skills/ionic/
└── nueva-seccion/
    ├── nueva-seccion.md              ← Índice de la sección
    └── references/
        └── ion-primer-componente.md  ← Al menos un componente
```

**Estructura del índice de sección** (`nueva-seccion.md`):

```markdown
---
name: ionic-nueva-seccion
description: >
  Descripción breve de qué cubre esta sección. Qué componentes incluye
  y cuándo usarla.
applyTo: "**/*.html"
---

# ionic-nueva-seccion

Descripción de los componentes de esta sección.

## Componentes de esta sección

- [reference: references/ion-primer-componente.md]

## Reglas de [nombre-sección]

1. Regla importante 1.
2. Regla importante 2.
```

### 2. Registrar en el SKILL.md raíz

Abrir `.agents/skills/ionic/SKILL.md` y agregar la sección en `## Secciones disponibles`:

```markdown
## Secciones disponibles

- [reference: layout/layout.md]
- [reference: navigation/navigation.md]
- [reference: ui-elements/ui-elements.md]
- [reference: overlays/overlays.md]
- [reference: forms/forms.md]
- [reference: nueva-seccion/nueva-seccion.md]   ← Agregar aquí
```

---

## Fuentes de documentación

Toda la información de componentes debe venir de la documentación oficial:

- **API de componentes**: `https://ionicframework.com/docs/api/<nombre-componente>`
- **Guías de uso**: `https://ionicframework.com/docs/components`
- **Standalone imports**: `https://ionicframework.com/docs/angular/standalone`

Ejemplos de URLs de API:
- `https://ionicframework.com/docs/api/modal`
- `https://ionicframework.com/docs/api/input`
- `https://ionicframework.com/docs/api/select`
- `https://ionicframework.com/docs/api/toggle`
- `https://ionicframework.com/docs/api/segment`
- `https://ionicframework.com/docs/api/datetime`
- `https://ionicframework.com/docs/api/fab`
- `https://ionicframework.com/docs/api/infinite-scroll`
- `https://ionicframework.com/docs/api/refresher`

---

## Reglas de calidad para los ejemplos

1. **No inventar código**: todos los ejemplos HTML/TS deben provenir de la documentación oficial.
2. **Standalone imports**: siempre usar `import { IonX } from '@ionic/angular/standalone'` — nunca `IonicModule`.
3. **Registrar iconos**: si el ejemplo usa `IonIcon`, incluir `addIcons({...})` en el constructor.
4. **Accesibilidad**: incluir `aria-label` en botones de solo icono; `alt` en imágenes; `aria-hidden="true"` en iconos decorativos.
5. **Comentarios en español**: los comentarios de los ejemplos HTML van en español para consistencia con el proyecto.
6. **Notas completas**: incluir siempre propiedades clave, CSS vars más usadas e imports TS al final de cada referencia.

---

## Componentes candidatos para agregar

Componentes Ionic importantes aún no documentados en este skill:

| Componente | Sección sugerida | URL de docs |
|---|---|---|
| `ion-select` | `forms` | `/api/select` |
| `ion-toggle` | `forms` | `/api/toggle` |
| `ion-checkbox` | `forms` | `/api/checkbox` |
| `ion-radio` | `forms` | `/api/radio` |
| `ion-textarea` | `forms` | `/api/textarea` |
| `ion-datetime` | `forms` | `/api/datetime` |
| `ion-range` | `forms` | `/api/range` |
| `ion-searchbar` | `forms` | `/api/searchbar` |
| `ion-fab` | `ui-elements` | `/api/fab` |
| `ion-chip` | `ui-elements` | `/api/chip` |
| `ion-badge` | `ui-elements` | `/api/badge` |
| `ion-avatar` | `ui-elements` | `/api/avatar` |
| `ion-infinite-scroll` | `ui-elements` | `/api/infinite-scroll` |
| `ion-refresher` | `ui-elements` | `/api/refresher` |
| `ion-skeleton-text` | `ui-elements` | `/api/skeleton-text` |
| `ion-segment` | `navigation` | `/api/segment` |
| `ion-back-button` | `navigation` | `/api/back-button` |
| `ion-loading` | `overlays` | `/api/loading` |
| `ion-progress-bar` | `layout` | `/api/progress-bar` |
| `ion-spinner` | `layout` | `/api/spinner` |
