using FamilyAccountApi.Features.FinancialObligations.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.FinancialObligations;

public static class FinancialObligationsModule
{
    public static IServiceCollection AddFinancialObligationsModule(this IServiceCollection services)
    {
        services.AddScoped<IFinancialObligationService, FinancialObligationService>();
        return services;
    }

    public static IEndpointRouteBuilder MapFinancialObligationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/financial-obligations")
            .WithTags("Financial Obligations")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllFinancialObligations")
            .WithSummary("Obtener todos los préstamos y obligaciones financieras");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetFinancialObligationById")
            .WithSummary("Obtener préstamo por ID con cuotas y pagos");

        group.MapGet("/{id:int}/summary.json", GetSummary)
            .WithName("GetFinancialObligationSummary")
            .WithSummary("Resumen ejecutivo: saldo, cuota actual, porción corriente");

        group.MapPost("/", Create)
            .WithName("CreateFinancialObligation")
            .WithSummary("Crear nueva obligación financiera")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateFinancialObligation")
            .WithSummary("Actualizar parámetros del préstamo (cuentas, keyword, estado)")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteFinancialObligation")
            .WithSummary("Eliminar préstamo sin pagos registrados")
            .RequireAuthorization("Admin");

        group.MapPost("/{id:int}/sync-excel", SyncExcel)
            .WithName("SyncFinancialObligationExcel")
            .WithSummary("Cargar Excel del banco: upsert cuotas, detectar pagos BAC, generar asientos y reclasificación")
            .RequireAuthorization("Admin")
            .DisableAntiforgery();

        group.MapPost("/{id:int}/installments/{installmentId:int}/payment", RegisterPayment)
            .WithName("RegisterInstallmentPayment")
            .WithSummary("Registrar pago manual de una cuota")
            .RequireAuthorization("Admin");

        group.MapPost("/{id:int}/reclassify", Reclassify)
            .WithName("ReclassifyFinancialObligation")
            .WithSummary("Reclasificación manual largo plazo → corto plazo a una fecha de corte")
            .RequireAuthorization("Admin");

        return app;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private static async Task<Ok<IReadOnlyList<FinancialObligationResponse>>> GetAll(
        IFinancialObligationService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<FinancialObligationResponse>, NotFound>> GetById(
        int id, IFinancialObligationService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<FinancialObligationSummaryResponse>, NotFound>> GetSummary(
        int id, IFinancialObligationService service, CancellationToken ct)
    {
        var item = await service.GetSummaryAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<FinancialObligationResponse>, ValidationProblem, BadRequest<ProblemDetails>>> Create(
        CreateFinancialObligationRequest request, IFinancialObligationService service, CancellationToken ct)
    {
        try
        {
            var created = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/financial-obligations/{created.IdFinancialObligation}.json", created);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    private static async Task<Results<Ok<FinancialObligationResponse>, NotFound, BadRequest<ProblemDetails>>> Update(
        int id, UpdateFinancialObligationRequest request, IFinancialObligationService service, CancellationToken ct)
    {
        try
        {
            var updated = await service.UpdateAsync(id, request, ct);
            return updated is not null ? TypedResults.Ok(updated) : TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ProblemDetails>>> Delete(
        int id, IFinancialObligationService service, CancellationToken ct)
    {
        try
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    private static async Task<Results<Ok<SyncExcelResult>, NotFound, BadRequest<ProblemDetails>>> SyncExcel(
        int id, IFormFile file, IFinancialObligationService service, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return TypedResults.BadRequest(new ProblemDetails { Detail = "Debe adjuntar un archivo Excel (.xlsx)." });

        try
        {
            using var stream = file.OpenReadStream();
            var result = await service.SyncExcelAsync(id, stream, ct);
            return TypedResults.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    private static async Task<Results<Created<FinancialObligationPaymentResponse>, NotFound, BadRequest<ProblemDetails>>> RegisterPayment(
        int id, int installmentId, RegisterPaymentRequest request, IFinancialObligationService service, CancellationToken ct)
    {
        try
        {
            var payment = await service.RegisterPaymentAsync(installmentId, request, ct);
            return TypedResults.Created($"/api/v1/financial-obligations/{id}.json", payment);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }

    private static async Task<Results<Ok<object>, NotFound, BadRequest<ProblemDetails>>> Reclassify(
        int id, [FromQuery] DateOnly? asOfDate, IFinancialObligationService service, CancellationToken ct)
    {
        var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);
        try
        {
            var entryId = await service.ReclassifyAsync(id, date, ct);
            return TypedResults.Ok<object>(new { idAccountingEntry = entryId, asOfDate = date });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails { Detail = ex.Message });
        }
    }
}
