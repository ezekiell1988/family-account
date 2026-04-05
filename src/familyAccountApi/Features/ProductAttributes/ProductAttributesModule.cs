using FamilyAccountApi.Features.ProductAttributes.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductAttributes;

public static class ProductAttributesModule
{
    public static IServiceCollection AddProductAttributesModule(this IServiceCollection services)
    {
        services.AddScoped<IProductAttributeService, ProductAttributeService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductAttributesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/products/{productId:int}/attributes")
            .WithTags("ProductAttributes")
            .RequireAuthorization();

        // T2.1 — GET /products/{id}/attributes
        group.MapGet("/data.json", GetAll)
            .WithName("GetProductAttributes")
            .WithSummary("Obtener atributos y valores del producto padre");

        // T2.2 — POST /products/{id}/attributes
        group.MapPost("/", CreateAttribute)
            .WithName("CreateProductAttribute")
            .WithSummary("Crear nuevo atributo para el producto padre")
            .RequireAuthorization("Admin");

        // T2.3 — PUT /products/{id}/attributes/{attrId}
        group.MapPut("/{attrId:int}", UpdateAttribute)
            .WithName("UpdateProductAttribute")
            .WithSummary("Renombrar o cambiar orden de un atributo")
            .RequireAuthorization("Admin");

        // T2.4 — DELETE /products/{id}/attributes/{attrId}
        group.MapDelete("/{attrId:int}", DeleteAttribute)
            .WithName("DeleteProductAttribute")
            .WithSummary("Eliminar atributo (error si hay variantes que lo usan)")
            .RequireAuthorization("Admin");

        // T2.5 — POST /products/{id}/attributes/{attrId}/values
        group.MapPost("/{attrId:int}/values", CreateValue)
            .WithName("CreateAttributeValue")
            .WithSummary("Agregar un valor al atributo (ej: M, L, Azul)")
            .RequireAuthorization("Admin");

        // T2.6 — PUT /products/{id}/attributes/{attrId}/values/{valueId}
        group.MapPut("/{attrId:int}/values/{valueId:int}", UpdateValue)
            .WithName("UpdateAttributeValue")
            .WithSummary("Editar nombre u orden de un valor de atributo")
            .RequireAuthorization("Admin");

        // T2.7 — DELETE /products/{id}/attributes/{attrId}/values/{valueId}
        group.MapDelete("/{attrId:int}/values/{valueId:int}", DeleteValue)
            .WithName("DeleteAttributeValue")
            .WithSummary("Eliminar un valor de atributo (cascade a productVariantAttribute)")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductAttributeResponse>>> GetAll(
        int productId,
        IProductAttributeService service,
        CancellationToken ct)
    {
        var items = await service.GetAllByProductAsync(productId, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Created<ProductAttributeResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> CreateAttribute(
        int productId,
        CreateProductAttributeRequest request,
        IProductAttributeService service,
        CancellationToken ct)
    {
        try
        {
            var (attr, error) = await service.CreateAttributeAsync(productId, request, ct);
            if (error == "Producto no encontrado.")
                return TypedResults.NotFound();

            return TypedResults.Created(
                $"/api/v1/products/{productId}/attributes/{attr!.IdProductAttribute}.json",
                attr);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productAttribute_idProduct_nameAttribute") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Atributo duplicado",
                Detail = $"El producto ya tiene un atributo llamado '{request.NameAttribute}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<ProductAttributeResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> UpdateAttribute(
        int productId,
        int attrId,
        UpdateProductAttributeRequest request,
        IProductAttributeService service,
        CancellationToken ct)
    {
        try
        {
            var (attr, _) = await service.UpdateAttributeAsync(productId, attrId, request, ct);
            return attr is not null
                ? TypedResults.Ok(attr)
                : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productAttribute_idProduct_nameAttribute") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Atributo duplicado",
                Detail = $"El producto ya tiene un atributo llamado '{request.NameAttribute}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> DeleteAttribute(
        int productId,
        int attrId,
        IProductAttributeService service,
        CancellationToken ct)
    {
        var (deleted, conflict) = await service.DeleteAttributeAsync(productId, attrId, ct);

        if (!deleted && conflict is not null)
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "No se puede eliminar",
                Detail = conflict,
                Status = StatusCodes.Status409Conflict
            });

        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }

    private static async Task<Results<Created<AttributeValueResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> CreateValue(
        int productId,
        int attrId,
        CreateAttributeValueRequest request,
        IProductAttributeService service,
        CancellationToken ct)
    {
        try
        {
            var (value, error) = await service.CreateValueAsync(productId, attrId, request, ct);
            if (error == "Atributo no encontrado.")
                return TypedResults.NotFound();

            return TypedResults.Created(
                $"/api/v1/products/{productId}/attributes/{attrId}/values/{value!.IdAttributeValue}.json",
                value);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_attributeValue_idProductAttribute_nameValue") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Valor duplicado",
                Detail = $"El atributo ya tiene un valor llamado '{request.NameValue}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<AttributeValueResponse>, NotFound, Conflict<ProblemDetails>, ValidationProblem>> UpdateValue(
        int productId,
        int attrId,
        int valueId,
        UpdateAttributeValueRequest request,
        IProductAttributeService service,
        CancellationToken ct)
    {
        try
        {
            var (value, _) = await service.UpdateValueAsync(productId, attrId, valueId, request, ct);
            return value is not null
                ? TypedResults.Ok(value)
                : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_attributeValue_idProductAttribute_nameValue") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Valor duplicado",
                Detail = $"El atributo ya tiene un valor llamado '{request.NameValue}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteValue(
        int productId,
        int attrId,
        int valueId,
        IProductAttributeService service,
        CancellationToken ct)
    {
        var deleted = await service.DeleteValueAsync(productId, attrId, valueId, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
