using LogiEdge.WarehouseService.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WebUI.Warehouse.Components.WarehouseItemTable
{
    internal class ItemsTableStateFilter
    {
        public IQueryable<ItemState> Filter(IQueryable<Item> itemsQueryableInput, ItemsTableStateParameters parameters) {
            IQueryable<ItemState> itemStatesQueryable = parameters.AtTime.HasValue
                ? itemsQueryableInput
                    .Select(it => it.ItemStates
                        .OrderByDescending(st => st.Date)
                        .First(st => st.Date <= parameters.AtTime))
                : itemsQueryableInput
                    .Select(it => it.ItemStates
                        .OrderByDescending(st => st.Date)
                        .First());

            if (!parameters.ShowShipped)
            {
                itemStatesQueryable = itemStatesQueryable
                    .Where(st => st.Location != SpecialLocations.SHIPPED);
            }

            return itemStatesQueryable;
        }
    }
}
