using FamilyAccountApi.Features.SalesOrders.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.SalesOrders;

public static class SalesOrdersModule
{
    public static IServiceCollection AddSalesOrdersModule(this IServiceCollection services)
    {
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        return services;
    }

    public static IEndpointRouteBuilder MapSalesOrdersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/sales-orders").RequireAuthorization();

        group.MapGet("/data.json", async (
            ISalesOrderService svc, CancellationToken ct) =>
        {
            var result = await svc.GetAllAsync(ct);
            return Results.Ok(result);
        }).WithTags("SalesOrders");

        group.MapGet("/by-period/{idFiscalPeriod:int}.json", async (
            int idFiscalPeriod, ISalesOrderService svc, CancellationToken ct) =>
        {
            var result = await svc.GetByFiscalPeriodAsync(idFiscalPeriod, ct);
            return Results.Ok(result);
        }).WithTags("SalesOrders");

        group.MapGet("/{id:int}.json", async (
            int id, ISalesOrderService svc, CancellationToken ct) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("SalesOrders");

        group.MapPost("/", async (
            [FromBody] CreateSalesOrderRequest request,
            ISalesOrderService svc, CancellationToken ct) =>
        {
            try
            {
                var result = await svc.CreateAsync(request, ct);
                return Results.Created($"/sales-orders/{result.IdSalesOrder}.json", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [ex.Message] });
            }
        }).WithTags("SalesOrders");

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] UpdateSalesOrderRequest request,
            ISalesOrderService svc, CancellationToken ct) =>
        {
            try
            {
                var result = await svc.UpdateAsync(id, request, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [ex.Message] });
            }
        }).WithTags("SalesOrders");

        group.MapPost("/{id:int}/confirm", async (
            int id, ISalesOrderService svc, CancellationToken ct) =>
        {
            var (ok, error) = await svc.ConfirmAsync(id, ct);
            return ok ? Results.Ok() : Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [error!] });
        }).WithTags("SalesOrders");

        group.MapPost("/{id:int}/cancel", async (
            int id, ISalesOrderService svc, CancellationToken ct) =>
        {
            var (ok, error) = await svc.CancelAsync(id, ct);
            return ok ? Results.Ok() : Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [error!] });
        }).WithTags("SalesOrders");

        group.MapDelete("/{id:int}", async (
            int id, ISalesOrderService svc, CancellationToken ct) =>
        {
            try
            {
                var deleted = await svc.DeleteAsync(id, ct);
                return deleted ? Results.NoContent() : Results.NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [ex.Message] });
            }
        }).WithTags("SalesOrders");

        // ── Fulfillments ──────────────────────────────────────────────────────
        group.MapGet("/{id:int}/fulfillments.json", async (
            int id, ISalesOrderService svc, CancellationToken ct) =>
        {
            var result = await svc.GetFulfillmentsAsync(id, ct);
            return Results.Ok(result);
        }).WithTags("SalesOrders");

        group.MapPost("/{id:int}/fulfillments", async (
            int id,
            [FromBody] AddFulfillmentRequest request,
            ISalesOrderService svc, CancellationToken ct) =>
        {
            var (result, error) = await svc.AddFulfillmentAsync(id, request, ct);
            return result is not null
                ? Results.Created($"/sales-orders/{id}/fulfillments.json", result)
                : Results.ValidationProblem(new Dictionary<string, string[]> { ["fulfillment"] = [error!] });
        }).WithTags("SalesOrders");

        group.MapDelete("/fulfillments/{idFulfillment:int}", async (
            int idFulfillment, ISalesOrderService svc, CancellationToken ct) =>
        {
            var deleted = await svc.RemoveFulfillmentAsync(idFulfillment, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithTags("SalesOrders");

        // ── Advances ──────────────────────────────────────────────────────────
        group.MapGet("/{id:int}/advances.json", async (
            int id, ISalesOrderService svc, CancellationToken ct) =>
        {
            var result = await svc.GetAdvancesAsync(id, ct);
            return Results.Ok(result);
        }).WithTags("SalesOrders");

        group.MapPost("/{id:int}/advances", async (
            int id,
            [FromBody] CreateSalesOrderAdvanceRequest request,
            ISalesOrderService svc, CancellationToken ct) =>
        {
            var (result, error) = await svc.AddAdvanceAsync(id, request, ct);
            return result is not null
                ? Results.Created($"/sales-orders/{id}/advances.json", result)
                : Results.ValidationProblem(new Dictionary<string, string[]> { ["advance"] = [error!] });
        }).WithTags("SalesOrders");

        group.MapDelete("/advances/{idAdvance:int}", async (
            int idAdvance, ISalesOrderService svc, CancellationToken ct) =>
        {
            var deleted = await svc.RemoveAdvanceAsync(idAdvance, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithTags("SalesOrders");

        return endpoints;
    }
}
