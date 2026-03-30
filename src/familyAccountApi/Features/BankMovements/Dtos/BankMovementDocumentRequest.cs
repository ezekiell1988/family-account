using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.BankMovements.Dtos;

public sealed record BankMovementDocumentRequest
{
    [Required, StringLength(20, MinimumLength = 1)]
    [Description("Tipo de documento: 'FacturaCompra', 'Recibo', 'Transferencia', 'Cheque' u 'Otro'")]
    public required string TypeDocument { get; init; }

    [StringLength(100)]
    [Description("Número o referencia del documento")]
    public string? NumberDocument { get; init; }

    [Required]
    [Description("Fecha del documento de soporte")]
    public required DateOnly DateDocument { get; init; }

    [Range(typeof(decimal), "0.01", "999999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
    [Description("Monto del documento de soporte")]
    public decimal AmountDocument { get; init; }

    [StringLength(500)]
    [Description("Descripción adicional del documento")]
    public string? DescriptionDocument { get; init; }

    [Range(1, int.MaxValue)]
    [Description("ID de la factura de compra vinculada (opcional)")]
    public int? IdPurchaseInvoice { get; init; }
}
