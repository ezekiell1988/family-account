---
name: family-account-endpoint
description: >
  Guía completa para crear una feature nueva en family-account: entidad EF Core,
  configuración Fluent API, DTOs, interface, service, module con endpoints Minimal API
  y registro en Program.cs. Usar SIEMPRE que se pida crear una tabla + sus endpoints
  CRUD en este proyecto.
---

# Crear Feature Completa — family-account

Patrón end-to-end usado en el proyecto: entidad → configuración → DbSet → DTOs → service → module → Program.cs.

## Estructura de Archivos

```
Domain/
  Entities/
    {Entidad}.cs

Infrastructure/
  Data/
    Configuration/
      {Entidad}Configuration.cs
    AppDbContext.cs                  ← agregar DbSet

Features/
  {Entidades}/                      ← plural, PascalCase
    Dtos/
      {Entidad}Response.cs
      Create{Entidad}Request.cs
      Update{Entidad}Request.cs
    I{Entidad}Service.cs
    {Entidad}Service.cs
    {Entidades}Module.cs

Program.cs                          ← AddXxxModule() + MapXxxEndpoints()
```

---

## 1. Entidad (`Domain/Entities/{Entidad}.cs`)

```csharp
namespace FamilyAccountApi.Domain.Entities;

public sealed class Product
{
    public int    IdProduct   { get; set; }           // PK: id{NombreEntidad}
    public string CodeProduct { get; set; } = null!;  // código único → índice UQ
    public string NameProduct { get; set; } = null!;

    // Relaciones (si las hay)
    public ICollection<ProductProductSKU> ProductProductSKUs { get; set; } = [];
}
```

**Reglas:**
- Clase `sealed`.
- PK: `id{NombreEntidad}` — entero (EF lo mapea a columna `id{NombreEntidad}`).
- Strings no nulables → `= null!;`  |  opcionales → `string?`
- Colecciones de navegación → `= [];`
- Sin lógica de negocio.

---

## 2. Configuración Fluent API (`Infrastructure/Data/Configuration/{Entidad}Configuration.cs`)

```csharp
using FamilyAccountApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FamilyAccountApi.Infrastructure.Data.Configuration;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // ── PK ──────────────────────────────────────────────
        builder.HasKey(p => p.IdProduct);
        builder.Property(p => p.IdProduct).ValueGeneratedOnAdd();

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(p => p.CodeProduct)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false);   // ASCII: códigos, emails, teléfonos

        builder.Property(p => p.NameProduct)
            .HasMaxLength(200)
            .IsRequired();       // sin IsUnicode → Unicode para nombres/textos

        // ── Campos opcionales ───────────────────────────────
        builder.Property(p => p.Description)
            .HasMaxLength(500);  // IsUnicode() por defecto

        // ── Índice único ─────────────────────────────────────
        builder.HasIndex(p => p.CodeProduct)
            .IsUnique()
            .HasDatabaseName("UQ_product_codeProduct");
            // patrón: UQ_{tabla}_{campo}

        // ── FK (si aplica) ───────────────────────────────────
        // builder.HasOne(p => p.Category)
        //     .WithMany(c => c.Products)
        //     .HasForeignKey(p => p.IdCategory)
        //     .OnDelete(DeleteBehavior.Cascade);
        //
        // Cambiar a ClientSetNull solo si SQL Server lanza
        // error de "multiple cascade paths" hacia la misma tabla.
    }
}
```

**Convenciones de nombres en BD** (la conversión camelCase es global en `AppDbContext`):

| Concepto | Patrón | Ejemplo |
|----------|--------|---------|
| Tabla | camelCase = nombre de clase | `Product` → `product` |
| Columna | camelCase = nombre de propiedad | `CodeProduct` → `codeProduct` |
| PK | `id{Entidad}` | `idProduct` |
| FK | `id{EntidadReferenciada}` | `idCategory` |
| Índice único | `UQ_{tabla}_{campo}` | `UQ_product_codeProduct` |
| Índice único compuesto | `UQ_{tabla}_{campo1}_{campo2}` | `UQ_productProductSKU_idProduct_idProductSKU` |

