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
        // Base item-table property names a lookup column must not collide with.
        private static readonly HashSet<string> _reservedLookupColumnNames = [
            "Id", "Customer", "ItemNumber", "Comments", "EntryDate", "Date", "Location", "Warehouse", "IsQuarantined", "RelatedTransactionId"
        ];

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
                .Include(s => s.LookupColumns)
                .ToListAsync();

            Dictionary<Guid, int> entryCountsByColumnId = await GetLookupEntryCountsAsync(ctx, schemas.Select(s => s.Id).ToList());

            return schemas.Select(schema => ItemSchemaToDto(schema, entryCountsByColumnId)).ToList();
        }

        public async Task<ItemSchemaDto?> GetItemSchema(Guid schemaId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemSchema? schema = await ctx.ItemSchemas
                .Include(s => s.Customers)
                .Include(s => s.LookupColumns)
                .FirstOrDefaultAsync(s => s.Id == schemaId);

            if (schema == null)
                return null;

            Dictionary<Guid, int> entryCountsByColumnId = await GetLookupEntryCountsAsync(ctx, [schema.Id]);

            return ItemSchemaToDto(schema, entryCountsByColumnId);
        }

        private static async Task<Dictionary<Guid, int>> GetLookupEntryCountsAsync(WarehouseDbContext ctx, List<Guid> schemaIds)
        {
            return await ctx.ItemSchemaLookupEntries
                .Where(e => schemaIds.Contains(e.Column.ItemSchemaId))
                .GroupBy(e => e.ColumnId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        private static ItemSchemaDto ItemSchemaToDto(ItemSchema schema, IReadOnlyDictionary<Guid, int> entryCountsByColumnId) => new()
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
            LookupColumns = schema.LookupColumns.Select(c => new ItemSchemaLookupColumnDto()
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                EntryCount = entryCountsByColumnId.GetValueOrDefault(c.Id)
            }).ToList()
        };

        public async Task<ItemSchemaLookupColumnDto> AddLookupColumnAsync(Guid schemaId, string name, string? description = null)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            string trimmedName = name?.Trim() ?? string.Empty;
            if (trimmedName.Length == 0)
                throw new InvalidOperationException("The lookup column needs a name.");
            if (trimmedName.Length > 64)
                throw new InvalidOperationException("The lookup column name must be at most 64 characters long.");

            ItemSchema? schema = await ctx.ItemSchemas
                .Include(s => s.LookupColumns)
                .FirstOrDefaultAsync(s => s.Id == schemaId);
            if (schema == null)
                throw new InvalidOperationException($"Item schema {schemaId} was not found.");

            if (schema.LookupColumns.Any(c => c.Name == trimmedName))
                throw new InvalidOperationException($"The schema already has a lookup column named \"{trimmedName}\".");
            if (schema.AdditionalProperties.Any(p => p.Name == trimmedName))
                throw new InvalidOperationException($"\"{trimmedName}\" is already an additional property of the schema.");
            if (_reservedLookupColumnNames.Contains(trimmedName))
                throw new InvalidOperationException($"\"{trimmedName}\" collides with a base item property name and cannot be used as a lookup column name.");

            string? trimmedDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

            ItemSchemaLookupColumn column = new()
            {
                ItemSchemaId = schemaId,
                Name = trimmedName,
                Description = trimmedDescription
            };

            ctx.ItemSchemaLookupColumns.Add(column);
            await ctx.SaveChangesAsync();

            return new ItemSchemaLookupColumnDto()
            {
                Id = column.Id,
                Name = column.Name,
                Description = column.Description,
                EntryCount = 0
            };
        }

        public async Task RemoveLookupColumnAsync(Guid columnId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemSchemaLookupColumn? column = await ctx.ItemSchemaLookupColumns
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == columnId);

            if (column == null)
                throw new InvalidOperationException($"Lookup column {columnId} was not found.");

            // the column's entries are deleted by the foreign key cascade
            ctx.ItemSchemaLookupColumns.Remove(column);
            await ctx.SaveChangesAsync();
        }

        public async Task<ItemSchemaLookupColumnDetailDto?> GetItemSchemaLookupColumnAsync(Guid columnId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemSchemaLookupColumn? column = await ctx.ItemSchemaLookupColumns
                .AsNoTracking()
                .Include(c => c.Entries)
                .FirstOrDefaultAsync(c => c.Id == columnId);

            if (column == null)
                return null;

            return new ItemSchemaLookupColumnDetailDto()
            {
                Id = column.Id,
                Name = column.Name,
                Description = column.Description,
                Entries = column.Entries.OrderBy(e => e.Key).Select(e => new ItemSchemaLookupEntryDto()
                {
                    Id = e.Id,
                    Key = e.Key,
                    Value = e.Value
                }).ToList()
            };
        }

        public async Task<(int added, int updated)> AddLookupEntriesAsync(Guid columnId, IEnumerable<(string Key, string Value)> entries)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemSchemaLookupColumn? column = await ctx.ItemSchemaLookupColumns
                .AsTracking()
                .FirstOrDefaultAsync(c => c.Id == columnId);

            if (column == null)
                throw new InvalidOperationException($"Lookup column {columnId} was not found.");

            List<ItemSchemaLookupEntry> existingEntries = await ctx.ItemSchemaLookupEntries
                .AsTracking()
                .Where(e => e.ColumnId == columnId)
                .ToListAsync();

            Dictionary<string, ItemSchemaLookupEntry> entriesByKey = existingEntries.ToDictionary(e => e.Key);

            int added = 0;
            int updated = 0;

            foreach ((string key, string value) in entries)
            {
                string trimmedKey = key.Trim();
                string trimmedValue = value.Trim();

                if (trimmedKey.Length == 0 || trimmedValue.Length == 0)
                    throw new InvalidOperationException("Every entry needs a non-empty key and value.");

                if (entriesByKey.TryGetValue(trimmedKey, out ItemSchemaLookupEntry? existing))
                {
                    existing.Value = trimmedValue;
                    updated++;
                }
                else
                {
                    ItemSchemaLookupEntry newEntry = new()
                    {
                        ColumnId = columnId,
                        Key = trimmedKey,
                        Value = trimmedValue
                    };

                    ctx.ItemSchemaLookupEntries.Add(newEntry);
                    entriesByKey[trimmedKey] = newEntry;
                    added++;
                }
            }

            await ctx.SaveChangesAsync();

            return (added, updated);
        }

        public async Task RemoveLookupEntryAsync(Guid entryId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemSchemaLookupEntry? entry = await ctx.ItemSchemaLookupEntries
                .AsTracking()
                .FirstOrDefaultAsync(e => e.Id == entryId);

            if (entry == null)
                throw new InvalidOperationException($"Lookup entry {entryId} was not found.");

            ctx.ItemSchemaLookupEntries.Remove(entry);
            await ctx.SaveChangesAsync();
        }

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
