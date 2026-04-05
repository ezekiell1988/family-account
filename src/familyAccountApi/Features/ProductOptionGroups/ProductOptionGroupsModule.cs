using FamilyAccountApi.Features.ProductOptionGroups.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

        // ── Availability rules ────────────────────────────────────────────────
        group.MapPost("/items/{idItem:int}/availability-rules", CreateAvailabilityRule)
            .WithName("CreateAvailabilityRule")
            .WithSummary("Crear regla de disponibilidad condicional entre ítems de opción")
            .RequireAuthorization("Admin");

        group.MapDelete("/availability-rules/{idRule:int}", DeleteAvailabilityRule)
            .WithName("DeleteAvailabilityRule")
            .WithSummary("Eliminar regla de disponibilidad")
            .RequireAuthorization("Admin");

        group.MapGet("/available-by-product/{idProduct:int}.json", GetAvailableGroups)
            .WithName("GetAvailableGroups")
            .WithSummary("Obtener grupos con isAvailable calculado según los ítems activos");

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

    private static async Task<Results<Created<AvailabilityRuleResponse>, ValidationProblem>> CreateAvailabilityRule(
        int idItem,
        [FromBody] CreateAvailabilityRuleRequest request,
        IProductOptionGroupService service, CancellationToken ct)
    {
        var (result, error) = await service.CreateAvailabilityRuleAsync(idItem, request, ct);
        if (error is not null)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["rule"] = [error] });
        return TypedResults.Created($"/api/v1/product-option-groups/availability-rules/{result!.IdProductOptionItemAvailability}", result);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAvailabilityRule(
        int idRule, IProductOptionGroupService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAvailabilityRuleAsync(idRule, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Ok<IReadOnlyList<AvailableGroupResponse>>> GetAvailableGroups(
        int idProduct,
        [FromQuery] string? activeItems,
        IProductOptionGroupService service, CancellationToken ct)
    {
        var ids = activeItems?.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var id) ? (int?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList() ?? [];

        var result = await service.GetAvailableGroupsAsync(idProduct, ids, ct);
        return TypedResults.Ok(result);
    }
}
