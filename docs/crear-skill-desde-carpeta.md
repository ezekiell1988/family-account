# Cómo agregar una sección al skill `color-admin`

Este documento explica cómo ampliar el skill **`color-admin`** agregando una nueva sección
(sub-carpeta) con su entry point y sus references, siguiendo la misma estructura que ya tienen
`page-options`, `ui-elements` y `form`.

---

## Estructura del skill `color-admin`

El skill **no** es un único `SKILL.md` en raíz — es una carpeta que agrupa **secciones**,
cada una con su propio entry point y sus propios references:

```
.agents/skills/color-admin/
├── page-options/                  ← sección existente
│   ├── page-options.md            ← entry point de la sección
│   └── references/
│       ├── page-blank.md
│       ├── page-with-boxed-layout.md
│       └── ...
├── ui-elements/                   ← sección existente
│   ├── ui-elements.md
│   └── references/
│       ├── buttons.md
│       └── ...
├── form/                          ← sección existente
│   ├── form.md
│   └── references/
│       └── ...
└── {nueva-seccion}/               ← sección nueva a agregar
    ├── {nueva-seccion}.md         ← entry point de la sección
    └── references/
        ├── {pantalla-1}.md        ← un reference por cada pantalla/variante
        ├── {pantalla-2}.md
        └── {pantalla-N}.md
```

> **Regla de oro:** 1 carpeta de template = 1 sección. Dentro de cada sección, 1 pantalla/variante = 1 reference.

---

## Paso 1 — Decidir el nombre de la sección

El nombre debe ser en kebab-case y reflejar el dominio de la carpeta fuente:

| Carpeta fuente del template              | Sección a crear          |
|------------------------------------------|--------------------------|
| `color-admin/.../page-options/`          | `page-options`           |
| `color-admin/.../ui-elements/`           | `ui-elements`            |
| `color-admin/.../form-elements/`         | `form`                   |
| `color-admin/.../extra-components/`      | `extra-components`       |
| `color-admin/.../charts/`               | `charts`                 |

---

## Paso 2 — Explorar la carpeta fuente

Listar las subcarpetas — cada una se convertirá en un **reference** independiente:

```
color-admin/template_angularjs20/src/pages/page-options/
├── blank/
│   ├── blank.html
│   └── blank.ts
├── with-boxed-layout/
│   ├── with-boxed-layout.html
│   └── with-boxed-layout.ts
└── with-top-menu/
    ├── with-top-menu.html
    └── with-top-menu.ts
```

> **Regla:** 1 subcarpeta = 1 archivo en `references/` con el mismo nombre descriptivo.

---

## Paso 3 — Leer los archivos fuente

Para cada subcarpeta, leer principalmente el **`.html`** (y el `.ts` si hay lógica relevante).

Identificar en el HTML:
- ¿Qué **clases CSS** se usan?
- ¿Qué **estructura HTML** se repite?
- ¿Qué **AppSettings** se configuran en el `.ts`?
- ¿Qué **variantes** existen (colores, tamaños, estados)?

---

## Paso 4 — Crear los references

Cada archivo `{nueva-seccion}/references/{pantalla}.md` debe seguir esta estructura:

```markdown
# Reference: {Nombre legible de la pantalla/variante}

Descripción breve de para qué sirve esta variante.

## Uso básico

\`\`\`html
<!-- ejemplo mínimo funcional, copiado/adaptado del HTML fuente -->
\`\`\`

## Component TS (si aplica)

\`\`\`typescript
// configuración de AppSettings u otra lógica relevante
\`\`\`

## Notas

- Aclaración importante 1
- Qué NO necesita configurarse
```

### Reglas para los references

1. **No inventar clases** — solo documentar las que aparecen en el fuente o en la documentación oficial.
2. **No agregar CSS personalizado** — si el template ya tiene la clase, solo muestra cómo usarla.
3. **Ejemplos reales** del HTML fuente, no ejemplos genéricos.
4. **Nombre del archivo** = nombre descriptivo de la pantalla/variante (ej. `page-blank.md`, `page-with-boxed-layout.md`).
5. Si hay configuración TypeScript relevante (`AppSettings`, decoradores, etc.), incluirla en un bloque `Component TS`.

---

## Paso 5 — Crear el entry point de la sección

El archivo `{nueva-seccion}/{nueva-seccion}.md` es el **entry point** de la sección. Debe tener frontmatter YAML + lista de todos sus references:

