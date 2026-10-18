using System;
using System.Collections.Generic;
using System.Linq;
using LogiEdge.WarehouseService.Data;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    /// <summary>
    /// Pure planning logic for editing the outbound part of a booked transaction. Computes the diff between
    /// the shipment's currently selected items and the desired selection, applies conflict rules to it, and
    /// returns a plan. It never throws for conflicts (the service turns violations into a
    /// TransactionEditConflictException) and mutates nothing: item history is read through the two delegates
    /// only, so this can be unit-tested without a database.
    ///
    /// Design decision (a deliberate, documented deviation from the literal section-5.4 table): the Add rule
    /// reuses BookingTimelineValidator.GetOutboundBookingViolation — the booking-time validation — instead of
    /// the looser BookedTransactionConflictChecker.CanShipItem. An item added to a shipment at edit time must
    /// pass exactly the same timeline rules as at booking time (section 5.7: timelines are consistent by
    /// construction, so edit checks only look forward). The validator subsumes both section-5.4 outbound rules
    /// (no state dated at/after Te with conservative ties; latest state not already SHIPPED) and additionally
    /// requires a prior in-warehouse state — an item that has never been observed in the warehouse (no states,
    /// or latest prior state PRE_ARRIVAL) must not become shippable through editing.
    /// </summary>
    public static class OutboundPartEditPlanner
    {
        /// <summary>The kind of change planned for one selected item.</summary>
        public enum ItemAction
        {
            Add,
            Remove,
        }

        /// <summary>One planned change to the shipment selection. Violations is empty when the change is allowed.</summary>
        public sealed record PlannedItem(
            ItemAction Action,
            Guid ItemId,
            string ItemNumber,          // for messages/audit
            IReadOnlyList<string> Violations);

        /// <summary>The result of planning an outbound part edit.</summary>
        public sealed record OutboundPartEditPlan(IReadOnlyList<PlannedItem> Items)
        {
            public bool HasViolations => Items.Any(i => i.Violations.Count > 0);
            public IReadOnlyList<string> AllViolations => Items.SelectMany(i => i.Violations).ToList();
        }

        /// <summary>
        /// Computes the diff between the current and desired outbound selection and applies the section-5.4
        /// conflict rules to it: Add via BookingTimelineValidator (booking-time timeline validation), Remove
        /// via BookedTransactionConflictChecker (no later history). Pure: reads via the two delegates only,
        /// mutates nothing. Items present in both selections are omitted from the plan entirely.
        /// </summary>
        public static OutboundPartEditPlan Plan(
            IReadOnlyCollection<Guid> currentSelectedIds,
            IReadOnlyCollection<Guid> desiredSelectedIds,
            DateTime eventDate,
            Func<Guid, IEnumerable<ItemState>> getOtherStates,  // the item's states EXCLUDING this transaction's own
            Func<Guid, string> getItemNumber)
        {
            HashSet<Guid> desiredIds = [];
            foreach (Guid itemId in desiredSelectedIds)
            {
                if (!desiredIds.Add(itemId))
                {
                    throw new ArgumentException($"Duplicate item ID {itemId} in the desired selection.");
                }
            }

            HashSet<Guid> currentIds = [.. currentSelectedIds];
            List<PlannedItem> planItems = [];

            foreach (Guid itemId in desiredSelectedIds)
            {
                if (currentIds.Contains(itemId))
                    continue; // unchanged items are omitted from the plan entirely

                string itemNumber = getItemNumber(itemId);
                List<string> violations = [];
                string? violation = BookingTimelineValidator.GetOutboundBookingViolation(getOtherStates(itemId), eventDate);
                if (violation != null)
                {
                    violations.Add($"Item {itemNumber}: cannot be added to the shipment — {violation}");
                }

                planItems.Add(new PlannedItem(ItemAction.Add, itemId, itemNumber, violations));
            }

            foreach (Guid itemId in currentSelectedIds)
            {
                if (desiredIds.Contains(itemId))
                    continue; // unchanged items are omitted from the plan entirely

                string itemNumber = getItemNumber(itemId);
                List<string> violations = [];
                List<ItemState> otherStates = getOtherStates(itemId).ToList();
                if (!BookedTransactionConflictChecker.CanRemoveShippedItem(otherStates, eventDate))
                {
                    ItemState? earliest = BookedTransactionConflictChecker.FindEarliestLaterState(otherStates, eventDate);
                    violations.Add(
                        $"Item {itemNumber}: cannot be removed from the shipment — the item has later history" +
                        $" (earliest later state on {earliest!.Date:o}).");
                }

                planItems.Add(new PlannedItem(ItemAction.Remove, itemId, itemNumber, violations));
            }

            return new OutboundPartEditPlan(planItems);
        }
    }
}
