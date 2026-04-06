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

    [Required, MinLength(1)]
    public required IReadOnlyList<PartialReturnLineRequest> Lines { get; init; }
}
