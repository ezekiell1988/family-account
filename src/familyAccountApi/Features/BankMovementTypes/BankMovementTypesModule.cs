using FamilyAccountApi.Features.BankMovementTypes.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.BankMovementTypes;

public static class BankMovementTypesModule
{
    public static IServiceCollection AddBankMovementTypesModule(this IServiceCollection services)
    {
        services.AddScoped<IBankMovementTypeService, BankMovementTypeService>();
        return services;
    }

    public static IEndpointRouteBuilder MapBankMovementTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bank-movement-types")
            .WithTags("BankMovementTypes")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllBankMovementTypes")
            .WithSummary("Obtener todos los tipos de movimiento bancario");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetBankMovementTypeById")
            .WithSummary("Obtener tipo de movimiento bancario por ID");

        group.MapPost("/", Create)
            .WithName("CreateBankMovementType")
            .WithSummary("Crear nuevo tipo de movimiento bancario")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateBankMovementType")
            .WithSummary("Actualizar tipo de movimiento bancario")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteBankMovementType")
            .WithSummary("Eliminar tipo de movimiento bancario")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<BankMovementTypeResponse>>> GetAll(
        IBankMovementTypeService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<BankMovementTypeResponse>, NotFound>> GetById(
        int id, IBankMovementTypeService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<BankMovementTypeResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateBankMovementTypeRequest request, IBankMovementTypeService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/bank-movement-types/{item.IdBankMovementType}", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_bankMovementType_codeBankMovementType") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un tipo de movimiento con el código '{request.CodeBankMovementType}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bankMovementType"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<Ok<BankMovementTypeResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> Update(
        int id, UpdateBankMovementTypeRequest request, IBankMovementTypeService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_bankMovementType_codeBankMovementType") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un tipo de movimiento con el código '{request.CodeBankMovementType}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bankMovementType"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IBankMovementTypeService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
