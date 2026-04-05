using FamilyAccountApi.Features.PriceLists.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.PriceLists;

public static class PriceListsModule
{
    public static IServiceCollection AddPriceListsModule(this IServiceCollection services)
    {
        services.AddScoped<IPriceListService, PriceListService>();
        return services;
    }

    public static IEndpointRouteBuilder MapPriceListsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/price-lists").RequireAuthorization();

        group.MapGet("/data.json", async (
            IPriceListService svc, CancellationToken ct) =>
        {
            var result = await svc.GetAllAsync(ct);
            return Results.Ok(result);
        }).WithTags("PriceLists");

        group.MapGet("/{id:int}.json", async (
            int id, IPriceListService svc, CancellationToken ct) =>
        {
            var result = await svc.GetByIdAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("PriceLists");

        group.MapGet("/by-product/{idProduct:int}.json", async (
            int idProduct, IPriceListService svc, CancellationToken ct) =>
        {
            var result = await svc.GetItemsByProductAsync(idProduct, ct);
            return Results.Ok(result);
        }).WithTags("PriceLists");

        group.MapPost("/", async (
            [FromBody] CreatePriceListRequest request,
            IPriceListService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(request, ct);
            return Results.Created($"/price-lists/{result.IdPriceList}.json", result);
        }).WithTags("PriceLists");

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] UpdatePriceListRequest request,
            IPriceListService svc, CancellationToken ct) =>
        {
            var result = await svc.UpdateAsync(id, request, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }).WithTags("PriceLists");

        group.MapDelete("/{id:int}", async (
            int id, IPriceListService svc, CancellationToken ct) =>
        {
            var deleted = await svc.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithTags("PriceLists");

        return endpoints;
    }
}
