using FamilyAccountApi.Features.ProductTypes.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductTypes;

public static class ProductTypesModule
{
    public static IServiceCollection AddProductTypesModule(this IServiceCollection services)
    {
        services.AddScoped<IProductTypeService, ProductTypeService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductTypesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/product-types")
            .WithTags("ProductTypes")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllProductTypes")
            .WithSummary("Obtener todos los tipos de producto");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetProductTypeById")
            .WithSummary("Obtener tipo de producto por ID");

        group.MapPost("/", Create)
            .WithName("CreateProductType")
            .WithSummary("Crear nuevo tipo de producto")
            .RequireAuthorization("Developer");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProductType")
            .WithSummary("Actualizar tipo de producto")
            .RequireAuthorization("Developer");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProductType")
            .WithSummary("Eliminar tipo de producto")
            .RequireAuthorization("Developer");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductTypeResponse>>> GetAll(
        IProductTypeService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ProductTypeResponse>, NotFound>> GetById(
        int id, IProductTypeService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ProductTypeResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateProductTypeRequest request, IProductTypeService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/product-types/{item.IdProductType}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productType_nameProductType") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Nombre duplicado",
                Detail = $"Ya existe un tipo de producto con el nombre '{request.NameProductType}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<ProductTypeResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id, UpdateProductTypeRequest request, IProductTypeService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productType_nameProductType") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Nombre duplicado",
                Detail = $"Ya existe un tipo de producto con el nombre '{request.NameProductType}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IProductTypeService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
