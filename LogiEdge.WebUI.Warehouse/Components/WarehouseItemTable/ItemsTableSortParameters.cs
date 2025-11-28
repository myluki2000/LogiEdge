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
        public required string[] GroupByProperties { get; init; } = [];
        public required string? SortByProperty { get; init; }
        public required bool SortOrderDescending { get; init; }

        public ItemsTableSortParameters WithGroupByProperty(string propertyName)
        {
            return this with
            {
                GroupByProperties = this.GroupByProperties.Contains(propertyName)
                    ? this.GroupByProperties
                    : this.GroupByProperties.Append(propertyName).ToArray(),
            };
        }

        public ItemsTableSortParameters WithoutGroupByProperty(string propertyName)
        {
            return this with
            {
                GroupByProperties = this.GroupByProperties.Where(x => x != propertyName).ToArray(),
            };
        }

        public virtual bool Equals(ItemsTableSortParameters? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!ReferenceEquals(GroupByProperties, other.GroupByProperties))
                return false;
            return GroupByProperties.SequenceEqual(other.GroupByProperties) && SortByProperty == other.SortByProperty && SortOrderDescending == other.SortOrderDescending;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GroupByProperties?.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())), SortByProperty, SortOrderDescending);
        }

        public Dictionary<string, StringValues> ToQueryParameters()
        {
            Dictionary<string, StringValues> result = [];
            if (SortByProperty is not null)
            {
                result["sortBy"] = SortByProperty;
            }
            result["orderDescending"] = SortOrderDescending.ToString();
            if (GroupByProperties.Length > 0)
            {
                result["groupBy"] = new StringValues(GroupByProperties);
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
                GroupByProperties = queryParameters
                    .PopWhere(param => param.Key == "groupBy")
                    .Select(param => param.Value.Cast<string>())
                    .FirstOrDefault()?
                    .ToArray() ?? [],
            };
        }
    }
}
