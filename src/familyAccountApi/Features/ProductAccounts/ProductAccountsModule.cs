using FamilyAccountApi.Features.ProductAccounts.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductAccounts;

public static class ProductAccountsModule
{
    public static IServiceCollection AddProductAccountsModule(this IServiceCollection services)
    {
        services.AddScoped<IProductAccountService, ProductAccountService>();
        return services;
    }

    public static IEndpointRouteBuilder MapProductAccountsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/product-accounts")
            .WithTags("ProductAccounts")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllProductAccounts")
            .WithSummary("Obtener todas las distribuciones contables de productos");

        group.MapGet("/by-product/{idProduct:int}.json", GetByProduct)
            .WithName("GetProductAccountsByProduct")
            .WithSummary("Obtener distribuciones contables de un producto");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetProductAccountById")
            .WithSummary("Obtener distribución contable por ID");

        group.MapPost("/", Create)
            .WithName("CreateProductAccount")
            .WithSummary("Crear distribución contable de producto")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateProductAccount")
            .WithSummary("Actualizar distribución contable de producto")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteProductAccount")
            .WithSummary("Eliminar distribución contable de producto")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ProductAccountResponse>>> GetAll(
        IProductAccountService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetAllAsync(ct));

    private static async Task<Ok<IReadOnlyList<ProductAccountResponse>>> GetByProduct(
        int idProduct, IProductAccountService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetByProductAsync(idProduct, ct));

    private static async Task<Results<Ok<ProductAccountResponse>, NotFound>> GetById(
        int id, IProductAccountService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ProductAccountResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateProductAccountRequest request, IProductAccountService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/product-accounts/{item.IdProductAccount}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productAccount_idProduct_idAccount_idCostCenter") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Distribución duplicada",
                Detail = "Ya existe una distribución contable para este producto, cuenta y centro de costo.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<ProductAccountResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id, UpdateProductAccountRequest request, IProductAccountService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_productAccount_idProduct_idAccount_idCostCenter") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Distribución duplicada",
                Detail = "Ya existe una distribución contable para este producto, cuenta y centro de costo.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IProductAccountService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
