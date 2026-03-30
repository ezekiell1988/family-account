---
name: dotnet-ef-re-create
description: >
  Usar cuando se necesite eliminar completamente la base de datos, borrar todas las migraciones
  existentes y regenerar desde cero con una sola migración limpia (InitialCreate).
  Aplica a cualquier proyecto .NET con EF Core — no asume nombre de BD ni rutas específicas.
  Disparar en: resetear BD dev desde cero, consolidar migraciones acumuladas, tablas duplicadas
  por snapshots corruptos, o limpiar historial de migraciones antes de un release.
---

# Regenerar BD y Migraciones EF Core desde Cero

## Cuándo usar este flujo

- La BD de desarrollo tiene datos viejos o tablas duplicadas y se quiere empezar limpio.
- Se acumularon demasiadas migraciones y se quiere consolidar en una sola.
- El snapshot (`*ModelSnapshot.cs`) está desfasado y la BD es descartable.

> **Solo para entornos de desarrollo.** En staging/producción usar migraciones
> incrementales, nunca `database drop`.

---

## Paso 1 — Eliminar la base de datos

```powershell
dotnet ef database drop --project <rutaProyecto> --force
```

EF lee el connection string del proyecto para saber a qué BD conectarse.
No es necesario hardcodear el nombre de la BD.

**Ejemplo:**
```powershell
dotnet ef database drop --project src/MiApi --force
```

Si el comando falla con `Unable to retrieve project metadata`, asegurarse de
estar en la raíz del workspace (donde está la solución `.sln`):
```powershell
Set-Location <raizWorkspace>
dotnet ef database drop --project src/MiApi --force
```

---

## Paso 2 — Borrar todos los archivos de migración

```powershell
Remove-Item "<rutaProyecto>/Infrastructure/Data/Migrations/*" -Force
```

Esto elimina:
- `*_NombreMigracion.cs`
- `*_NombreMigracion.Designer.cs`
- `*ModelSnapshot.cs`

Verificar que la carpeta quede vacía:
```powershell
Get-ChildItem "<rutaProyecto>/Infrastructure/Data/Migrations"
# Sin output = OK
```

> Adaptar la ruta `Infrastructure/Data/Migrations` si el proyecto usa una
> ubicación distinta para las migraciones.

---

## Paso 3 — Regenerar la migración inicial

```powershell
dotnet ef migrations add InitialCreate `
  --project <rutaProyecto> `
  --output-dir Infrastructure/Data/Migrations
```

EF leerá todas las `IEntityTypeConfiguration<T>` del ensamblado y generará:
- `<timestamp>_InitialCreate.cs` — contiene `Up` y `Down` con todas las tablas
- `<timestamp>_InitialCreate.Designer.cs` — snapshot interno
- `<DbContext>ModelSnapshot.cs` — estado completo del modelo

> **Nunca editar** `*.Designer.cs` ni `*ModelSnapshot.cs` manualmente.

---

## Paso 4 — Aplicar la migración a la BD

```powershell
dotnet ef database update --project <rutaProyecto>
```

Esto crea la BD (si no existe) y aplica `InitialCreate`.
Verificar en el output que dice `Applying migration '<timestamp>_InitialCreate'.` y termina con `Done.`

---

## Flujo completo en un bloque

```powershell
# Desde la raíz del workspace
dotnet ef database drop --project src/MiApi --force
Remove-Item "src/MiApi/Infrastructure/Data/Migrations/*" -Force
dotnet ef migrations add InitialCreate --project src/MiApi --output-dir Infrastructure/Data/Migrations
dotnet ef database update --project src/MiApi
```

---

## Verificar el resultado

```powershell
# Ver migraciones registradas en la BD
dotnet ef migrations list --project src/MiApi

# Ver archivos generados
Get-ChildItem "src/MiApi/Infrastructure/Data/Migrations"
```

Resultado esperado: una sola migración `InitialCreate` con estado `[applied]`.

---

## Advertencia de versión de herramientas (no es error)

```
The Entity Framework tools version 'X.X.X' is older than that of the runtime 'X.X.X'.
```

Es informativa. Actualizar cuando sea conveniente:
```powershell
dotnet tool update --global dotnet-ef
```
