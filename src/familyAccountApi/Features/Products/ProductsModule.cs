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

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllProducts")
            .WithSummary("Obtener todos los productos");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetProductById")
            .WithSummary("Obtener producto por ID");

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

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> Delete(
        int id,
        IProductService service,
        CancellationToken ct)
    {
        var (deleted, conflict) = await service.DeleteAsync(id, ct);
        if (conflict is not null)
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Conflicto de dependencias",
                Detail = conflict,
                Status = StatusCodes.Status409Conflict
            });
        return deleted
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
