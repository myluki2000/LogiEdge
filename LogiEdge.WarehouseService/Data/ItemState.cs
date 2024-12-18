using LogiEdge.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data
{
    public class ItemState
    {
        public Guid Id { get; set; }

        public Guid ItemId { get; set; }

        public Item Item { get; set; } = null!;

        [FilterableProperty]
        public required DateTime Date { get; set; }

        [FilterableProperty]
        public required Guid WarehouseId { get; set; }

        public Warehouse Warehouse { get; set; } = null!;

        [FilterableProperty]
        [MaxLength(32)]
        public required string Location { get; set; }
    }
}
