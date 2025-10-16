using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data.Transactions
{
    public class InboundTransaction : InventoryTransaction
    {
        public List<InboundDraftItem> DraftItems { get; set; } = [];
        public Guid? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; } = null!;
    }
}
