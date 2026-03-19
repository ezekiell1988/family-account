using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FamilyAccountApi.Features.Auth.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FamilyAccountApi.Features.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        return services;
    }

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth")
            .WithTags("Auth");

        group.MapPost("/request-pin", RequestPin)
            .WithName("RequestPin")
            .WithSummary("Solicitar PIN de acceso por correo")
            .AllowAnonymous();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Iniciar sesión con email y PIN")
            .AllowAnonymous();

        group.MapPost("/refresh", Refresh)
            .WithName("RefreshToken")
            .WithSummary("Renovar access token con refresh token")
            .AllowAnonymous();

        group.MapGet("/me", Me)
            .WithName("GetMe")
            .WithSummary("Obtener datos del usuario autenticado")
            .RequireAuthorization();

        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithSummary("Cerrar sesión y revocar tokens")
            .RequireAuthorization();

        return app;
    }

    private static async Task<Results<Ok<MessageResponse>, NotFound<ProblemDetails>>> RequestPin(
        RequestPinRequest request,
        IAuthService authService,
        CancellationToken ct)
    {
        var (success, message) = await authService.RequestPinAsync(request.EmailUser, ct);

        return success
            ? TypedResults.Ok(new MessageResponse(message))
            : TypedResults.NotFound(new ProblemDetails
            {
                Title = "Usuario no encontrado",
                Detail = message,
                Status = StatusCodes.Status404NotFound
            });
    }

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> Login(
        LoginRequest request,
        IAuthService authService,
        CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.Unauthorized();
    }

    private static async Task<Results<Ok<AuthResponse>, UnauthorizedHttpResult>> Refresh(
        RefreshTokenRequest request,
        IAuthService authService,
        CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, ct);
        return result is not null
            ? TypedResults.Ok(result)
            : TypedResults.Unauthorized();
    }

    private static async Task<Results<Ok<MeResponse>, UnauthorizedHttpResult>> Me(
        ClaimsPrincipal user,
        IAuthService authService,
        CancellationToken ct)
    {
        if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var idUser))
            return TypedResults.Unauthorized();

        var me = await authService.GetMeAsync(idUser, ct);
        return me is not null
            ? TypedResults.Ok(me)
            : TypedResults.Unauthorized();
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult>> Logout(
        LogoutRequest request,
        ClaimsPrincipal user,
        IAuthService authService,
        CancellationToken ct)
    {
        if (!int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var idUser))
            return TypedResults.Unauthorized();

        var jti = user.FindFirstValue(JwtRegisteredClaimNames.Jti) ?? string.Empty;
        var expClaim = user.FindFirstValue(JwtRegisteredClaimNames.Exp);
        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim ?? "0")).UtcDateTime;

        await authService.LogoutAsync(idUser, jti, expiresAt, request.RefreshToken ?? string.Empty, ct);

        return TypedResults.NoContent();
    }
}
