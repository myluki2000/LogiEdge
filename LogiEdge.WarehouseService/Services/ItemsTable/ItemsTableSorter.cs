using System;
using System.Collections.Generic;
using System.Linq;
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
                // return the value (not the property DTO), so that sorting works for any comparable value type
                lambda = it => (it.AdditionalProperties.FirstOrDefault(x => x.Name == parameters.SortByProperty)
                               ?? it.LookupProperties.FirstOrDefault(x => x.Name == parameters.SortByProperty))?.Value;
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
