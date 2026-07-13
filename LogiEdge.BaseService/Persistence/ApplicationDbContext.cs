using LogiEdge.BaseService.Data;
using LogiEdge.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace LogiEdge.BaseService.Persistence
{
    internal class ApplicationDbContext : LogiEdgeDbContext<ApplicationDbContext>
    {
        /// <summary>
        /// Key-Value (both strings) store for global settings.
        /// </summary>
        public required DbSet<SettingsEntry> Settings { get; init; }
        public required DbSet<FileAttachment> Attachments { get; init; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration) : base(options, configuration)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
