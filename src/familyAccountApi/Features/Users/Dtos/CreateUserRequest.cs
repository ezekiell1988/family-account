using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Users.Dtos;

public sealed record CreateUserRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Description("Código único del usuario")]
    public required string CodeUser { get; init; }

    [Required, StringLength(150, MinimumLength = 2)]
    [Description("Nombre completo del usuario")]
    public required string NameUser { get; init; }

    [StringLength(20)]
    [Description("Teléfono del usuario (opcional)")]
    public string? PhoneUser { get; init; }

    [Required, EmailAddress, StringLength(200)]
    [Description("Correo electrónico del usuario")]
    public required string EmailUser { get; init; }
}
