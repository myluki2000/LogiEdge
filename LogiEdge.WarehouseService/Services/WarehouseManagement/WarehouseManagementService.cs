using AutoMapper;
using EntityFrameworkCore.Projectables.Extensions;
using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Utility;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    public class WarehouseManagementService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        public async Task<InventoryTransaction> BookTransactionAsync(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefault(t => t.Id == transactionId);

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

            if(transaction.InboundTransactionPartId.HasValue)
                await BookInboundTransactionPart(ctx, transaction.InboundTransactionPartId.Value, transaction.BookedDate.Value);

            if(transaction.OutboundTransactionPartId.HasValue)
                await BookOutboundTransactionPart(ctx, transaction.OutboundTransactionPartId.Value, transaction.BookedDate.Value);

            if(transaction.RelocationTransactionPartId.HasValue)
                await BookRelocationTransactionPart(ctx, transaction.RelocationTransactionPartId.Value);

            await ctx.SaveChangesAsync();

            return transaction;
        }

        public async Task DeleteTransactionAsync(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? dbTransaction = await ctx.InventoryTransactions
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
                .Include(i => i.Transaction)
                .Include(i => i.DraftItems)
                .Include(i => i.NewItemStates)
                .FirstOrDefaultAsync();

            if (transactionPart == null)
                throw new Exception("BookInboundTransactionPart called with transaction part id " + transactionPartId +
                                    ", which could not be found.");

            transactionPart.NewItemStates ??= [];

            foreach (InboundDraftItem draftItem in transactionPart.DraftItems)
            {
                try
                {
                    Item item = CreateItemForDraftItem(transactionPart, draftItem, bookDate);
                    ctx.Items.Add(item);
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
            var a = await ctx.OutboundTransactionParts
                .Select(x =>
                new {
                    x.Id,
                    DraftSelectedItems = x.DraftSelectedItems.Select(y => new
                    {
                        y.CurrentState,
                        y
                    }),
                    x.NewItemStates,
                    x.Transaction
                })
                .ExpandProjectables()
                .FirstOrDefaultAsync(t => t.Id == transactionPartId);

            OutboundTransactionPart? transactionPart = await ctx.OutboundTransactionParts
                .AsTracking()
                .Include(o => o.DraftSelectedItems)
                .Include(o => o.NewItemStates)
                .Include(p => p.Transaction)
                .FirstOrDefaultAsync(t => t.Id == transactionPartId);

            if (transactionPart == null)
                return;

            transactionPart.NewItemStates ??= [];

            foreach (Item item in transactionPart.DraftSelectedItems!)
            {
                ItemState newItemState = new()
                {
                    Date = bookDate,
                    Location = SpecialLocations.SHIPPED,
                    WarehouseId = item.CurrentState!.WarehouseId,
                    IsQuarantined = item.CurrentState!.IsQuarantined,
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