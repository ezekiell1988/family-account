---
name: family-account-launch
description: >
  Guía para configurar el launch de family-account en VS Code con build previo.
  Usar SIEMPRE que se necesite compilar primero Angular y luego compilar o ejecutar
  el API desde launch.json o tasks.json, especialmente al trabajar con preLaunchTask,
  tareas compuestas y secuencia Angular -> API en este proyecto.
applyTo: ".vscode/**"
---

# Launch de family-account en VS Code

Este proyecto usa una secuencia de arranque en VS Code donde primero se compila el frontend Angular y luego se compila o ejecuta el API .NET.

---

## Objetivo

Cuando se ejecute el launch del API, VS Code debe correr antes una tarea compuesta que haga esto en orden:

1. Build de Angular en `src/familyAccountWeb`
2. Build del API en `src/familyAccountApi`
3. Ejecución del launch del backend

---

## Archivos a modificar

- `.vscode/tasks.json`
- `.vscode/launch.json`

---

## Patrón recomendado

### 1. Definir tarea de Angular

```json
{
  "label": "Angular: build dev",
  "type": "shell",
  "command": "npm run build:dev",
  "options": {
    "cwd": "${workspaceFolder}/src/familyAccountWeb"
  },
  "group": "build",
  "presentation": {
    "reveal": "always",
    "panel": "shared"
  },
  "problemMatcher": []
}
```

### 2. Definir tarea de build del API

```json
{
  "label": "API: build",
  "type": "shell",
  "command": "dotnet build",
  "options": {
    "cwd": "${workspaceFolder}/src/familyAccountApi"
  },
  "group": "build",
  "presentation": {
    "reveal": "always",
    "panel": "shared"
  },
  "problemMatcher": ["$msCompile"]
}
```

### 3. Crear tarea compuesta en secuencia

```json
{
  "label": "Launch: build prerequisites",
  "dependsOn": [
    "Angular: build dev",
    "API: build"
  ],
  "dependsOrder": "sequence",
  "problemMatcher": []
}
```

### 4. Referenciarla desde el launch

```json
{
  "name": "FamilyAccountApi — Launch",
  "type": "coreclr",
  "request": "launch",
  "preLaunchTask": "Launch: build prerequisites",
  "program": "dotnet",
  "args": [
    "${workspaceFolder}/src/familyAccountApi/bin/Debug/net10.0/FamilyAccountApi.dll"
  ],
  "cwd": "${workspaceFolder}/src/familyAccountApi",
  "stopAtEntry": false,
  "env": {
    "ASPNETCORE_ENVIRONMENT": "Development"
  }
}
```

---

## Reglas para este proyecto

- Mantener la secuencia Angular -> API usando `dependsOrder: "sequence"`.
- No reemplazar el launch por comandos inline si ya existe `launch.json` funcional.
- Reutilizar nombres claros de tareas: `Angular: build dev`, `API: build`, `Launch: build prerequisites`.
- El build de Angular corre en `src/familyAccountWeb`.
- El build del API corre en `src/familyAccountApi`.
- El `preLaunchTask` debe apuntar a la tarea compuesta, no directamente a `dotnet build`.

---

## Cuándo usar una versión más corta

Si el usuario pide explícitamente la menor cantidad posible de líneas, se puede condensar en una sola tarea shell. Aun así, en este repo se prefiere la versión separada porque:

- deja la secuencia explícita,
- permite reutilizar cada build por separado,
- facilita depuración desde VS Code.

---

## Validación esperada

Después de editar `.vscode/tasks.json` y `.vscode/launch.json`:

- verificar que ambos JSON queden válidos,
- ejecutar el launch del API,
- confirmar que Angular compila primero,
- confirmar que luego corre `dotnet build`,
- confirmar que finalmente levanta `FamilyAccountApi`.

---

## Evitar

- Duplicar tareas con nombres casi iguales.
- Poner `preLaunchTask` apuntando a una tarea inexistente.
- Ejecutar Angular y API en paralelo cuando el usuario pidió orden estricto.
- Cambiar la configuración del launch más allá de lo necesario.