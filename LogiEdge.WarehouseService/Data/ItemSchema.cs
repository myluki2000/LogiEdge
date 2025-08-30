using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.CustomerService.Data;

namespace LogiEdge.WarehouseService.Data
{
    public class ItemSchema
    {
        [Key]
        public Guid Id { get; set; }
        /// <summary>
        /// Name of the item schema. This is used to identify the schema and should be unique.
        /// </summary>
        [MaxLength(128)]
        public required string Name { get; set; }
        /// <summary>
        /// Items that use this schema have these additional properties.
        /// </summary>
        public required List<ItemSchemaProperty> AdditionalProperties { get; set; }
        /// <summary>
        /// List of customers whose items use this schema.
        /// </summary>
        public List<Customer> Customers { get; set; } = [];
    }
}
