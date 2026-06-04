using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LogiEdge.BaseService.Data;

namespace LogiEdge.WarehouseService.Data.Transactions;

public class InventoryTransaction
{
    [Key]
    public Guid Id { get; set; }
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    public required DateTime CreatedDate { get; set; }
    /// <summary>
    /// DateTime when the transaction is booked. Null if the transaction is still in DRAFT state.
    /// </summary>
    public DateTime? BookedDate { get; set; } = null;
    public required Guid CreatedByUserId { get; set; }
    public required string HandledByWorker { get; set; }
    public SortedSet<Comment> Comments { get; set; } = [];
    public List<Guid> AttachmentIds { get; set; } = [];
    public TransactionState State { get; set; }

    public Guid? InboundTransactionPartId { get; set; } = null;
    public InboundTransactionPart? InboundTransactionPart { get; set; } = null;

    public Guid? OutboundTransactionPartId { get; set; } = null;
    public OutboundTransactionPart? OutboundTransactionPart { get; set; } = null;

    public Guid? RelocationTransactionPartId { get; set; } = null;
    public RelocationTransactionPart? RelocationTransactionPart { get; set; } = null;
}
public enum TransactionState
{
    DRAFT,
    BOOKED,
}
