using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiEdge.WarehouseService.Data.Transactions
{
    public class InboundTransactionPart : InventoryTransactionPart
    {
        public List<InboundDraftItem> DraftItems { get; set; } = [];
    }
}
