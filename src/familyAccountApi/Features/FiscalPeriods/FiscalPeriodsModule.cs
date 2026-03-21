using FamilyAccountApi.Features.FiscalPeriods.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.FiscalPeriods;

public static class FiscalPeriodsModule
{
    public static IServiceCollection AddFiscalPeriodsModule(this IServiceCollection services)
    {
        services.AddScoped<IFiscalPeriodService, FiscalPeriodService>();
        return services;
    }

    public static IEndpointRouteBuilder MapFiscalPeriodsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/fiscal-periods")
            .WithTags("FiscalPeriods")
            .RequireAuthorization();

        group.MapGet(".json", GetAll)
            .WithName("GetAllFiscalPeriods")
            .WithSummary("Obtener todos los períodos fiscales");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetFiscalPeriodById")
            .WithSummary("Obtener período fiscal por ID");

        group.MapGet("/year/{year:int}.json", GetByYear)
            .WithName("GetFiscalPeriodsByYear")
            .WithSummary("Obtener todos los períodos fiscales de un año");

        group.MapPost("/", Create)
            .WithName("CreateFiscalPeriod")
            .WithSummary("Crear nuevo período fiscal")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateFiscalPeriod")
            .WithSummary("Actualizar período fiscal")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteFiscalPeriod")
            .WithSummary("Eliminar período fiscal")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<FiscalPeriodResponse>>> GetAll(
        IFiscalPeriodService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<FiscalPeriodResponse>, NotFound>> GetById(
        int id, IFiscalPeriodService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Ok<IReadOnlyList<FiscalPeriodResponse>>> GetByYear(
        int year, IFiscalPeriodService service, CancellationToken ct)
    {
        var items = await service.GetByYearAsync(year, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Created<FiscalPeriodResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateFiscalPeriodRequest request, IFiscalPeriodService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/fiscal-periods/{item.IdFiscalPeriod}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_fiscalPeriod_yearPeriod_monthPeriod") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Período duplicado",
                Detail = $"Ya existe un período para {request.MonthPeriod}/{request.YearPeriod}.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<FiscalPeriodResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> Update(
        int id, UpdateFiscalPeriodRequest request, IFiscalPeriodService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_fiscalPeriod_yearPeriod_monthPeriod") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Período duplicado",
                Detail = $"Ya existe un período para {request.MonthPeriod}/{request.YearPeriod}.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IFiscalPeriodService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
