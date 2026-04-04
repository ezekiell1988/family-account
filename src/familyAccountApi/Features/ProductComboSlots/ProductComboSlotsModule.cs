using FamilyAccountApi.Features.ProductComboSlots.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FamilyAccountApi.Features.ProductComboSlots;

public static class ProductComboSlotsModule
{
    public static IServiceCollection AddProductComboSlotsModule(this IServiceCollection services)
    {
        services.AddScoped<IProductComboSlotService, ProductComboSlotService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductComboSlotsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/product-combo-slots")
            .WithTags("ProductComboSlots")
            .RequireAuthorization();

        group.MapGet("/by-combo/{idProductCombo:int}.json", GetByCombo)
            .WithName("GetProductComboSlotsByCombo")
            .WithSummary("Obtener slots de un combo (con productos permitidos)");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetProductComboSlotById")
            .WithSummary("Obtener slot por ID");

        group.MapPost("/", Create)
            .WithName("CreateProductComboSlot")
            .WithSummary("Crear slot con productos permitidos")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProductComboSlot")
            .WithSummary("Reemplazar slot y productos permitidos")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProductComboSlot")
            .WithSummary("Eliminar slot del combo")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductComboSlotResponse>>> GetByCombo(
        int idProductCombo, IProductComboSlotService service, CancellationToken ct)
    {
        var items = await service.GetByComboAsync(idProductCombo, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ProductComboSlotResponse>, NotFound>> GetById(
        int id, IProductComboSlotService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ProductComboSlotResponse>, ValidationProblem>> Create(
        CreateProductComboSlotRequest request, IProductComboSlotService service, CancellationToken ct)
    {
        var (result, error) = await service.CreateAsync(request, ct);

        if (error is not null)
        {
            var errors = new Dictionary<string, string[]> { [""] = [error] };
            return TypedResults.ValidationProblem(errors);
        }

        return TypedResults.Created($"/api/v1/product-combo-slots/{result.IdProductComboSlot}.json", result);
    }

    private static async Task<Results<Ok<ProductComboSlotResponse>, NotFound, ValidationProblem>> Update(
        int id, UpdateProductComboSlotRequest request, IProductComboSlotService service, CancellationToken ct)
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
        int id, IProductComboSlotService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
