using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LogiEdge.WarehouseService.Services
{
    public class WarehouseItemService
    {
        private readonly IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory;

        internal WarehouseItemService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
        {
            this.warehouseDbContextFactory = warehouseDbContextFactory;
        }

        public async Task<Item> AddNewItem(Item item)
        {
            WarehouseDbContext dbContext = await warehouseDbContextFactory.CreateDbContextAsync();

            EntityEntry<Item> entry = await dbContext.CurrentItems.AddAsync(item);

            return entry.Entity;
        }
        
        public async IAsyncEnumerable<Item> GetItemsForCustomerAsync(string customerPrefix, bool includePastItems = false)
        {
            WarehouseDbContext dbContext = await warehouseDbContextFactory.CreateDbContextAsync();

            IQueryable<Item> items = dbContext.CurrentItems
                .Where(x => x.Id.CustomerPrefix == customerPrefix);

            foreach (Item item in items)
                yield return item;

            if (!includePastItems)
                yield break;

            items = dbContext.HistoricItems
                .Where(x => x.Id.CustomerPrefix == customerPrefix);

            foreach (Item item in items)
                yield return item;
        }

        /// <summary>
        /// Gets the states over time for the specified item ordered from oldest to newest.
        /// </summary>
        /// <param name="itemId">The item to get the transactions for</param>
        /// <returns>Returns an IAsyncEnumerable yielding the transactions ordered from oldest to newest.</returns>
        public async IAsyncEnumerable<ItemState> GetStatesOfItemAsync(ItemId itemId)
        {
            WarehouseDbContext dbContext = await warehouseDbContextFactory.CreateDbContextAsync();

            IOrderedQueryable<ItemState> itemStates = dbContext.ItemStates
                .Where(x => x.ItemId.Equals(itemId))
                .OrderBy(x => x.Date);

            foreach (ItemState state in itemStates)
                yield return state;
        }
    }
}