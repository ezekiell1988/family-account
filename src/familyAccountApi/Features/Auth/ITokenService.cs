using FamilyAccountApi.Features.Auth.Dtos;

namespace FamilyAccountApi.Features.Auth;

public interface ITokenService
{
    (string accessToken, string jti, DateTime expiresAt) GenerateAccessToken(
        int idUser, string emailUser, string codeUser, string nameUser,
        IReadOnlyList<string> roles);
    string GenerateRefreshToken();
    (int idUser, string jti, DateTime expiresAt)? ValidateAccessToken(string token);
}
