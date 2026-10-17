using LogiEdge.WarehouseService.Data;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    /// <summary>
    /// Booking-time timeline validation (see .notes/edit-booked-transactions.md, section 5.7). Booking
    /// stamps all created item states with the transaction's EventDate; these pure checks ensure the
    /// existing history of each affected item stays consistent with that date so timelines remain
    /// consistent by construction. Operates on in-memory state lists only, for unit testing without a DB.
    /// </summary>
    public static class BookingTimelineValidator
    {
        /// <summary>
        /// Validates that an item can be shipped (outbound booking) at the given event date. Returns null
        /// when valid, otherwise a human-readable violation message. Rules:
        /// - No existing state may be dated at or after the event date — the item must not be observed
        ///   anywhere after it supposedly shipped; exact ties are treated conservatively as "at/after".
        /// - The item's latest state before the event date must exist and be in-warehouse (IsInWarehouse)
        ///   — the item must have actually been in the warehouse at the event date, i.e. arrived and not
        ///   yet shipped.
        /// </summary>
        public static string? GetOutboundBookingViolation(IEnumerable<ItemState> existingStates, DateTime eventDate)
        {
            ItemState? earliestAtOrAfter = existingStates
                .Where(st => st.Date >= eventDate)
                .OrderBy(st => st.Date)
                .FirstOrDefault();

            if (earliestAtOrAfter != null)
            {
                return $"the item is observed at or after the event date (state on {earliestAtOrAfter.Date:o}).";
            }

            ItemState? latestBefore = existingStates
                .Where(st => st.Date < eventDate)
                .OrderByDescending(st => st.Date)
                .FirstOrDefault();

            if (latestBefore == null)
            {
                return "the item has no state before the event date — it was not in the warehouse yet.";
            }

            if (!latestBefore.IsInWarehouse)
            {
                return latestBefore.IsShipped
                    ? $"the item was already shipped before the event date (shipped on {latestBefore.Date:o})."
                    : "the item had not arrived in the warehouse yet at the event date.";
            }

            return null;
        }

        /// <summary>
        /// True if the item can be booked as outbound at the given event date.
        /// </summary>
        public static bool CanBookOutboundForItem(IEnumerable<ItemState> existingStates, DateTime eventDate)
            => GetOutboundBookingViolation(existingStates, eventDate) is null;
    }
}
