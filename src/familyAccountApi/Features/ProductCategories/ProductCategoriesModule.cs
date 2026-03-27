using FamilyAccountApi.Features.ProductCategories.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.ProductCategories;

public static class ProductCategoriesModule
{
    public static IServiceCollection AddProductCategoriesModule(this IServiceCollection services)
    {
        services.AddScoped<IProductCategoryService, ProductCategoryService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductCategoriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/product-categories")
            .WithTags("ProductCategories")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllProductCategories")
            .WithSummary("Obtener todas las categorías de producto");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetProductCategoryById")
            .WithSummary("Obtener categoría por ID");

        group.MapPost("/", Create)
            .WithName("CreateProductCategory")
            .WithSummary("Crear nueva categoría de producto")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProductCategory")
            .WithSummary("Actualizar categoría de producto")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProductCategory")
            .WithSummary("Eliminar categoría de producto")
            .RequireAuthorization("Admin");

        // ── Asociación product ↔ category ────────────────────
        group.MapPost("/{id:int}/products/{idProduct:int}", AddToProduct)
            .WithName("AddCategoryToProduct")
            .WithSummary("Asociar categoría a un producto")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}/products/{idProduct:int}", RemoveFromProduct)
            .WithName("RemoveCategoryFromProduct")
            .WithSummary("Desasociar categoría de un producto")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductCategoryResponse>>> GetAll(
        IProductCategoryService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ProductCategoryResponse>, NotFound>> GetById(
        int id, IProductCategoryService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ProductCategoryResponse>, ValidationProblem>> Create(
        CreateProductCategoryRequest request, IProductCategoryService service, CancellationToken ct)
    {
        var item = await service.CreateAsync(request, ct);
        return TypedResults.Created($"/api/v1/product-categories/{item.IdProductCategory}.json", item);
    }

    private static async Task<Results<Ok<ProductCategoryResponse>, NotFound>> Update(
        int id, UpdateProductCategoryRequest request, IProductCategoryService service, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, request, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IProductCategoryService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> AddToProduct(
        int id, int idProduct, IProductCategoryService service, CancellationToken ct)
    {
        var ok = await service.AddToProductAsync(idProduct, id, ct);
        return ok ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> RemoveFromProduct(
        int id, int idProduct, IProductCategoryService service, CancellationToken ct)
    {
        var ok = await service.RemoveFromProductAsync(idProduct, id, ct);
        return ok ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
