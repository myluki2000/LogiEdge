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

        public required ItemSchema ItemSchema { get; set; }

        [ForeignKey("Customer")]
        public required Guid CustomerId { get; set; }

        [DisplayColumnProperty]
        [QueryFilterableProperty(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        [MaxLength(64)]
        [QueryFilterableProperty]
        [DisplayColumnProperty]
        public required string ItemNumber { get; set; }

        public JsonDocument AdditionalProperties { get; set; } = JsonDocument.Parse("{}");

        [DisplayColumnProperty]
        public string Comments { get; set; } = string.Empty;

        public List<ItemState> ItemStates { get; set; } = [];

        public List<ItemState> PendingItemStates { get; set; } = [];

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

        /// <summary>
        /// The date when the item first entered one of the warehouses of the system, i.e. the date of the first ItemState which
        /// is not in the PRE_ARRIVAL or SHIPPED location.
        /// </summary>
        [DisplayColumnProperty]
        public DateTime? EntryDate
        {
            get
            {
                ItemState? state = ItemStates
                    .Where(st => st.Location != SpecialLocations.PRE_ARRIVAL)
                    .MinBy(st => st.Date);

                return state?.Date;
            }
        }

        /// <summary>
        /// Returns the value of the additional property with the given property name, deserialized to an object of appropriate type.
        /// If the item does not have an additional property with the given name, returns null.
        ///
        /// Item.ItemSchema must be .Included() for this method to be used.
        /// </summary>
        public object? GetAdditionalProperty(string propertyName)
        {
            if (ItemSchema == null)
                throw new Exception("ItemSchema of this item needs to be .Include()ed to use .GetAdditionalProperty() method.");

            int index = ItemSchema.AdditionalProperties.IndexOf(propertyName);
            if (index == -1)
                return null;

            string type = ItemSchema.AdditionalPropertiesTypes[index];

            JsonElement element = AdditionalProperties.RootElement.GetProperty(propertyName);
            object? value = type switch
            {
                nameof(String) => element.GetString(),
                nameof(Int32) => element.GetInt32(),
                nameof(Single) => element.GetSingle(),
                nameof(Boolean) => element.GetBoolean(),
                nameof(DateTime) => element.GetDateTime(),
                _ => throw new Exception($"Unknown type '{type}' for property '{propertyName}'.")
            };

            return value;
        }

        public void Dispose()
        {
            AdditionalProperties?.Dispose();
        }
    }
}
