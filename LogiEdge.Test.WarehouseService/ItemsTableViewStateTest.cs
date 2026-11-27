using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Services.ItemsTable;

namespace LogiEdge.Test.WarehouseService
{
    public class ItemsTableViewStateTest
    {
        private static ItemsTableFilterParameters EmptyFilter() => new()
        {
            BaseParameters = new Dictionary<string, object>(),
            StateParameters = new Dictionary<string, object>(),
            AdditionalPropertyParameters = new Dictionary<string, string>(),
        };

        private static ItemsTableSortParameters EmptySort() => new()
        {
            GroupByProperties = [],
            SortByProperty = null,
            SortOrderDescending = false,
        };

        private static ItemsTableViewState BuildState(
            ItemsTableFilterParameters filter,
            ItemsTableSortParameters sort,
            ItemsTableColumnDisplayParameters columns,
            ItemsTableStateParameters state) => new()
        {
            Filter = filter,
            Sort = sort,
            Columns = columns,
            State = state,
        };

        private static ItemsTableViewState RoundTrip(ItemsTableViewState state) =>
            ItemsTableViewState.FromJson(state.ToJson());

        [Fact]
        public void RoundTrip_EmptyState_ReturnsAllEmptyRecords()
        {
            ItemsTableViewState state = BuildState(
                EmptyFilter(),
                EmptySort(),
                new ItemsTableColumnDisplayParameters(),
                new ItemsTableStateParameters { AtTime = null, ShowShipped = false });

            Assert.Equal(state, RoundTrip(state));
        }

        [Fact]
        public void RoundTrip_FilterWithTypedValuesAndCollidingAdditionalProperties_PreservesEverything()
        {
            Guid customerId = Guid.NewGuid();
            ItemsTableViewState state = BuildState(
                new ItemsTableFilterParameters
                {
                    BaseParameters = new Dictionary<string, object>
                    {
                        [nameof(Item.Customer)] = customerId,
                        [nameof(Item.ItemNumber)] = "ABC-123",
                    },
                    StateParameters = new Dictionary<string, object>
                    {
                        [nameof(ItemState.Location)] = "A 1-1",
                        [nameof(ItemState.IsQuarantined)] = true,
                    },
                    AdditionalPropertyParameters = new Dictionary<string, string>
                    {
                        ["col"] = "some-column-value",
                        ["sortBy"] = "ItemNumber",
                    },
                },
                EmptySort(),
                new ItemsTableColumnDisplayParameters(),
                new ItemsTableStateParameters { AtTime = null, ShowShipped = false });

            Assert.Equal(state, RoundTrip(state));
        }

        [Fact]
        public void RoundTrip_SortWithMultipleGroupByAndDescending_PreservesValues()
        {
            ItemsTableViewState state = BuildState(
                EmptyFilter(),
                new ItemsTableSortParameters
                {
                    GroupByProperties = ["Location", "ItemNumber"],
                    SortByProperty = "EntryDate",
                    SortOrderDescending = true,
                },
                new ItemsTableColumnDisplayParameters(),
                new ItemsTableStateParameters { AtTime = null, ShowShipped = false });

            ItemsTableViewState result = RoundTrip(state);

            Assert.Equal(state, result);
            Assert.Equal(["Location", "ItemNumber"], result.Sort.GroupByProperties);
        }

        [Fact]
        public void RoundTrip_ColumnDisplayOrder_IsPreserved()
        {
            ItemsTableViewState state = BuildState(
                EmptyFilter(),
                EmptySort(),
                new ItemsTableColumnDisplayParameters { ColumnsToDisplay = ["ItemNumber", "Location", "Customer", "Id"] },
                new ItemsTableStateParameters { AtTime = null, ShowShipped = false });

            ItemsTableViewState result = RoundTrip(state);

            Assert.Equal(state, result);
            Assert.Equal(["ItemNumber", "Location", "Customer", "Id"], result.Columns.ColumnsToDisplay);
        }

        [Fact]
        public void RoundTrip_StateParametersWithAtTimeAndShowShipped_PreservesValues()
        {
            DateTime atTime = new(2026, 10, 29, 12, 30, 45, DateTimeKind.Utc);
            ItemsTableViewState state = BuildState(
                EmptyFilter(),
                EmptySort(),
                new ItemsTableColumnDisplayParameters(),
                new ItemsTableStateParameters { AtTime = atTime, ShowShipped = true });

            Assert.Equal(state, RoundTrip(state));
        }

        [Fact]
        public void RoundTrip_FullState_WithKeysCollidingAcrossSections_PreservesAllSections()
        {
            ItemsTableViewState state = BuildState(
                new ItemsTableFilterParameters
                {
                    BaseParameters = new Dictionary<string, object>(),
                    StateParameters = new Dictionary<string, object>
                    {
                        [nameof(ItemState.Location)] = "A 1-1",
                    },
                    AdditionalPropertyParameters = new Dictionary<string, string>
                    {
                        ["col"] = "column-value",
                        ["sortBy"] = "ItemNumber",
                    },
                },
                new ItemsTableSortParameters
                {
                    GroupByProperties = ["Location"],
                    SortByProperty = "EntryDate",
                    SortOrderDescending = true,
                },
                new ItemsTableColumnDisplayParameters { ColumnsToDisplay = ["Id", "Customer"] },
                new ItemsTableStateParameters { AtTime = null, ShowShipped = true });

            Assert.Equal(state, RoundTrip(state));
        }
    }
}
