using FamilyAccountApi.BackgroundJobs;
using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Auth.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using FamilyAccountApi.Infrastructure.Options;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace FamilyAccountApi.Features.Auth;

public sealed class AuthService(
    AppDbContext db,
    ITokenService tokenService,
    IDistributedCache cache,
    IOptions<JwtOptions> jwtOptions,
    IBackgroundJobClient jobClient) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    // Prefijos de clave Redis
    private static string RefreshKey(string token) => $"rt:{token}";
    private static string RevokedKey(string jti) => $"revoked:{jti}";

    public async Task<(bool success, string message)> RequestPinAsync(
        string emailUser, CancellationToken ct = default)
    {
        var user = await db.User
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmailUser == emailUser, ct);

        if (user is null)
            return (false, "No existe un usuario con ese correo electrónico.");

        var pin = Random.Shared.Next(0, 99999).ToString("D5");

        db.UserPin.Add(new UserPin { IdUser = user.IdUser, Pin = pin });
        await db.SaveChangesAsync(ct);

        // Enviar correo con PIN de forma asíncrona via Hangfire
        jobClient.Enqueue<EmailJobs>(j => j.SendPinEmailAsync(user.EmailUser, user.NameUser, pin));

        return (true, "PIN enviado a tu correo electrónico.");
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await db.User
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmailUser == request.EmailUser, ct);

        if (user is null) return null;

        // Verificar que el PIN existe para este usuario
        var pinExists = await db.UserPin
            .AnyAsync(up => up.IdUser == user.IdUser && up.Pin == request.Pin, ct);

        if (!pinExists) return null;

        // Cargar roles del usuario
        var roles = await db.UserRole
            .AsNoTracking()
            .Where(ur => ur.IdUser == user.IdUser)
            .Select(ur => ur.Role.NameRole)
            .ToListAsync(ct);

        // Generar tokens
        var (accessToken, jti, expiresAt) = tokenService.GenerateAccessToken(
            user.IdUser, user.EmailUser, user.CodeUser, user.NameUser, roles);
        var refreshToken = tokenService.GenerateRefreshToken();

        // Guardar refresh token en Redis
        var refreshOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_jwt.RefreshTokenExpirationDays)
        };
        await cache.SetStringAsync(RefreshKey(refreshToken), user.IdUser.ToString(), refreshOptions, ct);

        // Eliminar todos los PINs del usuario de forma asíncrona via Hangfire
        jobClient.Enqueue<PinJobs>(j => j.DeleteAllUserPinsAsync(user.IdUser));

        return new AuthResponse(accessToken, refreshToken, expiresAt);
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var userIdStr = await cache.GetStringAsync(RefreshKey(refreshToken), ct);
        if (userIdStr is null || !int.TryParse(userIdStr, out var idUser))
            return null;

        var user = await db.User
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUser == idUser, ct);

        if (user is null) return null;

        // Revocar refresh token anterior
        await cache.RemoveAsync(RefreshKey(refreshToken), ct);

        // Cargar roles del usuario
        var roles = await db.UserRole
            .AsNoTracking()
            .Where(ur => ur.IdUser == idUser)
            .Select(ur => ur.Role.NameRole)
            .ToListAsync(ct);

        // Generar nuevos tokens
        var (accessToken, _, expiresAt) = tokenService.GenerateAccessToken(
            user.IdUser, user.EmailUser, user.CodeUser, user.NameUser, roles);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        var refreshOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(_jwt.RefreshTokenExpirationDays)
        };
        await cache.SetStringAsync(RefreshKey(newRefreshToken), user.IdUser.ToString(), refreshOptions, ct);

        return new AuthResponse(accessToken, newRefreshToken, expiresAt);
    }

    public async Task<bool> LogoutAsync(
        int idUser, string jti, DateTime tokenExpiresAt, string refreshToken, CancellationToken ct = default)
    {
        // Agregar JTI a la blacklist de Redis (TTL = tiempo restante del token)
        var remaining = tokenExpiresAt - DateTime.UtcNow;
        if (remaining > TimeSpan.Zero)
        {
            await cache.SetStringAsync(
                RevokedKey(jti),
                "1",
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = remaining },
                ct);
        }

        // Eliminar refresh token
        await cache.RemoveAsync(RefreshKey(refreshToken), ct);

        return true;
    }

    public async Task<MeResponse?> GetMeAsync(int idUser, CancellationToken ct = default)
    {
        var user = await db.User
            .AsNoTracking()
            .Where(u => u.IdUser == idUser)
            .Select(u => new { u.IdUser, u.CodeUser, u.NameUser, u.EmailUser })
            .FirstOrDefaultAsync(ct);

        if (user is null) return null;

        var roleIds = await db.UserRole
            .AsNoTracking()
            .Where(ur => ur.IdUser == idUser)
            .Select(ur => ur.IdRole)
            .ToListAsync(ct);

        return new MeResponse(user.IdUser, user.CodeUser, user.NameUser, user.EmailUser, roleIds);
    }

    public async Task<CheckAuthResponse> CheckAuthAsync(int idUser, DateTime tokenExpiresAt, CancellationToken ct = default)
    {
        var user = await db.User
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUser == idUser, ct);

        if (user is null)
            return new CheckAuthResponse(false, false, "Usuario no encontrado", null, null);

        var roleIds = await db.UserRole
            .AsNoTracking()
            .Where(ur => ur.IdUser == idUser)
            .Select(ur => ur.IdRole)
            .ToListAsync(ct);

        var userResponse = new CheckAuthUserResponse(
            user.IdUser,
            user.CodeUser,
            user.NameUser,
            user.PhoneUser,
            user.EmailUser,
            roleIds);

        return new CheckAuthResponse(true, true, "Token válido", userResponse, tokenExpiresAt);
    }
}
