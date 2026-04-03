using FamilyAccountApi.Features.UnitsOfMeasure.Dtos;

namespace FamilyAccountApi.Features.UnitsOfMeasure;

public interface IUnitOfMeasureService
{
    Task<IReadOnlyList<UnitOfMeasureResponse>> GetAllAsync(CancellationToken ct = default);
    Task<UnitOfMeasureResponse?> GetByIdAsync(int idUnit, CancellationToken ct = default);
    Task<UnitOfMeasureResponse> CreateAsync(CreateUnitOfMeasureRequest request, CancellationToken ct = default);
    Task<UnitOfMeasureResponse?> UpdateAsync(int idUnit, UpdateUnitOfMeasureRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idUnit, CancellationToken ct = default);
}
