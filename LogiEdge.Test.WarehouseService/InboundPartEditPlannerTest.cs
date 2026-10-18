using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Dto;
using LogiEdge.WarehouseService.Services.WarehouseManagement;
using static LogiEdge.WarehouseService.Services.WarehouseManagement.InboundPartEditPlanner;

namespace LogiEdge.Test.WarehouseService
{
    public class InboundPartEditPlannerTest
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

        private static CurrentInboundLine Line(
            string itemNumber = "ABC-1",
            bool createdItemUnlinked = false,
            Guid? createdItemId = null,
            Guid customerId = default,
            Guid itemSchemaId = default,
            Guid warehouseId = default,
            string location = "A1",
            int count = 1,
            bool isQuarantined = false,
            string comments = "",
            IReadOnlyDictionary<string, object?>? additionalProperties = null)
            => new(
                Guid.NewGuid(),
                createdItemUnlinked ? null : (createdItemId ?? Guid.NewGuid()),
                itemNumber,
                customerId,
                itemSchemaId,
                warehouseId,
                location,
                count,
                isQuarantined,
                comments,
                additionalProperties ?? new Dictionary<string, object?>());

        private static InboundTransactionPartEditRequest.InboundDraftItemEdit Edit(
            Guid? draftItemId = null,
            string itemNumber = "ABC-1",
            Guid customerId = default,
            Guid itemSchemaId = default,
            Guid warehouseId = default,
            string location = "A1",
            int count = 1,
            bool isQuarantined = false,
            string comments = "",
            List<ItemAdditionalPropertyDto>? additionalProperties = null)
            => new()
            {
                DraftItemId = draftItemId,
                ItemNumber = itemNumber,
                CustomerId = customerId,
                ItemSchemaId = itemSchemaId,
                WarehouseId = warehouseId,
                Location = location,
                Count = count,
                IsQuarantined = isQuarantined,
                Comments = comments,
                AdditionalProperties = additionalProperties ?? [],
            };

        private static InboundPartEditPlan Plan(
            IReadOnlyList<CurrentInboundLine> currentLines,
            IReadOnlyList<InboundTransactionPartEditRequest.InboundDraftItemEdit> desiredLines,
            Dictionary<Guid, ItemState[]>? otherStatesByItem = null,
            HashSet<Guid>? referencedByDraftOutbound = null)
            => InboundPartEditPlanner.Plan(
                currentLines,
                desiredLines,
                EventDate,
                itemId => otherStatesByItem?.GetValueOrDefault(itemId) ?? [],
                itemId => referencedByDraftOutbound is not null && referencedByDraftOutbound.Contains(itemId));

        // --- No-op and always-allowed edits ---

        [Fact]
        public void Plan_ReturnsEmptyPlan_WhenNothingChanged()
        {
            CurrentInboundLine current = Line(comments: "hello", additionalProperties: new Dictionary<string, object?> { ["Color"] = "red" });

            InboundPartEditPlan plan = Plan(
                [current],
                [Edit(draftItemId: current.DraftItemId, comments: "hello",
                    additionalProperties: [new ItemAdditionalPropertyDto { Name = "Color", Value = "red", Type = ItemSchemaProperty.SupportedTypes.String }])]);

            Assert.Empty(plan.Lines);
            Assert.False(plan.HasViolations);
        }

        [Fact]
        public void Plan_TreatsNullAdditionalPropertyValueAsMissing()
        {
            CurrentInboundLine current = Line(additionalProperties: new Dictionary<string, object?> { ["Color"] = "red", ["Weight"] = null });

            InboundPartEditPlan plan = Plan(
                [current],
                [Edit(draftItemId: current.DraftItemId,
                    additionalProperties: [new ItemAdditionalPropertyDto { Name = "Color", Value = "red", Type = ItemSchemaProperty.SupportedTypes.String }])]);

            Assert.Empty(plan.Lines);
        }

        [Fact]
        public void Plan_AllowsCommentsChange_WhenItemHasLaterHistory()
        {
            CurrentInboundLine current = Line(comments: "old note");
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [State(EventDate.AddDays(1), "B2")] };

            InboundPartEditPlan plan = Plan([current], [Edit(draftItemId: current.DraftItemId, comments: "new note")], otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            Assert.Equal(LineAction.Update, line.Action);
            Assert.Empty(line.Violations);
            Assert.True(line.ChangedFields.ContainsKey("Comments"));
        }