---

## 3. Registrar en AppDbContext

Agregar **una línea** en el bloque de DbSets de `Infrastructure/Data/AppDbContext.cs`:

```csharp
public DbSet<Product> Product => Set<Product>();
```

El `ApplyConfigurationsFromAssembly` en `OnModelCreating` registra la configuración automáticamente — no es necesario llamarla manualmente.

---

## 4. DTOs

### Response (`Dtos/{Entidad}Response.cs`)

```csharp
namespace FamilyAccountApi.Features.Products.Dtos;

public sealed record ProductResponse(
    int    IdProduct,
    string CodeProduct,
    string NameProduct);
```

### Create Request (`Dtos/Create{Entidad}Request.cs`)

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Products.Dtos;

public sealed record CreateProductRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Description("Código único del producto")]
    public required string CodeProduct { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre del producto")]
    public required string NameProduct { get; init; }

    // Campos opcionales:
    // [StringLength(500)]
    // public string? Description { get; init; }
}
```

### Update Request (`Dtos/Update{Entidad}Request.cs`)

Idéntico a `Create` — mismos campos y validaciones.

**Reglas de DTOs:**
- `sealed record` siempre.
- Request: propiedades con `required` + `{ get; init; }`.
- Validación con atributos `[Required]`, `[StringLength]`, `[EmailAddress]`, etc. — el middleware `.AddValidation()` los procesa automáticamente con código 400.
- `[Description]` para documentar en Scalar/OpenAPI.

---

## 5. Interface (`I{Entidad}Service.cs`)

```csharp
using FamilyAccountApi.Features.Products.Dtos;

namespace FamilyAccountApi.Features.Products;

public interface IProductService
{
    Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default);
    Task<ProductResponse?> GetByIdAsync(int idProduct, CancellationToken ct = default);
    Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default);
    Task<ProductResponse?> UpdateAsync(int idProduct, UpdateProductRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idProduct, CancellationToken ct = default);
}
```

---

## 6. Service (`{Entidad}Service.cs`)

```csharp
using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Products.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Products;

