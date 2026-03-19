using FamilyAccountApi.Features.Users.Dtos;

namespace FamilyAccountApi.Features.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct = default);
    Task<UserResponse?> GetByIdAsync(int idUser, CancellationToken ct = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserResponse?> UpdateAsync(int idUser, UpdateUserRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(int idUser, CancellationToken ct = default);
}
