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
        public ItemId ItemId { get; set; }
        public DateTime Date { get; set; }
        public string? Location { get; set; }
    }
}
