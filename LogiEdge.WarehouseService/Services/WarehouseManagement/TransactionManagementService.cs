using LogiEdge.CustomerService.Data;
using LogiEdge.Shared.Utility;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Data.Transactions;
using LogiEdge.WarehouseService.Persistence;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LogiEdge.BaseService.Data;
using LogiEdge.BaseService.Dto;
using LogiEdge.BaseService.Services;
using LogiEdge.WarehouseService.Dto;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace LogiEdge.WarehouseService.Services.WarehouseManagement
{
    public class TransactionManagementService(
        IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory,
        FileAttachmentService attachmentService,
        AuthenticationStateProvider authenticationStateProvider)
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

            OutboundTransactionPart? outboundPart = await ctx.OutboundTransactionParts
                .Where(t => t.TransactionId == transactionId)
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
                .FirstOrDefaultAsync();

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
                        DraftItems = t.DraftItems.Select(InboundDraftItemToDto).ToList(),
                        NewItemStates = t.NewItemStates,
                    })
                    .FirstOrDefault(),
                OutboundTransactionPart = outboundPart == null ? null : OutboundTransactionPartToDto(outboundPart),
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

        public async Task<InboundTransactionPartDto> GetInboundTransactionPart(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InboundTransactionPart? transactionPart = await ctx.InboundTransactionParts
                .AsNoTracking()
                .Include(t => t.NewItemStates)
                .Include(t => t.Transaction)
                .Include(t => t.DraftItems)
                    .ThenInclude(di => di.Customer)
                .Include(t => t.DraftItems)
                    .ThenInclude(di => di.Warehouse)
                .Include(t => t.DraftItems)
                    .ThenInclude(di => di.ItemSchema)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transactionPart == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} does not exist or has no inbound transaction part.");
            }

            if (transactionPart.Transaction.State != TransactionState.DRAFT)
            {
                throw new InvalidOperationException($"Transaction {transactionId} is not in DRAFT state.");
            }

            return new InboundTransactionPartDto()
            {
                TransactionId = transactionPart.TransactionId,
                DraftItems = transactionPart.DraftItems.Select(InboundDraftItemToDto).ToList(),
                NewItemStates = transactionPart.NewItemStates,
            };
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

        private InboundTransactionPartDto.InboundDraftItemDto InboundDraftItemToDto(InboundDraftItem di)
        {
            return new InboundTransactionPartDto.InboundDraftItemDto()
            {
                Id = di.Id,
                Location = di.Location,
                Count = di.Count,
                AdditionalProperties = di.ItemSchema.AdditionalProperties.Select(p =>
                    new ItemAdditionalPropertyDto()
                    {
                        Name = p.Name,
                        Value = di.GetAdditionalProperty(p),
                        Type = p.Type
                    }).ToList(),
                Comments =
                [
                    new CommentDto()
                    {
                        AuthorId = Guid.Empty,
                        Date = DateTime.MinValue,
                        Id = Guid.Empty,
                        Retracted = false,
                        Text = di.Comments
                    }
                ],
                CustomerId = di.CustomerId,
                IsQuarantined = di.IsQuarantined,
                ItemNumber = di.ItemNumber,
                ItemSchemaId = di.ItemSchemaId,
                WarehouseId = di.WarehouseId,
            };
        }

        public async Task<InboundTransactionPartDto.InboundDraftItemDto> AddInboundDraftItem(Guid transactionId, Guid customerId, Guid itemSchemaId, Guid warehouseId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InboundTransactionPart? transactionPart = await ctx.InboundTransactionParts
                .AsTracking()
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transactionPart == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} does not exist or has no inbound transaction part.");
            }

            Customer? customer = await ctx.Set<Customer>().FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer == null)
            {
                throw new InvalidOperationException($"Customer with ID {customerId} not found.");
            }

            ItemSchema? itemSchema = await ctx.ItemSchemas.FirstOrDefaultAsync(s => s.Id == itemSchemaId);
            if (itemSchema == null)
            {
                throw new InvalidOperationException($"Item schema with ID {itemSchemaId} not found.");
            }

            Warehouse? warehouse = await ctx.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId);
            if (warehouse == null)
            {
                throw new InvalidOperationException($"Warehouse with ID {warehouseId} not found.");
            }

            InboundDraftItem newItem = new()
            {
                Customer = customer,
                CustomerId = customerId,
                ItemSchema = itemSchema,
                ItemSchemaId = itemSchemaId,
                Warehouse = warehouse,
                WarehouseId = warehouseId,
            };

            transactionPart.DraftItems.Add(newItem);

            await ctx.SaveChangesAsync();

            return InboundDraftItemToDto(newItem);
        }

        public async Task<InboundTransactionPartDto.InboundDraftItemDto> UpdateInboundDraftItemPropertyValue(Guid transactionId, Guid draftItemId, string propertyName, object? newValue)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InboundTransactionPart? tp = await ctx.InboundTransactionParts
                .AsTracking()
                .Include(tp => tp.DraftItems)
                    .ThenInclude(di => di.ItemSchema)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (tp == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} does not exist or has no inbound transaction part.");
            }

            InboundDraftItem? existing = tp.DraftItems.FirstOrDefault(di => di.Id == draftItemId);
            if (existing == null)
            {
                throw new InvalidOperationException($"Inbound draft item with ID {draftItemId} does not belong to transaction {transactionId}.");
            }

            PropertyInfo? nativeProperty = existing.GetType().GetProperty(propertyName);
            if (nativeProperty != null)
            {
                nativeProperty.SetValue(existing, newValue);
            }
            else
            {
                if (existing.ItemSchema.AdditionalProperties.All(p => p.Name != propertyName))
                {
                    throw new InvalidOperationException($"Inbound draft item with ID {draftItemId} has no property named '{propertyName}'.");
                }

                JsonObject jsonProperties = existing.AdditionalProperties.RootElement.Deserialize<JsonObject>() ?? new();
                jsonProperties[propertyName] = JsonSerializer.SerializeToNode(newValue);
                existing.AdditionalProperties = jsonProperties.Deserialize<JsonDocument>();
            }

            await ctx.SaveChangesAsync();
            return InboundDraftItemToDto(existing);
        }

        public async Task DeleteInboundDraftItem(Guid transactionId, Guid draftItemId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InboundTransactionPart? transactionPart = await ctx.InboundTransactionParts
                .AsTracking()
                .Include(t => t.DraftItems)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transactionPart == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} does not exist or has no inbound transaction part.");
            }

            InboundDraftItem? existing = transactionPart.DraftItems.FirstOrDefault(di => di.Id == draftItemId);
            if (existing == null)
            {
                throw new InvalidOperationException($"Inbound draft item with ID {draftItemId} does not belong to transaction {transactionId}.");
            }

            transactionPart.DraftItems.Remove(existing);

            await ctx.SaveChangesAsync();
        }

        public async Task DeleteInboundDraftItemList(Guid transactionId, Guid customerId, Guid itemSchemaId, Guid warehouseId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InboundTransactionPart? transactionPart = await ctx.InboundTransactionParts
                .AsTracking()
                .Include(t => t.DraftItems)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);

            if (transactionPart == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} does not exist or has no inbound transaction part.");
            }

            List<InboundDraftItem> items = transactionPart.DraftItems
                .Where(di => di.CustomerId == customerId && di.ItemSchemaId == itemSchemaId && di.WarehouseId == warehouseId)
                .ToList();

            if (items.Count == 0)
            {
                throw new InvalidOperationException($"No draft items found for the given item list in transaction {transactionId}.");
            }

            foreach (InboundDraftItem item in items)
                transactionPart.DraftItems.Remove(item);

            await ctx.SaveChangesAsync();
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

        public async Task<List<FileAttachmentDto>> GetAttachments(Guid transactionId, bool includeData = false)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            List<Guid>? attachmentIds =
                (await ctx.InventoryTransactions.FirstOrDefaultAsync(t => t.Id == transactionId))?
                .AttachmentIds;

            if (attachmentIds == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            return await attachmentService.GetAttachments(attachmentIds, includeData);
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

        public async Task<InventoryTransactionFullInfoDto> BookTransaction(Guid transactionId)
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
           
            InboundTransactionPartDto? inboundPart = await TryBookInboundTransactionPart(ctx, transactionId, transaction.BookedDate.Value);
            OutboundTransactionPartDto? outboundPart = await TryBookOutboundTransactionPart(ctx, transactionId, transaction.BookedDate.Value);
            RelocationTransactionPartDto? relocationPart = await TryBookRelocationTransactionPart(ctx, transactionId);

            await ctx.SaveChangesAsync();

            return new InventoryTransactionFullInfoDto()
            {
                Id = transaction.Id,
                Title = transaction.Title,
                CreatedDate = transaction.CreatedDate,
                BookedDate = transaction.BookedDate,
                CreatedByUserId = transaction.CreatedByUserId,
                HandledByWorker = transaction.HandledByWorker,
                State = transaction.State,
                HasInboundPart = inboundPart != null,
                HasOutboundPart = outboundPart != null,
                HasRelocationPart = relocationPart != null,
                InboundTransactionPart = inboundPart,
                OutboundTransactionPart = outboundPart,
                RelocationTransactionPart = relocationPart,
            };
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
        /// Creates a new DRAFT transaction attributed to the currently signed-in user and returns its ID.
        /// </summary>
        public async Task<Guid> CreateTransaction(string handledByWorker)
        {
            Guid userId = await authenticationStateProvider.GetUserIdAsync();
            return await CreateTransactionForUser(userId, handledByWorker);
        }

        /// <summary>
        /// Creates a new DRAFT transaction attributed to the given user and returns its ID.
        /// </summary>
        public async Task<Guid> CreateTransactionForUser(Guid createdById, string handledByWorker)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InventoryTransaction transaction = new()
            {
                CreatedDate = DateTime.UtcNow,
                CreatedByUserId = createdById,
                HandledByWorker = handledByWorker,
                AttachmentIds = []
            };

            ctx.InventoryTransactions.Add(transaction);
            await ctx.SaveChangesAsync();

            return transaction.Id;
        }

        private async Task<InboundTransactionPartDto?> TryBookInboundTransactionPart(WarehouseDbContext ctx, Guid transactionId, DateTime bookDate)
        {
            InboundTransactionPart? transactionPart = await ctx.InboundTransactionParts
                .AsTracking()
                .Include(i => i.Transaction)
                .Include(i => i.DraftItems)
                    .ThenInclude(di => di.ItemSchema)
                .Include(i => i.NewItemStates)
                .FirstOrDefaultAsync(i => i.TransactionId == transactionId);

            if (transactionPart == null)
            {
                return null;
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

            return new InboundTransactionPartDto()
            {
                TransactionId = transactionPart.TransactionId,
                DraftItems = transactionPart.DraftItems.Select(InboundDraftItemToDto).ToList(),
                NewItemStates = transactionPart.NewItemStates,
            };
        }

        private async Task<OutboundTransactionPartDto?> TryBookOutboundTransactionPart(WarehouseDbContext ctx, Guid transactionId, DateTime bookDate)
        {
            OutboundTransactionPart? transactionPart = await ctx.OutboundTransactionParts
                .AsTracking()
                .Include(p => p.DraftSelectedItems!)
                    .ThenInclude(i => i.ItemSchema)
                        .ThenInclude(sch => sch.AdditionalProperties)
                .Include(p => p.DraftSelectedItems!)
                    .ThenInclude(i => i.Customer)
                .Include(p => p.DraftSelectedItems!)
                    .ThenInclude(i => i.Comments)
                .Include(p => p.DraftSelectedItems!)
                    .ThenInclude(i => i.ItemStates)
                        .ThenInclude(st => st.Warehouse)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (transactionPart == null)
            {
                return null;
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

            return OutboundTransactionPartToDto(transactionPart);
        }

        private async Task<RelocationTransactionPartDto?> TryBookRelocationTransactionPart(WarehouseDbContext ctx, Guid transactionId)
        {
            RelocationTransactionPart? transactionPart = await ctx.RelocationTransactionParts
                .AsTracking()
                .Include(p => p.NewItemStates)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (transactionPart == null)
            {
                return null;
            }

            // TODO: Implement booking logic for relocation transaction part

            return new RelocationTransactionPartDto()
            {
                TransactionId = transactionPart.TransactionId,
                NewItemStates = transactionPart.NewItemStates,
            };
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
