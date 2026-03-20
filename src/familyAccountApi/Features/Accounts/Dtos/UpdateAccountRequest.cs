using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.Accounts.Dtos;

public sealed record UpdateAccountRequest
{
    [Required, StringLength(20, MinimumLength = 1)]
    [Description("Código único de la cuenta (ej: '1', '1.1', '1.1.01')")]
    public required string CodeAccount { get; init; }

    [Required, StringLength(150, MinimumLength = 1)]
    [Description("Nombre descriptivo de la cuenta")]
    public required string NameAccount { get; init; }

    [Required]
    [AllowedValues("Activo", "Pasivo", "Capital", "Ingreso", "Gasto", "Control",
        ErrorMessage = "typeAccount debe ser: Activo, Pasivo, Capital, Ingreso, Gasto o Control.")]
    [Description("Tipo de cuenta contable: Activo, Pasivo, Capital, Ingreso, Gasto, Control")]
    public required string TypeAccount { get; init; }

    [Range(1, 20)]
    [Description("Nivel jerárquico (1 = raíz)")]
    public int LevelAccount { get; init; } = 1;

    [Description("ID de la cuenta padre (null si es cuenta raíz)")]
    public int? IdAccountParent { get; init; }

    [Description("Permite registrar asientos contables directamente en esta cuenta")]
    public bool AllowsMovements { get; init; } = true;

    [Description("Indica si la cuenta está activa")]
    public bool IsActive { get; init; } = true;
}
