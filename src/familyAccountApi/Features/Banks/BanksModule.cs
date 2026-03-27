using FamilyAccountApi.Features.Banks.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Banks;

public static class BanksModule
{
    public static IServiceCollection AddBanksModule(this IServiceCollection services)
    {
        services.AddScoped<IBankService, BankService>();
        return services;
    }

    public static IEndpointRouteBuilder MapBanksEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/banks")
            .WithTags("Banks")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllBanks")
            .WithSummary("Obtener todos los bancos");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetBankById")
            .WithSummary("Obtener banco por ID");

        group.MapPost("/", Create)
            .WithName("CreateBank")
            .WithSummary("Crear nuevo banco")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateBank")
            .WithSummary("Actualizar banco")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteBank")
            .WithSummary("Eliminar banco")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<BankResponse>>> GetAll(
        IBankService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<BankResponse>, NotFound>> GetById(
        int id, IBankService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<BankResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateBankRequest request, IBankService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/banks/{item.IdBank}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_bank_codeBank") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un banco con el código '{request.CodeBank.Trim().ToUpper()}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<BankResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id, UpdateBankRequest request, IBankService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_bank_codeBank") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un banco con el código '{request.CodeBank.Trim().ToUpper()}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IBankService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
