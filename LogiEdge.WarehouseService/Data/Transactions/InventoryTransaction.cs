using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LogiEdge.BaseService.Data;

namespace LogiEdge.WarehouseService.Data.Transactions;

public abstract class InventoryTransaction
{
    [Key]
    public Guid Id { get; set; }
    public required DateTime Date { get; set; }
    public required Guid CreatedByUserId { get; set; }
    public required string HandledByWorker { get; set; }
    public string Comments { get; set; } = string.Empty;
    public List<Guid> AttachmentIds { get; set; } = [];
    public TransactionState State { get; set; }
    /// <summary>
    /// If this transaction has been booked, this will contain the item states created
    /// on the items affected by the transaction.
    /// If this transaction has not been booked, this will be NULL.
    /// </summary>
    public List<ItemState>? NewItemStates { get; set; } = null;

    public IEnumerable<Item>? AffectedItems => NewItemStates?.Select(st => st.Item);
}
    public enum TransactionState
{
    DRAFT,
    BOOKED,
}
