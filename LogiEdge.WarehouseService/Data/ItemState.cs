using System.ComponentModel.DataAnnotations;
using LogiEdge.Shared.Attributes;

namespace LogiEdge.WarehouseService.Data
{
    public class ItemState
    {
        public Guid Id { get; set; }

        public Guid ItemId { get; set; }

        public Item Item { get; set; } = null!;

        [QueryFilterableProperty]
        public required DateTime Date { get; set; }

        public required Guid WarehouseId { get; set; }

        [QueryFilterableProperty(nameof(WarehouseId))]
        [DisplayColumnProperty]
        public Warehouse Warehouse { get; set; } = null!;

        [QueryFilterableProperty]
        [DisplayColumnProperty]
        [MaxLength(32)]
        public required string Location { get; set; }
    }
}
