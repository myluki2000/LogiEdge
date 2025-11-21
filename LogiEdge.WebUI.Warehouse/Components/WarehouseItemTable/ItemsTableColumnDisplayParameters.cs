using LogiEdge.WarehouseService.Data;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.Shared.Utility;

namespace LogiEdge.WebUI.Warehouse.Components.WarehouseItemTable
{
    public record ItemsTableColumnDisplayParameters()
    {
        public IReadOnlyList<string> ColumnsToDisplay { get; init; } = [];

        public virtual bool Equals(ItemsTableColumnDisplayParameters? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ColumnsToDisplay.SequenceEqual(other.ColumnsToDisplay);
        }

        public override int GetHashCode()
        {
            return ColumnsToDisplay.GetHashCode();
        }

        /// <summary>
        /// Returns the columns to display according to the query parameters, or a default selection of columns if none were specified.
        /// </summary>
        /// <param name="itemSchemas">The item schemas of the items.</param>
        /// <returns></returns>
        public IEnumerable<string> GetColumnsToDisplayOrDefault(IReadOnlySet<ItemSchema> itemSchemas)
        {
            if (ColumnsToDisplay.Count > 0)
                return ColumnsToDisplay;
            
            return
                new List<string>
                {
                    nameof(Item.Id),
                    nameof(Item.Customer),
                    nameof(Item.ItemNumber),
                    nameof(ItemState.Warehouse),
                    nameof(ItemState.Location),
                    nameof(Item.EntryDate)
                }.Concat(itemSchemas.SelectMany(sch => sch.AdditionalProperties).Select(pr => pr.Name));

        }

        public Dictionary<string, StringValues> ToQueryParameters()
        {
            Dictionary<string, StringValues> result = [];
            foreach (string col in ColumnsToDisplay)
            {
                if (result.ContainsKey("col"))
                {
                    var existing = result["col"];
                    result["col"] = StringValues.Concat(existing, col);
                }
                else
                {
                    result["col"] = new StringValues(col);
                }
            }
            return result;
        }

        public static ItemsTableColumnDisplayParameters FromQueryParameters(Dictionary<string, StringValues> queryParameters)
        {
            List<string> columnsToDisplayParam = queryParameters
                .PopWhere(x => x.Key == "col")
                .SelectMany(x => x.Value.ToList())
                .OfType<string>()
                .ToList();

            return new ItemsTableColumnDisplayParameters() { ColumnsToDisplay = columnsToDisplayParam };
        }
    }
}