        [Fact]
        public void Plan_AllowsIsQuarantinedChange_WhenItemHasLaterHistory()
        {
            CurrentInboundLine current = Line();
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [State(EventDate.AddDays(1), "B2")] };

            InboundPartEditPlan plan = Plan([current], [Edit(draftItemId: current.DraftItemId, isQuarantined: true)], otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            Assert.Equal(LineAction.Update, line.Action);
            Assert.Empty(line.Violations);
            Assert.True(line.ChangedFields.ContainsKey("IsQuarantined"));
        }

        [Fact]
        public void Plan_AllowsAdditionalPropertiesChange_WhenItemHasLaterHistory()
        {
            CurrentInboundLine current = Line(additionalProperties: new Dictionary<string, object?> { ["Color"] = "red" });
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [State(EventDate.AddDays(1), "B2")] };

            InboundPartEditPlan plan = Plan(
                [current],
                [Edit(draftItemId: current.DraftItemId,
                    additionalProperties: [new ItemAdditionalPropertyDto { Name = "Color", Value = "blue", Type = ItemSchemaProperty.SupportedTypes.String }])],
                otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            Assert.Equal(LineAction.Update, line.Action);
            Assert.Empty(line.Violations);
            Assert.True(line.ChangedFields.ContainsKey("AdditionalProperties"));
        }

        // --- Location and warehouse: only allowed without later history ---

        [Fact]
        public void Plan_AllowsLocationChange_WhenNoLaterStates()
        {
            CurrentInboundLine current = Line();
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [State(EventDate.AddDays(-1))] };

            InboundPartEditPlan plan = Plan([current], [Edit(draftItemId: current.DraftItemId, location: "B2")], otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            Assert.Equal(LineAction.Update, line.Action);
            Assert.Empty(line.Violations);
        }

        [Fact]
        public void Plan_BlocksLocationChange_WhenItemHasLaterHistory()
        {
            CurrentInboundLine current = Line();
            ItemState later = State(EventDate.AddDays(1), "B2");
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [later] };

            InboundPartEditPlan plan = Plan([current], [Edit(draftItemId: current.DraftItemId, location: "C3")], otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            string violation = Assert.Single(line.Violations);
            Assert.Contains("ABC-1", violation);
            Assert.Contains(later.Date.ToString("o"), violation);
        }

        [Fact]
        public void Plan_BlocksLocationChange_WhenLaterStateTiesEventDate()
        {
            CurrentInboundLine current = Line();
            ItemState tied = State(EventDate, "B2");
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [tied] };

            InboundPartEditPlan plan = Plan([current], [Edit(draftItemId: current.DraftItemId, location: "C3")], otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            string violation = Assert.Single(line.Violations);
            Assert.Contains("ABC-1", violation);
            Assert.Contains(tied.Date.ToString("o"), violation);
        }

        [Fact]
        public void Plan_AllowsWarehouseIdChange_WhenNoLaterStates()
        {
            CurrentInboundLine current = Line();
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [State(EventDate.AddDays(-1))] };

            InboundPartEditPlan plan = Plan([current], [Edit(draftItemId: current.DraftItemId, warehouseId: Guid.NewGuid())], otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            Assert.Equal(LineAction.Update, line.Action);
            Assert.Empty(line.Violations);
        }

        [Fact]
        public void Plan_BlocksWarehouseIdChange_WhenItemHasLaterHistory()
        {
            CurrentInboundLine current = Line();
            ItemState later = State(EventDate.AddMinutes(45), "B2");
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [later] };

            InboundPartEditPlan plan = Plan([current], [Edit(draftItemId: current.DraftItemId, warehouseId: Guid.NewGuid())], otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            string violation = Assert.Single(line.Violations);
            Assert.Contains("ABC-1", violation);
            Assert.Contains(later.Date.ToString("o"), violation);
        }

        // --- Identity fields (item number, customer, schema): same rules ---

        [Fact]
        public void Plan_AllowsIdentityChanges_WhenNoLaterHistory()
        {
            CurrentInboundLine current = Line();
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [State(EventDate.AddDays(-1))] };

            InboundPartEditPlan itemNumberPlan = Plan([current], [Edit(draftItemId: current.DraftItemId, itemNumber: "XYZ-9")], otherStates);
            Assert.Empty(Assert.Single(itemNumberPlan.Lines).Violations);

