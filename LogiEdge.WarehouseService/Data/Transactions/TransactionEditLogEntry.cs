using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace LogiEdge.WarehouseService.Data.Transactions;

/// <summary>
/// Audit entry written whenever a booked transaction is edited. Records who edited which part,
/// when, a short human-readable summary and the full old → new change set as JSON (including
/// the affected item ids), so that original values remain recoverable even though item states
/// are rewritten in place.
/// </summary>
public class TransactionEditLogEntry
{
    [Key]
    public Guid Id { get; set; }

    public Guid TransactionId { get; set; }
    public required InventoryTransaction Transaction { get; set; }

    /// <summary>
    /// The part of the transaction this edit applies to. Null for edits that span the whole
    /// transaction (e.g. metadata changes).
    /// </summary>
    public TransactionEditPartType? PartType { get; set; }

    public required DateTime EditedDate { get; set; }
    public required Guid EditedByUserId { get; set; }

    [MaxLength(255)]
    public required string ChangeSummary { get; set; }

    /// <summary>
    /// JSON document describing the change: old → new values per field plus the affected item ids.
    /// </summary>
    public JsonDocument ChangesJson { get; set; } = JsonDocument.Parse("{}");
}

public enum TransactionEditPartType
{
    METADATA,
    INBOUND,
    OUTBOUND,
}
