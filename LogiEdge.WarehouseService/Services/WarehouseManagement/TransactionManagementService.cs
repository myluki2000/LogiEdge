using LogiEdge.Shared.Utility;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using LogiEdge.WarehouseService.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    public class TransactionManagementService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        public async Task<InventoryTransaction> BookTransactionAsync(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Inbound transaction with ID {transactionId} not found.");
            }

            if (transaction.State == TransactionState.BOOKED)
            {
                throw new InvalidOperationException($"Inbound transaction with ID {transactionId} is already booked.");
            }

            transaction.State = TransactionState.BOOKED;
            transaction.BookedDate = DateTime.UtcNow;

            if (transaction.InboundTransactionPartId.HasValue)
                await BookInboundTransactionPart(ctx, transaction.InboundTransactionPartId.Value, transaction.BookedDate.Value);

            if (transaction.OutboundTransactionPartId.HasValue)
                await BookOutboundTransactionPart(ctx, transaction.OutboundTransactionPartId.Value, transaction.BookedDate.Value);

            if (transaction.RelocationTransactionPartId.HasValue)
                await BookRelocationTransactionPart(ctx, transaction.RelocationTransactionPartId.Value);

            await ctx.SaveChangesAsync();

            return transaction;
        }

        public async Task DeleteTransactionAsync(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? dbTransaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (dbTransaction == null)
                return;

            if (dbTransaction.State != TransactionState.DRAFT)
                throw new Exception("Only DRAFT transactions can be deleted!");

            ctx.InventoryTransactions.Remove(dbTransaction);
            await ctx.SaveChangesAsync();
        }

        private async Task BookInboundTransactionPart(WarehouseDbContext ctx, Guid transactionPartId, DateTime bookDate)
        {
            InboundTransactionPart? transactionPart = await ctx.InboundTransactionParts
                .AsTracking()
                .Include(i => i.Transaction)
                .Include(i => i.DraftItems)
                .Include(i => i.NewItemStates)
                .FirstOrDefaultAsync(i => i.Id == transactionPartId);

            if (transactionPart == null)
                throw new Exception("BookInboundTransactionPart called with transaction part id " + transactionPartId +
                                    ", which could not be found.");

            transactionPart.NewItemStates ??= [];

            foreach (InboundDraftItem draftItem in transactionPart.DraftItems)
            {
                try
                {
                    Item item = CreateItemForDraftItem(transactionPart, draftItem, bookDate);
                    await ctx.Items.AddAsync(item);
                    transactionPart.NewItemStates.AddRange(item.ItemStates);
                }
                catch (Exception ex)
                {
                    throw new BookingException($"Could not book inbound transaction part with Id {transactionPart.Id}.", ex);
                }
            }
        }

        private async Task BookOutboundTransactionPart(WarehouseDbContext ctx, Guid transactionPartId, DateTime bookDate)
        {
            OutboundTransactionPart? transactionPart = await ctx.OutboundTransactionParts
                .AsTracking()
                .Include(p => p.DraftSelectedItems!)
                    .ThenInclude(i => i.ItemStates)
                .FirstOrDefaultAsync(p => p.Id == transactionPartId);

            if (transactionPart == null)
            {
                throw new Exception("BookOutboundTransactionPart called with transaction part id " + transactionPartId +
                                    ", which could not be found.");
            }

            transactionPart.NewItemStates ??= [];

            foreach (Item item in transactionPart.DraftSelectedItems)
            {
                ItemState? currentState = item.ItemStates
                    .OrderByDescending(s => s.Date)
                    .FirstOrDefault();

                if (currentState == null)
                {
                    throw new InvalidOperationException("Item has no states.");
                }

                if (currentState.IsShipped)
                {
                    throw new InvalidOperationException("Item is already shipped.");
                }

                ItemState newItemState = new()
                {
                    Date = bookDate,
                    Location = SpecialLocations.SHIPPED,
                    WarehouseId = currentState.WarehouseId,
                    IsQuarantined = currentState.IsQuarantined,
                    Item = item,
                };

                item.ItemStates.Add(newItemState);
                transactionPart.NewItemStates.Add(newItemState);
            }
        }

        private async Task BookRelocationTransactionPart(WarehouseDbContext ctx, Guid transactionPartId)
        {
            // TODO: Implement booking logic for relocation transaction part
        }

        private static Item CreateItemForDraftItem(InboundTransactionPart transactionPart, InboundDraftItem draftItem, DateTime bookDate)
        {
            Item item = new()
            {
                Id = Guid.NewGuid(),
                ItemSchemaId = draftItem.ItemSchemaId,
                CustomerId = draftItem.CustomerId,
                ItemNumber = draftItem.ItemNumber,
                AdditionalProperties = draftItem.AdditionalProperties,
                Comments = new SortedSet<Comment>(IEnumerable<string>.Of(draftItem.Comments)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => new Comment()
                    {
                        Text = c,
                        AuthorId = transactionPart.Transaction.CreatedByUserId,
                        Date = bookDate,
                        Retracted = false,
                    })),
                ItemStates = [],
            };
            item.ItemStates.Add(new ItemState()
            {
                Date = bookDate,
                Location = draftItem.Location,
                RelatedTransactionId = transactionPart.Transaction.Id,
                WarehouseId = draftItem.WarehouseId,
                Item = item,
                IsQuarantined = draftItem.IsQuarantined,
            });
            return item;
        }
    }
}
