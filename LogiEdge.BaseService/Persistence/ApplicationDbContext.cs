using Microsoft.EntityFrameworkCore;

namespace LogiEdge.BaseService.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Key-Value (both strings) store for global settings.
        /// </summary>
        public required DbSet<SettingsEntry> Settings { get; init; }

        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }
    }
}
