using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Contacts.Dtos;

public sealed record GetOrCreateContactRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre del contacto (se crea si no existe)")]
    public required string Name { get; init; }

    [Required, StringLength(50, MinimumLength = 1)]
    [Description("Código del tipo de contacto (ej: PRO, CLI)")]
    public required string ContactTypeCode { get; init; }
}