```markdown
---
name: color-admin-{nueva-seccion}
description: >
  Descripción de una línea. Incluir:
  - Qué documenta esta sección
  - Cuándo usarla (palabras clave que disparan el skill)
  - Qué NO necesita escribirse (ej: "CSS ya compilado, solo clases")
applyTo: "**/*.html"          ← "**/*.ts" si la sección es de lógica TypeScript
---

# Skill: Color Admin — {Nombre legible de la sección}

## Propósito

Breve explicación del dominio.

**Disparar cuando:**
- Condición 1
- Condición 2

## Referencias

[reference: references/{pantalla-1}.md]
> Descripción de una línea

[reference: references/{pantalla-2}.md]
> ...

## Reglas de uso (opcional)

1. Regla específica de esta sección
```

### Campos del frontmatter

| Campo         | Obligatorio | Descripción                                                          |
|---------------|-------------|----------------------------------------------------------------------|
| `name`        | ✅           | `color-admin-{nombre-seccion}` — debe ser único entre todas las secciones |
| `description` | ✅           | Visible para el agente al decidir si usar la sección                |
| `applyTo`     | Opcional    | `**/*.html` para secciones de markup, `**/*.ts` para secciones de lógica |

---

## Paso 6 — Verificar la estructura

```
.agents/skills/color-admin/
└── {nueva-seccion}/
    ├── {nueva-seccion}.md        ← frontmatter + lista de todos los references
    └── references/
        ├── {pantalla-1}.md       ← un archivo por subcarpeta fuente
        ├── {pantalla-2}.md
        └── {pantalla-N}.md
```

Checklist:
- [ ] El `name` en el frontmatter sigue el patrón `color-admin-{nombre-seccion}`
- [ ] Cada subcarpeta fuente tiene su `reference` correspondiente en `references/`
- [ ] Cada reference tiene estructura: título + uso básico + component TS (si aplica) + notas
- [ ] El entry point lista todos sus references con `[reference: references/{nombre}.md]`
- [ ] La `description` en el frontmatter menciona los casos de uso clave

---

## Ejemplo real: sección `page-options`

### Carpeta fuente explorada
```
color-admin/template_angularjs20/src/pages/page-options/
├── blank/                      → references/page-blank.md
├── with-boxed-layout/          → references/page-with-boxed-layout.md
├── full-height/                → references/page-full-height.md
├── with-fixed-footer/          → references/page-with-fixed-footer.md
├── with-footer/                → references/page-with-footer.md
├── with-light-sidebar/         → references/page-with-light-sidebar.md
├── with-minified-sidebar/      → references/page-with-minified-sidebar.md
├── with-wide-sidebar/          → references/page-with-wide-sidebar.md
├── with-right-sidebar/         → references/page-with-right-sidebar.md
├── with-search-sidebar/        → references/page-with-search-sidebar.md
├── with-transparent-sidebar/   → references/page-with-transparent-sidebar.md
├── with-two-sidebar/           → references/page-with-two-sidebar.md
├── without-sidebar/            → references/page-without-sidebar.md
├── with-top-menu/              → references/page-with-top-menu.md
├── with-mixed-menu/            → references/page-with-mixed-menu.md
└── with-mega-menu/             → references/page-with-mega-menu.md
```

### Sección resultante
```
.agents/skills/color-admin/page-options/
├── page-options.md             ← entry point con frontmatter + lista de references
└── references/
    ├── page-blank.md
    ├── page-with-boxed-layout.md
    ├── page-full-height.md
    ├── page-with-fixed-footer.md
    ├── page-with-footer.md
    ├── page-with-light-sidebar.md
    ├── page-with-minified-sidebar.md
    ├── page-with-wide-sidebar.md
    ├── page-with-right-sidebar.md
    ├── page-with-search-sidebar.md
    ├── page-with-transparent-sidebar.md
    ├── page-with-two-sidebar.md
    ├── page-without-sidebar.md
    ├── page-with-top-menu.md
    ├── page-with-mixed-menu.md
    └── page-with-mega-menu.md
```

---

## Secciones activas y candidatas

| Sección                  | Estado      | Carpeta fuente del template          |
|--------------------------|-------------|--------------------------------------|
| `page-options`           | ✅ Activa   | `pages/page-options/`                |
| `ui-elements`            | ✅ Activa   | `pages/ui-elements/`                 |
| `form`                   | ✅ Activa   | `pages/form-elements/`               |
| `extra-components`       | Pendiente   | `pages/extra-component/`             |
| `charts`                 | Pendiente   | `pages/charts/`                      |
| `data-management`        | Pendiente   | `pages/data-management/`             |
| `email-templates`        | Pendiente   | `pages/email-template/`              |

