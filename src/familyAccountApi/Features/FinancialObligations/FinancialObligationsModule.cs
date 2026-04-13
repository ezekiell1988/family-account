using FamilyAccountApi.BackgroundJobs;
using FamilyAccountApi.Features.FinancialObligations.Dtos;
using Hangfire;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FamilyAccountApi.Features.FinancialObligations;

public static class FinancialObligationsModule
{
    public static IServiceCollection AddFinancialObligationsModule(this IServiceCollection services)
    {
        services.AddScoped<IFinancialObligationService, FinancialObligationService>();
        services.AddScoped<FinancialObligationBacTcSyncJob>();
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

        group.MapPost("/sync-bac-financiamientos", SyncBacFinanciamientos)
            .WithName("SyncBacFinanciamientos")
            .WithSummary("Cargar XLS de Financiamientos (Tasa Cero) BAC: upsert de obligaciones y cuotas vía Hangfire")
            .RequireAuthorization("Admin")
            .DisableAntiforgery();

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

    // ── Regex para extraer el sufijo de 4 dígitos de la tarjeta del nombre del archivo ──
    // Ejemplo: "BAC-5466-37XX-XXXX-8608-202603-Financiamientos.xls" → "8608"
    private static readonly Regex CardSuffixRegex =
        new(@"-(\d{4})-\d{6}-Financiamientos", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static async Task<Results<Accepted<SyncBacFinanciamientosResponse>, BadRequest<ProblemDetails>>> SyncBacFinanciamientos(
        HttpContext                  ctx,
        IBackgroundJobClient         backgroundJobs,
        IDistributedCache            cache,
        ClaimsPrincipal              user,
        CancellationToken            ct)
    {
        var files = ctx.Request.Form.Files;
        if (files.Count == 0)
            return TypedResults.BadRequest(new ProblemDetails { Detail = "Debe adjuntar al menos un archivo XLS de Financiamientos BAC." });

        var syncId  = Guid.NewGuid().ToString("N");
        var entries = new List<BacTcFileEntry>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            // Extraer sufijo de tarjeta del nombre del archivo
            var match = CardSuffixRegex.Match(file.FileName);
            if (!match.Success)
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Detail = $"El archivo '{file.FileName}' no tiene el sufijo de tarjeta esperado. " +
                             "Nombre esperado: BAC-XXXX-XXXX-XXXX-{{sufijo}}-YYYYMM-Financiamientos.xls"
                });

            var cardSuffix = match.Groups[1].Value;
            var redisKey   = FinancialObligationBacTcSyncJob.BuildRedisKey(syncId, cardSuffix);

            // Guardar bytes en Redis (TTL 30 min)
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            await cache.SetAsync(redisKey, ms.ToArray(),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
                }, ct);

            entries.Add(new BacTcFileEntry(cardSuffix, redisKey));
        }

        if (entries.Count == 0)
            return TypedResults.BadRequest(new ProblemDetails { Detail = "No se procesó ningún archivo válido." });

        // Encolar job de Hangfire
        var jobId = backgroundJobs.Enqueue<FinancialObligationBacTcSyncJob>(
            job => job.ProcessAsync(syncId, entries));

        var response = new SyncBacFinanciamientosResponse(
            SyncId:         syncId,
            JobId:          jobId,
            FilesSubmitted: entries.Count,
            Status:         "Enqueued");

        return TypedResults.Accepted((string?)null, response);
    }
}
