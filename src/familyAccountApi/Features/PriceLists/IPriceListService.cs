using FamilyAccountApi.Features.PriceLists.Dtos;

namespace FamilyAccountApi.Features.PriceLists;

public interface IPriceListService
{
    Task<IReadOnlyList<PriceListResponse>> GetAllAsync(CancellationToken ct = default);
    Task<PriceListResponse?> GetByIdAsync(int idPriceList, CancellationToken ct = default);
    Task<IReadOnlyList<PriceListItemResponse>> GetItemsByProductAsync(int idProduct, CancellationToken ct = default);
    Task<PriceListResponse> CreateAsync(CreatePriceListRequest request, CancellationToken ct = default);
    Task<PriceListResponse?> UpdateAsync(int idPriceList, UpdatePriceListRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idPriceList, CancellationToken ct = default);
}
