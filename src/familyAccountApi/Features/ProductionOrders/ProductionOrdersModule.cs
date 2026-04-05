using FamilyAccountApi.Features.ProductionOrders.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.ProductionOrders;

public static class ProductionOrdersModule
{
    public static IServiceCollection AddProductionOrdersModule(this IServiceCollection services)
    {
        services.AddScoped<IProductionOrderService, ProductionOrderService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductionOrdersEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/production-orders").RequireAuthorization();

        group.MapGet("/data.json", async (
            IProductionOrderService svc, CancellationToken ct) =>
        {
            var result = await svc.GetAllAsync(ct);
            return Results.Ok(result);
        }).WithTags("ProductionOrders");

        group.MapGet("/by-period/{idFiscalPeriod:int}.json", async (
            int idFiscalPeriod, IProductionOrderService svc, CancellationToken ct) =>
        {
            var result = await svc.GetByFiscalPeriodAsync(idFiscalPeriod, ct);
            return Results.Ok(result);
        }).WithTags("ProductionOrders");

        group.MapGet("/by-sales-order/{idSalesOrder:int}.json", async (
            int idSalesOrder, IProductionOrderService svc, CancellationToken ct) =>
        {
            var result = await svc.GetBySalesOrderAsync(idSalesOrder, ct);
            return Results.Ok(result);
        }).WithTags("ProductionOrders");

        group.MapGet("/{id:int}.json", async (
            int id, IProductionOrderService svc, CancellationToken ct) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("ProductionOrders");

        group.MapPost("/", async (
            [FromBody] CreateProductionOrderRequest request,
            IProductionOrderService svc, CancellationToken ct) =>
        {
            try
            {
                var result = await svc.CreateAsync(request, ct);
                return Results.Created($"/production-orders/{result.IdProductionOrder}.json", result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [ex.Message] });
            }
        }).WithTags("ProductionOrders");

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] UpdateProductionOrderRequest request,
            IProductionOrderService svc, CancellationToken ct) =>
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
        }).WithTags("ProductionOrders");

        group.MapPatch("/{id:int}/status", async (
            int id,
            [FromBody] UpdateProductionOrderStatusRequest request,
            IProductionOrderService svc, CancellationToken ct) =>
        {
            var (ok, error) = await svc.UpdateStatusAsync(id, request, ct);
            return ok ? Results.Ok() : Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = [error!] });
        }).WithTags("ProductionOrders");

        group.MapDelete("/{id:int}", async (
            int id, IProductionOrderService svc, CancellationToken ct) =>
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
        }).WithTags("ProductionOrders");

        return endpoints;
    }
}
