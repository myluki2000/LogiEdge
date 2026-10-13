using LogiEdge.Shared.Utility;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using LogiEdge.WarehouseService.Dto;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    public class WarehouseManagementService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        public async Task<List<WarehouseBaseInfoDto>> GetWarehouses()
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            return ctx.Warehouses.AsNoTracking().AsEnumerable().Select(WarehouseToDto).ToList();
        }

        public async Task<List<ItemSchemaDto>> GetItemSchemas()
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            List<ItemSchema> schemas = await ctx.ItemSchemas
                .AsNoTracking()
                .Include(s => s.Customers)
                .ToListAsync();

            return schemas.Select(ItemSchemaToDto).ToList();
        }

        public async Task<ItemSchemaDto?> GetItemSchema(Guid schemaId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemSchema? schema = await ctx.ItemSchemas
                .Include(s => s.Customers)
                .FirstOrDefaultAsync(s => s.Id == schemaId);

            if (schema == null)
                return null;

            return ItemSchemaToDto(schema);
        }

        private ItemSchemaDto ItemSchemaToDto(ItemSchema schema) => new()
        {
            Id = schema.Id,
            Name = schema.Name,
            AdditionalProperties = schema.AdditionalProperties.Select(p => new ItemPropertyInfoDto()
            {
                Name = p.Name,
                IsDisplayable = true,
                IsFilterable = true,
                Type = p.Type.ToType()
            }).ToList(),
            CustomerIds = schema.Customers.Select(c => c.Id).ToList(),
        };

        public async Task<WarehouseBaseInfoDto?> GetWarehouseById(Guid id)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            Warehouse? warehouse = await ctx.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            return warehouse is null ? null : WarehouseToDto(warehouse);
        }

        public async Task<WarehouseBaseInfoDto> CreateWarehouse(string name)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            EntityEntry<Warehouse> entity = ctx.Warehouses.Add(new Warehouse()
            {
                Name = name,
            });

            await ctx.SaveChangesAsync();

            return WarehouseToDto(entity.Entity);
        }

        private WarehouseBaseInfoDto WarehouseToDto(Warehouse w) => new()
        {
            Id = w.Id,
            Name = w.Name
        };
    }
}
