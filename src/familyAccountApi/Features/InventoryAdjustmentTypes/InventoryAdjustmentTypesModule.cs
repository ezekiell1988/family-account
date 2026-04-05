using FamilyAccountApi.Features.InventoryAdjustmentTypes.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.InventoryAdjustmentTypes;

public static class InventoryAdjustmentTypesModule
{
    public static IServiceCollection AddInventoryAdjustmentTypesModule(this IServiceCollection services)
    {
        services.AddScoped<IInventoryAdjustmentTypeService, InventoryAdjustmentTypeService>();
        return services;
    }

    public static IEndpointRouteBuilder MapInventoryAdjustmentTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventory-adjustment-types")
            .WithTags("InventoryAdjustmentTypes")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllInventoryAdjustmentTypes")
            .WithSummary("Obtener todos los tipos de ajuste de inventario");

        group.MapGet("/active.json", GetActive)
            .WithName("GetActiveInventoryAdjustmentTypes")
            .WithSummary("Obtener tipos de ajuste activos");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetInventoryAdjustmentTypeById")
            .WithSummary("Obtener tipo de ajuste por ID");

        group.MapPost("/", Create)
            .WithName("CreateInventoryAdjustmentType")
            .WithSummary("Crear nuevo tipo de ajuste de inventario")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateInventoryAdjustmentType")
            .WithSummary("Actualizar tipo de ajuste (nombre, cuentas contables, estado)")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteInventoryAdjustmentType")
            .WithSummary("Eliminar tipo de ajuste (solo si no tiene ajustes asociados)")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<InventoryAdjustmentTypeResponse>>> GetAll(
        IInventoryAdjustmentTypeService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Ok<IReadOnlyList<InventoryAdjustmentTypeResponse>>> GetActive(
        IInventoryAdjustmentTypeService service, CancellationToken ct)
    {
        var items = await service.GetActiveAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<InventoryAdjustmentTypeResponse>, NotFound>> GetById(
        int id, IInventoryAdjustmentTypeService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<InventoryAdjustmentTypeResponse>, ValidationProblem>> Create(
        CreateInventoryAdjustmentTypeRequest request, IInventoryAdjustmentTypeService service, CancellationToken ct)
    {
        var item = await service.CreateAsync(request, ct);
        return TypedResults.Created($"/api/v1/inventory-adjustment-types/{item.IdInventoryAdjustmentType}.json", item);
    }

    private static async Task<Results<Ok<InventoryAdjustmentTypeResponse>, NotFound>> Update(
        int id, UpdateInventoryAdjustmentTypeRequest request, IInventoryAdjustmentTypeService service, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, request, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> Delete(
        int id, IInventoryAdjustmentTypeService service, CancellationToken ct)
    {
        try
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
        }
        catch (Exception ex) when (ex.InnerException?.Message.Contains("REFERENCE constraint") == true
                                 || ex.InnerException?.Message.Contains("FOREIGN KEY") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Tipo en uso",
                Detail = "No se puede eliminar el tipo de ajuste porque tiene ajustes de inventario asociados.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
