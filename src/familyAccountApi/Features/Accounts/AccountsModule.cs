using FamilyAccountApi.Features.Accounts.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Accounts;

public static class AccountsModule
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services)
    {
        services.AddScoped<IAccountService, AccountService>();
        return services;
    }

    public static IEndpointRouteBuilder MapAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts")
            .WithTags("Accounts")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllAccounts")
            .WithSummary("Obtener todas las cuentas contables");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetAccountById")
            .WithSummary("Obtener cuenta contable por ID");

        group.MapGet("/{id:int}/children.json", GetChildren)
            .WithName("GetAccountChildren")
            .WithSummary("Obtener cuentas hijas de una cuenta");

        group.MapPost("/", Create)
            .WithName("CreateAccount")
            .WithSummary("Crear nueva cuenta contable")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateAccount")
            .WithSummary("Actualizar cuenta contable")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteAccount")
            .WithSummary("Eliminar cuenta contable")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<AccountResponse>>> GetAll(
        IAccountService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<AccountResponse>, NotFound>> GetById(
        int id, IAccountService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Ok<IReadOnlyList<AccountResponse>>> GetChildren(
        int id, IAccountService service, CancellationToken ct)
    {
        var items = await service.GetChildrenAsync(id, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Created<AccountResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateAccountRequest request, IAccountService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/accounts/{item.IdAccount}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_account_codeAccount") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe una cuenta con el código '{request.CodeAccount}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<AccountResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id, UpdateAccountRequest request, IAccountService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_account_codeAccount") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe una cuenta con el código '{request.CodeAccount}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> Delete(
        int id, IAccountService service, CancellationToken ct)
    {
        try
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("FOREIGN KEY") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Cuenta con subcuentas",
                Detail = "No se puede eliminar una cuenta que tiene subcuentas asociadas.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
