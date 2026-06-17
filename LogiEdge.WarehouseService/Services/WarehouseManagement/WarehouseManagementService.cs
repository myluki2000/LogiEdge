using AutoMapper;
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
                .Include(t => t.InboundTransactionPart).ThenInclude(i => i.DraftItems)
                .Include(t => t.InboundTransactionPart).ThenInclude(i => i.NewItemStates)
                .Include(t => t.OutboundTransactionPart).ThenInclude(o => o.DraftSelectedItems)
                .Include(t => t.OutboundTransactionPart).ThenInclude(o => o.NewItemStates)
                .Include(t => t.RelocationTransactionPart)
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

            BookInboundTransactionPart(ctx, transaction.InboundTransactionPart);
            BookOutboundTransactionPart(transaction.OutboundTransactionPart);
            BookRelocationTransactionPart(transaction.RelocationTransactionPart);

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

        private void BookInboundTransactionPart(WarehouseDbContext ctx, InboundTransactionPart? transactionPart)
        {
            if (transactionPart == null)
                return;

            transactionPart.NewItemStates ??= [];

            foreach (InboundDraftItem draftItem in transactionPart.DraftItems)
            {
                try
                {
                    Item item = CreateItemForDraftItem(transactionPart, draftItem);
                    ctx.Items.Add(item);
                    transactionPart.NewItemStates.AddRange(item.ItemStates);
                }
                catch (Exception ex)
                {
                    throw new BookingException($"Could not book inbound transaction with Id {transactionPart.Transaction.Id}.", ex);
                }
            }
        }

        private void BookOutboundTransactionPart(OutboundTransactionPart? transactionPart)
        {
            if (transactionPart == null)
                return;

            transactionPart.NewItemStates ??= [];

            if (transactionPart.Transaction.BookedDate == null)
                throw new Exception("Transaction has no BookedDate set even though it was booked!");

            foreach (Item item in transactionPart.DraftSelectedItems!)
            {
                ItemState newItemState = new()
                {
                    Date = transactionPart.Transaction.BookedDate.Value,
                    Location = SpecialLocations.SHIPPED,
                    WarehouseId = item.ItemStates.Last().WarehouseId,
                    IsQuarantined = item.ItemStates.Last().IsQuarantined,
                };

                item.ItemStates.Add(newItemState);
                transactionPart.NewItemStates.Add(newItemState);
            }
        }

        private void BookRelocationTransactionPart(RelocationTransactionPart? transactionPart)
        {
            if (transactionPart == null)
                return;
        }

        private static Item CreateItemForDraftItem(InboundTransactionPart transactionPart, InboundDraftItem draftItem)
        {
            if (transactionPart.Transaction.BookedDate == null)
                throw new Exception("Transaction has no BookedDate set even though it was booked!");

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
                        Date = transactionPart.Transaction.BookedDate.Value,
                        Retracted = false,
                    })),
                ItemStates = [],
            };
            item.ItemStates.Add(new ItemState()
            {
                Date = transactionPart.Transaction.BookedDate.Value,
                Location = draftItem.Location,
                RelatedTransaction = transactionPart.Transaction,
                WarehouseId = draftItem.WarehouseId,
                Item = item,
                IsQuarantined = draftItem.IsQuarantined,
            });
            return item;
        }
    }
}