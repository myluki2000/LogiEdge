using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using LogiEdge.WarehouseService.Dto;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    public class OutboundTransactionService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        public async Task<OutboundTransactionPartDto> GetOutboundTransactionPart(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            OutboundTransactionPart? transactionPart = await ctx.OutboundTransactionParts
                .Include(t => t.DraftSelectedItems)
                    .ThenInclude(it => it.ItemSchema)
                        .ThenInclude(sch => sch.AdditionalProperties)
                .Include(t => t.DraftSelectedItems)
                    .ThenInclude(it => it.Customer)
                .Include(t => t.DraftSelectedItems)
                    .ThenInclude(it => it.Comments)
                .Include(t => t.DraftSelectedItems)
                    .ThenInclude(it => it.ItemStates)
                        .ThenInclude(st => st.Warehouse)
                .Include(t => t.NewItemStates)
                .FirstOrDefaultAsync(o => o.TransactionId == transactionId);

            if (transactionPart == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} does not exist or has no outbound transaction part.");
            }

            return transactionPart.ToDto();
        }

        public async Task DeleteOutboundItem(Guid transactionId, Guid itemId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            OutboundTransactionPart? transactionPart = await ctx.OutboundTransactionParts
                .AsTracking()
                .FirstOrDefaultAsync(o => o.TransactionId == transactionId);

            if (transactionPart == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} does not exist or has no outbound transaction part.");
            }

            transactionPart.DraftSelectedItems!.RemoveAll(it => it.Id == itemId);

            await ctx.SaveChangesAsync();
        }

        public async Task AddOutboundItems(Guid transactionId, IEnumerable<Guid> itemIds)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            OutboundTransactionPart? transactionPart = await ctx.OutboundTransactionParts
                .AsTracking()
                .Include(t => t.DraftSelectedItems)
                .FirstOrDefaultAsync(o => o.TransactionId == transactionId);

            if (transactionPart == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} does not exist or has no outbound transaction part.");
            }

            List<Item> itemsToAdd = ctx.Items.Where(it => itemIds.Contains(it.Id)).ToList();

            transactionPart.DraftSelectedItems!.AddRange(itemsToAdd);

            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Gets the ids of each shipped item's last in-warehouse state (the state right before the
        /// SHIPPED state this transaction created), so that a booked outbound transaction can be
        /// rendered as "items just before shipment".
        /// </summary>
        public async Task<List<Guid>> GetLastInWarehouseStateIds(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            OutboundTransactionPart? transactionPart = await ctx.OutboundTransactionParts
                .AsNoTracking()
                .Include(t => t.NewItemStates)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transactionPart?.NewItemStates == null || transactionPart.NewItemStates.Count == 0)
            {
                return [];
            }

            // For each shipped item: the date of its SHIPPED state created by this transaction.
            Dictionary<Guid, DateTime> shippedDateByItemId = transactionPart.NewItemStates
                .GroupBy(st => st.ItemId)
                .ToDictionary(g => g.Key, g => g.Max(st => st.Date));

            List<Guid> itemIds = shippedDateByItemId.Keys.ToList();

            // The latest non-SHIPPED state of each shipped item that predates its shipment by this transaction.
            List<ItemState> inWarehouseStates = await ctx.ItemsStates
                .Where(st => itemIds.Contains(st.ItemId) && st.Location != SpecialLocations.SHIPPED)
                .ToListAsync();

            return inWarehouseStates
                .Where(st => st.Date < shippedDateByItemId[st.ItemId])
                .GroupBy(st => st.ItemId)
                .Select(g => g.OrderByDescending(st => st.Date).First().Id)
                .ToList();
        }
    }
}
 
