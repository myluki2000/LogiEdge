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
    public class WarehouseItemDto
    {
        [DisplayColumnProperty]
        public Guid Id { get; set; }

        public required ItemSchema ItemSchema { get; set; }

        public required Guid CustomerId { get; set; }

        [DisplayColumnProperty]
        [QueryFilterableProperty(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        public required Guid WarehouseId { get; set; }

        [DisplayColumnProperty]
        [QueryFilterableProperty(nameof(CustomerId))]
        public Warehouse Warehouse { get; set; } = null!;

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

        public required string? CurrentLocation { get; set; }

        public object? GetPropertyValue(string propertyName)
        {
            PropertyInfo? itemProperty = GetType().GetProperty(propertyName);

            if (itemProperty != null)
                return itemProperty.GetValue(this);

            ItemAdditionalPropertyDto? additionalProperty = AdditionalProperties.FirstOrDefault(x => x.Name == propertyName);
            return additionalProperty?.Value;
        }

        /// <summary>
        /// Creates a DTO from an Item entity. The ItemSchema (including its AdditionalProperties), Customer, Comments and
        /// ItemStates (including their Warehouses) navigations must be loaded, e.g. via .Include().
        /// </summary>
        public static WarehouseItemDto FromItem(Item item)
        {
            ItemState? currentState = item.ItemStates.OrderByDescending(st => st.Date).FirstOrDefault();

            return new WarehouseItemDto()
            {
                Id = item.Id,
                ItemSchema = item.ItemSchema!,
                CustomerId = item.CustomerId,
                Customer = item.Customer,
                WarehouseId = currentState?.WarehouseId ?? Guid.Empty,
                Warehouse = currentState?.Warehouse,
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
                CurrentLocation = currentState?.Location,
            };
        }
    }
}
