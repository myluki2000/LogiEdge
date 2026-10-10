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
using static LogiEdge.WarehouseService.Dto.ItemsTableResultDto;

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
                .Where(st => stateIds.Contains(st.Id));


            ItemsTablePropertyFilter propertyFilter = new();
            q = propertyFilter.Filter(q, filterParams);

            List<Guid> itemSchemaIds = q.Select(x => x.Item.ItemSchemaId).Distinct().ToList();
            IReadOnlySet<ItemSchema> itemSchemas = await ctx.ItemSchemas.Where(sch => itemSchemaIds.Contains(sch.Id)).ToHashSetAsync();

            List<ItemPropertyInfoDto> itemProperties = [];

            // get a list of column property names which are allowed
            itemProperties.AddRange(typeof(ItemsTableItem)
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

            var result = new ItemsTableResultDto()
            {
                ItemProperties = itemProperties,
                Items = await q.Select(st => new ItemsTableItem()
                {
                    Id = st.ItemId,
                    ItemSchema = st.Item.ItemSchema,
                    CustomerId = st.Item.CustomerId,
                    Customer = st.Item.Customer,
                    WarehouseId = st.WarehouseId,
                    Warehouse = st.Warehouse,
                    ItemNumber = st.Item.ItemNumber,
                    AdditionalProperties = st.Item.ItemSchema.AdditionalProperties.Select(ap => new ItemAdditionalPropertyDto()
                    {
                        Name = ap.Name,
                        Value = st.Item.GetAdditionalProperty(ap),
                    }).ToList(),
                    Comments = st.Item.Comments,
                    EntryDate = st.Item.ItemStates
                        .AsQueryable()
                        .Where(ItemState.IsInWarehouseExpr)
                        .Min(st => st.Date),
                    CurrentLocation = st.Item.ItemStates.OrderByDescending(x => x.Date).FirstOrDefault().Location,
                }).ToListAsync(),
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
