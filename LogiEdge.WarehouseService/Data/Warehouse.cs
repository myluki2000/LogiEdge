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
    public class Warehouse
    {
        [Key]
        public Guid Id { get; set; }
        public required string Name { get; set; }

        public required Customer Customer { get; set; }

        public required List<string> AdditionalProperties { get; set; }
        public required List<string> AdditionalPropertiesTypes { get; set; }

        public required List<Item> CurrentItems { get; init; }
        public required List<Item> HistoricItems { get; init; }

        public IEnumerable<(string propertyName, string typeName)> GetAdditionalPropertiesWithTypes()
        {
            return AdditionalProperties.Zip(AdditionalPropertiesTypes);
        }
    }
}
