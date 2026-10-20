using LogiEdge.Shared.Attributes;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Dto;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;
using MigraDoc.DocumentObjectModel;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace LogiEdge.WarehouseService.Services.ItemsTable
{
    public class ItemsTableService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        public async Task<ItemsTableResultDto> GetItemsAtStateAsync(
            IEnumerable<Guid> stateIds,
            ItemsTableFilterParameters filterParams)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            IQueryable<ItemState> q = ctx.ItemsStates
                .Include(st => st.Item)
                    .ThenInclude(it => it.ItemSchema)
                        .ThenInclude(sch => sch.AdditionalProperties)
                .Include(st => st.Warehouse)
                .Include(st => st.Item)
                    .ThenInclude(it => it.Customer)
                .Include(st => st.Item)
                    .ThenInclude(it => it.ItemStates)
                .Where(st => stateIds.Contains(st.Id));


            ItemsTablePropertyFilter propertyFilter = new();
            q = propertyFilter.Filter(q, filterParams);

            List<Guid> itemSchemaIds = q.Select(x => x.Item.ItemSchemaId).Distinct().ToList();
            IReadOnlySet<ItemSchema> itemSchemas = await ctx.ItemSchemas.Where(sch => itemSchemaIds.Contains(sch.Id)).ToHashSetAsync();

            List<ItemPropertyInfoDto> itemProperties = [];

            // get a list of column property names which are allowed
            itemProperties.AddRange(typeof(ItemAtStateDto)
                .GetProperties()
                .Select(p => new ItemPropertyInfoDto()
                {
                    Name = p.Name,
                    IsDisplayable = Attribute.IsDefined(p, typeof(DisplayColumnPropertyAttribute)),
                    IsFilterable = Attribute.IsDefined(p, typeof(QueryFilterablePropertyAttribute)),
                    Type = p.PropertyType,
                }));
            itemProperties.AddRange(itemSchemas.SelectMany(sch => sch.AdditionalProperties).Select(pr => new ItemPropertyInfoDto()
            {
                Name = pr.Name,
                Type = pr.Type.ToType(),
                IsDisplayable = true,
                IsFilterable = true,
            }));

            // "st" is the item state selected for the requested point in time (see ItemsTableStateFilter), so mapping
            // it via ItemAtStateDto.FromItem yields the item's properties plus those of exactly that state. Tracking is
            // required because the Item -> ItemStates Include (needed for EntryDate) would otherwise form a cycle, which
            // EF Core does not allow in no-tracking queries.
            List<ItemState> selectedStates = await q.AsTracking().ToListAsync();

            var result = new ItemsTableResultDto()
            {
                ItemProperties = itemProperties,
                Items = selectedStates.Select(st => ItemAtStateDto.FromItem(st.Item, st)).ToList(),
            };

            return result;
        }

        public async Task<ItemsTableResultDto> GetItemsAsync(IEnumerable<Guid> itemIds,
            ItemsTableFilterParameters filterParams,
            ItemsTableStateParameters stateParams)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemsTableStateFilter stateFilter = new();
            List<Guid> stateIds = await stateFilter.Filter(ctx.Items.Where(it => itemIds.Contains(it.Id)), stateParams).Select(x => x.Id).ToListAsync();

            return await GetItemsAtStateAsync(stateIds, filterParams);
        }

        public async Task<List<ItemStateDto>> GetItemStatesAsync(Guid itemId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            return await ctx.ItemsStates
                .Where(st => st.ItemId == itemId)
                .Include(st => st.Warehouse)
                .OrderByDescending(st => st.Date)
                .Select(st => new ItemStateDto()
                {
                    Id = st.Id,
                    Date = st.Date,
                    Location = st.Location,
                    IsQuarantined = st.IsQuarantined,
                    WarehouseId = st.WarehouseId,
                    Warehouse = st.Warehouse,
                    RelatedTransactionId = st.RelatedTransactionId,
                })
                .ToListAsync();
        }

        public async Task<ItemsTableResultDto> GetAllItemsAsync(
            ItemsTableFilterParameters filterParams,
            ItemsTableStateParameters stateParams)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemsTableStateFilter stateFilter = new();
            List<Guid> stateIds = await stateFilter.Filter(ctx.Items, stateParams).Select(x => x.Id).ToListAsync();

            return await GetItemsAtStateAsync(stateIds, filterParams);
        }
    }
}
