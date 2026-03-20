using FamilyAccountApi.Features.CostCenters.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.CostCenters;

public static class CostCentersModule
{
    public static IServiceCollection AddCostCentersModule(this IServiceCollection services)
    {
        services.AddScoped<ICostCenterService, CostCenterService>();
        return services;
    }

    public static IEndpointRouteBuilder MapCostCentersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/cost-centers")
            .WithTags("CostCenters")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllCostCenters")
            .WithSummary("Obtener todos los centros de costo");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetCostCenterById")
            .WithSummary("Obtener centro de costo por ID");

        group.MapPost("/", Create)
            .WithName("CreateCostCenter")
            .WithSummary("Crear nuevo centro de costo")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateCostCenter")
            .WithSummary("Actualizar centro de costo")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteCostCenter")
            .WithSummary("Eliminar centro de costo")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<CostCenterResponse>>> GetAll(
        ICostCenterService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<CostCenterResponse>, NotFound>> GetById(
        int id, ICostCenterService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<CostCenterResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateCostCenterRequest request, ICostCenterService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/cost-centers/{item.IdCostCenter}", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_costCenter_codeCostCenter") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un centro de costo con el código '{request.CodeCostCenter}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<CostCenterResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id, UpdateCostCenterRequest request, ICostCenterService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_costCenter_codeCostCenter") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe un centro de costo con el código '{request.CodeCostCenter}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound, Conflict<ProblemDetails>>> Delete(
        int id, ICostCenterService service, CancellationToken ct)
    {
        try
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
        }
        catch (DbUpdateException)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Centro de costo en uso",
                Detail = "No se puede eliminar el centro de costo porque está referenciado en líneas de asientos contables.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }
}
