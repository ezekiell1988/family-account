using FamilyAccountApi.Features.SalesInvoiceTypes.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.SalesInvoiceTypes;

public static class SalesInvoiceTypesModule
{
    public static IServiceCollection AddSalesInvoiceTypesModule(this IServiceCollection services)
    {
        services.AddScoped<ISalesInvoiceTypeService, SalesInvoiceTypeService>();
        return services;
    }

    public static IEndpointRouteBuilder MapSalesInvoiceTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sales-invoice-types")
            .WithTags("SalesInvoiceTypes")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllSalesInvoiceTypes")
            .WithSummary("Obtener todos los tipos de factura de venta");

        group.MapGet("/active.json", GetActive)
            .WithName("GetActiveSalesInvoiceTypes")
            .WithSummary("Obtener tipos de factura de venta activos");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetSalesInvoiceTypeById")
            .WithSummary("Obtener tipo de factura de venta por ID");

        group.MapPost("/", Create)
            .WithName("CreateSalesInvoiceType")
            .WithSummary("Crear tipo de factura de venta")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateSalesInvoiceType")
            .WithSummary("Actualizar tipo de factura de venta")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteSalesInvoiceType")
            .WithSummary("Eliminar tipo de factura de venta")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<SalesInvoiceTypeResponse>>> GetAll(
        ISalesInvoiceTypeService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetAllAsync(ct));

    private static async Task<Ok<IReadOnlyList<SalesInvoiceTypeResponse>>> GetActive(
        ISalesInvoiceTypeService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetActiveAsync(ct));

    private static async Task<Results<Ok<SalesInvoiceTypeResponse>, NotFound>> GetById(
        int id, ISalesInvoiceTypeService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<SalesInvoiceTypeResponse>, Conflict<ProblemDetails>>> Create(
        CreateSalesInvoiceTypeRequest request, ISalesInvoiceTypeService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/sales-invoice-types/{item.IdSalesInvoiceType}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_salesInvoiceType_codeSalesInvoiceType") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un tipo de factura de venta con el código '{request.CodeSalesInvoiceType}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<SalesInvoiceTypeResponse>, NotFound>> Update(
        int id, UpdateSalesInvoiceTypeRequest request, ISalesInvoiceTypeService service, CancellationToken ct)
    {
        var item = await service.UpdateAsync(id, request, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, ISalesInvoiceTypeService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
