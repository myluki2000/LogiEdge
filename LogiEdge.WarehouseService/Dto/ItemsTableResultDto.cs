using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Attributes;
using LogiEdge.WarehouseService.Data;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace LogiEdge.WarehouseService.Dto
{
    public class ItemsTableResultDto
    {
        public required List<ItemsTableItem> Items { get; set; }
        public required List<ItemPropertyInfo> ItemProperties { get; set; }

        public class ItemsTableItem
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

            public required List<AdditionalProperty> AdditionalProperties { get; set; }

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

                AdditionalProperty? additionalProperty = AdditionalProperties.FirstOrDefault(x => x.Name == propertyName);
                return additionalProperty?.Value;
            }

            public class AdditionalProperty
            {
                public required string Name { get; set; }
                public required object? Value { get; set; }
            }
        }

        public class ItemPropertyInfo
        {
            public string Name { get; set; }
            public Type Type { get; set; }
            public bool IsFilterable { get; set; }
            public bool IsDisplayable { get; set; }
        }
    }
}
