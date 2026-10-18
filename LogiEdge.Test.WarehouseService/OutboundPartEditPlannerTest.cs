using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Services.WarehouseManagement;
using static LogiEdge.WarehouseService.Services.WarehouseManagement.OutboundPartEditPlanner;

namespace LogiEdge.Test.WarehouseService
{
    public class OutboundPartEditPlannerTest
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

        private static OutboundPartEditPlan Plan(
            IReadOnlyCollection<Guid> currentSelectedIds,
            IReadOnlyCollection<Guid> desiredSelectedIds,
            Dictionary<Guid, ItemState[]>? otherStatesByItem = null,
            Dictionary<Guid, string>? itemNumbers = null)
            => OutboundPartEditPlanner.Plan(
                currentSelectedIds,
                desiredSelectedIds,
                EventDate,
                itemId => otherStatesByItem?.GetValueOrDefault(itemId) ?? [],
                itemId => itemNumbers?.GetValueOrDefault(itemId) ?? $"ITEM-{itemId}");

        // --- Add rule (booking-time timeline validation) ---

        [Fact]
        public void Add_Allowed_WhenLatestStateIsInWarehouseBeforeEventDate()
        {
            Guid itemId = Guid.NewGuid();
            Dictionary<Guid, ItemState[]> otherStates = new() { [itemId] = [State(EventDate.AddDays(-1))] };

            OutboundPartEditPlan plan = Plan([], [itemId], otherStates);

            PlannedItem item = Assert.Single(plan.Items);
            Assert.Equal(ItemAction.Add, item.Action);
            Assert.Equal(itemId, item.ItemId);
            Assert.Equal($"ITEM-{itemId}", item.ItemNumber);
            Assert.Empty(item.Violations);
            Assert.False(plan.HasViolations);
        }

        [Fact]
        public void Add_Blocked_WhenAStateAfterEventDateExists()
        {
            Guid itemId = Guid.NewGuid();
            Dictionary<Guid, ItemState[]> otherStates = new()
                { [itemId] = [State(EventDate.AddDays(-1)), State(EventDate.AddDays(1))] };

            OutboundPartEditPlan plan = Plan([], [itemId], otherStates);

            PlannedItem item = Assert.Single(plan.Items);
            string violation = Assert.Single(item.Violations);
            Assert.Contains("cannot be added to the shipment", violation);
            Assert.Contains("observed at or after the event date", violation);
        }

        [Fact]
        public void Add_Blocked_WhenStateIsTiedToEventDate()
        {
            Guid itemId = Guid.NewGuid();
            Dictionary<Guid, ItemState[]> otherStates = new() { [itemId] = [State(EventDate)] };

            OutboundPartEditPlan plan = Plan([], [itemId], otherStates);

            PlannedItem item = Assert.Single(plan.Items);
            string violation = Assert.Single(item.Violations);
            Assert.Contains("cannot be added to the shipment", violation);
            Assert.Contains("observed at or after the event date", violation);
        }

        [Fact]
        public void Add_Blocked_WhenItemWasAlreadyShippedBeforeEventDate()
        {
            Guid itemId = Guid.NewGuid();
            Dictionary<Guid, ItemState[]> otherStates = new()
                { [itemId] = [State(EventDate.AddDays(-1), SpecialLocations.SHIPPED)] };

            OutboundPartEditPlan plan = Plan([], [itemId], otherStates);

            PlannedItem item = Assert.Single(plan.Items);
            string violation = Assert.Single(item.Violations);
            Assert.Contains("cannot be added to the shipment", violation);
            Assert.Contains("already shipped", violation);
        }

        [Fact]
        public void Add_Blocked_WhenItemHasNoStatesAtAll()
        {
            Guid itemId = Guid.NewGuid();

            OutboundPartEditPlan plan = Plan([], [itemId]);

            PlannedItem item = Assert.Single(plan.Items);
            string violation = Assert.Single(item.Violations);
            Assert.Contains("cannot be added to the shipment", violation);
            Assert.Contains("no state before the event date", violation);
        }

        [Fact]
        public void Add_Blocked_WhenLatestStateIsPreArrival()
        {
            Guid itemId = Guid.NewGuid();
            Dictionary<Guid, ItemState[]> otherStates = new()
                { [itemId] = [State(EventDate.AddDays(-1), SpecialLocations.PRE_ARRIVAL)] };

            OutboundPartEditPlan plan = Plan([], [itemId], otherStates);

            PlannedItem item = Assert.Single(plan.Items);
            string violation = Assert.Single(item.Violations);
            Assert.Contains("cannot be added to the shipment", violation);
            Assert.Contains("had not arrived", violation);
        }

