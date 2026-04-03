using FamilyAccountApi.Features.ProductRecipes.Dtos;

namespace FamilyAccountApi.Features.ProductRecipes;

public interface IProductRecipeService
{
    Task<IReadOnlyList<ProductRecipeResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProductRecipeResponse>> GetByProductOutputAsync(int idProductOutput, CancellationToken ct = default);
    Task<ProductRecipeResponse?> GetByIdAsync(int idProductRecipe, CancellationToken ct = default);
    Task<ProductRecipeResponse> CreateAsync(CreateProductRecipeRequest request, CancellationToken ct = default);
    Task<ProductRecipeResponse?> UpdateAsync(int idProductRecipe, UpdateProductRecipeRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idProductRecipe, CancellationToken ct = default);
}
