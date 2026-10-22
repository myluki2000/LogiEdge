using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.Data
{
    public class AppIdentityDbContext : IdentityDbContext
    {
        public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApiToken> ApiTokens => Set<ApiToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApiToken>(entity =>
            {
                entity.HasIndex(token => token.HashedToken).IsUnique();
                entity.HasIndex(token => token.UserId);
                entity.Property(token => token.HashedToken).HasMaxLength(64);
            });
        }
    }
}