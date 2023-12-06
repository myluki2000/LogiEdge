using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.WarehouseService.Data;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Persistence
{
    internal class WarehouseDbContext : DbContext
    {
        public required DbSet<Item> CurrentItems { get; init; }
        public required DbSet<Item> HistoricItems { get; init; }
        public required DbSet<ItemState> ItemStates { get; init; }
    }
}
