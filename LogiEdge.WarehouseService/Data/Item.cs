using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LogiEdge.CustomerService.Data;
using LogiEdge.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogiEdge.WarehouseService.Data
{
    public class Item : IDisposable
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("Customer")]
        [FilterableProperty]
        public required Guid CustomerId { get; set; }

        public Customer Customer { get; set; } = null!;

        [MaxLength(64)]
        [FilterableProperty]
        public required string ItemNumber { get; set; }

        public JsonDocument AdditionalProperties { get; set; } = JsonDocument.Parse("{}");

        [FilterableProperty]
        public string Comments { get; set; } = string.Empty;

        public List<ItemState> ItemStates { get; set; } = [];

        public bool InWarehouse
        {
            get
            {
                if (ItemStates.Count == 0)
                    return false;

                string location = ItemStates.MaxBy(x => x.Date)!.Location;
                bool inWarehouse = location != SpecialLocations.PRE_ARRIVAL && location != SpecialLocations.SHIPPED;
                return inWarehouse;
            }
        }

        public void Dispose()
        {
            AdditionalProperties?.Dispose();
        }
    }
}
