using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Auth.Dtos;

public sealed record LoginRequest
{
    [Required, EmailAddress, StringLength(200)]
    [Description("Correo electrónico del usuario")]
    public required string EmailUser { get; init; }

    [Required, StringLength(5, MinimumLength = 5)]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "El PIN debe ser exactamente 5 dígitos numéricos")]
    [Description("PIN de 5 dígitos enviado por correo")]
    public required string Pin { get; init; }
}
