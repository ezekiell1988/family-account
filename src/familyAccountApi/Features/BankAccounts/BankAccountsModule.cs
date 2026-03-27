using FamilyAccountApi.Features.BankAccounts.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.BankAccounts;

public static class BankAccountsModule
{
    public static IServiceCollection AddBankAccountsModule(this IServiceCollection services)
    {
        services.AddScoped<IBankAccountService, BankAccountService>();
        return services;
    }

    public static IEndpointRouteBuilder MapBankAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bank-accounts")
            .WithTags("BankAccounts")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllBankAccounts")
            .WithSummary("Obtener todas las cuentas bancarias");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetBankAccountById")
            .WithSummary("Obtener cuenta bancaria por ID");

        group.MapPost("/", Create)
            .WithName("CreateBankAccount")
            .WithSummary("Crear nueva cuenta bancaria")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateBankAccount")
            .WithSummary("Actualizar cuenta bancaria")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteBankAccount")
            .WithSummary("Eliminar cuenta bancaria")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<BankAccountResponse>>> GetAll(
        IBankAccountService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<BankAccountResponse>, NotFound>> GetById(
        int id, IBankAccountService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<BankAccountResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateBankAccountRequest request, IBankAccountService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/bank-accounts/{item.IdBankAccount}.json", item);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("UQ_bankAccount_codeBankAccount") == true ||
            ex.InnerException?.Message.Contains("UQ_bankAccount_accountNumber") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Cuenta bancaria duplicada",
                Detail = "Ya existe una cuenta bancaria con el código o número indicado.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bankAccount"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<Ok<BankAccountResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> Update(
        int id, UpdateBankAccountRequest request, IBankAccountService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("UQ_bankAccount_codeBankAccount") == true ||
            ex.InnerException?.Message.Contains("UQ_bankAccount_accountNumber") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Cuenta bancaria duplicada",
                Detail = "Ya existe una cuenta bancaria con el código o número indicado.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["bankAccount"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IBankAccountService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}