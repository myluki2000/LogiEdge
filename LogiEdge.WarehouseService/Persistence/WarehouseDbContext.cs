using LogiEdge.BaseService.Data;
using LogiEdge.CustomerService.Data;
using LogiEdge.Shared;
using LogiEdge.Shared.Conventions;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;

namespace LogiEdge.WarehouseService.Persistence
{
    public class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options, IConfiguration configuration) 
        : LogiEdgeDbContext<WarehouseDbContext>(options, configuration)
    {
        public DbSet<Warehouse> Warehouses { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<ItemState> ItemsStates { get; set; }

        public DbSet<ItemSchema> ItemSchemas { get; set; }

        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }

        public DbSet<InboundTransactionPart> InboundTransactionParts { get; set; }

        public DbSet<OutboundTransactionPart> OutboundTransactionParts { get; set; }

        public DbSet<RelocationTransactionPart> RelocationTransactionParts { get; set; }

        public DbSet<TransactionEditLogEntry> TransactionEditLogEntries { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("hstore");

            modelBuilder.Entity<Customer>().ToTable("Customers", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<ItemSchema>().HasMany(sch => sch.Customers).WithMany();

            modelBuilder.Entity<Item>()
                .HasMany(x => x.ItemStates)
                .WithOne(x => x.Item)
                .HasForeignKey(x => x.ItemId)
                .IsRequired();

            modelBuilder.Entity<InventoryTransaction>()
                .Property(t => t.RowVersion)
                .IsConcurrencyToken();

            // BookedDate must be unique and strictly increasing across all booked transactions so that
            // item states created by transactions are globally orderable by date alone (see
            // .notes/edit-booked-transactions.md, section 5.2). Drafts have a NULL BookedDate and are
            // excluded from the index.
            modelBuilder.Entity<InventoryTransaction>()
                .HasIndex(t => t.BookedDate)
                .IsUnique()
                .HasFilter("BookedDate IS NOT NULL");

            // Configure Table-Per-Type (TPT) inheritance for InventoryTransactionPart
            modelBuilder.Entity<InventoryTransactionPart>()
                .UseTpcMappingStrategy()
                .HasKey(p => p.TransactionId);

            // Configure relationships
            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(t => t.InboundTransactionPart)
                .WithOne(p => p.Transaction)
                .HasForeignKey<InboundTransactionPart>(p => p.TransactionId);
            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(t => t.OutboundTransactionPart)
                .WithOne(p => p.Transaction)
                .HasForeignKey<OutboundTransactionPart>(p => p.TransactionId);
            modelBuilder.Entity<InventoryTransaction>()
                .HasOne(t => t.RelocationTransactionPart)
                .WithOne(p => p.Transaction)
                .HasForeignKey<RelocationTransactionPart>(p => p.TransactionId);
        }

        /// <summary>
        /// Bumps the RowVersion concurrency token of all modified transactions before saving, so that a
        /// concurrent editor working on a stale copy fails with a DbUpdateConcurrencyException.
        /// </summary>
        private void BumpTransactionRowVersions()
        {
            ChangeTracker.DetectChanges();
            foreach (EntityEntry<InventoryTransaction> entry in ChangeTracker.Entries<InventoryTransaction>())
            {
                if (entry.State == EntityState.Modified)
                {
                    entry.Entity.RowVersion++;
                }
            }
        }

        public override int SaveChanges()
        {
            BumpTransactionRowVersions();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            BumpTransactionRowVersions();
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
