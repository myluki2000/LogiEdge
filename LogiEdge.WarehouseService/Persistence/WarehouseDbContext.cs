using LogiEdge.BaseService.Data;
using LogiEdge.CustomerService.Data;
using LogiEdge.Shared;
using LogiEdge.Shared.Conventions;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using Microsoft.EntityFrameworkCore;
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
    }
}
