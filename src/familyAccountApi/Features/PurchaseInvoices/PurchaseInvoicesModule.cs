using FamilyAccountApi.Features.PurchaseInvoices.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.PurchaseInvoices;

public static class PurchaseInvoicesModule
{
    public static IServiceCollection AddPurchaseInvoicesModule(this IServiceCollection services)
    {
        services.AddScoped<IPurchaseInvoiceService, PurchaseInvoiceService>();
        return services;
    }

    public static IEndpointRouteBuilder MapPurchaseInvoicesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/purchase-invoices").RequireAuthorization();

        group.MapGet("/data.json", async (
            IPurchaseInvoiceService svc,
            CancellationToken ct) =>
        {
            var result = await svc.GetAllAsync(ct);
            return Results.Ok(result);
        }).WithTags("PurchaseInvoices");

        group.MapGet("/by-period/{idFiscalPeriod:int}.json", async (
            int idFiscalPeriod,
            IPurchaseInvoiceService svc,
            CancellationToken ct) =>
        {
            var result = await svc.GetByFiscalPeriodAsync(idFiscalPeriod, ct);
            return Results.Ok(result);
        }).WithTags("PurchaseInvoices");

        group.MapGet("/{id:int}.json", async (
            int id,
            IPurchaseInvoiceService svc,
            CancellationToken ct) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("PurchaseInvoices");

        group.MapPost("/", async (
            [FromBody] CreatePurchaseInvoiceRequest request,
            IPurchaseInvoiceService svc,
            CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(request, ct);
            return Results.Created($"/purchase-invoices/{result.IdPurchaseInvoice}.json", result);
        }).WithTags("PurchaseInvoices");

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] UpdatePurchaseInvoiceRequest request,
            IPurchaseInvoiceService svc,
            CancellationToken ct) =>
        {
            var result = await svc.UpdateAsync(id, request, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("PurchaseInvoices");

        group.MapDelete("/{id:int}", async (
            int id,
            IPurchaseInvoiceService svc,
            CancellationToken ct) =>
        {
            var deleted = await svc.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithTags("PurchaseInvoices");

        group.MapPost("/{id:int}/confirm", async (
            int id,
            IPurchaseInvoiceService svc,
            CancellationToken ct) =>
        {
            var (success, error, invoice) = await svc.ConfirmAsync(id, ct);
            if (!success) return Results.UnprocessableEntity(new { error });
            return Results.Ok(invoice);
        }).WithTags("PurchaseInvoices").RequireAuthorization("Admin");

        group.MapPost("/{id:int}/cancel", async (
            int id,
            IPurchaseInvoiceService svc,
            CancellationToken ct) =>
        {
            var result = await svc.CancelAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("PurchaseInvoices").RequireAuthorization("Admin");

        return endpoints;
    }
}
