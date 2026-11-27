using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LogiEdge.WarehouseService.Services.ItemsTable
{
    public class ItemsTableViewService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        public async Task<IReadOnlyList<ItemsTableViewDto>> GetViewsAsync(Guid userId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            return await ctx.ItemTableViews
                .Where(v => v.UserId == userId)
                .OrderBy(v => v.Name)
                .Select(v => new ItemsTableViewDto(v.Id, v.Name, v.IsDefault))
                .ToListAsync();
        }

        public async Task<ItemsTableViewState?> GetViewAsync(Guid userId, Guid viewId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemsTableView? view = await ctx.ItemTableViews
                .FirstOrDefaultAsync(v => v.Id == viewId && v.UserId == userId);

            return view is null ? null : ItemsTableViewState.FromJson(view.ViewState);
        }

        public async Task<ItemsTableViewDto> SaveNewViewAsync(Guid userId, string name, ItemsTableViewState state)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("A view name must be provided.", nameof(name));

            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            bool nameTaken = await ctx.ItemTableViews
                .AnyAsync(v => v.UserId == userId && v.Name.ToLower() == name.ToLower());
            if (nameTaken)
                throw new InvalidOperationException($"A view named '{name}' already exists.");

            ItemsTableView view = new()
            {
                Id = Guid.NewGuid(),
                Name = name,
                UserId = userId,
                IsDefault = false,
                ViewState = state.ToJson(),
            };

            ctx.ItemTableViews.Add(view);
            await ctx.SaveChangesAsync();

            return new ItemsTableViewDto(view.Id, view.Name, view.IsDefault);
        }

        public async Task SetDefaultViewAsync(Guid userId, Guid viewId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            await using IDbContextTransaction transaction = await ctx.Database.BeginTransactionAsync();

            ItemsTableView? view = await ctx.ItemTableViews.AsTracking()
                .FirstOrDefaultAsync(v => v.Id == viewId && v.UserId == userId);
            if (view is null)
                throw new InvalidOperationException($"View {viewId} not found.");

            List<ItemsTableView> otherDefaults = await ctx.ItemTableViews.AsTracking()
                .Where(v => v.UserId == userId && v.Id != viewId && v.IsDefault)
                .ToListAsync();

            view.IsDefault = true;
            foreach (ItemsTableView other in otherDefaults)
                other.IsDefault = false;

            await ctx.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        public async Task UpdateViewStateAsync(Guid userId, Guid viewId, ItemsTableViewState state)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemsTableView? view = await ctx.ItemTableViews.AsTracking()
                .FirstOrDefaultAsync(v => v.Id == viewId && v.UserId == userId);
            if (view is null)
                throw new InvalidOperationException($"View {viewId} not found.");

            view.ViewState = state.ToJson();
            await ctx.SaveChangesAsync();
        }

        public async Task DeleteViewAsync(Guid userId, Guid viewId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            ItemsTableView? view = await ctx.ItemTableViews
                .FirstOrDefaultAsync(v => v.Id == viewId && v.UserId == userId);
            if (view is null)
                throw new InvalidOperationException($"View {viewId} not found.");

            ctx.ItemTableViews.Remove(view);
            await ctx.SaveChangesAsync();
        }
    }
}
