using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Dto;

namespace LogiEdge.WarehouseService.Services.ItemsTable
{
    public class ItemsTableSorter
    {
        // TODO: Should probably be moved to the WebUI.Warehouse project, as it is used directly in components, not by services.

        public void Sort(ItemsTableResultDto items, ItemsTableSortParameters parameters)
        {
            if (parameters.SortByProperty == null)
                return;

            Func<ItemAtStateDto, object?>? lambda = null;
            if (typeof(ItemAtStateDto).GetProperty(parameters.SortByProperty) is { } itemInfo)
            {
                lambda = it => itemInfo.GetValue(it);
            }
            else
            {
                lambda = it => (it.AdditionalProperties.FirstOrDefault(x => x.Name == parameters.SortByProperty) ?? null);
            }

            if (parameters.SortOrderDescending)
            {
                items.Items = items.Items.OrderByDescending(lambda).ToList();
            }
            else
            {
                items.Items = items.Items.OrderBy(lambda).ToList();
            }
        }
    }
}