public sealed class ProductService(AppDbContext db) : IProductService
{
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.Product
            .AsNoTracking()
            .Select(p => new ProductResponse(p.IdProduct, p.CodeProduct, p.NameProduct))
            .ToListAsync(ct);
    }

    public async Task<ProductResponse?> GetByIdAsync(int idProduct, CancellationToken ct = default)
    {
        return await db.Product
            .AsNoTracking()
            .Where(p => p.IdProduct == idProduct)
            .Select(p => new ProductResponse(p.IdProduct, p.CodeProduct, p.NameProduct))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        var entity = new Product
        {
            CodeProduct = request.CodeProduct,
            NameProduct = request.NameProduct
        };

        db.Product.Add(entity);
        await db.SaveChangesAsync(ct);

        return new ProductResponse(entity.IdProduct, entity.CodeProduct, entity.NameProduct);
    }

    public async Task<ProductResponse?> UpdateAsync(int idProduct, UpdateProductRequest request, CancellationToken ct = default)
    {
        var entity = await db.Product.FindAsync([idProduct], ct);
        if (entity is null) return null;

        entity.CodeProduct = request.CodeProduct;
        entity.NameProduct = request.NameProduct;

        await db.SaveChangesAsync(ct);

        return new ProductResponse(entity.IdProduct, entity.CodeProduct, entity.NameProduct);
    }

    public async Task<bool> DeleteAsync(int idProduct, CancellationToken ct = default)
    {
        var deleted = await db.Product
            .Where(p => p.IdProduct == idProduct)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
```

**Reglas del service:**
- Constructor primario con `AppDbContext db` (DI por parámetro).
- `AsNoTracking()` en lecturas.
- `Select()` en proyección directa (evita cargar la entidad completa en lecturas simples).
- Si se necesitan navegaciones → usar `Include().ThenInclude()` en lugar de `Select()`.
- `FindAsync([id], ct)` para cargar por PK en escrituras (usa el caché de tracking de EF).
- `ExecuteDeleteAsync()` para deletes sin cargar la entidad.
- No atrapar `DbUpdateException` aquí — se atrapa en el Module.

---

## 7. Module con Endpoints (`{Entidades}Module.cs`)

```csharp
using FamilyAccountApi.Features.Products.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Products;

public static class ProductsModule
{
    public static IServiceCollection AddProductsModule(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products")
            .WithTags("Products")
            .RequireAuthorization();             // todos los endpoints requieren token

        group.MapGet("/", GetAll)
            .WithName("GetAllProducts")
            .WithSummary("Obtener todos los productos");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetProductById")
            .WithSummary("Obtener producto por ID");

        group.MapPost("/", Create)
            .WithName("CreateProduct")
            .WithSummary("Crear nuevo producto")
            .RequireAuthorization("Admin");      // Admin + Developer

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProduct")
            .WithSummary("Actualizar producto")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProduct")
            .WithSummary("Eliminar producto")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductResponse>>> GetAll(
        IProductService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetById(
        int id, IProductService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ProductResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateProductRequest request, IProductService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/products/{item.IdProduct}", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_product_codeProduct") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un producto con el código '{request.CodeProduct}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id, UpdateProductRequest request, IProductService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_product_codeProduct") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un producto con el código '{request.CodeProduct}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IProductService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
```

**Reglas del module:**
- Clase `static`.
- Método de registro: `Add{Entidades}Module` → `services.AddScoped<IXxx, Xxx>()`.
- Método de endpoints: `Map{Entidades}Endpoints` → retorna `IEndpointRouteBuilder`.
- Usar `TypedResults` (no `Results`) — necesario para que OpenAPI genere los tipos correctamente.
- `ValidationProblem` se incluye en la firma de Create/Update aunque el middleware lo maneja — mantiene el contrato OpenAPI visible.
- Atrapar `DbUpdateException` en Create y Update con `when` filtrando por el nombre del índice único (`UQ_{tabla}_{campo}`).
- Políticas de autorización del proyecto:
  - `RequireAuthorization()` sin política → roles: Developer, Admin, User
  - `RequireAuthorization("Admin")` → roles: Developer, Admin
  - `RequireAuthorization("Developer")` → solo Developer

---

## 8. Registrar en Program.cs

Dos líneas en `Program.cs`:

```csharp
// En el bloque de módulos de features (~línea 180):
builder.Services.AddProductsModule();

// En el bloque de endpoints (~línea 230):
v1.MapProductsEndpoints();
```

El `using` del namespace se agrega automáticamente o manualmente:
```csharp
using FamilyAccountApi.Features.Products;
```

---

## 9. Migración

```bash
dotnet ef migrations add Add{Entidad} \
  --project src/familyAccountApi \
  --output-dir Infrastructure/Data/Migrations

dotnet ef database update --project src/familyAccountApi
```

> Ver skill `family-account-ef-migrate` para resolución de errores comunes
> (`PendingModelChangesWarning`, tablas duplicadas, BD existente).

---

## Checklist Completo

- [ ] `Domain/Entities/{Entidad}.cs` — PK `id{Entidad}`, strings `= null!`
- [ ] `Infrastructure/Data/Configuration/{Entidad}Configuration.cs` — PK, campos, índices, FKs
- [ ] `AppDbContext.cs` — `DbSet<{Entidad}>`
- [ ] `Features/{Entidades}/Dtos/{Entidad}Response.cs` — sealed record positional
- [ ] `Features/{Entidades}/Dtos/Create{Entidad}Request.cs` — validaciones
- [ ] `Features/{Entidades}/Dtos/Update{Entidad}Request.cs` — validaciones
- [ ] `Features/{Entidades}/I{Entidad}Service.cs` — 5 métodos CRUD
- [ ] `Features/{Entidades}/`{Entidad}Service.cs` — implementación
- [ ] `Features/{Entidades}/{Entidades}Module.cs` — Add + Map, 5 endpoints
- [ ] `Program.cs` — `AddXxxModule()` + `MapXxxEndpoints()`
- [ ] Migración generada y aplicada
