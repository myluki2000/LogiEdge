using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    /// <summary>
    /// Computes the next booked date for a transaction. Every booking path must use this helper as the
    /// single source of booking timestamps: it guarantees BookedDate is unique and strictly increasing,
    /// even when two bookings happen within the same millisecond or the system clock steps backwards.
    /// This keeps item states created by transactions globally orderable by date alone (see
    /// .notes/edit-booked-transactions.md, section 5.2).
    /// </summary>
    public static class BookedDateHelper
    {
        /// <summary>
        /// Pure computation of the next booked date: returns <paramref name="utcNow"/> unless it would
        /// not be strictly after <paramref name="lastBookedDate"/>, in which case last + 1 ms. The value
        /// is only ever clamped upwards; nothing waits. Does no I/O — see GetNextBookedDate for the
        /// context-reading wrapper used by the booking paths.
        /// </summary>
        public static DateTime ComputeNextBookedDate(DateTime utcNow, DateTime? lastBookedDate)
        {
            return lastBookedDate is { } last && utcNow <= last
                ? last.AddMilliseconds(1)
                : utcNow;
        }

        /// <summary>
        /// Reads the highest BookedDate currently stored (in the same context) and computes the next
        /// booked date from it.
        /// </summary>
        public static async Task<DateTime> GetNextBookedDate(WarehouseDbContext ctx)
        {
            DateTime? lastBookedDate = await ctx.InventoryTransactions.MaxAsync(t => (DateTime?)t.BookedDate);
            return ComputeNextBookedDate(DateTime.UtcNow, lastBookedDate);
        }
    }
}
