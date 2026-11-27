using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Persistence;
using LogiEdge.WarehouseService.Services.ItemsTable;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.Test.WarehouseService
{
    /// <summary>
    /// Each test method gets its own DatabaseFixture (fresh database), because the tests create views
    /// with fixed names and would otherwise contaminate each other.
    /// </summary>
    public class ItemsTableViewServiceTest : IAsyncLifetime
    {
        private static readonly Guid OwnerId = new("aaaaaaaa-bbbb-cccc-dddd-eeeeffff0001");
        private static readonly Guid OtherUserId = new("bbbbbbbb-cccc-dddd-eeee-ffffffff0002");

        private readonly DatabaseFixture dbFixture = new();
        private ItemsTableViewService service = null!;

        public async Task InitializeAsync()
        {
            await dbFixture.InitializeAsync();
            service = new ItemsTableViewService(dbFixture.WarehouseDbContextFactory);
        }

        public async Task DisposeAsync() => await dbFixture.DisposeAsync();

        [Fact]
        public async Task SaveNewView_ThenGetViews_ReturnsOwnerScopedListOrderedByName()
        {
            ItemsTableViewDto zebra = await service.SaveNewViewAsync(OwnerId, "Zebra View", BuildState("ABC", ["Location"], ["ItemNumber", "Location"], true));
            ItemsTableViewDto alpha = await service.SaveNewViewAsync(OwnerId, "Alpha View", BuildState("DEF", ["Customer"], ["Location"], false));
            await service.SaveNewViewAsync(OtherUserId, "Zebra View", BuildState("GHI", ["Location"], ["ItemNumber"], true));

            IReadOnlyList<ItemsTableViewDto> ownerViews = await service.GetViewsAsync(OwnerId);

            Assert.Equal(new[] { "Alpha View", "Zebra View" }, ownerViews.Select(v => v.Name));
            Assert.All(ownerViews, v => Assert.False(v.IsDefault));
            Assert.Contains(ownerViews, v => v.Id == alpha.Id && v.Name == "Alpha View");
            Assert.Contains(ownerViews, v => v.Id == zebra.Id && v.Name == "Zebra View");

            IReadOnlyList<ItemsTableViewDto> otherUserViews = await service.GetViewsAsync(OtherUserId);
            Assert.Single(otherUserViews);
            Assert.Equal("Zebra View", otherUserViews[0].Name);
        }

        [Fact]
        public async Task SaveNewView_NameExistsWithDifferentCasing_ThrowsInvalidOperationException()
        {
            await service.SaveNewViewAsync(OwnerId, "My View", BuildState("ABC", ["Location"], ["ItemNumber", "Location"], true));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SaveNewViewAsync(OwnerId, "my view", BuildState("XYZ", ["Customer"], ["Location"], false)));
        }

        [Fact]
        public async Task GetView_Owner_ReturnsExactSavedState()
        {
            ItemsTableViewState savedState = BuildState("ABC", ["Location"], ["ItemNumber", "Location"], true);
            ItemsTableViewDto saved = await service.SaveNewViewAsync(OwnerId, "My View", savedState);

            ItemsTableViewState? loaded = await service.GetViewAsync(OwnerId, saved.Id);

            Assert.Equal(savedState, loaded);
        }

        [Fact]
        public async Task GetView_MissingOrForeignId_ReturnsNull()
        {
            ItemsTableViewDto saved = await service.SaveNewViewAsync(OwnerId, "My View", BuildState("ABC", ["Location"], ["ItemNumber", "Location"], true));

            Assert.Null(await service.GetViewAsync(OwnerId, Guid.NewGuid()));
            Assert.Null(await service.GetViewAsync(OtherUserId, saved.Id));
        }

        [Fact]
        public async Task SetDefaultView_SecondDefault_ClearsFirst_ExactlyOneDefaultPerUser()
        {
            ItemsTableViewDto first = await service.SaveNewViewAsync(OwnerId, "First View", BuildState("ABC", ["Location"], ["ItemNumber", "Location"], true));
            ItemsTableViewDto second = await service.SaveNewViewAsync(OwnerId, "Second View", BuildState("DEF", ["Customer"], ["Location"], false));

            await service.SetDefaultViewAsync(OwnerId, first.Id);
            using (WarehouseDbContext ctx = dbFixture.WarehouseDbContextFactory.CreateDbContext())
            {
                List<ItemsTableView> views = await ctx.ItemTableViews.ToListAsync();
                Assert.Equal(2, views.Count);
                Assert.All(views, v => Assert.Equal(OwnerId, v.UserId));
                Assert.Single(views, v => v.IsDefault);
                Assert.True(views.First(v => v.Id == first.Id).IsDefault);
                Assert.False(views.First(v => v.Id == second.Id).IsDefault);
            }

            await service.SetDefaultViewAsync(OwnerId, second.Id);
            using (WarehouseDbContext ctx = dbFixture.WarehouseDbContextFactory.CreateDbContext())
            {
                List<ItemsTableView> views = await ctx.ItemTableViews.ToListAsync();
                Assert.Single(views, v => v.IsDefault);
                Assert.True(views.First(v => v.Id == second.Id).IsDefault);
                Assert.False(views.First(v => v.Id == first.Id).IsDefault);
            }
        }

        [Fact]
        public async Task UpdateViewState_OverwritesStoredState()
        {
            ItemsTableViewState stateA = BuildState("ABC", ["Location"], ["ItemNumber", "Location"], true);
            ItemsTableViewDto saved = await service.SaveNewViewAsync(OwnerId, "My View", stateA);

            ItemsTableViewState stateB = BuildState("XYZ", ["Customer"], ["Location"], false);
            await service.UpdateViewStateAsync(OwnerId, saved.Id, stateB);

            ItemsTableViewState? loaded = await service.GetViewAsync(OwnerId, saved.Id);

            Assert.Equal(stateB, loaded);
        }

        [Fact]
        public async Task DeleteView_RemovesViewFromList()
        {
            ItemsTableViewDto saved = await service.SaveNewViewAsync(OwnerId, "My View", BuildState("ABC", ["Location"], ["ItemNumber", "Location"], true));

            await service.DeleteViewAsync(OwnerId, saved.Id);

            Assert.Empty(await service.GetViewsAsync(OwnerId));
        }

        [Fact]
        public async Task DeleteView_MissingOrForeignId_ThrowsInvalidOperationException()
        {
            ItemsTableViewDto saved = await service.SaveNewViewAsync(OwnerId, "My View", BuildState("ABC", ["Location"], ["ItemNumber", "Location"], true));

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteViewAsync(OwnerId, Guid.NewGuid()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteViewAsync(OtherUserId, saved.Id));
        }

        private static ItemsTableViewState BuildState(
            string itemNumber,
            string[] groupByProperties,
            string[] columnsToDisplay,
            bool showShipped) => new()
        {
            Filter = new ItemsTableFilterParameters
            {
                BaseParameters = new Dictionary<string, object> { [nameof(Item.ItemNumber)] = itemNumber },
                StateParameters = new Dictionary<string, object>(),
                AdditionalPropertyParameters = new Dictionary<string, string>(),
            },
            Sort = new ItemsTableSortParameters
            {
                GroupByProperties = groupByProperties,
                SortByProperty = null,
                SortOrderDescending = false,
            },
            Columns = new ItemsTableColumnDisplayParameters { ColumnsToDisplay = columnsToDisplay },
            State = new ItemsTableStateParameters { AtTime = null, ShowShipped = showShipped },
        };
    }
}
