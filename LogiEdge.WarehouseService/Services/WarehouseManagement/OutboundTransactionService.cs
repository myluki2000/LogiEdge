using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    public class OutboundTransactionService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        public async Task<OutboundTransactionPart> GetOutboundTransactionPart(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            OutboundTransactionPart? transactionPart = await ctx.OutboundTransactionParts
                .FirstOrDefaultAsync(o => o.TransactionId == transactionId);

            if (transactionPart == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} does not exist or has no outbound transaction part.");
            }

            return transactionPart;
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
        /// Gets the ids of the last states of the outbound items before they left the warehouse for a given transaction.
        /// </summary>
        public async Task<List<Guid>> GetLastInWarehouseStateIds(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            List<Guid> ids = ctx.OutboundTransactionParts
                .Select(t => t.NewItemStates
                    .OrderByDescending(st => st.Date)
                    .FirstOrDefault(st => st.Location != SpecialLocations.SHIPPED))
                .Select(st => st.Id)
                .ToList();

            return ids;
        }
    }
}
 