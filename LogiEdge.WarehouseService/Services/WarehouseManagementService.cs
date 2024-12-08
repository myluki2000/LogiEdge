using LogiEdge.CustomerService.Data;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LogiEdge.WarehouseService.Services
{
    public class WarehouseManagementService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        
    }
}