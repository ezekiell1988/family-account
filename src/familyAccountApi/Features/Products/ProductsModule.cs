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
            .RequireAuthorization();

        // ── CRUD base ────────────────────────────────────────
        group.MapGet(".json", GetAll)
            .WithName("GetAllProducts")
            .WithSummary("Obtener todos los productos");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetProductById")
            .WithSummary("Obtener producto por ID (incluye SKUs asociados)");

        group.MapPost("/", Create)
            .WithName("CreateProduct")
            .WithSummary("Crear nuevo producto")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProduct")
            .WithSummary("Actualizar producto")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProduct")
            .WithSummary("Eliminar producto")
            .RequireAuthorization("Admin");

        // ── Gestión de SKUs asociados ─────────────────────────
        group.MapPost("/{id:int}/skus/{idSku:int}", AddSKU)
            .WithName("AddSKUToProduct")
            .WithSummary("Asociar un ProductSKU a un producto")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}/skus/{idSku:int}", RemoveSKU)
            .WithName("RemoveSKUFromProduct")
            .WithSummary("Desasociar un ProductSKU de un producto")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductResponse>>> GetAll(
        IProductService service,
        CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ProductResponse>, NotFound>> GetById(
        int id,
        IProductService service,
        CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null
            ? TypedResults.Ok(item)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ProductResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateProductRequest request,
        IProductService service,
        CancellationToken ct)
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
        int id,
        UpdateProductRequest request,
        IProductService service,
        CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null
                ? TypedResults.Ok(item)
                : TypedResults.NotFound();
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
        int id,
        IProductService service,
        CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> AddSKU(
        int id,
        int idSku,
        IProductService service,
        CancellationToken ct)
    {
        var ok = await service.AddSKUAsync(id, idSku, ct);
        return ok
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> RemoveSKU(
        int id,
        int idSku,
        IProductService service,
        CancellationToken ct)
    {
        var ok = await service.RemoveSKUAsync(id, idSku, ct);
        return ok
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
