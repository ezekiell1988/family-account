using FamilyAccountApi.Features.BankStatementTemplates.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.BankStatementTemplates;

public static class BankStatementTemplatesModule
{
    public static IServiceCollection AddBankStatementTemplatesModule(this IServiceCollection services)
    {
        services.AddScoped<IBankStatementTemplateService, BankStatementTemplateService>();
        return services;
    }

    public static IEndpointRouteBuilder MapBankStatementTemplatesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/bank-statement-templates")
            .WithTags("Bank Statement Templates")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllBankStatementTemplates")
            .WithSummary("Obtener todas las plantillas de extractos bancarios");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetBankStatementTemplateById")
            .WithSummary("Obtener plantilla por ID");

        group.MapPost("/", Create)
            .WithName("CreateBankStatementTemplate")
            .WithSummary("Crear nueva plantilla")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateBankStatementTemplate")
            .WithSummary("Actualizar plantilla")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteBankStatementTemplate")
            .WithSummary("Eliminar plantilla")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<BankStatementTemplateResponse>>> GetAll(
        IBankStatementTemplateService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<BankStatementTemplateResponse>, NotFound>> GetById(
        int id, IBankStatementTemplateService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<BankStatementTemplateResponse>, ValidationProblem, Conflict<ProblemDetails>>> Create(
        CreateBankStatementTemplateRequest request, IBankStatementTemplateService service, CancellationToken ct)
    {
        try
        {
            var created = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/bank-statement-templates/{created.IdBankStatementTemplate}", created);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_bankStatementTemplate_codeTemplate") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe una plantilla con el código '{request.CodeTemplate}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<BankStatementTemplateResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id, UpdateBankStatementTemplateRequest request, IBankStatementTemplateService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_bankStatementTemplate_codeTemplate") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe una plantilla con el código '{request.CodeTemplate}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IBankStatementTemplateService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
