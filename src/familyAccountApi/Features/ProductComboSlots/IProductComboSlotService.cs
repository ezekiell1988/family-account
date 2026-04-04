using FamilyAccountApi.Features.ProductComboSlots.Dtos;

namespace FamilyAccountApi.Features.ProductComboSlots;

public interface IProductComboSlotService
{
    Task<IReadOnlyList<ProductComboSlotResponse>> GetByComboAsync(int idProductCombo, CancellationToken ct = default);
    Task<ProductComboSlotResponse?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(ProductComboSlotResponse result, string? error)> CreateAsync(CreateProductComboSlotRequest request, CancellationToken ct = default);
    Task<(ProductComboSlotResponse? result, string? error)> UpdateAsync(int id, UpdateProductComboSlotRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
