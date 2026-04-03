using FamilyAccountApi.Features.ProductUnits.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductUnits;

public static class ProductUnitsModule
{
    public static IServiceCollection AddProductUnitsModule(this IServiceCollection services)
    {
        services.AddScoped<IProductUnitService, ProductUnitService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductUnitsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/product-units")
            .WithTags("ProductUnits")
            .RequireAuthorization();

        group.MapGet("/by-product/{idProduct:int}.json", GetByProduct)
            .WithName("GetProductUnitsByProduct")
            .WithSummary("Obtener presentaciones de un producto");

        group.MapGet("/barcode/{barcode}.json", GetByBarcode)
            .WithName("GetProductUnitByBarcode")
            .WithSummary("Buscar presentación por código de barras EAN");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetProductUnitById")
            .WithSummary("Obtener presentación por ID");

        group.MapPost("/", Create)
            .WithName("CreateProductUnit")
            .WithSummary("Crear nueva presentación de producto")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProductUnit")
            .WithSummary("Actualizar presentación de producto")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProductUnit")
            .WithSummary("Eliminar presentación de producto")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductUnitResponse>>> GetByProduct(
        int idProduct, IProductUnitService service, CancellationToken ct)
    {
        var items = await service.GetByProductAsync(idProduct, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ProductUnitResponse>, NotFound>> GetByBarcode(
        string barcode, IProductUnitService service, CancellationToken ct)
    {
        var item = await service.GetByBarcodeAsync(barcode, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Ok<ProductUnitResponse>, NotFound>> GetById(
        int id, IProductUnitService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ProductUnitResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateProductUnitRequest request, IProductUnitService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/product-units/{item.IdProductUnit}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productUnit_idProduct_idUnit") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Presentación duplicada",
                Detail = "Este producto ya tiene una presentación con esa unidad de medida.",
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productUnit_codeBarcode") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código de barras duplicado",
                Detail = $"El código de barras '{request.CodeBarcode}' ya está registrado en otra presentación.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<ProductUnitResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id, UpdateProductUnitRequest request, IProductUnitService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productUnit_codeBarcode") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código de barras duplicado",
                Detail = $"El código de barras '{request.CodeBarcode}' ya está registrado en otra presentación.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IProductUnitService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
