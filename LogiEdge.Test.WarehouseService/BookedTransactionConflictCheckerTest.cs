using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Services.WarehouseManagement;

namespace LogiEdge.Test.WarehouseService
{
    public class BookedTransactionConflictCheckerTest
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

        // --- HasLaterStates / FindEarliestLaterState ---

        [Fact]
        public void HasLaterStates_ReturnsFalse_WhenThereAreNoOtherStates()
        {
            Assert.False(BookedTransactionConflictChecker.HasLaterStates([], EventDate));
        }

        [Fact]
        public void HasLaterStates_ReturnsFalse_WhenAllOtherStatesAreEarlier()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-2)),
                State(EventDate.AddHours(-1)),
            ];

            Assert.False(BookedTransactionConflictChecker.HasLaterStates(states, EventDate));
        }

        [Fact]
        public void HasLaterStates_ReturnsTrue_WhenAStateIsAfterTheEventDate()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-1)),
                State(EventDate.AddMinutes(30), SpecialLocations.SHIPPED),
            ];

            Assert.True(BookedTransactionConflictChecker.HasLaterStates(states, EventDate));
        }

        [Fact]
        public void HasLaterStates_TreatsTiedDatesAsLater()
        {
            List<ItemState> states = [State(EventDate)];

            Assert.True(BookedTransactionConflictChecker.HasLaterStates(states, EventDate));
        }

        [Fact]
        public void FindEarliestLaterState_ReturnsEarliestStateAtOrAfterEventDate()
        {
            ItemState first = State(EventDate.AddMinutes(10));
            ItemState second = State(EventDate.AddDays(1));

            Assert.Same(first, BookedTransactionConflictChecker.FindEarliestLaterState([first, second], EventDate));
        }

        [Fact]
        public void FindEarliestLaterState_ReturnsNull_WhenThereAreNoLaterStates()
        {
            List<ItemState> states = [State(EventDate.AddDays(-1))];

            Assert.Null(BookedTransactionConflictChecker.FindEarliestLaterState(states, EventDate));
        }

        // --- Inbound part: arrival/identity updates and item deletion ---

        [Fact]
        public void CanUpdateArrivalOrIdentity_Allowed_WhenItemHasNoLaterHistory()
        {
            List<ItemState> states = [State(EventDate.AddDays(-1))];

            Assert.True(BookedTransactionConflictChecker.CanUpdateArrivalOrIdentity(states, EventDate));
        }

        [Fact]
        public void CanUpdateArrivalOrIdentity_Blocked_WhenItemHasLaterHistory()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-1)),
                State(EventDate.AddDays(1), "B2"),
            ];

            Assert.False(BookedTransactionConflictChecker.CanUpdateArrivalOrIdentity(states, EventDate));
        }

        [Fact]
        public void CanUpdateArrivalOrIdentity_Blocked_WhenLaterHistoryIsTiedToEventDate()
        {
            List<ItemState> states = [State(EventDate, "B2")];

            Assert.False(BookedTransactionConflictChecker.CanUpdateArrivalOrIdentity(states, EventDate));
        }

        [Fact]
        public void CanDeleteCreatedItem_Allowed_WhenNoLaterHistoryAndNotReferencedByDraft()
        {
            List<ItemState> states = [State(EventDate.AddDays(-1))];

            Assert.True(BookedTransactionConflictChecker.CanDeleteCreatedItem(states, EventDate, referencedByDraftTransaction: false));
        }

        [Fact]
        public void CanDeleteCreatedItem_Blocked_WhenReferencedByDraftTransaction()
        {
            List<ItemState> states = [State(EventDate.AddDays(-1))];

            Assert.False(BookedTransactionConflictChecker.CanDeleteCreatedItem(states, EventDate, referencedByDraftTransaction: true));
        }

        [Fact]
        public void CanDeleteCreatedItem_Blocked_WhenItemHasLaterHistory()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-1)),
                State(EventDate.AddMinutes(5), SpecialLocations.SHIPPED),
            ];

            Assert.False(BookedTransactionConflictChecker.CanDeleteCreatedItem(states, EventDate, referencedByDraftTransaction: false));
        }

        // --- Outbound part: removing and adding shipped items ---

        [Fact]
        public void CanRemoveShippedItem_Allowed_WhenNoStateAfterEventDate()
        {
            List<ItemState> states = [State(EventDate.AddDays(-1))];

            Assert.True(BookedTransactionConflictChecker.CanRemoveShippedItem(states, EventDate));
        }

        [Fact]
        public void CanRemoveShippedItem_Blocked_WhenAStateAfterEventDateExists()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-1)),
                State(EventDate.AddMinutes(5), "B2"),
            ];

            Assert.False(BookedTransactionConflictChecker.CanRemoveShippedItem(states, EventDate));
        }

        [Fact]
        public void CanRemoveShippedItem_Blocked_WhenStateIsTiedToEventDate()
        {
            List<ItemState> states = [State(EventDate, "B2")];

            Assert.False(BookedTransactionConflictChecker.CanRemoveShippedItem(states, EventDate));
        }

        [Fact]
        public void CanShipItem_Allowed_WhenLatestStateIsInWarehouseBeforeEventDate()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-3)),
                State(EventDate.AddDays(-1), "B2"),
            ];

            Assert.True(BookedTransactionConflictChecker.CanShipItem(states, EventDate));
        }

        [Fact]
        public void CanShipItem_Allowed_WhenItemHasNoStatesAtAll()
        {
            Assert.True(BookedTransactionConflictChecker.CanShipItem([], EventDate));
        }

        [Fact]
        public void CanShipItem_Blocked_WhenItemIsAlreadyShipped()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-3)),
                State(EventDate.AddDays(-1), SpecialLocations.SHIPPED),
            ];

            Assert.False(BookedTransactionConflictChecker.CanShipItem(states, EventDate));
        }

        [Fact]
        public void CanShipItem_Blocked_WhenItemWasObservedAfterEventDate()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-1)),
                State(EventDate.AddMinutes(5), "B2"),
            ];

            Assert.False(BookedTransactionConflictChecker.CanShipItem(states, EventDate));
        }

        [Fact]
        public void CanShipItem_Blocked_WhenStateIsTiedToEventDate()
        {
            List<ItemState> states =
            [
                State(EventDate.AddDays(-1)),
                State(EventDate, "B2"),
            ];

            Assert.False(BookedTransactionConflictChecker.CanShipItem(states, EventDate));
        }
    }
}
