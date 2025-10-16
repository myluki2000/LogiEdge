using AutoMapper;
using LogiEdge.CustomerService.Data;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    public class WarehouseManagementService(IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory)
    {
        public async Task<InventoryTransaction> BookInboundTransactionAsync(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InboundTransaction? transaction = ctx.InboundTransactions
                .Include(t => t.DraftItems)
                .Include(t => t.NewItemStates!)
                .ThenInclude(st => st.Item)
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
            transaction.NewItemStates ??= [];

            foreach (InboundDraftItem draftItem in transaction.DraftItems)
            {
                try
                {
                    Item item = CreateItemForDraftItem(transaction, draftItem);
                    ctx.Items.Add(item);
                    transaction.NewItemStates.AddRange(item.ItemStates);
                }
                catch (Exception ex)
                {
                    throw new BookingException($"Could not book inbound transaction with Id {transactionId}.", ex);
                }
            }

            await ctx.SaveChangesAsync();

            return transaction;
        }

        private static Item CreateItemForDraftItem(InboundTransaction transaction, InboundDraftItem draftItem)
        {
            if (!transaction.WarehouseId.HasValue)
            {
                throw new InvalidOperationException("Cannot create item for draft item because the transaction does not have a warehouse assigned.");
            }

            Item item = new()
            {
                Id = Guid.NewGuid(),
                ItemSchemaId = draftItem.ItemSchemaId,
                CustomerId = draftItem.CustomerId,
                ItemNumber = draftItem.ItemNumber,
                AdditionalProperties = draftItem.AdditionalProperties,
                Comments = draftItem.Comments,
                ItemStates = [],
            };
            item.ItemStates.Add(new ItemState()
            {
                Date = transaction.Date,
                Location = draftItem.Location,
                RelatedTransaction = transaction,
                WarehouseId = transaction.WarehouseId.Value,
                Item = item,
            });
            return item;
        }
    }
}