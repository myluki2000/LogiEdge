using LogiEdge.WarehouseService.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WarehouseService.Dto
{
    public abstract class InventoryTransactionPartDto
    {
        public Guid TransactionId { get; set; }
        /// <summary>
        /// If parent transaction this part is a part of has been booked, this will contain the
        /// item states created on the items affected by the transaction.
        /// If this transaction has not been booked, this will be NULL.
        /// </summary>
        public List<ItemState>? NewItemStates { get; set; } = null;
    }
}
