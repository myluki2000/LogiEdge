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
        public Guid WarehouseId { get; set; }
        public required Warehouse Warehouse { get; set; }
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
        [Range(1, int.MaxValue)]
        public int Count { get; set; } = 1;
        [DisplayColumnProperty]
        public bool IsQuarantined { get; set; }

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
    }
}
