using FamilyAccountApi.Features.Budgets.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Budgets;

public static class BudgetsModule
{
    public static IServiceCollection AddBudgetsModule(this IServiceCollection services)
    {
        services.AddScoped<IBudgetService, BudgetService>();
        return services;
    }

    public static IEndpointRouteBuilder MapBudgetsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/budgets")
            .WithTags("Budgets")
            .RequireAuthorization();

        group.MapGet(".json", GetAll)
            .WithName("GetAllBudgets")
            .WithSummary("Obtener todos los presupuestos");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetBudgetById")
            .WithSummary("Obtener presupuesto por ID");

        group.MapPost("/", Create)
            .WithName("CreateBudget")
            .WithSummary("Crear nuevo presupuesto")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateBudget")
            .WithSummary("Actualizar presupuesto")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteBudget")
            .WithSummary("Eliminar presupuesto")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<BudgetResponse>>> GetAll(
        IBudgetService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<BudgetResponse>, NotFound>> GetById(
        int id, IBudgetService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<BudgetResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateBudgetRequest request, IBudgetService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/budgets/{item.IdBudget}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_budget_idAccount_idFiscalPeriod") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Presupuesto duplicado",
                Detail = "Ya existe un presupuesto para la cuenta y período fiscal indicados.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["budget"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<Ok<BudgetResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> Update(
        int id, UpdateBudgetRequest request, IBudgetService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_budget_idAccount_idFiscalPeriod") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Presupuesto duplicado",
                Detail = "Ya existe un presupuesto para la cuenta y período fiscal indicados.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["budget"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IBudgetService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}