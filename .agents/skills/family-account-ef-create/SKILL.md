---
name: family-account-ef-create
description: >
  Usar SIEMPRE que se cree una nueva entidad EF Core en este proyecto (family-account).
  Cubre convenciones de nombres (camelCase en BD, PK id{Entidad} entero autoincremental),
  estructura de archivos, configuración Fluent API y registro en AppDbContext.
  Disparar en: creación de tabla nueva, agregar entidad de dominio, configurar FK/índices.
---

# Creación de Entidades EF Core — family-account

## Convenciones del Proyecto

| Concepto | Regla | Ejemplo |
|----------|-------|---------|
| **Tabla en BD** | camelCase, igual al nombre de clase | clase `Contact` → tabla `contact` |
| **Columna en BD** | camelCase, igual al nombre de propiedad | prop `FullName` → columna `fullName` |
| **PK** | `id{NombreEntidad}` entero autoincremental | `idContact` |
| **FK** | `id{EntidadReferenciada}` | `idContactType` |
| **Índice único** | `UQ_{tabla}_{campo}` | `UQ_contact_codeContact` |
| **Índice único compuesto** | `UQ_{tabla}_{campo1}_{campo2}` | `UQ_userRole_idUser_idRole` |
| **DeleteBehavior FK** | `Cascade` siempre que no haya rutas en cascada múltiples hacia la misma tabla | ver sección FKs |
| **Namespace entidad** | `FamilyAccountApi.Domain.Entities` | — |
| **Namespace config** | `FamilyAccountApi.Infrastructure.Data.Configuration` | — |

> La conversión a camelCase en BD es **global**: `AppDbContext.OnModelCreating` aplica
> `ToCamelCase()` a todas las tablas y columnas automáticamente.
> No es necesario llamar a `.ToTable()` ni `.HasColumnName()` salvo casos especiales.

---

## Estructura de Archivos

```
Domain/
  Entities/
    {Entidad}.cs                          ← clase POCO
Infrastructure/
  Data/
    Configuration/
      {Entidad}Configuration.cs           ← IEntityTypeConfiguration<T>
    AppDbContext.cs                        ← agregar DbSet<{Entidad}>
```

---

## 1. Clase Entidad (`Domain/Entities/{Entidad}.cs`)

```csharp
namespace FamilyAccountApi.Domain.Entities;

public sealed class Contact
{
    // PK: id{NombreEntidad} — entero autoincremental
    public int IdContact { get; set; }

    // Discriminador de tipo: 'C' = Cliente, 'S' = Proveedor, 'B' = Ambos
    public string TypeContact { get; set; } = null!;

    public string CodeContact { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;

    // Navegaciones (agregar según relaciones)
    // public ICollection<Order> Orders { get; set; } = [];
}
```

