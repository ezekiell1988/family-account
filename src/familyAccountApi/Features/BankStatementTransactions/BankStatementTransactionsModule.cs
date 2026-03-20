using FamilyAccountApi.Features.BankMovements.Dtos;
using FamilyAccountApi.Features.BankStatementTransactions.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.BankStatementTransactions;

public static class BankStatementTransactionsModule
{
    public static IServiceCollection AddBankStatementTransactionsModule(this IServiceCollection services)
    {
        services.AddScoped<IBankStatementTransactionService, BankStatementTransactionService>();
        return services;
    }

    public static IEndpointRouteBuilder MapBankStatementTransactionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bank-statement-transactions")
            .WithTags("Bank Statement Transactions")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllBankStatementTransactions")
            .WithSummary("Obtener todas las transacciones de extractos bancarios");

        group.MapGet("/import/{importId:int}", GetByImportId)
            .WithName("GetBankStatementTransactionsByImportId")
            .WithSummary("Obtener transacciones por ID de importación");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetBankStatementTransactionById")
            .WithSummary("Obtener transacción por ID");

        group.MapPost("/", Create)
            .WithName("CreateBankStatementTransaction")
            .WithSummary("Crear nueva transacción")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateBankStatementTransaction")
            .WithSummary("Actualizar transacción")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteBankStatementTransaction")
            .WithSummary("Eliminar transacción")
            .RequireAuthorization("Admin");

        group.MapPatch("/{id:int}/classify", Classify)
            .WithName("ClassifyBankStatementTransaction")
            .WithSummary("Clasificar (o reclasificar) una transacción: asignar tipo de movimiento y cuenta contrapartida")
            .RequireAuthorization("Admin");

        group.MapPost("/{id:int}/create-movement", CreateMovement)
            .WithName("CreateMovementFromBankStatementTransaction")
            .WithSummary("Crear un BankMovement a partir de una transacción clasificada y marcarla como conciliada")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<BankStatementTransactionResponse>>> GetAll(
        IBankStatementTransactionService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Ok<IReadOnlyList<BankStatementTransactionResponse>>> GetByImportId(
        int importId, IBankStatementTransactionService service, CancellationToken ct)
    {
        var items = await service.GetByImportIdAsync(importId, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<BankStatementTransactionResponse>, NotFound>> GetById(
        int id, IBankStatementTransactionService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<BankStatementTransactionResponse>, ValidationProblem, BadRequest<ProblemDetails>>> Create(
        CreateBankStatementTransactionRequest request, IBankStatementTransactionService service, CancellationToken ct)
    {
        try
        {
            var created = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/bank-statement-transactions/{created.IdBankStatementTransaction}", created);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Validación fallida",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static async Task<Results<Ok<BankStatementTransactionResponse>, NotFound, ValidationProblem, BadRequest<ProblemDetails>>> Update(
        int id, UpdateBankStatementTransactionRequest request, IBankStatementTransactionService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Validación fallida",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IBankStatementTransactionService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<BankStatementTransactionResponse>, NotFound, BadRequest<ProblemDetails>>> Classify(
        int id,
        ClassifyBankStatementTransactionRequest request,
        IBankStatementTransactionService service,
        CancellationToken ct)
    {
        try
        {
            var result = await service.ClassifyAsync(id, request, ct);
            return result is not null ? TypedResults.Ok(result) : TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title  = "Clasificación fallida",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static async Task<Results<Created<BankMovementResponse>, NotFound, BadRequest<ProblemDetails>>> CreateMovement(
        int id,
        CreateMovementFromTransactionRequest request,
        IBankStatementTransactionService service,
        CancellationToken ct)
    {
        try
        {
            var movement = await service.CreateMovementFromTransactionAsync(id, request, ct);
            return TypedResults.Created($"/bank-movements/{movement.IdBankMovement}", movement);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title  = "Error al crear movimiento",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
