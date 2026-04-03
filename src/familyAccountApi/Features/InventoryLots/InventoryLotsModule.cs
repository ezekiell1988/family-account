using FamilyAccountApi.Features.InventoryLots.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FamilyAccountApi.Features.InventoryLots;

public static class InventoryLotsModule
{
    public static IServiceCollection AddInventoryLotsModule(this IServiceCollection services)
    {
        services.AddScoped<IInventoryLotService, InventoryLotService>();
        return services;
    }

    public static IEndpointRouteBuilder MapInventoryLotsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventory-lots")
            .WithTags("InventoryLots")
            .RequireAuthorization();

        group.MapGet("/by-product/{idProduct:int}.json", GetByProduct)
            .WithName("GetInventoryLotsByProduct")
            .WithSummary("Obtener lotes de inventario de un producto ordenados FEFO");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetInventoryLotById")
            .WithSummary("Obtener lote de inventario por ID");

        group.MapGet("/stock/{idProduct:int}.json", GetStock)
            .WithName("GetStockTotal")
            .WithSummary("Obtener stock total disponible de un producto en unidad base");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<InventoryLotResponse>>> GetByProduct(
        int idProduct, IInventoryLotService service, CancellationToken ct)
    {
        var items = await service.GetByProductAsync(idProduct, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<InventoryLotResponse>, NotFound>> GetById(
        int id, IInventoryLotService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Ok<decimal>> GetStock(
        int idProduct, IInventoryLotService service, CancellationToken ct)
    {
        var total = await service.GetStockTotalAsync(idProduct, ct);
        return TypedResults.Ok(total);
    }
}
