namespace FamilyAccountApi.Features.Auth.Dtos;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt);

public sealed record MeResponse(
    int IdUser,
    string CodeUser,
    string NameUser,
    string EmailUser);

public sealed record MessageResponse(string Message);

/// <summary>Datos del usuario devueltos por GET /auth/check (coincide con UserData del frontend).</summary>
public sealed record CheckAuthUserResponse(
    int IdLogin,
    string CodeLogin,
    string NameLogin,
    string? PhoneLogin,
    string? EmailLogin,
    IReadOnlyList<int> Roles);

/// <summary>Response de GET /auth/check.</summary>
public sealed record CheckAuthResponse(
    bool Success,
    bool IsValid,
    string Message,
    CheckAuthUserResponse? User,
    DateTime? ExpiresAt);
