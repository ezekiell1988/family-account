using FamilyAccountApi.Domain.Entities;
using FamilyAccountApi.Features.Users.Dtos;
using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.Features.Users;

public sealed class UserService(AppDbContext db) : IUserService
{
    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.User
            .AsNoTracking()
            .Select(u => new UserResponse(u.IdUser, u.CodeUser, u.NameUser, u.PhoneUser, u.EmailUser))
            .ToListAsync(ct);
    }

    public async Task<UserResponse?> GetByIdAsync(int idUser, CancellationToken ct = default)
    {
        return await db.User
            .AsNoTracking()
            .Where(u => u.IdUser == idUser)
            .Select(u => new UserResponse(u.IdUser, u.CodeUser, u.NameUser, u.PhoneUser, u.EmailUser))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var user = new User
        {
            CodeUser = request.CodeUser,
            NameUser = request.NameUser,
            PhoneUser = request.PhoneUser,
            EmailUser = request.EmailUser
        };

        db.User.Add(user);
        await db.SaveChangesAsync(ct);

        return new UserResponse(user.IdUser, user.CodeUser, user.NameUser, user.PhoneUser, user.EmailUser);
    }

    public async Task<UserResponse?> UpdateAsync(int idUser, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await db.User.FindAsync([idUser], ct);
        if (user is null) return null;

        user.CodeUser = request.CodeUser;
        user.NameUser = request.NameUser;
        user.PhoneUser = request.PhoneUser;
        user.EmailUser = request.EmailUser;

        await db.SaveChangesAsync(ct);

        return new UserResponse(user.IdUser, user.CodeUser, user.NameUser, user.PhoneUser, user.EmailUser);
    }

    public async Task<bool> DeleteAsync(int idUser, CancellationToken ct = default)
    {
        var deleted = await db.User
            .Where(u => u.IdUser == idUser)
            .ExecuteDeleteAsync(ct);

        return deleted > 0;
    }
}
