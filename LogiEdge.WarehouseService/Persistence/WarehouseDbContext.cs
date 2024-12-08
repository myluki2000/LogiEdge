using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.CustomerService.Data;
using LogiEdge.WarehouseService.Data;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Persistence
{
    public class WarehouseDbContext : DbContext
    {
        public DbSet<Warehouse> Warehouses { get; init; }

        public DbSet<Item> Items { get; init; }
        public DbSet<ItemState> ItemStates { get; init; }

        public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>().ToTable("Customers", t => t.ExcludeFromMigrations());

            modelBuilder.Entity<Item>()
                .HasMany(x => x.ItemStates)
                .WithOne(x => x.Item)
                .HasForeignKey(x => x.ItemId)
                .IsRequired();
        }
    }
}
