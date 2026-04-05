using FamilyAccountApi.Features.InventoryLots.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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
            .WithSummary("Obtener lotes de inventario de un producto ordenados FEFO. " +
                         "Parámetro opcional ?idWarehouse={id} para filtrar por almacén.");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetInventoryLotById")
            .WithSummary("Obtener lote de inventario por ID");

        group.MapGet("/stock/{idProduct:int}.json", GetStock)
            .WithName("GetStockTotal")
            .WithSummary("Obtener stock total disponible de un producto en unidad base");

        group.MapGet("/suggest/{idProduct:int}.json", GetSuggestedLot)
            .WithName("GetSuggestedInventoryLot")
            .WithSummary("Sugerir el lote más antiguo no vencido con stock disponible (FEFO). " +
                         "Parámetros opcionales: ?date=yyyy-MM-dd (por defecto: fecha UTC de hoy), " +
                         "?idWarehouse={id} para restringir la búsqueda a un almacén.");

        group.MapPatch("/{id:int}/status", UpdateStatus)
            .WithName("UpdateInventoryLotStatus")
            .WithSummary("Cambiar el estado de calidad de un lote: Disponible | Cuarentena | Bloqueado | Vencido. " +
                         "Los lotes no Disponibles quedan excluidos de la selección FEFO.")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<InventoryLotResponse>>> GetByProduct(
        int idProduct, [FromQuery] int? idWarehouse, IInventoryLotService service, CancellationToken ct)
    {
        var items = await service.GetByProductAsync(idProduct, idWarehouse, ct);
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

    private static async Task<Results<Ok<InventoryLotResponse>, NotFound>> GetSuggestedLot(
        int idProduct,
        [FromQuery] DateOnly? date,
        [FromQuery] int? idWarehouse,
        IInventoryLotService service,
        CancellationToken ct)
    {
        var referenceDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var item = await service.GetSuggestedLotAsync(idProduct, referenceDate, idWarehouse, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<InventoryLotResponse>, NotFound, ValidationProblem>> UpdateStatus(
        int id, UpdateInventoryLotStatusRequest request, IInventoryLotService service, CancellationToken ct)
    {
        var validStatuses = new[] { "Disponible", "Cuarentena", "Bloqueado", "Vencido" };
        if (!validStatuses.Contains(request.StatusLot))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["statusLot"] = [$"El estado debe ser uno de: {string.Join(", ", validStatuses)}."]
            });
        }

        var item = await service.UpdateStatusAsync(id, request, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }
}
