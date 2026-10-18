using System;
using System.Collections.Generic;
using System.Linq;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Dto;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    /// <summary>
    /// Pure planning logic for editing the inbound part of a booked transaction. Computes the re-booking
    /// diff between the part's current draft lines and the desired state, applies conflict rules to
    /// it, and returns a plan. It never throws for conflicts (the service turns violations into a
    /// TransactionEditConflictException) and mutates nothing: item history is read through the two
    /// delegates only, so this can be unit-tested without a database.
    /// </summary>
    public static class InboundPartEditPlanner
    {
        /// <summary>The kind of change planned for one draft line.</summary>
        public enum LineAction
        {
            Update,
            Add,
            Remove,
        }

        /// <summary>The current state of one booked inbound draft line and its created item.</summary>
        public sealed record CurrentInboundLine(
            Guid DraftItemId,
            Guid? CreatedItemId,
            string ItemNumber,
            Guid CustomerId,
            Guid ItemSchemaId,
            Guid WarehouseId,
            string Location,
            int Count,
            bool IsQuarantined,
            string Comments,
            IReadOnlyDictionary<string, object?> AdditionalProperties);

        /// <summary>One planned change. ChangedFields holds only the fields that actually differ (old → new).</summary>
        public sealed record PlannedLine(
            LineAction Action,
            Guid? DraftItemId,           // null for Add
            Guid? CreatedItemId,         // null for Add
            string ItemNumber,           // desired value (for Remove: the old value) — used in messages/audit
            IReadOnlyDictionary<string, (object? Old, object? New)> ChangedFields,
            IReadOnlyList<string> Violations);

        /// <summary>The result of planning an inbound part edit.</summary>
        public sealed record InboundPartEditPlan(IReadOnlyList<PlannedLine> Lines)
        {
            public bool HasViolations => Lines.Any(l => l.Violations.Count > 0);
            public IReadOnlyList<string> AllViolations => Lines.SelectMany(l => l.Violations).ToList();
        }

        /// <summary>
        /// Identity and arrival fields that may only change while the created item has no later history.
        /// Comments, IsQuarantined and AdditionalProperties are always editable (section 5.4).
        /// </summary>
        private static readonly string[] GatedFields = ["ItemNumber", "CustomerId", "ItemSchemaId", "WarehouseId", "Location"];

        private static readonly Dictionary<string, (object? Old, object? New)> NoChanges = new();
        private static readonly List<string> NoViolations = [];

        /// <summary>
        /// Computes the re-booking diff between the current and desired state of a booked inbound part and
        /// applies the section-5.4 conflict rules to it. Pure: reads via the two delegates only, mutates nothing.
        /// </summary>
        public static InboundPartEditPlan Plan(
            IReadOnlyList<CurrentInboundLine> currentLines,
            IReadOnlyList<InboundTransactionPartEditRequest.InboundDraftItemEdit> desiredLines,
            DateTime eventDate,
            Func<Guid, IEnumerable<ItemState>> getOtherStates,  // the created item's states EXCLUDING this transaction's own
            Func<Guid, bool> isReferencedByDraftOutbound)
        {
            Dictionary<Guid, CurrentInboundLine> currentById = currentLines.ToDictionary(l => l.DraftItemId);

            HashSet<Guid> seenDraftItemIds = [];
            foreach (InboundTransactionPartEditRequest.InboundDraftItemEdit desired in desiredLines)
            {
                if (desired.DraftItemId == null)
                    continue;

                if (!seenDraftItemIds.Add(desired.DraftItemId.Value))
                {
                    throw new ArgumentException($"Duplicate draft item ID {desired.DraftItemId} in the desired lines.");
                }

                if (!currentById.ContainsKey(desired.DraftItemId.Value))
                {
                    throw new ArgumentException($"Draft item ID {desired.DraftItemId} is not part of the current inbound part.");
                }
            }

            List<PlannedLine> planLines = [];

            foreach (InboundTransactionPartEditRequest.InboundDraftItemEdit desired in desiredLines)
            {
                if (desired.DraftItemId == null)
                {
                    planLines.Add(new PlannedLine(LineAction.Add, null, null, desired.ItemNumber, NoChanges, NoViolations));
                    continue;
                }

                CurrentInboundLine current = currentById[desired.DraftItemId.Value];
                Dictionary<string, (object? Old, object? New)> changedFields = Diff(current, desired);
                if (changedFields.Count == 0)
                    continue; // unchanged lines are omitted from the plan entirely

                List<string> violations = [];
                Guid? createdItemId = current.CreatedItemId;

                if (createdItemId == null)
                {
                    violations.Add($"Item {current.ItemNumber}: cannot be edited — the draft line has no link to its created item and must be relinked first.");
                }
                else
                {
                    string[] gatedChangedFields = GatedFields.Where(f => changedFields.ContainsKey(f)).ToArray();
                    if (gatedChangedFields.Length > 0)
                    {
                        List<ItemState> otherStates = getOtherStates(createdItemId.Value).ToList();
                        if (BookedTransactionConflictChecker.HasLaterStates(otherStates, eventDate))
                        {
                            ItemState? earliest = BookedTransactionConflictChecker.FindEarliestLaterState(otherStates, eventDate);
                            violations.Add(
                                $"Item {current.ItemNumber}: cannot change {string.Join(", ", gatedChangedFields)}" +
                                $" — the item has later history (earliest later state on {earliest!.Date:o}).");
                        }
                    }
                }

                planLines.Add(new PlannedLine(LineAction.Update, desired.DraftItemId.Value, createdItemId, desired.ItemNumber, changedFields, violations));
            }

            HashSet<Guid> keptDraftItemIds = desiredLines.Where(d => d.DraftItemId != null).Select(d => d.DraftItemId!.Value).ToHashSet();
            foreach (CurrentInboundLine current in currentLines)
            {
                if (keptDraftItemIds.Contains(current.DraftItemId))
                    continue;

                List<string> violations = [];
                Guid? createdItemId = current.CreatedItemId;

                if (createdItemId != null)
                {
                    List<ItemState> otherStates = getOtherStates(createdItemId.Value).ToList();
                    if (BookedTransactionConflictChecker.HasLaterStates(otherStates, eventDate))
                    {
                        ItemState? earliest = BookedTransactionConflictChecker.FindEarliestLaterState(otherStates, eventDate);
                        violations.Add(
                            $"Item {current.ItemNumber}: cannot be removed — the item has later history" +
                            $" (earliest later state on {earliest!.Date:o}).");
                    }

                    if (isReferencedByDraftOutbound(createdItemId.Value))
                    {
                        violations.Add($"Item {current.ItemNumber}: cannot be removed — it is selected in a DRAFT outbound transaction.");
                    }
                }

                planLines.Add(new PlannedLine(LineAction.Remove, current.DraftItemId, createdItemId, current.ItemNumber, NoChanges, violations));
            }

            return new InboundPartEditPlan(planLines);
        }

        /// <summary>
        /// Field-by-field diff of one draft line. Comments are compared trimmed; AdditionalProperties by
        /// name (ordinal) and value (Equals), where a missing name and an explicit null value count as the same.
        /// </summary>
        private static Dictionary<string, (object? Old, object? New)> Diff(
            CurrentInboundLine current,
            InboundTransactionPartEditRequest.InboundDraftItemEdit desired)
        {
            Dictionary<string, (object? Old, object? New)> changedFields = [];

            if (!string.Equals(current.ItemNumber, desired.ItemNumber, StringComparison.Ordinal))
                changedFields["ItemNumber"] = (current.ItemNumber, desired.ItemNumber);
            if (current.CustomerId != desired.CustomerId)
                changedFields["CustomerId"] = (current.CustomerId, desired.CustomerId);
            if (current.ItemSchemaId != desired.ItemSchemaId)
                changedFields["ItemSchemaId"] = (current.ItemSchemaId, desired.ItemSchemaId);
            if (current.WarehouseId != desired.WarehouseId)
                changedFields["WarehouseId"] = (current.WarehouseId, desired.WarehouseId);
            if (!string.Equals(current.Location, desired.Location, StringComparison.Ordinal))
                changedFields["Location"] = (current.Location, desired.Location);
            if (current.IsQuarantined != desired.IsQuarantined)
                changedFields["IsQuarantined"] = (current.IsQuarantined, desired.IsQuarantined);

            string newComments = desired.Comments.Trim();
            if (current.Comments.Trim() != newComments)
                changedFields["Comments"] = (current.Comments, newComments);

            Dictionary<string, object?> desiredProperties = desired.AdditionalProperties.ToDictionary(p => p.Name, p => p.Value);
            if (!AdditionalPropertiesEqual(current.AdditionalProperties, desiredProperties))
                changedFields["AdditionalProperties"] = (current.AdditionalProperties, desiredProperties);

            return changedFields;
        }

        /// <summary>
        /// Dictionary equality for additional properties: same names (ordinal) and values by Equals. A name
        /// that is missing on one side and explicitly null on the other counts as equal.
        /// </summary>
        private static bool AdditionalPropertiesEqual(
            IReadOnlyDictionary<string, object?> current,
            IReadOnlyDictionary<string, object?> desired)
        {
            HashSet<string> allNames = [.. current.Keys.Union(desired.Keys)];
            foreach (string name in allNames)
            {
                if (!Equals(current.GetValueOrDefault(name), desired.GetValueOrDefault(name)))
                    return false;
            }

            return true;
        }
    }
}
