using System.ComponentModel.DataAnnotations;

namespace FamilyAccountApi.Features.SalesInvoices.Dtos;

public sealed record PartialReturnLineRequest
{
    [Required, Range(1, int.MaxValue)]
    public required int IdInventoryLot { get; init; }

    [Required, Range(0.0001, double.MaxValue)]
    public required decimal Quantity { get; init; }

    [Required, Range(0, double.MaxValue)]
    public required decimal TotalLineAmount { get; init; }

    [StringLength(300)]
    public string? DescriptionLine { get; init; }
}

public sealed record PartialReturnRequest
{
    [Required]
    public required DateOnly DateReturn { get; init; }

    [StringLength(500)]
    public string? DescriptionReturn { get; init; }

    /// <summary>
    /// Modo de reintegro al cliente.<br/>
    /// <b>EfectivoInmediato</b> — genera asiento de reversión de ingresos contra Banco/Caja automáticamente.<br/>
    /// <b>NotaCredito</b> — genera asiento contra la cuenta indicada en <see cref="IdAccountCreditNote"/> (saldo a favor del cliente).
    /// </summary>
    [Required, StringLength(30)]
    public required string RefundMode { get; init; }

    /// <summary>
    /// Cuenta contable de crédito para el saldo a favor del cliente.<br/>
    /// Requerido cuando <see cref="RefundMode"/> = <c>NotaCredito</c>.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? IdAccountCreditNote { get; init; }

    [Required, MinLength(1)]
    public required IReadOnlyList<PartialReturnLineRequest> Lines { get; init; }
}
