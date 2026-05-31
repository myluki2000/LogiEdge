using AutoMapper;
using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Utility;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using LogiEdge.WarehouseService.Persistence;
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
                .Include(t => t.InboundTransactionPart)
                .Include(t => t.OutboundTransactionPart)
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

            BookInboundTransactionPart(ctx, transaction.InboundTransactionPart);
            BookOutboundTransactionPart(transaction.OutboundTransactionPart);
            BookRelocationTransactionPart(transaction.RelocationTransactionPart);

            await ctx.SaveChangesAsync();

            return transaction;
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

            foreach (Item item in transactionPart.DraftSelectedItems!)
            {
                ItemState newItemState = new()
                {
                    Date = transactionPart.Transaction.Date,
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
                        Date = transactionPart.Transaction.Date,
                        Retracted = false,
                    })),
                ItemStates = [],
            };
            item.ItemStates.Add(new ItemState()
            {
                Date = transactionPart.Transaction.Date,
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