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

Este skill cubre dos escenarios:
- **Entidad simple CRUD**: una tabla principal con endpoints CRUD directos.
- **Agregado cabecera/detalle o tabla intermedia**: una tabla principal con hijos (ej. `accountingEntry` + `accountingEntryLine`) o una tabla de relación explícita (ej. `productProductCategory`).

Cuando la feature tiene hijos, validaciones entre filas o reglas contables, **no seguir el ejemplo simple de manera literal**: adaptar DTOs, service, module y migración según las reglas de este mismo skill.

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

### Estructura cuando la feature es cabecera/detalle

```text
Domain/
    Entities/
        {Cabecera}.cs
        {Detalle}.cs

Infrastructure/
    Data/
        Configuration/
            {Cabecera}Configuration.cs
            {Detalle}Configuration.cs
        AppDbContext.cs

Features/
    {Cabeceras}/
        Dtos/
            {Cabecera}Response.cs
            {Detalle}Response.cs
            {Detalle}Request.cs
            Create{Cabecera}Request.cs
            Update{Cabecera}Request.cs
        I{Cabecera}Service.cs
        {Cabecera}Service.cs
        {Cabeceras}Module.cs
```

### Estructura cuando la feature es tabla intermedia explícita

Usar una entidad propia cuando la relación necesita columnas adicionales, comentarios, índices únicos o endpoints específicos.

Ejemplo:
- `ProductProductCategory`
- `ContactContactType`
- `AccountingEntryLine` si se desea tratarla como tabla hija explícita del asiento

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
- Las navegaciones inversas son **opcionales**. Solo agregarlas si realmente aportan claridad o facilitan la consulta.
- Si la relación es cabecera/detalle, modelar la FK real en la tabla hija (`Id{Cabecera}`) y usar colección en la cabecera solo si conviene.
- Si la relación requiere una tabla explícita, crear una entidad propia; no depender de many-to-many implícito.
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
        // ── Comentario de tabla ──────────────────────────────
        builder.ToTable(t => t.HasComment("Catálogo de productos disponibles para venta o uso interno"));
        // Si la tabla también tiene check constraint, combinar en el mismo lambda:
        // builder.ToTable(t =>
        // {
        //     t.HasComment("...");
        //     t.HasCheckConstraint("CK_product_typeProduct", "typeProduct IN ('A', 'B')");
        // });

        // ── PK ──────────────────────────────────────────────
        builder.HasKey(p => p.IdProduct);
        builder.Property(p => p.IdProduct)
            .ValueGeneratedOnAdd()
            .HasComment("Identificador único del producto");

        // ── Campos obligatorios ─────────────────────────────
        builder.Property(p => p.CodeProduct)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(false)   // ASCII: códigos, emails, teléfonos
            .HasComment("Código único del producto (p. ej. PROD-001)");

        builder.Property(p => p.NameProduct)
            .HasMaxLength(200)
            .IsRequired()       // sin IsUnicode → Unicode para nombres/textos
            .HasComment("Nombre descriptivo del producto");

        // ── Campos opcionales ───────────────────────────────
        builder.Property(p => p.Description)
            .HasMaxLength(500)  // IsUnicode() por defecto
            .HasComment("Descripción detallada del producto");

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

**Reglas de `HasComment()` — OBLIGATORIO en TODAS las configuraciones:**
- **Tabla**: `builder.ToTable(t => t.HasComment("Descripción de la tabla"))`.  
  Si la tabla tiene check constraint, combinar ambas en el mismo lambda:
  ```csharp
  builder.ToTable(t =>
  {
      t.HasComment("...");
      t.HasCheckConstraint("CK_tabla_campo", "campo IN ('A', 'B')");
  });
  ```
- **Columnas**: encadenar `.HasComment("Descripción del campo")` al final de cada `Property()`.
- Los comentarios se guardan en SQL Server como `sp_addextendedproperty` — permiten documentar el esquema directamente en la BD.
- Ser descriptivo: mencionar propósito, rango de valores o ejemplos cuando sea útil.

---

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

Para **entidades simples**, puede ser idéntico a `Create`.

Para **cabecera/detalle**, no asumir que es igual:
- puede requerir una colección `Lines` o `Details`
- puede requerir reemplazar por completo las líneas hijas
- puede prohibir edición según estado (`Publicado`, `Anulado`, etc.)
- puede requerir validaciones adicionales entre filas

**Reglas de DTOs:**
- `sealed record` siempre.
- Request: propiedades con `required` + `{ get; init; }`.
- Validación con atributos `[Required]`, `[StringLength]`, `[EmailAddress]`, etc. — el middleware `.AddValidation()` los procesa automáticamente con código 400.
- `[Description]` para documentar en Scalar/OpenAPI.
- Al usar `[Range]` sobre `decimal`, **siempre** incluir `ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true`; sin ellos el servidor intenta parsear los límites con la cultura del sistema (que usa coma decimal) y falla con 400 aunque el valor enviado sea válido:
  ```csharp
  [Range(typeof(decimal), "0", "999999999999.99",
      ParseLimitsInInvariantCulture = true,
      ConvertValueInInvariantCulture = true)]
  public required decimal SubTotalAmount { get; init; }
  ```
