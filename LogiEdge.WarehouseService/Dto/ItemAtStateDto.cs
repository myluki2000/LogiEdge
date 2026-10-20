using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Attributes;
using LogiEdge.WarehouseService.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace LogiEdge.WarehouseService.Dto
{
    /// <summary>
    /// Represents an item together with one of its states, i.e. the item as it was at a particular point in time.
    /// Contains the properties of the item entity as well as the properties of that specific ItemState.
    /// </summary>
    public class ItemAtStateDto
    {
        // ---- Properties of the Item entity ----

        [DisplayColumnProperty]
        public Guid Id { get; set; }

        public required ItemSchema ItemSchema { get; set; }

        public required Guid CustomerId { get; set; }

        [DisplayColumnProperty]
        [QueryFilterableProperty(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        [MaxLength(64)]
        [QueryFilterableProperty]
        [DisplayColumnProperty]
        public required string ItemNumber { get; set; }

        public required List<ItemAdditionalPropertyDto> AdditionalProperties { get; set; }

        [DisplayColumnProperty]
        public required SortedSet<Comment> Comments { get; set; }

        /// <summary>
        /// The date when the item first entered one of the warehouses of the system, i.e. the date of the first ItemState which
        /// is not in the PRE_ARRIVAL or SHIPPED location.
        /// </summary>
        [DisplayColumnProperty]
        public DateTime? EntryDate { get; set; }

        // ---- Properties of the ItemState this DTO represents ----

        [DisplayColumnProperty]
        public required DateTime Date { get; set; }

        [QueryFilterableProperty]
        [DisplayColumnProperty]
        public required string Location { get; set; }

        public required Guid WarehouseId { get; set; }

        [DisplayColumnProperty]
        [QueryFilterableProperty(nameof(WarehouseId))]
        public required Warehouse Warehouse { get; set; } = null!;

        public required bool IsQuarantined { get; set; }

        public Guid? RelatedTransactionId { get; set; }

        public object? GetPropertyValue(string propertyName)
        {
            PropertyInfo? itemProperty = GetType().GetProperty(propertyName);

            if (itemProperty != null)
                return itemProperty.GetValue(this);

            ItemAdditionalPropertyDto? additionalProperty = AdditionalProperties.FirstOrDefault(x => x.Name == propertyName);
            return additionalProperty?.Value;
        }

        /// <summary>
        /// Creates a DTO from an Item entity and the specific ItemState to represent. The Item's ItemSchema (including its
        /// AdditionalProperties), Customer, Comments and ItemStates navigations must be loaded, e.g. via .Include(), as well
        /// as the state's Warehouse navigation.
        /// </summary>
        public static ItemAtStateDto FromItem(Item item, ItemState state)
        {
            return new ItemAtStateDto()
            {
                Id = item.Id,
                ItemSchema = item.ItemSchema!,
                CustomerId = item.CustomerId,
                Customer = item.Customer,
                ItemNumber = item.ItemNumber,
                AdditionalProperties = item.ItemSchema.AdditionalProperties
                    .Select(ap => new ItemAdditionalPropertyDto()
                    {
                        Name = ap.Name,
                        Value = item.GetAdditionalProperty(ap),
                        Type = ap.Type,
                    })
                    .ToList(),
                Comments = item.Comments,
                EntryDate = item.ItemStates.Where(st => st.IsInWarehouse).Min(st => (DateTime?)st.Date),
                Date = state.Date,
                Location = state.Location,
                WarehouseId = state.WarehouseId,
                Warehouse = state.Warehouse,
                IsQuarantined = state.IsQuarantined,
                RelatedTransactionId = state.RelatedTransactionId,
            };
        }
    }
}
