using FamilyAccountApi.Features.ExchangeRates.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ExchangeRates;

public static class ExchangeRatesModule
{
    public static IServiceCollection AddExchangeRatesModule(this IServiceCollection services)
    {
        services.AddScoped<IExchangeRateService, ExchangeRateService>();
        return services;
    }

    public static IEndpointRouteBuilder MapExchangeRatesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/exchange-rates")
            .WithTags("ExchangeRates")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllExchangeRates")
            .WithSummary("Obtener todos los tipos de cambio");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetExchangeRateById")
            .WithSummary("Obtener tipo de cambio por ID");

        group.MapGet("/currency/{idCurrency:int}", GetByCurrency)
            .WithName("GetExchangeRatesByCurrency")
            .WithSummary("Obtener tipos de cambio por moneda");

        group.MapPost("/", Create)
            .WithName("CreateExchangeRate")
            .WithSummary("Crear nuevo tipo de cambio")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateExchangeRate")
            .WithSummary("Actualizar tipo de cambio")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteExchangeRate")
            .WithSummary("Eliminar tipo de cambio")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ExchangeRateResponse>>> GetAll(
        IExchangeRateService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ExchangeRateResponse>, NotFound>> GetById(
        int id, IExchangeRateService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Ok<IReadOnlyList<ExchangeRateResponse>>> GetByCurrency(
        int idCurrency, IExchangeRateService service, CancellationToken ct)
    {
        var items = await service.GetByCurrencyAsync(idCurrency, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Created<ExchangeRateResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateExchangeRateRequest request, IExchangeRateService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/exchange-rates/{item.IdExchangeRate}", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_exchangeRate_idCurrency_rateDate") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Tipo de cambio duplicado",
                Detail = "Ya existe un tipo de cambio para la moneda y fecha indicadas.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["exchangeRate"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<Ok<ExchangeRateResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> Update(
        int id, UpdateExchangeRateRequest request, IExchangeRateService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_exchangeRate_idCurrency_rateDate") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Tipo de cambio duplicado",
                Detail = "Ya existe un tipo de cambio para la moneda y fecha indicadas.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["exchangeRate"] = [ex.Message]
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IExchangeRateService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}