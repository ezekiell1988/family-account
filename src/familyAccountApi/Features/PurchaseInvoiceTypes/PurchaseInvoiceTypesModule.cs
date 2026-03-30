using FamilyAccountApi.Features.PurchaseInvoiceTypes.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.PurchaseInvoiceTypes;

public static class PurchaseInvoiceTypesModule
{
    public static IServiceCollection AddPurchaseInvoiceTypesModule(this IServiceCollection services)
    {
        services.AddScoped<IPurchaseInvoiceTypeService, PurchaseInvoiceTypeService>();
        return services;
    }

    public static IEndpointRouteBuilder MapPurchaseInvoiceTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/purchase-invoice-types")
            .WithTags("PurchaseInvoiceTypes")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllPurchaseInvoiceTypes")
            .WithSummary("Obtener todos los tipos de factura de compra");

        group.MapGet("/active.json", GetActive)
            .WithName("GetActivePurchaseInvoiceTypes")
            .WithSummary("Obtener tipos de factura de compra activos");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetPurchaseInvoiceTypeById")
            .WithSummary("Obtener tipo de factura de compra por ID");

        group.MapPost("/", Create)
            .WithName("CreatePurchaseInvoiceType")
            .WithSummary("Crear tipo de factura de compra")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdatePurchaseInvoiceType")
            .WithSummary("Actualizar tipo de factura de compra")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeletePurchaseInvoiceType")
            .WithSummary("Eliminar tipo de factura de compra")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<PurchaseInvoiceTypeResponse>>> GetAll(
        IPurchaseInvoiceTypeService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetAllAsync(ct));

    private static async Task<Ok<IReadOnlyList<PurchaseInvoiceTypeResponse>>> GetActive(
        IPurchaseInvoiceTypeService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetActiveAsync(ct));

    private static async Task<Results<Ok<PurchaseInvoiceTypeResponse>, NotFound>> GetById(
        int id, IPurchaseInvoiceTypeService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<PurchaseInvoiceTypeResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreatePurchaseInvoiceTypeRequest request, IPurchaseInvoiceTypeService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/purchase-invoice-types/{item.IdPurchaseInvoiceType}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_purchaseInvoiceType_codePurchaseInvoiceType") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un tipo de factura con el código '{request.CodePurchaseInvoiceType}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<PurchaseInvoiceTypeResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id, UpdatePurchaseInvoiceTypeRequest request, IPurchaseInvoiceTypeService service, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, request, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IPurchaseInvoiceTypeService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
