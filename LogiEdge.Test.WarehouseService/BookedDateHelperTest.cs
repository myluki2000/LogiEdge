using LogiEdge.WarehouseService.Services.WarehouseManagement;

namespace LogiEdge.Test.WarehouseService
{
    public class BookedDateHelperTest
    {
        [Fact]
        public void ComputeNextBookedDate_ReturnsUtcNow_WhenNoTransactionWasBookedBefore()
        {
            DateTime now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

            Assert.Equal(now, BookedDateHelper.ComputeNextBookedDate(now, null));
        }

        [Fact]
        public void ComputeNextBookedDate_ReturnsUtcNow_WhenAfterLastBookedDate()
        {
            DateTime last = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
            DateTime now = last.AddMinutes(5);

            Assert.Equal(now, BookedDateHelper.ComputeNextBookedDate(now, last));
        }

        [Fact]
        public void ComputeNextBookedDate_ClampsToLastPlusOneMs_WhenClockStepsBackwards()
        {
            DateTime last = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);
            DateTime now = last.AddHours(-1);

            Assert.Equal(last.AddMilliseconds(1), BookedDateHelper.ComputeNextBookedDate(now, last));
        }

        [Fact]
        public void ComputeNextBookedDate_ClampsToLastPlusOneMs_WhenEqualToLastBookedDate()
        {
            DateTime last = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

            Assert.Equal(last.AddMilliseconds(1), BookedDateHelper.ComputeNextBookedDate(last, last));
        }

        [Fact]
        public void ComputeNextBookedDate_ProducesDistinctIncreasingDates_ForRapidConsecutiveBookings()
        {
            DateTime now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

            // Simulate several bookings within the same clock tick.
            DateTime? last = null;
            List<DateTime> bookedDates = [];
            for (int i = 0; i < 10; i++)
            {
                last = BookedDateHelper.ComputeNextBookedDate(now, last);
                bookedDates.Add(last.Value);
            }

            Assert.Equal(bookedDates.Count, bookedDates.Distinct().Count());
            for (int i = 1; i < bookedDates.Count; i++)
            {
                Assert.True(bookedDates[i] > bookedDates[i - 1], $"BookedDate[{i}] must be after BookedDate[{i - 1}].");
            }
        }
    }
}
