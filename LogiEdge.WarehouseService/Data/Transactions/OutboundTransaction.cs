using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data.Transactions
{
    public class OutboundTransaction : InventoryTransaction
    {
        /// <summary>
        /// If this transaction is in DRAFT state, this will contain the items which
        /// have been selected for outbound.
        /// </summary>
        public List<Item>? DraftSelectedItems { get; set; }
    }
}