        // --- Remove rule (no later history) ---

        [Fact]
        public void Remove_Allowed_WhenOnlyEarlierOtherStatesExist()
        {
            Guid itemId = Guid.NewGuid();
            Dictionary<Guid, ItemState[]> otherStates = new() { [itemId] = [State(EventDate.AddDays(-1))] };

            OutboundPartEditPlan plan = Plan([itemId], [], otherStates);

            PlannedItem item = Assert.Single(plan.Items);
            Assert.Equal(ItemAction.Remove, item.Action);
            Assert.Equal(itemId, item.ItemId);
            Assert.Empty(item.Violations);
            Assert.False(plan.HasViolations);
        }

        [Fact]
        public void Remove_Blocked_WhenAStateAfterEventDateExists()
        {
            Guid itemId = Guid.NewGuid();
            DateTime laterDate = EventDate.AddDays(2);
            Dictionary<Guid, ItemState[]> otherStates = new()
                { [itemId] = [State(EventDate.AddDays(-1)), State(laterDate, SpecialLocations.SHIPPED)] };

            OutboundPartEditPlan plan = Plan([itemId], [], otherStates);

            PlannedItem item = Assert.Single(plan.Items);
            string violation = Assert.Single(item.Violations);
            Assert.Contains("cannot be removed from the shipment", violation);
            Assert.Contains("later history", violation);
            Assert.Contains(laterDate.ToString("o"), violation);
        }

        [Fact]
        public void Remove_Blocked_WhenStateIsTiedToEventDate()
        {
            Guid itemId = Guid.NewGuid();
            Dictionary<Guid, ItemState[]> otherStates = new() { [itemId] = [State(EventDate)] };

            OutboundPartEditPlan plan = Plan([itemId], [], otherStates);

            PlannedItem item = Assert.Single(plan.Items);
            string violation = Assert.Single(item.Violations);
            Assert.Contains("cannot be removed from the shipment", violation);
            Assert.Contains("later history", violation);
        }

        // --- Plan structure ---

        [Fact]
        public void UnchangedItems_AreNotPartOfPlan()
        {
            Guid itemId = Guid.NewGuid();
            Dictionary<Guid, ItemState[]> otherStates = new() { [itemId] = [State(EventDate.AddDays(-1))] };

            OutboundPartEditPlan plan = Plan([itemId], [itemId], otherStates);

            Assert.Empty(plan.Items);
            Assert.False(plan.HasViolations);
        }

        [Fact]
        public void DuplicateDesiredIds_ThrowsArgumentException()
        {
            Guid itemId = Guid.NewGuid();

            Assert.Throws<ArgumentException>(() => Plan([], [itemId, itemId]));
        }

        [Fact]
        public void EmptyCurrentAndDesired_EmptyPlanWithoutViolations()
        {
            OutboundPartEditPlan plan = Plan([], []);

            Assert.Empty(plan.Items);
            Assert.False(plan.HasViolations);
            Assert.Empty(plan.AllViolations);
        }

        [Fact]
        public void MultipleViolations_AreAllCollected()
        {
            Guid addItemId = Guid.NewGuid();
            Guid removeItemId = Guid.NewGuid();
            Dictionary<Guid, ItemState[]> otherStates = new()
            {
                [addItemId] = [State(EventDate.AddMinutes(30))],  // observed at/after the event date → Add blocked
                [removeItemId] = [State(EventDate.AddDays(1))],   // later history → Remove blocked
            };

            OutboundPartEditPlan plan = Plan([removeItemId], [addItemId], otherStates);

            Assert.Equal(2, plan.Items.Count);
            Assert.Contains(plan.Items, i => i.Action == ItemAction.Add && i.ItemId == addItemId);
            Assert.Contains(plan.Items, i => i.Action == ItemAction.Remove && i.ItemId == removeItemId);
            Assert.True(plan.HasViolations);

            List<string> violations = plan.AllViolations.ToList();
            Assert.Equal(2, violations.Count);
            Assert.Contains(violations, v => v.Contains("cannot be added to the shipment"));
            Assert.Contains(violations, v => v.Contains("cannot be removed from the shipment"));
        }
    }
}
