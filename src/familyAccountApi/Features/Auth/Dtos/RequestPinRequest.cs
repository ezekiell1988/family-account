using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Auth.Dtos;

public sealed record RequestPinRequest
{
    [Required, EmailAddress, StringLength(200)]
    [Description("Correo electrónico del usuario para recibir el PIN")]
    public required string EmailUser { get; init; }
}
