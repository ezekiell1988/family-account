using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Banks.Dtos;

public sealed record UpdateBankRequest
{
    [Required, StringLength(20, MinimumLength = 1)]
    [Description("Código único de la entidad bancaria. Ejemplo: BCR, BN, BAC")]
    public required string CodeBank { get; init; }

    [Required, StringLength(150, MinimumLength = 1)]
    [Description("Nombre completo del banco o institución financiera")]
    public required string NameBank { get; init; }

    [Description("Indica si el banco está activo")]
    public required bool IsActive { get; init; }
}
