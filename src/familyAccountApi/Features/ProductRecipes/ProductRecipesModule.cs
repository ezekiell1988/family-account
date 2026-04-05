using FamilyAccountApi.Features.ProductRecipes.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.ProductRecipes;

public static class ProductRecipesModule
{
    public static IServiceCollection AddProductRecipesModule(this IServiceCollection services)
    {
        services.AddScoped<IProductRecipeService, ProductRecipeService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/product-recipes")
            .WithTags("ProductRecipes")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllProductRecipes")
            .WithSummary("Obtener todas las recetas de producción");

        group.MapGet("/by-output/{idProductOutput:int}.json", GetByProductOutput)
            .WithName("GetProductRecipesByOutput")
            .WithSummary("Obtener recetas de un producto output");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetProductRecipeById")
            .WithSummary("Obtener receta por ID con sus ingredientes");

        group.MapPost("/", Create)
            .WithName("CreateProductRecipe")
            .WithSummary("Crear nueva receta de producción")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProductRecipe")
            .WithSummary("Actualizar receta de producción")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProductRecipe")
            .WithSummary("Eliminar receta de producción")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductRecipeResponse>>> GetAll(
        IProductRecipeService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Ok<IReadOnlyList<ProductRecipeResponse>>> GetByProductOutput(
        int idProductOutput, IProductRecipeService service, CancellationToken ct)
    {
        var items = await service.GetByProductOutputAsync(idProductOutput, ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<ProductRecipeResponse>, NotFound>> GetById(
        int id, IProductRecipeService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ProductRecipeResponse>, ValidationProblem>> Create(
        CreateProductRecipeRequest request, IProductRecipeService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/product-recipes/{item.IdProductRecipe}.json", item);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [""] = [ex.Message] });
        }
    }

    private static async Task<Results<Ok<ProductRecipeResponse>, NotFound, ValidationProblem>> Update(
        int id, UpdateProductRecipeRequest request, IProductRecipeService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [""] = [ex.Message] });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IProductRecipeService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
