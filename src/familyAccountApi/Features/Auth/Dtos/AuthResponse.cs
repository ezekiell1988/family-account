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
