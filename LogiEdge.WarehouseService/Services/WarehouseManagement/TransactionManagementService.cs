using LogiEdge.Shared.Utility;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using LogiEdge.WarehouseService.Persistence;
using System;
using System.Collections.Generic;
using System.Text;
using LogiEdge.BaseService.Data;
using LogiEdge.BaseService.Services;
using LogiEdge.WarehouseService.Dto;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    public class TransactionManagementService(
        IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory,
        FileAttachmentService attachmentService)
    {
        public async Task<InventoryTransactionBaseInfoDto?> GetTransactionBaseInfo(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                return null;
            }

            return new InventoryTransactionBaseInfoDto()
            {
                Id = transactionId,
                Title = transaction.Title,
                CreatedDate = transaction.CreatedDate,
                BookedDate = transaction.BookedDate,
                CreatedByUserId = transaction.CreatedByUserId,
                HandledByWorker = transaction.HandledByWorker,
                State = transaction.State,
                HasInboundPart = await ctx.InboundTransactionParts.AnyAsync(t => t.TransactionId == transactionId),
                HasOutboundPart = await ctx.OutboundTransactionParts.AnyAsync(t => t.TransactionId == transactionId),
                HasRelocationPart = await ctx.RelocationTransactionParts.AnyAsync(t => t.TransactionId == transactionId),
            };
        }

        public async Task<InventoryTransactionFullInfoDto?> GetTransactionFullInfo(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                return null;
            }

            return new InventoryTransactionFullInfoDto()
            {
                Id = transactionId,
                Title = transaction.Title,
                CreatedDate = transaction.CreatedDate,
                BookedDate = transaction.BookedDate,
                CreatedByUserId = transaction.CreatedByUserId,
                HandledByWorker = transaction.HandledByWorker,
                State = transaction.State,
                InboundTransactionPart = ctx.InboundTransactionParts
                    .Where(t => t.TransactionId == transactionId)
                    .Include(t => t.DraftItems)
                        .ThenInclude(di => di.ItemSchema)
                    .Include(t => t.NewItemStates)
                    .ToList()
                    .Select(t => new InboundTransactionPartDto()
                    {
                        TransactionId = t.TransactionId,
                        DraftItems = t.DraftItems.Select(di => new InboundTransactionPartDto.InboundDraftItemDto()
                        {
                            Id = di.Id,
                            Location = di.Location,
                            Count = di.Count,
                            AdditionalProperties = di.ItemSchema.AdditionalProperties.Select(p => new ItemAdditionalPropertyDto()
                            {
                                Name = p.Name,
                                Value = di.GetAdditionalProperty(p.Name),
                            }).ToList(),
                            Comments = [new CommentDto()
                            {
                                AuthorId = Guid.Empty,
                                Date = DateTime.MinValue,
                                Id = Guid.Empty,
                                Retracted = false,
                                Text = di.Comments
                            }],
                            CustomerId = di.CustomerId,
                            IsQuarantined = di.IsQuarantined,
                            ItemNumber = di.ItemNumber,
                            ItemSchemaId = di.ItemSchemaId,
                            WarehouseId = di.WarehouseId,
                        }).ToList(),
                        NewItemStates = t.NewItemStates,
                    })
                    .FirstOrDefault(),
                OutboundTransactionPart = await ctx.OutboundTransactionParts
                    .Where(t => t.TransactionId == transactionId)
                    .Select(t => new OutboundTransactionPartDto()
                    {
                        TransactionId = t.TransactionId,
                        DraftPlaceholderItems = t.DraftPlaceholderItems,
                        DraftSelectedItems = t.DraftSelectedItems,
                        NewItemStates = t.NewItemStates,
                    })
                    .FirstOrDefaultAsync(),
                RelocationTransactionPart = await ctx.RelocationTransactionParts
                    .Where(t => t.TransactionId == transactionId)
                    .Select(t => new RelocationTransactionPartDto()
                    {
                        TransactionId = t.TransactionId,
                        NewItemStates = t.NewItemStates
                    })
                    .FirstOrDefaultAsync(),
            };
        }

        /// <summary>
        /// Gets the ids of the item states created by the inbound part of a booked transaction.
        /// </summary>
        public async Task<List<Guid>> GetInboundNewItemStateIds(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .FirstOrDefaultAsync(t => t.Id == transactionId);
            
            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction {transactionId} not found.");
            }

            if (transaction.State != TransactionState.BOOKED)
            {
                throw new InvalidOperationException($"Transaction {transactionId} is not in BOOKED state.");
            }

            return await ctx.InboundTransactionParts
                .Where(t => t.TransactionId == transactionId)
                .SelectMany(t => t.NewItemStates!)
                .Select(st => st.Id)
                .ToListAsync();
        }

        public async Task SetTransactionTitle(Guid transactionId, string title)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Inbound transaction with ID {transactionId} not found.");
            }

            transaction.Title = title;

            await ctx.SaveChangesAsync();
        }

        public async Task<List<FileAttachment>> GetAttachments(Guid transactionId, bool includeData = false)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            List<Guid>? attachmentIds =
                (await ctx.InventoryTransactions.FirstOrDefaultAsync(t => t.Id == transactionId))?
                .AttachmentIds;

            if (attachmentIds == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            return await attachmentService.GetAttachmentsAsync(attachmentIds, includeData);
        }

        public async Task AddAttachment(Guid transactionId, Guid attachmentId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            transaction.AttachmentIds.Add(attachmentId);
            await ctx.SaveChangesAsync();
        }

        public async Task RemoveAttachment(Guid transactionId, Guid attachmentId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            transaction.AttachmentIds.Remove(attachmentId);
            await ctx.SaveChangesAsync();
        }

        public async Task<InventoryTransaction> BookTransactionAsync(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            if (transaction.State == TransactionState.BOOKED)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} is already booked.");
            }

            transaction.State = TransactionState.BOOKED;
            transaction.BookedDate = DateTime.UtcNow;
           
            await TryBookInboundTransactionPart(ctx, transactionId, transaction.BookedDate.Value);

            await TryBookOutboundTransactionPart(ctx, transactionId, transaction.BookedDate.Value);

            await TryBookRelocationTransactionPart(ctx, transactionId);

            await ctx.SaveChangesAsync();

            return transaction;
        }

        public async Task AddInboundPartToTransaction(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            if (transaction.State != TransactionState.DRAFT)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} is not in DRAFT state.");
            }

            transaction.InboundTransactionPart = new InboundTransactionPart()
            {
                Transaction = transaction,
                DraftItems = [],
            };

            await ctx.SaveChangesAsync();
        }

        public async Task AddOutboundPartToTransaction(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            if (transaction.State != TransactionState.DRAFT)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} is not in DRAFT state.");
            }

            transaction.OutboundTransactionPart = new OutboundTransactionPart()
            {
                Transaction = transaction,
                DraftSelectedItems = [],
            };

            await ctx.SaveChangesAsync();
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

        /// <summary>
        /// Gets the base info of all transactions, ordered by creation date (newest first).
        /// </summary>
        public async Task<List<InventoryTransactionBaseInfoDto>> GetTransactionsAsync()
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            return await ctx.InventoryTransactions
                .OrderByDescending(t => t.CreatedDate)
                .Select(t => new InventoryTransactionBaseInfoDto()
                {
                    Id = t.Id,
                    Title = t.Title,
                    CreatedDate = t.CreatedDate,
                    BookedDate = t.BookedDate,
                    CreatedByUserId = t.CreatedByUserId,
                    HandledByWorker = t.HandledByWorker,
                    State = t.State,
                    HasInboundPart = ctx.InboundTransactionParts.Any(p => p.TransactionId == t.Id),
                    HasOutboundPart = ctx.OutboundTransactionParts.Any(p => p.TransactionId == t.Id),
                    HasRelocationPart = ctx.RelocationTransactionParts.Any(p => p.TransactionId == t.Id),
                })
                .ToListAsync();
        }

        /// <summary>
        /// Creates a new DRAFT transaction and returns its ID.
        /// </summary>
        public async Task<Guid> CreateTransaction(string handledByWorker)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InventoryTransaction transaction = new()
            {
                CreatedDate = DateTime.UtcNow,
                CreatedByUserId = Guid.Empty, // TODO: Actual user ID should be set here
                HandledByWorker = handledByWorker,
                AttachmentIds = []
            };

            ctx.InventoryTransactions.Add(transaction);
            await ctx.SaveChangesAsync();

            return transaction.Id;
        }

        private async Task TryBookInboundTransactionPart(WarehouseDbContext ctx, Guid transactionId, DateTime bookDate)
        {
            InboundTransactionPart? transactionPart = await ctx.InboundTransactionParts
                .AsTracking()
                .Include(i => i.Transaction)
                .Include(i => i.DraftItems)
                .Include(i => i.NewItemStates)
                .FirstOrDefaultAsync(i => i.TransactionId == transactionId);

            if (transactionPart == null)
            {
                return;
            }

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
                    throw new BookingException($"Could not book inbound transaction part with Transaction Id {transactionId}.", ex);
                }
            }
        }

        private async Task TryBookOutboundTransactionPart(WarehouseDbContext ctx, Guid transactionId, DateTime bookDate)
        {
            OutboundTransactionPart? transactionPart = await ctx.OutboundTransactionParts
                .AsTracking()
                .Include(p => p.DraftSelectedItems!)
                    .ThenInclude(i => i.ItemStates)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (transactionPart == null)
            {
                return;
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

        private async Task TryBookRelocationTransactionPart(WarehouseDbContext ctx, Guid transactionId)
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