- Cuando existe detalle hijo, crear DTO separado para la línea: `AccountingEntryLineRequest`, `AccountingEntryLineResponse`, etc.
- Si una regla depende de más de una fila, validarla también en el service y, si es crítica, reforzarla en BD.

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
- Si hay hijos o navegaciones, **preferir seguir proyectando con `Select()` a DTO**. Usar `Include().ThenInclude()` solo cuando realmente necesites materializar entidades completas.
- `FindAsync([id], ct)` para cargar por PK en escrituras (usa el caché de tracking de EF).
- `ExecuteDeleteAsync()` para deletes sin cargar la entidad.
- No atrapar `DbUpdateException` aquí — se atrapa en el Module.
- Para cabecera/detalle, usar una sola unidad de trabajo para guardar cabecera e hijos en la misma transacción implícita de `SaveChangesAsync()`.
- Para updates de cabecera/detalle, normalmente: cargar cabecera + hijos, validar estado, eliminar/reemplazar o sincronizar líneas, luego `SaveChangesAsync()`.
- Reglas de negocio críticas como “la suma del débito debe ser igual a la suma del crédito” deben validarse aquí **y también en la BD si no se puede confiar solo en la API**.

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

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllProducts")
            .WithSummary("Obtener todos los productos");

        group.MapGet("/{id:int}.json", GetById)
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
            return TypedResults.Created($"/api/v1/products/{item.IdProduct}.json", item);
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
- Para agregados complejos, el module no está limitado a 5 endpoints. Se pueden agregar endpoints adicionales como `GetByYear`, `PostLine`, `ClosePeriod`, `PublishEntry`, etc.
- **Rutas GET terminan en `.json`** — para habilitar caché de Cloudflare en el futuro:
  - Lista raíz: `MapGet(".json", ...)` (ya que el grupo es `/productos`, la URL final es `/api/v1/productos.json`)
  - Por ID: `MapGet("/{id:int}.json", ...)`
  - Rutas especiales: `MapGet("/year/{year:int}.json", ...)`, `MapGet("/currency/{id:int}.json", ...)`, etc.
  - La URL en `TypedResults.Created(...)` también debe llevar `.json`: `$"/api/v1/products/{item.IdProduct}.json"`
  - **Excepción**: ASP.NET no permite litreales después de parámetros opcionales (`{x?}.json` lanza ASP0017). En ese caso, declarar dos rutas explícitas: una sin parámetro y otra con parámetro requerido.
  - `POST`, `PUT`, `DELETE` **no** llevan `.json` — Cloudflare solo cachea GET.
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

### Cuando la regla NO puede expresarse con check constraint

Si la regla depende de múltiples filas o del agregado completo, **crear la migración primero y luego editarla manualmente**.

Ejemplo típico:
- `SUM(debitAmount) = SUM(creditAmount)` por asiento contable

En ese caso:
1. Generar la migración con `dotnet ef migrations add ...`
2. Editar el archivo de migración recién generado
3. Agregar `migrationBuilder.Sql(...)` en `Up()` para crear el trigger
4. Agregar `migrationBuilder.Sql(...)` en `Down()` para eliminar el trigger
5. Luego ejecutar `dotnet ef database update`

Plantilla base:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ... tablas e índices generados por EF

    migrationBuilder.Sql(
        """
        CREATE TRIGGER TR_accountingEntryLine_ValidateBalance
        ON accountingEntryLine
        AFTER INSERT, UPDATE, DELETE
        AS
        BEGIN
            SET NOCOUNT ON;

            IF EXISTS
            (
                SELECT 1
                FROM accountingEntryLine ael
                WHERE ael.idAccountingEntry IN
                (
                    SELECT idAccountingEntry FROM inserted
                    UNION
                    SELECT idAccountingEntry FROM deleted
                )
                GROUP BY ael.idAccountingEntry
                HAVING SUM(ael.debitAmount) <> SUM(ael.creditAmount)
            )
            BEGIN
                THROW 50001, 'El asiento contable está desbalanceado.', 1;
            END
        END
        """);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        """
        IF OBJECT_ID('TR_accountingEntryLine_ValidateBalance', 'TR') IS NOT NULL
            DROP TRIGGER TR_accountingEntryLine_ValidateBalance;
        """);

    // ... resto del down generado por EF
}
```

**Importante:**
- Un `CHECK CONSTRAINT` no puede resolver correctamente reglas agregadas entre varias filas.
- Para integridad real de contabilidad, la validación debe vivir también en la BD.
- Si el trigger bloquea inserts parciales de líneas, considerar insertar primero todas las líneas del asiento en la misma operación o crear la cabecera y sus detalles dentro de la misma transacción desde la API.

---

## Checklist Completo

- [ ] `Domain/Entities/{Entidad}.cs` — PK `id{Entidad}`, strings `= null!`
- [ ] `Infrastructure/Data/Configuration/{Entidad}Configuration.cs` — PK, campos, índices, FKs, **HasComment() en tabla y TODAS las columnas**
- [ ] `AppDbContext.cs` — `DbSet<{Entidad}>`
- [ ] `Features/{Entidades}/Dtos/{Entidad}Response.cs` — sealed record positional
- [ ] `Features/{Entidades}/Dtos/Create{Entidad}Request.cs` — validaciones; `[Range]` de decimal con `ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true`
- [ ] `Features/{Entidades}/Dtos/Update{Entidad}Request.cs` — validaciones; `[Range]` de decimal con `ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true`
- [ ] Si hay detalle: DTOs hijos `LineRequest` / `LineResponse`
- [ ] `Features/{Entidades}/I{Entidad}Service.cs` — 5 métodos CRUD
- [ ] `Features/{Entidades}/`{Entidad}Service.cs` — implementación
- [ ] `Features/{Entidades}/{Entidades}Module.cs` — Add + Map, endpoints necesarios para la feature
- [ ] `Program.cs` — `AddXxxModule()` + `MapXxxEndpoints()`
- [ ] Si hay regla agregada crítica: trigger o SQL manual agregado en la migración
- [ ] Migración generada y aplicada
