using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.WarehouseService.Data;

namespace LogiEdge.WebUI.Warehouse.Components.WarehouseItemTable
{
    internal class ItemsTableSorter
    {
        public List<ItemState> Sort(List<ItemState> items, ItemsTableSortParameters parameters)
        {
            if (parameters.SortByProperty == null)
                return [..items];

            Func<ItemState, object?>? lambda = null;
            if (typeof(Item).GetProperty(parameters.SortByProperty) is { } itemInfo)
            {
                lambda = st => itemInfo.GetValue(st.Item);
            }
            else if (typeof(ItemState).GetProperty(parameters.SortByProperty) is { } stateInfo)
            {
                lambda = st => stateInfo.GetValue(st);
            }
            else
            {
                lambda = st => (st.Item.GetAdditionalProperty(parameters.SortByProperty) ?? null);
            }

            if (parameters.SortOrderDescending)
            {

                return items
                    .OrderByDescending(lambda)
                    .ToList();
            }

            return items
                .OrderBy(lambda)
                .ToList();
        }
    }
}
