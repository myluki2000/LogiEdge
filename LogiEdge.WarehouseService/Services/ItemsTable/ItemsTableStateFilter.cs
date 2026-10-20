using LogiEdge.WarehouseService.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WarehouseService.Services.ItemsTable
{
    internal class ItemsTableStateFilter
    {
        public IQueryable<ItemState> Filter(IQueryable<Item> itemsQueryableInput, ItemsTableStateParameters parameters) {
            // when filtering for a point in time, exclude items which did not exist yet at that time; selecting the
            // matching state with .First() would otherwise throw for them
            if (parameters.AtTime.HasValue)
            {
                itemsQueryableInput = itemsQueryableInput.Where(it => it.ItemStates.Any(st => st.Date <= parameters.AtTime));
            }

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
