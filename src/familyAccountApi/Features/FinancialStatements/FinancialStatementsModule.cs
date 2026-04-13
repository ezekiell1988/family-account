using FamilyAccountApi.Features.FinancialStatements.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.FinancialStatements;

public static class FinancialStatementsModule
{
    public static IServiceCollection AddFinancialStatementsModule(this IServiceCollection services)
    {
        services.AddScoped<IFinancialStatementService, FinancialStatementService>();
        return services;
    }

    public static IEndpointRouteBuilder MapFinancialStatementsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/financial-statements")
            .WithTags("Financial Statements")
            .RequireAuthorization();

        group.MapGet("/income-statement.json", GetIncomeStatement)
            .WithName("GetIncomeStatement")
            .WithSummary("Estado de Resultado")
            .WithDescription(
                "Ingresos y gastos de asientos publicados para el período indicado. " +
                "Filtros (usar uno): idFiscalPeriod | dateFrom+dateTo | year+month | year.");

        group.MapGet("/balance-sheet.json", GetBalanceSheet)
            .WithName("GetBalanceSheet")
            .WithSummary("Estado de Situación Financiera")
            .WithDescription(
                "Saldos acumulados de activos, pasivos y capital hasta el fin del período indicado. " +
                "Solo usa 'dateTo' como corte; ignora 'dateFrom'. " +
                "Filtros (usar uno): idFiscalPeriod | dateFrom+dateTo | year+month | year.");

        group.MapGet("/cash-flow.json", GetCashFlowStatement)
            .WithName("GetCashFlowStatement")
            .WithSummary("Estado de Flujo de Efectivo")
            .WithDescription(
                "Movimientos de apertura/cierre de Activo, Pasivo y Capital más el resultado del período " +
                "(método indirecto simplificado). Identifique sus cuentas de caja/banco en AssetMovements " +
                "para determinar el cambio real de efectivo. " +
                "Filtros (usar uno): idFiscalPeriod | dateFrom+dateTo | year+month | year.");

        group.MapGet("/equity-changes.json", GetEquityStatement)
            .WithName("GetEquityStatement")
            .WithSummary("Estado de Cambios en el Patrimonio")
            .WithDescription(
                "Aportes, retiros y variación en cuentas de Capital durante el período, " +
                "más el resultado del ejercicio no transferido. " +
                "Filtros (usar uno): idFiscalPeriod | dateFrom+dateTo | year+month | year.");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private static async Task<Results<Ok<IncomeStatementResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> GetIncomeStatement(
        [AsParameters] FinancialStatementFilterRequest filter,
        IFinancialStatementService service,
        CancellationToken ct)
    {
        try
        {
            var result = await service.GetIncomeStatementAsync(filter, ct);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title  = "Parámetros de período inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title  = "Período fiscal no encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    private static async Task<Results<Ok<BalanceSheetResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> GetBalanceSheet(
        [AsParameters] FinancialStatementFilterRequest filter,
        IFinancialStatementService service,
        CancellationToken ct)
    {
        try
        {
            var result = await service.GetBalanceSheetAsync(filter, ct);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title  = "Parámetros de período inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title  = "Período fiscal no encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    private static async Task<Results<Ok<CashFlowStatementResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> GetCashFlowStatement(
        [AsParameters] FinancialStatementFilterRequest filter,
        IFinancialStatementService service,
        CancellationToken ct)
    {
        try
        {
            var result = await service.GetCashFlowStatementAsync(filter, ct);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title  = "Parámetros de período inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title  = "Período fiscal no encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    private static async Task<Results<Ok<EquityStatementResponse>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> GetEquityStatement(
        [AsParameters] FinancialStatementFilterRequest filter,
        IFinancialStatementService service,
        CancellationToken ct)
    {
        try
        {
            var result = await service.GetEquityStatementAsync(filter, ct);
            return TypedResults.Ok(result);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title  = "Parámetros de período inválidos",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ProblemDetails
            {
                Title  = "Período fiscal no encontrado",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }
}
