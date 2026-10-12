using LogiEdge.Shared.Utility;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    public class WarehouseManagementService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        public async Task<List<Warehouse>> GetWarehousesAsync()
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            return await ctx.Warehouses.AsNoTracking().ToListAsync();
        }
    }
}