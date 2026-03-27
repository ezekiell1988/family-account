using FamilyAccountApi.Features.Currencies.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Currencies;

public static class CurrenciesModule
{
    public static IServiceCollection AddCurrenciesModule(this IServiceCollection services)
    {
        services.AddScoped<ICurrencyService, CurrencyService>();
        return services;
    }

    public static IEndpointRouteBuilder MapCurrenciesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/currencies")
            .WithTags("Currencies")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllCurrencies")
            .WithSummary("Obtener todas las monedas");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetCurrencyById")
            .WithSummary("Obtener moneda por ID");

        group.MapPost("/", Create)
            .WithName("CreateCurrency")
            .WithSummary("Crear nueva moneda")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateCurrency")
            .WithSummary("Actualizar moneda")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteCurrency")
            .WithSummary("Eliminar moneda")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<CurrencyResponse>>> GetAll(
        ICurrencyService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<CurrencyResponse>, NotFound>> GetById(
        int id, ICurrencyService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<CurrencyResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateCurrencyRequest request, ICurrencyService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/currencies/{item.IdCurrency}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_currency_codeCurrency") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Código duplicado",
                Detail = $"Ya existe una moneda con el código '{request.CodeCurrency}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<CurrencyResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> Update(
        int id, UpdateCurrencyRequest request, ICurrencyService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_currency_codeCurrency") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Código duplicado",
                Detail = $"Ya existe una moneda con el código '{request.CodeCurrency}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> Delete(
        int id, ICurrencyService service, CancellationToken ct)
    {
        try
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
        }
        catch (DbUpdateException)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Moneda en uso",
                Detail = "No se puede eliminar la moneda porque tiene tipos de cambio asociados.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}