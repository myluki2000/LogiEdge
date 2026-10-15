using LogiEdge.Data;
using LogiEdge.Shared;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.Identity
{
    public class UserDirectory(IDbContextFactory<AppIdentityDbContext> dbContextFactory) : IUserDirectory
    {
        public async Task<string?> GetUserNameAsync(Guid userId)
        {
            IReadOnlyDictionary<Guid, string> names = await GetUserNamesAsync([userId]);
            return names.GetValueOrDefault(userId);
        }

        public async Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds)
        {
            // IdentityUser.Id is a string column holding Guid values, so compare in that form.
            string[] idStrings = userIds.Distinct().Select(id => id.ToString()).ToArray();
            if (idStrings.Length == 0)
            {
                return new Dictionary<Guid, string>();
            }

            await using AppIdentityDbContext ctx = await dbContextFactory.CreateDbContextAsync();

            return await ctx.Users
                .Where(u => idStrings.Contains(u.Id))
                .ToDictionaryAsync(u => Guid.Parse(u.Id), u => u.UserName ?? u.Email ?? string.Empty);
        }
    }
}
