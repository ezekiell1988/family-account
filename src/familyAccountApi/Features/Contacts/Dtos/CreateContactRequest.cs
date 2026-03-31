using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Contacts.Dtos;

public sealed record CreateContactRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre del contacto")]
    public required string Name { get; init; }
}
