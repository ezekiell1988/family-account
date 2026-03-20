using FamilyAccountApi.Features.BankStatementImports.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FamilyAccountApi.Features.BankStatementImports;

public static class BankStatementImportsModule
{
    public static IServiceCollection AddBankStatementImportsModule(this IServiceCollection services)
    {
        services.AddScoped<IBankStatementImportService, BankStatementImportService>();
        return services;
    }

    public static IEndpointRouteBuilder MapBankStatementImportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bank-statement-imports")
            .WithTags("Bank Statement Imports")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllBankStatementImports")
            .WithSummary("Obtener todas las importaciones de extractos bancarios");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetBankStatementImportById")
            .WithSummary("Obtener importación por ID");

        group.MapPost("/", Create)
            .WithName("CreateBankStatementImport")
            .WithSummary("Crear nueva importación")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateBankStatementImport")
            .WithSummary("Actualizar importación")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteBankStatementImport")
            .WithSummary("Eliminar importación")
            .RequireAuthorization("Admin");

        group.MapPost("/upload/{idBankAccount:int}/{idBankStatementTemplate:int}", Upload)
            .WithName("UploadBankStatement")
            .WithSummary("Cargar extracto bancario en formato HTML-XLS (BCR)")
            .RequireAuthorization("Admin")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<Ok<IReadOnlyList<BankStatementImportResponse>>> GetAll(
        IBankStatementImportService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<BankStatementImportResponse>, NotFound>> GetById(
        int id, IBankStatementImportService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<BankStatementImportResponse>, ValidationProblem, BadRequest<ProblemDetails>>> Create(
        CreateBankStatementImportRequest request, IBankStatementImportService service, ClaimsPrincipal user, CancellationToken ct)
    {
        try
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return TypedResults.BadRequest(new ProblemDetails
                {
                    Title = "Usuario no identificado",
                    Detail = "No se pudo obtener el ID del usuario del token.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var created = await service.CreateAsync(request, userId, ct);
            return TypedResults.Created($"/bank-statement-imports/{created.IdBankStatementImport}", created);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Validación fallida",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }

    private static async Task<Results<Ok<BankStatementImportResponse>, NotFound, ValidationProblem>> Update(
        int id, UpdateBankStatementImportRequest request, IBankStatementImportService service, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, request, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IBankStatementImportService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<Created<BankStatementImportResponse>, BadRequest<ProblemDetails>>> Upload(
        int                  idBankAccount,
        int                  idBankStatementTemplate,
        [FromForm] IFormFile file,
        IBankStatementImportService service,
        ClaimsPrincipal user,
        CancellationToken ct)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title  = "Usuario no identificado",
                Detail = "No se pudo obtener el ID del usuario del token.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (file.Length == 0)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title  = "Archivo vacío",
                Detail = "El archivo enviado no contiene datos.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            var result = await service.UploadAsync(file, idBankAccount, idBankStatementTemplate, userId, ct);
            return TypedResults.Created($"/bank-statement-imports/{result.IdBankStatementImport}", result);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title  = "Error de importación",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    }
}
