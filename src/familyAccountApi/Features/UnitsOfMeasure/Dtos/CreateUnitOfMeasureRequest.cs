using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.UnitsOfMeasure.Dtos;

public sealed record CreateUnitOfMeasureRequest
{
    [Required, StringLength(10, MinimumLength = 1)]
    [Description("Código corto único: ML, GR, KG, BOT160, LATA400, UNI, etc.")]
    public required string CodeUnit { get; init; }

    [Required, StringLength(80, MinimumLength = 1)]
    [Description("Nombre legible: Mililitro, Gramo, Botella 160ml, etc.")]
    public required string NameUnit { get; init; }

    [Required]
    [Description("Identificador del tipo dimensional (idUnitType): 1=Unidad | 2=Volumen | 3=Masa | 4=Longitud.")]
    public required int IdUnitType { get; init; }
}
