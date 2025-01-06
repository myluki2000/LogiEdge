using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.BaseService.Data;
using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Conventions;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.WarehouseService.Persistence
{
    public class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
    {
        public DbSet<Warehouse> Warehouses { get; init; }

        public DbSet<Item> Items { get; init; }
        public DbSet<ItemState> ItemStates { get; init; }
        public DbSet<ItemSchema> ItemSchemas { get; init; }

        public DbSet<InventoryTransaction> Transactions { get; init; }
        public DbSet<InboundTransaction> InboundTransactions { get; init; }
        public DbSet<OutboundTransaction> OutboundTransactions { get; init; }
        public DbSet<RelocationTransaction> RelocationTransactions { get; init; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().ToTable("Customers", t => t.ExcludeFromMigrations());

            modelBuilder.Entity<Item>()
                .HasMany(x => x.ItemStates)
                .WithOne(x => x.Item)
                .HasForeignKey(x => x.ItemId)
                .IsRequired();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            configurationBuilder.Conventions.Add(sp => new AutoIncludeAttributeConvention(sp.GetRequiredService<ProviderConventionSetBuilderDependencies>()));
        }
    }
}
