using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Services
{
    public class WarehouseItemService
    {
        private readonly IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory;

        internal WarehouseItemService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
        {
            this.warehouseDbContextFactory = warehouseDbContextFactory;
        }
        
        public async IAsyncEnumerable<Item> GetItemsForCustomerAsync(string customerPrefix, bool includePastItems = false)
        {
            WarehouseDbContext dbContext = await warehouseDbContextFactory.CreateDbContextAsync();

            IQueryable<Item> items = dbContext.Items
                .Where(x => includePastItems || (x.ShippedOn == null))
                .Where(x => x.Id.CustomerPrefix == customerPrefix);

            foreach (Item item in items)
                yield return item;
        }

        /// <summary>
        /// Gets the transactions for the specified item ordered from oldest transaction to newest.
        /// </summary>
        /// <param name="itemId">The item to get the transactions for</param>
        /// <returns>Returns an IAsyncEnumerable yielding the transactions ordered from oldest to newest.</returns>
        public async IAsyncEnumerable<ItemState> GetStatesOfItem(ItemId itemId)
        {
            WarehouseDbContext dbContext = await warehouseDbContextFactory.CreateDbContextAsync();

            IOrderedQueryable<ItemState> transactions = dbContext.HistoricItemStates
                .Where(x => x.ItemId.Equals(itemId))
                .OrderBy(x => x.TransactionDate);

            foreach (ItemTransaction transaction in transactions)
                yield return transaction;
        }
    }
}