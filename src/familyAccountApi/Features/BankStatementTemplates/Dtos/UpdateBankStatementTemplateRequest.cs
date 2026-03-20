using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankStatementTemplates.Dtos;

public sealed record UpdateBankStatementTemplateRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    [Description("Código único de la plantilla")]
    public required string CodeTemplate { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre descriptivo de la plantilla")]
    public required string NameTemplate { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    [Description("Nombre del banco emisor")]
    public required string BankName { get; init; }

    [Required]
    [Description("Mapeo de columnas en formato JSON")]
    public required string ColumnMappings { get; init; }

    [StringLength(50)]
    [Description("Formato de fecha (ej: dd/MM/yyyy)")]
    public string? DateFormat { get; init; }

    [StringLength(50)]
    [Description("Formato de hora (ej: HH:mm)")]
    public string? TimeFormat { get; init; }

    [Description("Indica si la plantilla está activa")]
    public bool IsActive { get; init; }

    [StringLength(1000)]
    [Description("Notas o instrucciones adicionales")]
    public string? Notes { get; init; }
}