            InboundPartEditPlan customerPlan = Plan([current], [Edit(draftItemId: current.DraftItemId, customerId: Guid.NewGuid())], otherStates);
            Assert.Empty(Assert.Single(customerPlan.Lines).Violations);

            InboundPartEditPlan schemaPlan = Plan([current], [Edit(draftItemId: current.DraftItemId, itemSchemaId: Guid.NewGuid())], otherStates);
            Assert.Empty(Assert.Single(schemaPlan.Lines).Violations);
        }

        [Fact]
        public void Plan_BlocksIdentityChanges_WhenItemHasLaterHistory()
        {
            CurrentInboundLine current = Line();
            ItemState later = State(EventDate.AddMinutes(30), "B2");
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [later] };

            InboundPartEditPlan itemNumberPlan = Plan([current], [Edit(draftItemId: current.DraftItemId, itemNumber: "XYZ-9")], otherStates);
            Assert.Contains("ABC-1", Assert.Single(Assert.Single(itemNumberPlan.Lines).Violations));

            InboundPartEditPlan customerPlan = Plan([current], [Edit(draftItemId: current.DraftItemId, customerId: Guid.NewGuid())], otherStates);
            Assert.Contains("ABC-1", Assert.Single(Assert.Single(customerPlan.Lines).Violations));

            InboundPartEditPlan schemaPlan = Plan([current], [Edit(draftItemId: current.DraftItemId, itemSchemaId: Guid.NewGuid())], otherStates);
            Assert.Contains("ABC-1", Assert.Single(Assert.Single(schemaPlan.Lines).Violations));
        }

        [Fact]
        public void Plan_CombinesGatedFieldsIntoSingleViolation_WhenMultipleFieldsChange()
        {
            CurrentInboundLine current = Line();
            ItemState later = State(EventDate.AddDays(2), "B2");
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [later] };

            InboundPartEditPlan plan = Plan(
                [current],
                [Edit(draftItemId: current.DraftItemId, itemNumber: "XYZ-9", location: "C3", comments: "also a comment")],
                otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            string violation = Assert.Single(line.Violations);
            Assert.Contains("ItemNumber", violation);
            Assert.Contains("Location", violation);
            Assert.DoesNotContain("Comments", violation);
            Assert.Equal(3, line.ChangedFields.Count); // ItemNumber, Location, Comments
        }

        [Fact]
        public void Plan_AddsNoLinkViolation_WhenDraftLineHasNoCreatedItem()
        {
            CurrentInboundLine current = Line(createdItemUnlinked: true);

            InboundPartEditPlan plan = Plan([current], [Edit(draftItemId: current.DraftItemId, location: "B2")]);

            PlannedLine line = Assert.Single(plan.Lines);
            string violation = Assert.Single(line.Violations);
            Assert.Contains("ABC-1", violation);
            Assert.Contains("no link to its created item", violation);
        }

        // --- Removing lines (deleting the created item) ---

        [Fact]
        public void Plan_AllowsRemoval_WhenNoLaterStatesAndNotReferencedByDraftOutbound()
        {
            CurrentInboundLine current = Line();
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [State(EventDate.AddDays(-1))] };

            InboundPartEditPlan plan = Plan([current], [], otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            Assert.Equal(LineAction.Remove, line.Action);
            Assert.Equal(current.ItemNumber, line.ItemNumber);
            Assert.Empty(line.Violations);
        }

        [Fact]
        public void Plan_BlocksRemoval_WhenItemHasLaterHistory()
        {
            CurrentInboundLine current = Line();
            ItemState later = State(EventDate.AddHours(2), "B2");
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [later] };

            InboundPartEditPlan plan = Plan([current], [], otherStates);

            PlannedLine line = Assert.Single(plan.Lines);
            string violation = Assert.Single(line.Violations);
            Assert.Contains("ABC-1", violation);
            Assert.Contains("cannot be removed", violation);
            Assert.Contains(later.Date.ToString("o"), violation);
        }

        [Fact]
        public void Plan_BlocksRemoval_WhenReferencedByDraftOutbound()
        {
            CurrentInboundLine current = Line();
            HashSet<Guid> referenced = [current.CreatedItemId!.Value];

            InboundPartEditPlan plan = Plan([current], [], null, referenced);

            PlannedLine line = Assert.Single(plan.Lines);
            string violation = Assert.Single(line.Violations);
            Assert.Contains("ABC-1", violation);
            Assert.Contains("DRAFT outbound", violation);
        }

        [Fact]
        public void Plan_AddsTwoViolations_ForRemoval_WhenLaterHistoryAndDraftReference()
        {
            CurrentInboundLine current = Line();
            ItemState later = State(EventDate.AddMinutes(5), "B2");
            Dictionary<Guid, ItemState[]> otherStates = new() { [current.CreatedItemId!.Value] = [later] };
            HashSet<Guid> referenced = [current.CreatedItemId!.Value];

            InboundPartEditPlan plan = Plan([current], [], otherStates, referenced);

            PlannedLine line = Assert.Single(plan.Lines);
            Assert.Equal(2, line.Violations.Count);
            Assert.Contains(line.Violations, v => v.Contains("later history"));
            Assert.Contains(line.Violations, v => v.Contains("DRAFT outbound"));
        }

        [Fact]
        public void Plan_AllowsRemoval_WhenCreatedItemIdIsNull()
        {
            CurrentInboundLine current = Line(createdItemUnlinked: true);

            InboundPartEditPlan plan = Plan([current], []);

            PlannedLine line = Assert.Single(plan.Lines);
            Assert.Equal(LineAction.Remove, line.Action);
            Assert.Empty(line.Violations);
        }

        // --- Adding lines and mixed plans ---

        [Fact]
        public void Plan_AddsAddLine_WithoutViolations()
        {
            CurrentInboundLine current = Line();

            InboundPartEditPlan plan = Plan([current], [Edit(draftItemId: current.DraftItemId), Edit(itemNumber: "NEW-1")]);

            // The unchanged line is omitted from the plan entirely; only the add remains.
            PlannedLine add = Assert.Single(plan.Lines);
            Assert.Equal(LineAction.Add, add.Action);
            Assert.Null(add.DraftItemId);
            Assert.Null(add.CreatedItemId);
            Assert.Equal("NEW-1", add.ItemNumber);
            Assert.Empty(add.ChangedFields);
            Assert.Empty(add.Violations);
            Assert.False(plan.HasViolations);
        }

        [Fact]
        public void Plan_CollectsAllLinesAndOnlyViolatingMessages_InMixedPlan()
        {
            CurrentInboundLine toUpdate = Line(itemNumber: "OLD-1");
            CurrentInboundLine toRemove = Line(itemNumber: "GONE-1", createdItemId: Guid.NewGuid());

            ItemState later = State(EventDate.AddDays(3), "B2");
            Dictionary<Guid, ItemState[]> otherStates = new() { [toUpdate.CreatedItemId!.Value] = [later] };
            HashSet<Guid> referenced = [];

            InboundPartEditPlan plan = Plan(
                [toUpdate, toRemove],
                [Edit(draftItemId: toUpdate.DraftItemId, location: "C3"), Edit(itemNumber: "NEW-1")],
                otherStates,
                referenced);

            Assert.True(plan.HasViolations);
            Assert.Equal(3, plan.Lines.Count);
            Assert.Contains(plan.Lines, l => l.Action == LineAction.Update && l.DraftItemId == toUpdate.DraftItemId);
            Assert.Contains(plan.Lines, l => l.Action == LineAction.Add && l.ItemNumber == "NEW-1");
            Assert.Contains(plan.Lines, l => l.Action == LineAction.Remove && l.DraftItemId == toRemove.DraftItemId);

            List<string> violations = plan.AllViolations.ToList();
            Assert.Single(violations);
            Assert.Contains("OLD-1", violations[0]);
        }

        // --- Planner preconditions (the service validates with friendlier messages) ---

        [Fact]
        public void Plan_Throws_WhenDesiredLinesContainDuplicateDraftItemIds()
        {
            CurrentInboundLine current = Line();

            Assert.Throws<ArgumentException>(
                () => Plan([current], [Edit(draftItemId: current.DraftItemId), Edit(draftItemId: current.DraftItemId, itemNumber: "X")]));
        }

        [Fact]
        public void Plan_Throws_WhenDesiredLineReferencesUnknownDraftItemId()
        {
            CurrentInboundLine current = Line();

            Assert.Throws<ArgumentException>(() => Plan([current], [Edit(draftItemId: Guid.NewGuid())]));
        }
    }
}
