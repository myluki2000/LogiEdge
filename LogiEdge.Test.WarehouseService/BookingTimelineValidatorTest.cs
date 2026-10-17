using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Services.WarehouseManagement;

namespace LogiEdge.Test.WarehouseService
{
    public class BookingTimelineValidatorTest
    {
        private static readonly DateTime EventDate = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

        private static ItemState State(DateTime date, string location = "A1")
            => new()
            {
                Date = date,
                Location = location,
                WarehouseId = Guid.NewGuid(),
                IsQuarantined = false,
            };

        // --- GetOutboundBookingViolation ---

        [Fact]
        public void GetOutboundBookingViolation_Violation_WhenItemHasNoStatesAtAll()
        {
            string? violation = BookingTimelineValidator.GetOutboundBookingViolation([], EventDate);

            Assert.NotNull(violation);
            Assert.Contains("not in the warehouse", violation);
        }

        [Fact]
        public void GetOutboundBookingViolation_Valid_WhenOnlyInWarehouseStatesStrictlyBeforeEventDate()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-2)),
                State(EventDate.AddHours(-1), "B2"),
            ];

            Assert.Null(BookingTimelineValidator.GetOutboundBookingViolation(states, EventDate));
            Assert.True(BookingTimelineValidator.CanBookOutboundForItem(states, EventDate));
        }

        [Fact]
        public void GetOutboundBookingViolation_Violation_WhenItemIsObservedAfterTheEventDate()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-1)),
                State(EventDate.AddMinutes(30), SpecialLocations.SHIPPED),
            ];

            string? violation = BookingTimelineValidator.GetOutboundBookingViolation(states, EventDate);

            Assert.NotNull(violation);
            Assert.Contains("observed at or after the event date", violation);
        }

        [Fact]
        public void GetOutboundBookingViolation_Violation_WhenLatestStateBeforeEventDateIsShipped()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-3)),
                State(EventDate.AddDays(-1), SpecialLocations.SHIPPED),
            ];

            string? violation = BookingTimelineValidator.GetOutboundBookingViolation(states, EventDate);

            Assert.NotNull(violation);
            Assert.Contains("already shipped", violation);
        }

        [Fact]
        public void GetOutboundBookingViolation_Violation_WhenLatestStateBeforeEventDateIsPreArrival()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-3)),
                State(EventDate.AddDays(-1), SpecialLocations.PRE_ARRIVAL),
            ];

            string? violation = BookingTimelineValidator.GetOutboundBookingViolation(states, EventDate);

            Assert.NotNull(violation);
            Assert.Contains("had not arrived", violation);
        }

        [Fact]
        public void GetOutboundBookingViolation_Violation_WhenStateIsTiedToEventDate()
        {
            List<ItemState> states = [State(EventDate)];

            string? violation = BookingTimelineValidator.GetOutboundBookingViolation(states, EventDate);

            Assert.NotNull(violation);
            Assert.Contains("observed at or after the event date", violation);
        }

        [Fact]
        public void GetOutboundBookingViolation_Valid_WhenSeveralInWarehouseStatesAreStrictlyBeforeEventDate()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-5)),
                State(EventDate.AddDays(-1), "B2"),
                State(EventDate.AddMinutes(-5), "C3"),
            ];

            Assert.Null(BookingTimelineValidator.GetOutboundBookingViolation(states, EventDate));
        }

        // --- CanBookOutboundForItem ---

        [Fact]
        public void CanBookOutboundForItem_False_WhenAStateIsTiedToEventDate()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-1)),
                State(EventDate, "B2"),
            ];

            Assert.False(BookingTimelineValidator.CanBookOutboundForItem(states, EventDate));
        }
    }
}
