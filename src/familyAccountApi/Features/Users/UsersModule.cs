using FamilyAccountApi.Features.Users.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Users;

public static class UsersModule
{
    public static IServiceCollection AddUsersModule(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        return services;
    }

    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/", GetAll)
            .WithName("GetAllUsers")
            .WithSummary("Obtener todos los usuarios");

        group.MapGet("/{id:int}", GetById)
            .WithName("GetUserById")
            .WithSummary("Obtener usuario por ID");

        group.MapPost("/", Create)
            .WithName("CreateUser")
            .WithSummary("Crear nuevo usuario")
            .RequireAuthorization("Admin");

        group.MapPut("/{id:int}", Update)
            .WithName("UpdateUser")
            .WithSummary("Actualizar usuario")
            .RequireAuthorization("Admin");

        group.MapDelete("/{id:int}", Delete)
            .WithName("DeleteUser")
            .WithSummary("Eliminar usuario")
            .RequireAuthorization("Admin");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<UserResponse>>> GetAll(
        IUserService userService,
        CancellationToken ct)
    {
        var users = await userService.GetAllAsync(ct);
        return TypedResults.Ok(users);
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> GetById(
        int id,
        IUserService userService,
        CancellationToken ct)
    {
        var user = await userService.GetByIdAsync(id, ct);
        return user is not null
            ? TypedResults.Ok(user)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<UserResponse>, Conflict<ProblemDetails>, ValidationProblem>> Create(
        CreateUserRequest request,
        IUserService userService,
        CancellationToken ct)
    {
        try
        {
            var user = await userService.CreateAsync(request, ct);
            return TypedResults.Created($"/api/v1/users/{user.IdUser}", user);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_user_codeUser") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Código duplicado",
                Detail = $"Ya existe un usuario con el código '{request.CodeUser}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<Ok<UserResponse>, NotFound, Conflict<ProblemDetails>>> Update(
        int id,
        UpdateUserRequest request,
        IUserService userService,
        CancellationToken ct)
    {
        try
        {
            var user = await userService.UpdateAsync(id, request, ct);
            return user is not null
                ? TypedResults.Ok(user)
                : TypedResults.NotFound();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UQ_user_codeUser") == true)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Código duplicado",
                Detail = $"Ya existe un usuario con el código '{request.CodeUser}'.",
                Status = StatusCodes.Status409Conflict
            });
        }
    }

    private static async Task<Results<NoContent, NotFound>> Delete(
        int id,
        IUserService userService,
        CancellationToken ct)
    {
        var deleted = await userService.DeleteAsync(id, ct);
        return deleted
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}
