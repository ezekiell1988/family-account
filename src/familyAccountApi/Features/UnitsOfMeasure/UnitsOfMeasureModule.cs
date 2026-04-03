using FamilyAccountApi.Features.UnitsOfMeasure.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.UnitsOfMeasure;

public static class UnitsOfMeasureModule
{
    public static IServiceCollection AddUnitsOfMeasureModule(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfMeasureService, UnitOfMeasureService>();
        return services;
    }

    public static IEndpointRouteBuilder MapUnitsOfMeasureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/units-of-measure")
            .WithTags("UnitsOfMeasure")
            .RequireAuthorization();

        group.MapGet("/data.json", GetAll)
            .WithName("GetAllUnitsOfMeasure")
            .WithSummary("Obtener todas las unidades de medida");

        group.MapGet("/{id:int}.json", GetById)
            .WithName("GetUnitOfMeasureById")
            .WithSummary("Obtener unidad de medida por ID");

        group.MapPost("/", Create)
            .WithName("CreateUnitOfMeasure")
            .WithSummary("Crear nueva unidad de medida")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateUnitOfMeasure")
            .WithSummary("Actualizar unidad de medida")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteUnitOfMeasure")
            .WithSummary("Eliminar unidad de medida")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<UnitOfMeasureResponse>>> GetAll(
        IUnitOfMeasureService service, CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<UnitOfMeasureResponse>, NotFound>> GetById(
        int id, IUnitOfMeasureService service, CancellationToken ct)
    {
        var item = await service.GetByIdAsync(id, ct);
        return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
    }

    private static async Task<Results<Created<UnitOfMeasureResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateUnitOfMeasureRequest request, IUnitOfMeasureService service, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/units-of-measure/{item.IdUnit}.json", item);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_unitOfMeasure_codeUnit") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe una unidad de medida con el código '{request.CodeUnit}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<UnitOfMeasureResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id, UpdateUnitOfMeasureRequest request, IUnitOfMeasureService service, CancellationToken ct)
    {
        try
        {
            var item = await service.UpdateAsync(id, request, ct);
            return item is not null ? TypedResults.Ok(item) : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_unitOfMeasure_codeUnit") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title  = "Código duplicado",
                Detail = $"Ya existe una unidad de medida con el código '{request.CodeUnit}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id, IUnitOfMeasureService service, CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
    }
}
