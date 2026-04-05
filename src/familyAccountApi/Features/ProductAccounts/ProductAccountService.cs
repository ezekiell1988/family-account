using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.ProductAccounts.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.ProductAccounts;

public sealed class ProductAccountService(AppDbContext db) : IProductAccountService
{
    private static ProductAccountResponse ToResponse(ProductAccount pa) => new(
        pa.IdProductAccount,
        pa.IdProduct,
        pa.IdProductNavigation.CodeProduct,
        pa.IdProductNavigation.NameProduct,
        pa.IdAccount,
        pa.IdAccountNavigation.CodeAccount,
        pa.IdAccountNavigation.NameAccount,
        pa.IdCostCenter,
        pa.IdCostCenterNavigation?.NameCostCenter,
        pa.PercentageAccount);

    private static IQueryable<ProductAccount> WithIncludes(IQueryable<ProductAccount> q) =>
        q.Include(pa => pa.IdProductNavigation)
         .Include(pa => pa.IdAccountNavigation)
         .Include(pa => pa.IdCostCenterNavigation);

    public async Task<IReadOnlyList<ProductAccountResponse>> GetAllAsync(CancellationToken ct = default) =>
        await WithIncludes(db.ProductAccount.AsNoTracking())
            .Select(pa => new ProductAccountResponse(
                pa.IdProductAccount,
                pa.IdProduct,
                pa.IdProductNavigation.CodeProduct,
                pa.IdProductNavigation.NameProduct,
                pa.IdAccount,
                pa.IdAccountNavigation.CodeAccount,
                pa.IdAccountNavigation.NameAccount,
                pa.IdCostCenter,
                pa.IdCostCenterNavigation != null ? pa.IdCostCenterNavigation.NameCostCenter : null,
                pa.PercentageAccount))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductAccountResponse>> GetByProductAsync(int idProduct, CancellationToken ct = default) =>
        await WithIncludes(db.ProductAccount.AsNoTracking().Where(pa => pa.IdProduct == idProduct))
            .Select(pa => new ProductAccountResponse(
                pa.IdProductAccount,
                pa.IdProduct,
                pa.IdProductNavigation.CodeProduct,
                pa.IdProductNavigation.NameProduct,
                pa.IdAccount,
                pa.IdAccountNavigation.CodeAccount,
                pa.IdAccountNavigation.NameAccount,
                pa.IdCostCenter,
                pa.IdCostCenterNavigation != null ? pa.IdCostCenterNavigation.NameCostCenter : null,
                pa.PercentageAccount))
            .ToListAsync(ct);

    public async Task<ProductAccountResponse?> GetByIdAsync(int idProductAccount, CancellationToken ct = default) =>
        await WithIncludes(db.ProductAccount.AsNoTracking().Where(pa => pa.IdProductAccount == idProductAccount))
            .Select(pa => new ProductAccountResponse(
                pa.IdProductAccount,
                pa.IdProduct,
                pa.IdProductNavigation.CodeProduct,
                pa.IdProductNavigation.NameProduct,
                pa.IdAccount,
                pa.IdAccountNavigation.CodeAccount,
                pa.IdAccountNavigation.NameAccount,
                pa.IdCostCenter,
                pa.IdCostCenterNavigation != null ? pa.IdCostCenterNavigation.NameCostCenter : null,
                pa.PercentageAccount))
            .FirstOrDefaultAsync(ct);

    public async Task<ProductAccountResponse> CreateAsync(CreateProductAccountRequest request, CancellationToken ct = default)
    {
        // V6: porcentaje debe ser > 0 y la suma para el producto no puede superar 100
        if (request.PercentageAccount <= 0)
            throw new InvalidOperationException("El porcentaje de la cuenta debe ser mayor que 0.");

        var existingSum = await db.ProductAccount
            .Where(pa => pa.IdProduct == request.IdProduct)
            .SumAsync(pa => pa.PercentageAccount, CancellationToken.None);

        if (existingSum + request.PercentageAccount > 100)
            throw new InvalidOperationException(
                $"La suma de porcentajes para este producto sería {existingSum + request.PercentageAccount:F2}%, que supera el 100%. Reduzca el porcentaje nuevo o ajuste los existentes.");

        var entity = new ProductAccount
        {
            IdProduct         = request.IdProduct,
            IdAccount         = request.IdAccount,
            IdCostCenter      = request.IdCostCenter,
            PercentageAccount = request.PercentageAccount
        };

        db.ProductAccount.Add(entity);
        await db.SaveChangesAsync(CancellationToken.None);

        return (await GetByIdAsync(entity.IdProductAccount, CancellationToken.None))!;
    }

    public async Task<ProductAccountResponse?> UpdateAsync(int idProductAccount, UpdateProductAccountRequest request, CancellationToken ct = default)
    {
        var entity = await db.ProductAccount.FindAsync([idProductAccount], ct);
        if (entity is null) return null;

        // V6: validar porcentaje y suma
        if (request.PercentageAccount <= 0)
            throw new InvalidOperationException("El porcentaje de la cuenta debe ser mayor que 0.");

        var existingSum = await db.ProductAccount
            .Where(pa => pa.IdProductAccount != idProductAccount && pa.IdProduct == entity.IdProduct)
            .SumAsync(pa => pa.PercentageAccount, CancellationToken.None);

        if (existingSum + request.PercentageAccount > 100)
            throw new InvalidOperationException(
                $"La suma de porcentajes para este producto sería {existingSum + request.PercentageAccount:F2}%, que supera el 100%. Reduzca el porcentaje o ajuste las distribuciones existentes.");

        entity.IdAccount         = request.IdAccount;
        entity.IdCostCenter      = request.IdCostCenter;
        entity.PercentageAccount = request.PercentageAccount;

        await db.SaveChangesAsync(CancellationToken.None);

        return await GetByIdAsync(idProductAccount, CancellationToken.None);
    }

    public async Task<bool> DeleteAsync(int idProductAccount, CancellationToken ct = default)
    {
        var deleted = await db.ProductAccount
            .Where(pa => pa.IdProductAccount == idProductAccount)
            .ExecuteDeleteAsync(CancellationToken.None);

        return deleted > 0;
    }
}
