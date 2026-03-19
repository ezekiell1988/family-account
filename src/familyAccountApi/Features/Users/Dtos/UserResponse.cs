namespace FamilyAccountApi.Features.Users.Dtos;

public sealed record UserResponse(
    int IdUser,
    string CodeUser,
    string NameUser,
    string? PhoneUser,
    string EmailUser);
