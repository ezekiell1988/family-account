using FamilyAccountApi.Features.InventoryAdjustments.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.InventoryAdjustments;

public static class InventoryAdjustmentsModule
{
    public static IServiceCollection AddInventoryAdjustmentsModule(this IServiceCollection services)
    {
        services.AddScoped<IInventoryAdjustmentService, InventoryAdjustmentService>();
        return services;
    }

    public static IEndpointRouteBuilder MapInventoryAdjustmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/inventory-adjustments")
            .WithTags("InventoryAdjustments")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllInventoryAdjustments")
            .WithSummary("Obtener todos los ajustes de inventario");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetInventoryAdjustmentById")
            .WithSummary("Obtener ajuste de inventario por ID con sus líneas");

        group.MapPost("/", Create)
            .WithName("CreateInventoryAdjustment")
            .WithSummary("Crear nuevo ajuste de inventario en estado Borrador")
            .RequireAuthorization("Admin");

        group.MapPost("/{id:int}/confirm", Confirm)
            .WithName("ConfirmInventoryAdjustment")
            .WithSummary("Confirmar ajuste: aplica los deltas de inventario y recalcula costos promedio")
            .RequireAuthorization("Admin");

        group.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelInventoryAdjustment")
            .WithSummary("Anular ajuste de inventario")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteInventoryAdjustment")
            .WithSummary("Eliminar ajuste en estado Borrador")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<InventoryAdjustmentResponse>>> GetAll(
        IInventoryAdjustmentService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<InventoryAdjustmentResponse>, NotFound>> GetById(
        int id, IInventoryAdjustmentService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<InventoryAdjustmentResponse>, ValidationProblem>> Create(
        CreateInventoryAdjustmentRequest request, IInventoryAdjustmentService service, CancellationToken ct)
    {
        var item = await service.CreateAsync(request, ct);
        return TypedResults.Created($"/api/v1/inventory-adjustments/{item.IdInventoryAdjustment}.json", item);
    }

    private static async Task<Results<Ok<InventoryAdjustmentResponse>, NotFound, Conflict<ProblemDetails>>> Confirm(
        int id, IInventoryAdjustmentService service, CancellationToken ct)
    {
        try
        {
            var item = await service.ConfirmAsync(id, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Stock insuficiente",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<InventoryAdjustmentResponse>, NotFound, Conflict<ProblemDetails>>> Cancel(
        int id, IInventoryAdjustmentService service, CancellationToken ct)
    {
        var (result, conflict) = await service.CancelAsync(id, ct);
        if (conflict is not null)
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "No se puede anular el ajuste",
                Detail = conflict,
                Status = StatusCodes.Status409Conflict
            });
        return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> Delete(
        int id, IInventoryAdjustmentService service, CancellationToken ct)
    {
        try
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Operación no permitida",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