**Reglas de la clase:**
- `sealed` por defecto.
- Strings no nulables → `= null!;` (EF los inicializa).
- Strings opcionales → `string?`.
- Colecciones de navegación → `= [];` (C# 12 collection expression).
- Sin lógica de negocio en la entidad (domain model anémico para este proyecto).

---

## 2. Configuración Fluent API (`Infrastructure/Data/Configuration/{Entidad}Configuration.cs`)

```csharp
using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        // ── PK ──────────────────────────────────────────────
        builder.HasKey(c => c.IdContact);
        builder.Property(c => c.IdContact).ValueGeneratedOnAdd();

        // ── Discriminador ───────────────────────────────────
        builder.Property(c => c.TypeContact)
            .HasMaxLength(1)
            .IsRequired()
            .IsUnicode(false);

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(c => c.CodeContact)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false);

        builder.Property(c => c.FullName)
            .HasMaxLength(200)
            .IsRequired()
            .IsUnicode();

        // ── Campos opcionales ───────────────────────────────
        builder.Property(c => c.Phone)
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.Property(c => c.Email)
            .HasMaxLength(200)
            .IsUnicode(false);

        // ── Índices ─────────────────────────────────────────
        // Código único por empresa (ajustar si hay partición por otro campo)
        builder.HasIndex(c => c.CodeContact)
            .IsUnique()
            .HasDatabaseName("UQ_contact_codeContact");

        // ── Seed data (opcional) ────────────────────────────
        // builder.HasData(new Contact { IdContact = 1, ... });
    }
}
```

**Reglas de configuración:**
- Siempre `sealed`.
- Implementar `IEntityTypeConfiguration<T>` (no configurar en `OnModelCreating`).
- `ValueGeneratedOnAdd()` en la PK para que EF sepa que es autoincremental.
- Usar `.IsUnicode(false)` para campos ASCII (códigos, emails, teléfonos).
- Usar `.IsUnicode()` (sin argumento = `true`) para nombres y textos libres.
- Nombrar índices explícitamente con `HasDatabaseName("UQ_{tabla}_{campo}")`.
- Índice único compuesto en tablas de unión (many-to-many): `.HasIndex(x => new { x.IdA, x.IdB }).IsUnique().HasDatabaseName("UQ_{tabla}_idA_idB")`.

---

## Reglas de FKs y DeleteBehavior

| Situación | DeleteBehavior | Notas |
|-----------|---------------|-------|
| FK estándar sin ciclos | `Cascade` | Preferido — borrar el padre elimina los hijos |
| Múltiple ruta de cascada hacia la misma tabla en SQL Server | `ClientSetNull` o `Restrict` | SQL Server lanza error si hay dos paths de Cascade hacia una misma tabla |

**Regla práctica:** Usar `DeleteBehavior.Cascade` siempre. Solo cambiar a `ClientSetNull` si EF o SQL Server lanzan error de  *"multiple cascade paths"* al generar la migración.

```csharp
// Tabla de unión — ambas FKs con Cascade (patrón join table)
builder.HasOne(ur => ur.User)
    .WithMany(u => u.UserRoles)
    .HasForeignKey(ur => ur.IdUser)
    .OnDelete(DeleteBehavior.Cascade);

builder.HasOne(ur => ur.Role)
    .WithMany(r => r.UserRoles)
    .HasForeignKey(ur => ur.IdRole)
    .OnDelete(DeleteBehavior.Cascade);

// Índice único compuesto para evitar duplicados en la unión
builder.HasIndex(ur => new { ur.IdUser, ur.IdRole })
    .IsUnique()
    .HasDatabaseName("UQ_userRole_idUser_idRole");
```

---

## 3. Registro en AppDbContext (`Infrastructure/Data/AppDbContext.cs`)

Agregar una línea `DbSet`:

```csharp
public DbSet<Contact> Contact => Set<Contact>();
```

El archivo completo queda así:

```csharp
using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Infrastructure.Data.Configuration;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>    User    => Set<User>();
    public DbSet<UserPin> UserPin => Set<UserPin>();
    public DbSet<Contact> Contact => Set<Contact>();   // ← nueva línea

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Convención global camelCase → NO tocar aquí
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.GetTableName() is { } tableName)
                entityType.SetTableName(ToCamelCase(tableName));

            foreach (var property in entityType.GetProperties())
            {
                var colName = property.GetColumnName();
                if (!string.IsNullOrEmpty(colName))
                    property.SetColumnName(ToCamelCase(colName));
            }
        }
    }

    private static string ToCamelCase(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s[1..];
}
```

---

## 4. Migración

```powershell
# Desde la raíz del proyecto API
dotnet ef migrations add Add{Entidad} `
  --project src/familyAccountApi `
  --output-dir Infrastructure/Data/Migrations

# Aplicar a la base de datos
dotnet ef database update `
  --project src/familyAccountApi
```

Nombre del migration: `Add{Entidad}` — PascalCase, sin fecha manual (EF agrega timestamp).

---

## Checklist Completo

- [ ] Clase entidad en `Domain/Entities/{Entidad}.cs`
- [ ] PK nombrada `id{Entidad}` con `ValueGeneratedOnAdd()`
- [ ] `{Entidad}Configuration` en `Infrastructure/Data/Configuration/`
- [ ] Índices nombrados con `UQ_{tabla}_{campo}`
- [ ] `DbSet<{Entidad}>` registrado en `AppDbContext`
- [ ] Migration generada y aplicada

---

## Ejemplo Completo — Tabla Unificada Contactos (Clientes + Proveedores)

Patrón **Single Table para terceros** usando un campo discriminador `typeContact`:

| typeContact | Significado |
|-------------|-------------|
| `C` | Cliente (Customer) |
| `S` | Proveedor (Supplier) |
| `B` | Ambos |

Este diseño evita duplicar personas/empresas que son a la vez clientes y proveedores,
y permite filtrar fácilmente: `WHERE typeContact IN ('C','B')` para clientes.
