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
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.WarehouseService.Persistence
{
    public class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : DbContext(options)
    {
        public DbSet<Warehouse> Warehouses { get; set; }

        public DbSet<Item> Items { get; set; }

        public DbSet<ItemSchema> ItemSchemas { get; set; }

        public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
        public DbSet<InboundTransaction> InboundTransactions { get; set; }
        public DbSet<OutboundTransaction> OutboundTransactions { get; set; }
        public DbSet<RelocationTransaction> RelocationTransactions { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().ToTable("Customers", t => t.ExcludeFromMigrations());
            modelBuilder.Entity<ItemSchema>().HasMany(sch => sch.Customers).WithMany();

            modelBuilder.Entity<Item>()
                .HasMany(x => x.ItemStates)
                .WithOne(x => x.Item)
                .HasForeignKey(x => x.ItemId)
                .IsRequired();

            modelBuilder.Entity<InventoryTransaction>()
                .UseTpcMappingStrategy();
        }

        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            base.ConfigureConventions(configurationBuilder);

            configurationBuilder.Conventions.Add(sp => new AutoIncludeAttributeConvention(sp.GetRequiredService<ProviderConventionSetBuilderDependencies>()));
        }
    }
}
