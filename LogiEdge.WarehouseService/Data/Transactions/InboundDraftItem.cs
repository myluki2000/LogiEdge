using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Attributes;

namespace LogiEdge.WarehouseService.Data.Transactions
{
    public class InboundDraftItem
    {
        [Key]
        public Guid Id { get; set; }
        public Guid ItemSchemaId { get; set; }
        public required ItemSchema ItemSchema { get; set; }
        public Guid CustomerId { get; set; }
        public required Customer Customer { get; set; }
        [DisplayColumnProperty]
        [MaxLength(128)]
        public string ItemNumber { get; set; } = string.Empty;
        public JsonDocument AdditionalProperties { get; set; } = JsonDocument.Parse("{}");
        [DisplayColumnProperty]
        public string Comments { get; set; } = string.Empty;
        [DisplayColumnProperty]
        [MaxLength(64)]
        public string Location { get; set; } = string.Empty;
        [DisplayColumnProperty]
        public int Count { get; set; }
    }
}
