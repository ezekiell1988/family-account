using FamilyAccountApi.Features.ProductUnits.Dtos;

namespace FamilyAccountApi.Features.ProductUnits;

public interface IProductUnitService
{
    Task<IReadOnlyList<ProductUnitResponse>> GetByProductAsync(int idProduct, CancellationToken ct = default);
    Task<ProductUnitResponse?> GetByBarcodeAsync(string barcode, CancellationToken ct = default);
    Task<ProductUnitResponse?> GetByIdAsync(int idProductUnit, CancellationToken ct = default);
    Task<ProductUnitResponse> CreateAsync(CreateProductUnitRequest request, CancellationToken ct = default);
    Task<ProductUnitResponse?> UpdateAsync(int idProductUnit, UpdateProductUnitRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idProductUnit, CancellationToken ct = default);
}
