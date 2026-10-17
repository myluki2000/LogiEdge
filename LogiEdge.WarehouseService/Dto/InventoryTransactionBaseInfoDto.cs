using LogiEdge.WarehouseService.Data.Transactions;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WarehouseService.Dto
{
    public class InventoryTransactionBaseInfoDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public required DateTime CreatedDate { get; set; }
        /// <summary>
        /// The date and time when the transaction's physical events actually happened. All item states
        /// created at booking are stamped with this date. Defaults to the creation date for new transactions.
        /// </summary>
        public required DateTime EventDate { get; set; }
        /// <summary>
        /// DateTime when the transaction is booked. Null if the transaction is still in DRAFT state.
        /// </summary>
        public DateTime? BookedDate { get; set; } = null;
        public required Guid CreatedByUserId { get; set; }
        public required string HandledByWorker { get; set; }
        public TransactionState State { get; set; }

        public bool HasInboundPart { get; set; }
        public bool HasOutboundPart { get; set; }
        public bool HasRelocationPart { get; set; }
    }
}
