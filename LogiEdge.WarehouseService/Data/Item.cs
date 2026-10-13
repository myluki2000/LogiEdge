using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

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

            return GetAdditionalProperty(property);
        }

        public object? GetAdditionalProperty(ItemSchemaProperty property)
        {
            JsonElement element = AdditionalProperties.RootElement.GetProperty(property.Name);
            object? value = property.Type switch
            {
                ItemSchemaProperty.SupportedTypes.String => element.GetString(),
                ItemSchemaProperty.SupportedTypes.Int32 => element.GetInt32(),
                ItemSchemaProperty.SupportedTypes.Single => element.GetSingle(),
                ItemSchemaProperty.SupportedTypes.Boolean => element.GetBoolean(),
                ItemSchemaProperty.SupportedTypes.DateTime => element.GetDateTime(),
                _ => throw new Exception($"Unknown type '{property.Type}' for property '{property.Name}'.")
            };

            return value;
        }

        public void Dispose()
        {
            AdditionalProperties?.Dispose();
        }
    }
}
