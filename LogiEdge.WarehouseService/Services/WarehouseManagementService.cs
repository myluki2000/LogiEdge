using LogiEdge.CustomerService.Data;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LogiEdge.WarehouseService.Services
{
    public class WarehouseManagementService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        public Warehouse CreateWarehouse(Warehouse warehouse)
        {
            WarehouseDbContext context = warehouseDbContextFactory.CreateDbContext();

            EntityEntry<Warehouse> entry = context.Warehouses.Add(warehouse);

            context.SaveChanges();

            return entry.Entity;
        }

        public IEnumerable<Warehouse> GetWarehouses()
        {
            WarehouseDbContext context = warehouseDbContextFactory.CreateDbContext();
            return context.Warehouses;
        }

        public IEnumerable<Warehouse> GetWarehousesForCustomer(Customer customer)
        {
            WarehouseDbContext context = warehouseDbContextFactory.CreateDbContext();
            return context.Warehouses.Where(x => x.Customer == customer);
        }

        public Item AddNewItem(Item item)
        {
            WarehouseDbContext context = warehouseDbContextFactory.CreateDbContext();

            EntityEntry<Item> entry = context.CurrentItems.Add(item);

            context.SaveChanges();

            return entry.Entity;
        }
        
        public IEnumerable<Item> GetItemsForCustomer(Customer customer, bool includePastItems = false)
        {
            WarehouseDbContext dbContext = warehouseDbContextFactory.CreateDbContext();

            IQueryable<Item> items = dbContext.CurrentItems
                .Where(x => x.Id.CustomerPrefix == customer.Abbreviation);

            foreach (Item item in items)
                yield return item;

            if (!includePastItems)
                yield break;

            items = dbContext.HistoricItems
                .Where(x => x.Id.CustomerPrefix == customer.Abbreviation);

            foreach (Item item in items)
                yield return item;
        }

        /// <summary>
        /// Gets the states over time for the specified item ordered from oldest to newest.
        /// </summary>
        /// <param name="itemId">The item to get the transactions for</param>
        /// <returns>Returns an IAsyncEnumerable yielding the transactions ordered from oldest to newest.</returns>
        public IEnumerable<ItemState> GetStatesOfItem(ItemId itemId)
        {
            WarehouseDbContext dbContext = warehouseDbContextFactory.CreateDbContext();

            IOrderedQueryable<ItemState> itemStates = dbContext.ItemStates
                .Where(x => x.ItemId.Equals(itemId))
                .OrderBy(x => x.Date);

            foreach (ItemState state in itemStates)
                yield return state;
        }
    }
}