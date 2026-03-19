using FamilyAccountApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FamilyAccountApi.BackgroundJobs;

public sealed class PinJobs(AppDbContext db)
{
    public async Task DeleteAllUserPinsAsync(int idUser)
    {
        await db.UserPin
            .Where(up => up.IdUser == idUser)
            .ExecuteDeleteAsync();
    }
}
