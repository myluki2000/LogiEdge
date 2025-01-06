using LogiEdge.BaseService.Data;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.BaseService.Persistence
{
    internal class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Key-Value (both strings) store for global settings.
        /// </summary>
        public required DbSet<SettingsEntry> Settings { get; init; }
        public required DbSet<FileAttachment> Attachments { get; init; }

        public ApplicationDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
