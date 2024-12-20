using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Attributes;

namespace LogiEdge.WarehouseService.Data
{
    public class Item : IDisposable
    {
        [Key]
        [DisplayColumnProperty]
        public Guid Id { get; set; }

        [ForeignKey("Customer")]
        [FilterableProperty]
        public required Guid CustomerId { get; set; }

        [DisplayColumnProperty]
        public Customer Customer { get; set; } = null!;

        [MaxLength(64)]
        [FilterableProperty]
        [DisplayColumnProperty]
        public required string ItemNumber { get; set; }

        public JsonDocument AdditionalProperties { get; set; } = JsonDocument.Parse("{}");

        [DisplayColumnProperty]
        public string Comments { get; set; } = string.Empty;

        public List<ItemState> ItemStates { get; set; } = [];

        // TODO: This property should probably be removed because it does not account for the fact that we might be
        // inspecting the item at a different point in time
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
