using FamilyAccountApi.Features.Warehouses.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;

namespace FamilyAccountApi.Features.Warehouses;

public static class WarehousesModule
{
    public static IServiceCollection AddWarehousesModule(this IServiceCollection services)
    {
        services.AddScoped<IWarehouseService, WarehouseService>();
        return services;
    }

    public static IEndpointRouteBuilder MapWarehousesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/warehouses")
            .WithTags("Warehouses")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetWarehouses")
            .WithSummary("Listar todos los almacenes.");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetWarehouseById")
            .WithSummary("Obtener un almacén por ID.");

        group.MapPost("", Create)
            .WithName("CreateWarehouse")
            .WithSummary("Crear un nuevo almacén. Si IsDefault=true, el almacén predeterminado anterior pierde esa condición.")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateWarehouse")
            .WithSummary("Actualizar un almacén.")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteWarehouse")
            .WithSummary("Eliminar un almacén. Falla si tiene lotes de inventario asociados.")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<WarehouseResponse>>> GetAll(
        IWarehouseService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<WarehouseResponse>, NotFound>> GetById(
        int id, IWarehouseService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Created<WarehouseResponse>> Create(
        CreateWarehouseRequest request, IWarehouseService service, CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return TypedResults.Created($"/api/v1/warehouses/{created.IdWarehouse}.json", created);
    }

    private static async Task<Results<Ok<WarehouseResponse>, NotFound>> Update(
        int id, UpdateWarehouseRequest request, IWarehouseService service, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(id, request, ct);
        return updated is not null ? TypedResults.Ok(updated) : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IWarehouseService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
