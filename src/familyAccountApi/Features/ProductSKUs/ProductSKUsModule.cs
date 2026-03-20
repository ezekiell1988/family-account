using FamilyAccountApi.Features.ProductSKUs.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductSKUs;

public static class ProductSKUsModule
{
    public static IServiceCollection AddProductSKUsModule(this IServiceCollection services)
    {
        services.AddScoped<IProductSKUService, ProductSKUService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductSKUsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/product-skus")
            .WithTags("ProductSKUs")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllProductSKUs")
            .WithSummary("Obtener todos los productos SKU");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetProductSKUById")
            .WithSummary("Obtener producto SKU por ID");

        group.MapPost("/", Create)
            .WithName("CreateProductSKU")
            .WithSummary("Crear nuevo producto SKU")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProductSKU")
            .WithSummary("Actualizar producto SKU")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProductSKU")
            .WithSummary("Eliminar producto SKU")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductSKUResponse>>> GetAll(
        IProductSKUService service,
        CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ProductSKUResponse>, NotFound>> GetById(
        int id,
        IProductSKUService service,
        CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null
            ? TypedResults.Ok(item)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ProductSKUResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateProductSKURequest request,
        IProductSKUService service,
        CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/product-skus/{item.IdProductSKU}", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productSKU_codeProductSKU") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un producto SKU con el código '{request.CodeProductSKU}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<ProductSKUResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id,
        UpdateProductSKURequest request,
        IProductSKUService service,
        CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null
                ? TypedResults.Ok(item)
                : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productSKU_codeProductSKU") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un producto SKU con el código '{request.CodeProductSKU}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id,
        IProductSKUService service,
        CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
