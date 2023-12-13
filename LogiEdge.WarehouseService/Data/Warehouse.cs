using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.CustomerService.Data;

namespace LogiEdge.WarehouseService.Data
{
    public class Warehouse
    {
        [Key]
        public int Id { get; set; }

        public required Customer Customer { get; set; }

        public required List<string> AdditionalProperties { get; set; }
        public required List<string> AdditionalPropertiesTypes { get; set; }

        public IEnumerable<(string propertyName, string typeName)> GetAdditionalPropertiesWithTypes()
        {
            return AdditionalProperties.Zip(AdditionalPropertiesTypes);
        }
    }
}
