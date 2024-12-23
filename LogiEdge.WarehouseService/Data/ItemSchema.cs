using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data
{
    public class ItemSchema
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(128)]
        public required string Name { get; set; }
        public required List<string> AdditionalProperties { get; set; }
        public required List<string> AdditionalPropertiesTypes { get; set; }

        public IEnumerable<(string propertyName, string typeName)> GetAdditionalPropertiesWithTypes()
        {
            return AdditionalProperties.Zip(AdditionalPropertiesTypes);
        }
    }
}
