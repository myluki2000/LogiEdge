using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data
{
    public class ItemState
    {
        public Guid Id { get; set; }
        public required Item ItemId { get; set; }
        public required DateTime Date { get; set; }
        public required Warehouse Warehouse { get; set; }
        public required string Location { get; set; }
    }
}
