using FamilyAccountApi.Features.AccountingEntries.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.AccountingEntries;

public static class AccountingEntriesModule
{
    public static IServiceCollection AddAccountingEntriesModule(this IServiceCollection services)
    {
        services.AddScoped<IAccountingEntryService, AccountingEntryService>();
        return services;
    }

    public static IEndpointRouteBuilder MapAccountingEntriesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounting-entries")
            .WithTags("AccountingEntries")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllAccountingEntries")
            .WithSummary("Obtener todos los asientos contables");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetAccountingEntryById")
            .WithSummary("Obtener asiento contable por ID");

        group.MapPost("/", Create)
            .WithName("CreateAccountingEntry")
            .WithSummary("Crear nuevo asiento contable")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateAccountingEntry")
            .WithSummary("Actualizar asiento contable")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteAccountingEntry")
            .WithSummary("Eliminar asiento contable")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<AccountingEntryResponse>>> GetAll(
        IAccountingEntryService service,
        CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<AccountingEntryResponse>, NotFound>> GetById(
        int id,
        IAccountingEntryService service,
        CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<AccountingEntryResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateAccountingEntryRequest request,
        IAccountingEntryService service,
        CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/accounting-entries/{item.IdAccountingEntry}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_accountingEntry_idFiscalPeriod_numberEntry") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Asiento duplicado",
                Detail = $"Ya existe un asiento con el número '{request.NumberEntry}' en el período fiscal indicado.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["accountingEntry"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<Ok<AccountingEntryResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> Update(
        int id,
        UpdateAccountingEntryRequest request,
        IAccountingEntryService service,
        CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_accountingEntry_idFiscalPeriod_numberEntry") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Asiento duplicado",
                Detail = $"Ya existe un asiento con el número '{request.NumberEntry}' en el período fiscal indicado.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["accountingEntry"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id,
        IAccountingEntryService service,
        CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
