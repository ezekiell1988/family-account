using FamilyAccountApi.Features.SalesInvoices.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.SalesInvoices;

public static class SalesInvoicesModule
{
    public static IServiceCollection AddSalesInvoicesModule(this IServiceCollection services)
    {
        services.AddScoped<ISalesInvoiceService, SalesInvoiceService>();
        return services;
    }

    public static IEndpointRouteBuilder MapSalesInvoicesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/sales-invoices").RequireAuthorization();

        group.MapGet("/data.json", async (
            ISalesInvoiceService svc,
            CancellationToken ct) =>
        {
            var result = await svc.GetAllAsync(ct);
            return Results.Ok(result);
        }).WithTags("SalesInvoices");

        group.MapGet("/by-period/{idFiscalPeriod:int}.json", async (
            int idFiscalPeriod,
            ISalesInvoiceService svc,
            CancellationToken ct) =>
        {
            var result = await svc.GetByFiscalPeriodAsync(idFiscalPeriod, ct);
            return Results.Ok(result);
        }).WithTags("SalesInvoices");

        group.MapGet("/{id:int}.json", async (
            int id,
            ISalesInvoiceService svc,
            CancellationToken ct) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("SalesInvoices");

        group.MapPost("/", async (
            [FromBody] CreateSalesInvoiceRequest request,
            ISalesInvoiceService svc,
            CancellationToken ct) =>
        {
            try
            {
                var result = await svc.CreateAsync(request, ct);
                return Results.Created($"/sales-invoices/{result.IdSalesInvoice}.json", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["lines"] = [ex.Message]
                });
            }
        }).WithTags("SalesInvoices");

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] UpdateSalesInvoiceRequest request,
            ISalesInvoiceService svc,
            CancellationToken ct) =>
        {
            try
            {
                var result = await svc.UpdateAsync(id, request, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["lines"] = [ex.Message]
                });
            }
        }).WithTags("SalesInvoices");

        group.MapDelete("/{id:int}", async (
            int id,
            ISalesInvoiceService svc,
            CancellationToken ct) =>
        {
            try
            {
                var deleted = await svc.DeleteAsync(id, ct);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        }).WithTags("SalesInvoices");

        group.MapPost("/{id:int}/confirm", async (
            int id,
            ISalesInvoiceService svc,
            CancellationToken ct) =>
        {
            var (success, error, invoice) = await svc.ConfirmAsync(id, ct);
            if (!success) return Results.UnprocessableEntity(new { error });
            return Results.Ok(invoice);
        }).WithTags("SalesInvoices").RequireAuthorization("Admin");

        group.MapPost("/{id:int}/cancel", async (
            int id,
            ISalesInvoiceService svc,
            CancellationToken ct) =>
        {
            var (result, conflict) = await svc.CancelAsync(id, ct);
            if (conflict is not null) return Results.Conflict(new { error = conflict });
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("SalesInvoices").RequireAuthorization("Admin");

        return endpoints;
    }
}
