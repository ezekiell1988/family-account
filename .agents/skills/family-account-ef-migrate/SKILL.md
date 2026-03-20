---
name: family-account-ef-migrate
description: >
  Usar SIEMPRE que se ejecute o repare una migración EF Core en este proyecto (family-account).
  Cubre el flujo correcto de generación y aplicación, cómo resolver PendingModelChangesWarning,
  cómo resetear la BD en desarrollo y qué hacer cuando las tablas ya existen.
  Disparar en: crear migración nueva, aplicar database update, error de migración, resetear BD dev.
---

# Migraciones EF Core — family-account

## Flujo Correcto (primera vez o migración nueva)

```powershell
# 1. Generar la migración desde el modelo
dotnet ef migrations add <NombreMigracion> `
  --project src/familyAccountApi `
  --output-dir Infrastructure/Data/Migrations

# 2. Aplicar a la base de datos
dotnet ef database update --project src/familyAccountApi
```

> **Regla clave:** NUNCA editar manualmente `*.Designer.cs`
> ni `AppDbContextModelSnapshot.cs`. EF genera estos archivos desde las
> `IEntityTypeConfiguration<T>` y el estado actual del modelo. Si se editan
> a mano, el snapshot quedará desfasado y `database update` fallará.
>
> **Excepción controlada:** El archivo `*_<NombreMigracion>.cs` (el `Up`/`Down`)
> SÍ puede editarse manualmente cuando el snapshot fue borrado y `migrations add`
> regeneró todas las tablas en lugar de solo las nuevas. En ese caso recortar el
> `Up` y el `Down` para que contengan únicamente las tablas de esa migración
> (ver sección "Snapshot borrado → migración con tablas duplicadas" más abajo).

---

## Error: `PendingModelChangesWarning`

```
An error was generated for warning 'Microsoft.EntityFrameworkCore.Migrations.PendingModelChangesWarning':
The model for context 'AppDbContext' has pending changes. Add a new migration before updating the database.
```

**Causa:** El snapshot (`AppDbContextModelSnapshot.cs`) no coincide con el modelo
actual (entidades + configuraciones). Ocurre cuando se editan los archivos de
migración a mano o se agrega/modifica una entidad sin regenerar la migración.

**Solución — Dev (BD descartable):**

```powershell
# Paso 1: eliminar la migración defectuosa
dotnet ef migrations remove --project src/familyAccountApi --force

# Paso 2: si quedan archivos .cs huérfanos de la migración, borrarlos
Remove-Item "src/familyAccountApi/Infrastructure/Data/Migrations/<timestamp>_<Nombre>.cs"
Remove-Item "src/familyAccountApi/Infrastructure/Data/Migrations/<timestamp>_<Nombre>.Designer.cs"
Remove-Item "src/familyAccountApi/Infrastructure/Data/Migrations/AppDbContextModelSnapshot.cs"

# Paso 3: regenerar limpio
dotnet ef migrations add <NombreMigracion> `
  --project src/familyAccountApi `
  --output-dir Infrastructure/Data/Migrations

# Paso 4: aplicar
dotnet ef database update --project src/familyAccountApi
```

> `migrations remove` revierte el snapshot pero no siempre borra los .cs.
> Verificar con `Get-ChildItem src/familyAccountApi/Infrastructure/Data/Migrations`
> antes del paso 3.

---

## Error: `There is already an object named 'X' in the database`

```
Microsoft.Data.SqlClient.SqlException: There is already an object named 'user' in the database.
```

**Causa:** La BD ya tiene tablas de una migración anterior, pero `__EFMigrationsHistory`
no las tiene registradas (o el timestamp de la migración cambió).

**Solución — Dev (BD descartable):**

```powershell
# Borrar la BD y recrearla desde cero
dotnet ef database drop --project src/familyAccountApi --force
dotnet ef database update --project src/familyAccountApi
```

> Solo usar `database drop` en entornos de desarrollo. En producción o staging
> usar scripts de migración incremental o renombrar la migración problemática.

---

## Snapshot borrado → migración con tablas duplicadas

**Situación:** Se borró `AppDbContextModelSnapshot.cs` (o se usó `migrations remove --force`
con error de BD). Al ejecutar `migrations add <Nuevo>`, EF genera el nuevo archivo
incluyendo TODAS las tablas del modelo (no solo las nuevas), porque no tiene referencia
del estado anterior. Al aplicar, falla con `There is already an object named 'X'`.

**Síntoma:** El archivo `<timestamp>_<Nuevo>.cs` contiene `CreateTable` para tablas
que ya existían (ej. `role`, `user`, `userPin`).

**Solución:**

1. Verificar qué tablas YA existen en la BD (no deben estar en el `Up` de la nueva migración).
2. Editar manualmente `<timestamp>_<Nuevo>.cs`:
   - En `Up`: dejar **únicamente** los `CreateTable`, `InsertData` y `CreateIndex`
     correspondientes a las tablas **nuevas**.
   - En `Down`: dejar únicamente el `DropTable` de esas mismas tablas nuevas.
3. Aplicar:

```powershell
dotnet ef database update --project src/familyAccountApi
```

> El `*.Designer.cs` y el `AppDbContextModelSnapshot.cs` NO se tocan — los generó EF
> correctamente con el estado completo del modelo; el problema era solo el `Up`/`Down`.

---

## Verificar estado de la migración

```powershell
# Ver migraciones pendientes y aplicadas
dotnet ef migrations list --project src/familyAccountApi

# Ver los archivos generados
Get-ChildItem "src/familyAccountApi/Infrastructure/Data/Migrations"
```

---

## Advertencia de versión de herramientas (no es error)

```
The Entity Framework tools version '10.0.2' is older than that of the runtime '10.0.5'.
Update the tools for the latest features and bug fixes.
```

Esta advertencia es informativa y no bloquea la ejecución. Para eliminarla:

```powershell
dotnet tool update --global dotnet-ef
```

---

## Checklist antes de `database update`

- [ ] El proyecto compila sin errores (`dotnet build`)
- [ ] Los archivos de migración fueron generados por EF (no editados a mano)
- [ ] `AppDbContextModelSnapshot.cs` existe y fue generado en el mismo paso que la migración
- [ ] Si es dev y la BD tiene datos viejos → `database drop --force` antes de `database update`

---

## Resumen de comandos

| Comando | Cuándo usarlo |
|---------|--------------|
| `migrations add <Nombre>` | Crear nueva migración desde el modelo actual |
| `migrations remove --force` | Deshacer la última migración (revierte snapshot) |
| `database update` | Aplicar migraciones pendientes a la BD |
| `database drop --force` | Borrar BD completa (solo dev) |
| `migrations list` | Ver estado de migraciones aplicadas/pendientes |
