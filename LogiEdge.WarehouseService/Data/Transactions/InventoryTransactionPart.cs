using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace LogiEdge.WarehouseService.Data.Transactions
{
    public class InventoryTransactionPart
    {
        [Key]
        public Guid Id { get; set; }
        public required InventoryTransaction Transaction { get; set; }

        /// <summary>
        /// If this transaction has been booked, this will contain the item states created
        /// on the items affected by the transaction.
        /// If this transaction has not been booked, this will be NULL.
        /// </summary>
        public List<ItemState>? NewItemStates { get; set; } = null;
    }
}
