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

        [ForeignKey("ItemSchema")]
        public required Guid ItemSchemaId { get; set; }
        public ItemSchema ItemSchema { get; set; }

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
        public required SortedSet<Comment> Comments { get; set; }

        public required List<ItemState> ItemStates { get; set; }

        // TODO: This property should probably be removed because it does not account for the fact that we might be
        // inspecting the item at a different point in time
        [NotMapped]
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
        [NotMapped]
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
        /// Item.ItemSchema must be .Include()-ed for this method to be used.
        /// </summary>
        public object? GetAdditionalProperty(string propertyName)
        {
            if (ItemSchema == null)
                throw new Exception("ItemSchema of this item needs to be .Include()ed to use .GetAdditionalProperty() method.");

            ItemSchemaProperty? property = ItemSchema.AdditionalProperties.FirstOrDefault(pr => pr.Name == propertyName);
            if (property == null)
                return null;
            
            JsonElement element = AdditionalProperties.RootElement.GetProperty(propertyName);
            object? value = property.Type switch
            {
                ItemSchemaProperty.SupportedTypes.String => element.GetString(),
                ItemSchemaProperty.SupportedTypes.Int32 => element.GetInt32(),
                ItemSchemaProperty.SupportedTypes.Single => element.GetSingle(),
                ItemSchemaProperty.SupportedTypes.Boolean => element.GetBoolean(),
                ItemSchemaProperty.SupportedTypes.DateTime => element.GetDateTime(),
                _ => throw new Exception($"Unknown type '{property.Type}' for property '{propertyName}'.")
            };

            return value;
        }

        public void Dispose()
        {
            AdditionalProperties?.Dispose();
        }
    }
}
