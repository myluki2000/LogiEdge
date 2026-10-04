using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Dto;

namespace LogiEdge.WarehouseService.Services.ItemsTable
{
    internal class ItemsTableSorter
    {
        public void Sort(ItemsTableResultDto items, ItemsTableSortParameters parameters)
        {
            if (parameters.SortByProperty == null)
                return;

            Func<ItemsTableResultDto.ItemsTableItem, object?>? lambda = null;
            if (typeof(ItemsTableResultDto.ItemsTableItem).GetProperty(parameters.SortByProperty) is { } itemInfo)
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

            items.Items = items.Items.OrderBy(lambda).ToList();
        }
    }
}
