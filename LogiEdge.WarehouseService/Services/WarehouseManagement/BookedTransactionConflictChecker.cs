using LogiEdge.WarehouseService.Data;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    /// <summary>
    /// Pure conflict checks for editing booked transactions (see .notes/edit-booked-transactions.md,
    /// section 5.4). The methods operate on in-memory item state lists only, so they can be unit-tested
    /// without a database. Callers must pass the affected item's history WITHOUT the states created by
    /// the transaction being edited — those are the only states an edit may rewrite; all other states
    /// are only checked for conflicts, never modified.
    /// </summary>
    public static class BookedTransactionConflictChecker
    {
        /// <summary>
        /// True if any of the given (other) states is dated at or after the transaction's event date.
        /// Exact ties are treated as later, conservatively: with equal timestamps it cannot be
        /// determined which event happened first.
        /// </summary>
        public static bool HasLaterStates(IEnumerable<ItemState> otherStates, DateTime eventDate)
            => otherStates.Any(st => st.Date >= eventDate);

        /// <summary>
        /// The earliest (other) state dated at or after the event date, or null if there is none.
        /// Useful for building per-item conflict messages ("it was relocated on ... first").
        /// </summary>
        public static ItemState? FindEarliestLaterState(IEnumerable<ItemState> otherStates, DateTime eventDate)
            => otherStates.Where(st => st.Date >= eventDate).OrderBy(st => st.Date).FirstOrDefault();

        // --- Inbound part: the transaction created the item and its initial state ---

        /// <summary>
        /// Changing the arrival location/warehouse or the item's identity fields (item number, customer,
        /// schema) is only allowed if the item has no later history — otherwise the change would break
        /// continuity with a later move/shipment state.
        /// </summary>
        public static bool CanUpdateArrivalOrIdentity(IEnumerable<ItemState> otherStates, DateTime eventDate)
            => !HasLaterStates(otherStates, eventDate);

        /// <summary>
        /// Deleting the item created for a draft line is only allowed if the item has no later history
        /// and no DRAFT transaction's outbound selection still references it.
        /// </summary>
        public static bool CanDeleteCreatedItem(
            IEnumerable<ItemState> otherStates,
            DateTime eventDate,
            bool referencedByDraftTransaction)
            => !HasLaterStates(otherStates, eventDate) && !referencedByDraftTransaction;

        // --- Outbound part: the transaction appended a SHIPPED state ---

        /// <summary>
        /// Removing an item from the shipment (deleting its SHIPPED state) is only allowed if no other
        /// state of the item is dated at or after the event date — otherwise the item would be observed
        /// in the warehouse after it supposedly shipped.
        /// </summary>
        public static bool CanRemoveShippedItem(IEnumerable<ItemState> otherStates, DateTime eventDate)
            => !HasLaterStates(otherStates, eventDate);

        /// <summary>
        /// Adding an item to the shipment (creating a new SHIPPED state at the event date) is only
        /// allowed if no state of the item is dated at or after the event date and its latest state is
        /// not already SHIPPED. The full history of the item is passed — the transaction has not created
        /// a state for it yet.
        /// </summary>
        public static bool CanShipItem(IEnumerable<ItemState> allStates, DateTime eventDate)
        {
            if (allStates.Any(st => st.Date >= eventDate))
            {
                return false;
            }

            ItemState? latest = allStates.OrderByDescending(st => st.Date).FirstOrDefault();
            return latest == null || !latest.IsShipped;
        }
    }
}
