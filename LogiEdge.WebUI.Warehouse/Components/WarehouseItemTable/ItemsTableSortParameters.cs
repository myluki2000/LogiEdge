using LogiEdge.Shared.Utility;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WebUI.Warehouse.Components.WarehouseItemTable
{
    public record ItemsTableSortParameters
    {
        public required string? GroupByProperty { get; init; }
        public required string? SortByProperty { get; init; }
        public required bool SortOrderDescending { get; init; }

        public virtual bool Equals(ItemsTableSortParameters? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return GroupByProperty == other.GroupByProperty && SortByProperty == other.SortByProperty && SortOrderDescending == other.SortOrderDescending;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GroupByProperty, SortByProperty, SortOrderDescending);
        }

        public Dictionary<string, StringValues> ToQueryParameters()
        {
            Dictionary<string, StringValues> result = [];
            if (SortByProperty is not null)
            {
                result["sortBy"] = SortByProperty;
            }
            result["orderDescending"] = SortOrderDescending.ToString();
            if (GroupByProperty is not null)
            {
                result["groupBy"] = GroupByProperty;
            }
            return result;
        }

        public static ItemsTableSortParameters FromQueryParameters(Dictionary<string, StringValues> queryParameters)
        {
            return new ItemsTableSortParameters()
            {
                SortByProperty = queryParameters
                    .PopWhere(param => param.Key == "sortBy")
                    .Select(param => param.Value.FirstOrDefault())
                    .FirstOrDefault(),
                SortOrderDescending = queryParameters
                    .PopWhere(param => param.Key == "orderDescending")
                    .Select(param => bool.Parse(param.Value.FirstOrDefault()!))
                    .FirstOrDefault(),
                GroupByProperty = queryParameters
                    .PopWhere(param => param.Key == "groupBy")
                    .Select(param => param.Value.FirstOrDefault())
                    .FirstOrDefault(),
            };
        }
    }
}
