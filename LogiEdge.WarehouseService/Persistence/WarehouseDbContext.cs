using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.WarehouseService.Data;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Persistence
{
    public class WarehouseDbContext : DbContext
    {
        public required DbSet<Item> CurrentItems { get; init; }
        public required DbSet<Item> HistoricItems { get; init; }
        public required DbSet<ItemState> ItemStates { get; init; }

        public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Item.CreateModel(modelBuilder);
        }
    }
}
