using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using EntityFrameworkCore.Projectables;
using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Attributes;
using Microsoft.EntityFrameworkCore;

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

        /// <summary>
        /// The date when the item first entered one of the warehouses of the system, i.e. the date of the first ItemState which
        /// is not in the PRE_ARRIVAL or SHIPPED location.
        /// </summary>
        [DisplayColumnProperty]
        [Projectable(UseMemberBody = nameof(EntryDateImpl))]
        public DateTime? EntryDate { get; private set; }
        private DateTime? EntryDateImpl => ItemStates
            .Where(state =>
                state.Location != SpecialLocations.PRE_ARRIVAL && state.Location != SpecialLocations.SHIPPED)
            .Select(x => x.Date)
            .Min();

        [Projectable(UseMemberBody = nameof(CurrentStateImpl))]
        public ItemState? CurrentState { get; private set; }
        private ItemState? CurrentStateImpl => ItemStates.OrderByDescending(s => s.Date).FirstOrDefault();

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
