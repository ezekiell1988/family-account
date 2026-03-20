using FamilyAccountApi.Features.BankMovements.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.BankMovements;

public static class BankMovementsModule
{
    public static IServiceCollection AddBankMovementsModule(this IServiceCollection services)
    {
        services.AddScoped<IBankMovementService, BankMovementService>();
        return services;
    }

    public static IEndpointRouteBuilder MapBankMovementsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bank-movements")
            .WithTags("BankMovements")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllBankMovements")
            .WithSummary("Obtener todos los movimientos bancarios");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetBankMovementById")
            .WithSummary("Obtener movimiento bancario por ID");

        group.MapPost("/", Create)
            .WithName("CreateBankMovement")
            .WithSummary("Crear nuevo movimiento bancario")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateBankMovement")
            .WithSummary("Actualizar movimiento bancario")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteBankMovement")
            .WithSummary("Eliminar movimiento bancario (solo en estado Borrador)")
            .RequireAuthorization("Admin");

        group.MapPost("/{id:int}/confirm", Confirm)
            .WithName("ConfirmBankMovement")
            .WithSummary("Confirmar movimiento bancario")
            .RequireAuthorization("Admin");

        group.MapPost("/{id:int}/cancel", Cancel)
            .WithName("CancelBankMovement")
            .WithSummary("Anular movimiento bancario")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<BankMovementResponse>>> GetAll(
        IBankMovementService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<BankMovementResponse>, NotFound>> GetById(
        int id, IBankMovementService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<BankMovementResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateBankMovementRequest request, IBankMovementService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/bank-movements/{item.IdBankMovement}", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_bankMovement_numberMovement") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Número duplicado",
                Detail = $"Ya existe un movimiento con el número '{request.NumberMovement}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bankMovement"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<Ok<BankMovementResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> Update(
        int id, UpdateBankMovementRequest request, IBankMovementService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_bankMovement_numberMovement") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Número duplicado",
                Detail = $"Ya existe un movimiento con el número '{request.NumberMovement}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bankMovement"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> Delete(
        int id, IBankMovementService service, CancellationToken ct)
    {
        try
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bankMovement"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<Ok<BankMovementResponse>, NotFound, ValidationProblem>> Confirm(
        int id, IBankMovementService service, CancellationToken ct)
    {
        try
        {
            var item = await service.ConfirmAsync(id, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bankMovement"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<Ok<BankMovementResponse>, NotFound, ValidationProblem>> Cancel(
        int id, IBankMovementService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CancelAsync(id, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bankMovement"] = [ex.Message]
            });
        }
    }
}
