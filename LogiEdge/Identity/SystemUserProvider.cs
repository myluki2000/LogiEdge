using LogiEdge.Data;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.Identity
{
    public class SystemUserProvider(IDbContextFactory<AppIdentityDbContext> dbContextFactory) : ISystemUserProvider
    {
        public async Task<Guid> GetSystemUserIdAsync()
        {
            await using AppIdentityDbContext ctx = await dbContextFactory.CreateDbContextAsync();

            IdentityUser? systemUser = await ctx.Users.FirstOrDefaultAsync(u => u.UserName == AppUsers.System);

            if (systemUser is null)
            {
                throw new InvalidOperationException(
                    $"System user '{AppUsers.System}' not found in the identity database. " +
                    "In Development it is seeded by TestUserSeeder at startup.");
            }

            return Guid.Parse(systemUser.Id);
        }
    }
}
