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
using LogiEdge.Shared;

namespace LogiEdge.WarehouseService.Data.Transactions;

public class InventoryTransaction
{
    [Key]
    public Guid Id { get; set; }
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;
    public required DateTime CreatedDate { get; set; }
    /// <summary>
    /// When the transaction's physical events (arrivals, moves, shipments) actually happened.
    /// Defaults to CreatedDate at creation and is editable while the transaction is in DRAFT state.
    /// Booking uses this date for all item states it creates; BookedDate remains the audit timestamp.
    /// </summary>
    public required DateTime EventDate { get; set; }
    /// <summary>
    /// DateTime when the transaction is booked. Null if the transaction is still in DRAFT state.
    /// </summary>
    public DateTime? BookedDate { get; set; } = null;
    /// <summary>
    /// Concurrency token. Bumped by WarehouseDbContext on every save of a modified transaction so that
    /// concurrent edits to the same transaction fail with a DbUpdateConcurrencyException instead of
    /// silently overwriting each other.
    /// </summary>
    public long RowVersion { get; set; }
    public required Guid CreatedByUserId { get; set; }
    public required string HandledByWorker { get; set; }
    public SortedSet<Comment> Comments { get; set; } = [];
    public List<Guid> AttachmentIds { get; set; } = [];
    public TransactionState State { get; set; }

    public InboundTransactionPart? InboundTransactionPart { get; set; } = null;
    public OutboundTransactionPart? OutboundTransactionPart { get; set; } = null;
    public RelocationTransactionPart? RelocationTransactionPart { get; set; } = null;
}
public enum TransactionState
{
    DRAFT,
    BOOKED,
}
