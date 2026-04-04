using FamilyAccountApi.Features.ProductOptionGroups.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FamilyAccountApi.Features.ProductOptionGroups;

public static class ProductOptionGroupsModule
{
    public static IServiceCollection AddProductOptionGroupsModule(this IServiceCollection services)
    {
        services.AddScoped<IProductOptionGroupService, ProductOptionGroupService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductOptionGroupsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/product-option-groups")
            .WithTags("ProductOptionGroups")
            .RequireAuthorization();

        group.MapGet("/by-product/{idProduct:int}.json", GetByProduct)
            .WithName("GetProductOptionGroupsByProduct")
            .WithSummary("Obtener grupos de opciones de un producto (con items)");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetProductOptionGroupById")
            .WithSummary("Obtener grupo de opciones por ID");

        group.MapPost("/", Create)
            .WithName("CreateProductOptionGroup")
            .WithSummary("Crear grupo de opciones con sus items")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProductOptionGroup")
            .WithSummary("Reemplazar grupo de opciones e items")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProductOptionGroup")
            .WithSummary("Eliminar grupo de opciones y sus items")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductOptionGroupResponse>>> GetByProduct(
        int idProduct, IProductOptionGroupService service, CancellationToken ct)
    {
        var items = await service.GetByProductAsync(idProduct, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ProductOptionGroupResponse>, NotFound>> GetById(
        int id, IProductOptionGroupService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ProductOptionGroupResponse>, ValidationProblem>> Create(
        CreateProductOptionGroupRequest request, IProductOptionGroupService service, CancellationToken ct)
    {
        var (result, error) = await service.CreateAsync(request, ct);

        if (error is not null)
        {
            var errors = new Dictionary<string, string[]> { [""] = [error] };
            return TypedResults.ValidationProblem(errors);
        }

        return TypedResults.Created($"/api/v1/product-option-groups/{result.IdProductOptionGroup}.json", result);
    }

    private static async Task<Results<Ok<ProductOptionGroupResponse>, NotFound, ValidationProblem>> Update(
        int id, UpdateProductOptionGroupRequest request, IProductOptionGroupService service, CancellationToken ct)
    {
        var (result, error) = await service.UpdateAsync(id, request, ct);

        if (error is not null)
        {
            var errors = new Dictionary<string, string[]> { [""] = [error] };
            return TypedResults.ValidationProblem(errors);
        }

        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IProductOptionGroupService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
