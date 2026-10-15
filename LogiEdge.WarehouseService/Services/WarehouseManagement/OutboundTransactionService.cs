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

            return OutboundTransactionPartToDto(transactionPart);
        }

        private static OutboundTransactionPartDto OutboundTransactionPartToDto(OutboundTransactionPart part)
        {
            return new OutboundTransactionPartDto()
            {
                TransactionId = part.TransactionId,
                DraftSelectedItems = part.DraftSelectedItems?.Select(WarehouseItemDto.FromItem).ToList(),
                DraftPlaceholderItems = part.DraftPlaceholderItems,
                NewItemStates = part.NewItemStates,
            };
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
 
