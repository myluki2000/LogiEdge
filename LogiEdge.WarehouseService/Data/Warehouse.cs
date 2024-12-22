using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.CustomerService.Data;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Data
{
    public class Warehouse : IComparable
    {
        [Key]
        public Guid Id { get; set; }
        public required string Name { get; set; }

        public required List<string> AdditionalProperties { get; set; }
        public required List<string> AdditionalPropertiesTypes { get; set; }

        public List<Item> Items { get; set; } = null!;

        public IEnumerable<(string propertyName, string typeName)> GetAdditionalPropertiesWithTypes()
        {
            return AdditionalProperties.Zip(AdditionalPropertiesTypes);
        }

        public int CompareTo(object? obj)
        {
            if (obj is not Warehouse otherWarehouse)
                return 1;

            return string.Compare(Name, otherWarehouse.Name, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
