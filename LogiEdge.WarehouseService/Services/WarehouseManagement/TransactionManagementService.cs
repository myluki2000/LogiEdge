using LogiEdge.CustomerService.Data;
using LogiEdge.Shared;
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
                EventDate = transaction.EventDate,
                BookedDate = transaction.BookedDate,
                CreatedByUserId = transaction.CreatedByUserId,
                HandledByWorker = transaction.HandledByWorker,
                State = transaction.State,
                HasEditHistory = await ctx.TransactionEditLogEntries.AnyAsync(e => e.TransactionId == transactionId),
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
                EventDate = transaction.EventDate,
                BookedDate = transaction.BookedDate,
                CreatedByUserId = transaction.CreatedByUserId,
                HandledByWorker = transaction.HandledByWorker,
                State = transaction.State,
                HasEditHistory = await ctx.TransactionEditLogEntries.AnyAsync(e => e.TransactionId == transactionId),
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
                OutboundTransactionPart = outboundPart?.ToDto(),
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

        /// <summary>
        /// Works in both DRAFT and BOOKED state: the draft editor loads the part with it, and booked
        /// edit mode preloads the existing draft lines from it.
        /// </summary>
        public async Task<InboundTransactionPartDto> GetInboundTransactionPart(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InboundTransactionPart? transactionPart = await ctx.InboundTransactionParts
                .AsNoTracking()
                .Include(t => t.NewItemStates)
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

            return new InboundTransactionPartDto()
            {
                TransactionId = transactionPart.TransactionId,
                DraftItems = transactionPart.DraftItems.Select(InboundDraftItemToDto).ToList(),
                NewItemStates = transactionPart.NewItemStates,
            };
        }

        private InboundTransactionPartDto.InboundDraftItemDto InboundDraftItemToDto(InboundDraftItem di)
        {
            return new InboundTransactionPartDto.InboundDraftItemDto()
            {
                Id = di.Id,
                CreatedItemIds = di.CreatedItemIds.ToList(),
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

        /// <summary>
        /// Edits the inbound part of a BOOKED transaction by re-booking the diff between its current draft
        /// lines and the desired state. Only items and states created by this transaction are rewritten; if
        /// any line conflicts with later item history or a DRAFT outbound selection, nothing is persisted and
        /// a TransactionEditConflictException carrying all violations is thrown instead.
        /// </summary>
        public async Task<InventoryTransactionFullInfoDto> UpdateBookedInboundPart(Guid transactionId, InboundTransactionPartEditRequest edit)
        {
            await EnsureCanEditBookedTransactionPartsAsync();

            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            if (transaction.State != TransactionState.BOOKED)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} is not in BOOKED state.");
            }

            InboundTransactionPart? part = await ctx.InboundTransactionParts
                .AsTracking()
                .Include(p => p.Transaction)
                .Include(p => p.DraftItems)
                    .ThenInclude(d => d.ItemSchema)
                .Include(p => p.NewItemStates)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (part == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} has no inbound transaction part.");
            }

            List<Guid> itemIds = part.DraftItems.SelectMany(d => d.CreatedItemIds).ToList();
            Dictionary<Guid, Item> itemById = itemIds.Count == 0 ? [] : await ctx.Items
                .AsTracking() // must be tracked: line updates rewrite the created items/states in place
                .Where(i => itemIds.Contains(i.Id))
                .Include(i => i.ItemStates)
                    .ThenInclude(st => st.Warehouse)
                .Include(i => i.Comments)
                .Include(i => i.ItemSchema)
                .Include(i => i.Customer)
                .ToDictionaryAsync(i => i.Id);

            // --- Input validation (read-only; the planner asserts on these with its own messages) ---

            HashSet<Guid> seenDraftItemIds = [];
            foreach (InboundTransactionPartEditRequest.InboundDraftItemEdit line in edit.DraftItems)
            {
                if (line.DraftItemId != null && !seenDraftItemIds.Add(line.DraftItemId.Value))
                {
                    throw new InvalidOperationException($"Duplicate draft item ID {line.DraftItemId} in the edit request.");
                }

                if (line.DraftItemId != null && part.DraftItems.All(d => d.Id != line.DraftItemId))
                {
                    throw new InvalidOperationException($"Inbound draft item with ID {line.DraftItemId} does not belong to transaction {transactionId}.");
                }

                if (string.IsNullOrWhiteSpace(line.ItemNumber))
                {
                    throw new InvalidOperationException("Item number must not be empty.");
                }

                if (string.IsNullOrWhiteSpace(line.Location))
                {
                    throw new InvalidOperationException("Location must not be empty.");
                }

                if (line.Count < 1)
                {
                    throw new InvalidOperationException($"Count of draft line '{line.ItemNumber}' must be at least 1.");
                }
            }

            HashSet<Guid> customerIds = edit.DraftItems.Select(d => d.CustomerId).ToHashSet();
            Dictionary<Guid, Customer> customersById = await ctx.Set<Customer>()
                .Where(c => customerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id);
            if (customersById.Count != customerIds.Count)
            {
                throw new InvalidOperationException($"Customers not found: {string.Join(", ", customerIds.Except(customersById.Keys))}.");
            }

            HashSet<Guid> schemaIds = edit.DraftItems.Select(d => d.ItemSchemaId).ToHashSet();
            Dictionary<Guid, ItemSchema> schemasById = await ctx.ItemSchemas
                .Where(s => schemaIds.Contains(s.Id))
                .Include(s => s.AdditionalProperties)
                .ToDictionaryAsync(s => s.Id);
            if (schemasById.Count != schemaIds.Count)
            {
                throw new InvalidOperationException($"Item schemas not found: {string.Join(", ", schemaIds.Except(schemasById.Keys))}.");
            }

            HashSet<Guid> warehouseIds = edit.DraftItems.Select(d => d.WarehouseId).ToHashSet();
            Dictionary<Guid, Warehouse> warehousesById = await ctx.Warehouses
                .Where(w => warehouseIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id);
            if (warehousesById.Count != warehouseIds.Count)
            {
                throw new InvalidOperationException($"Warehouses not found: {string.Join(", ", warehouseIds.Except(warehousesById.Keys))}.");
            }

            foreach (InboundTransactionPartEditRequest.InboundDraftItemEdit line in edit.DraftItems)
            {
                ItemSchema schema = schemasById[line.ItemSchemaId];
                foreach (ItemAdditionalPropertyDto property in line.AdditionalProperties)
                {
                    if (schema.AdditionalProperties.All(p => p.Name != property.Name))
                    {
                        string subject = line.DraftItemId == null
                            ? $"New inbound draft line for item number '{line.ItemNumber}'"
                            : $"Inbound draft item with ID {line.DraftItemId}";
                        throw new InvalidOperationException($"{subject} has no property named '{property.Name}'.");
                    }
                }
            }

            // --- Plan (pure: nothing is mutated yet) ---

            List<InboundPartEditPlanner.CurrentInboundLine> currentLines = part.DraftItems
                .Select(di => new InboundPartEditPlanner.CurrentInboundLine(
                    di.Id,
                    di.CreatedItemIds,
                    di.ItemNumber,
                    di.CustomerId,
                    di.ItemSchemaId,
                    di.WarehouseId,
                    di.Location,
                    di.Count,
                    di.IsQuarantined,
                    di.Comments,
                    di.ItemSchema.AdditionalProperties.ToDictionary(p => p.Name, p => di.GetAdditionalProperty(p))))
                .ToList();

            InboundPartEditPlanner.InboundPartEditPlan plan = InboundPartEditPlanner.Plan(
                currentLines,
                edit.DraftItems,
                transaction.EventDate,
                itemId => itemById[itemId].ItemStates.Where(st => st.RelatedTransactionId != transaction.Id),
                itemId => ctx.OutboundTransactionParts.Any(p => p.Transaction.State == TransactionState.DRAFT && p.DraftSelectedItems != null && p.DraftSelectedItems.Any(i => i.Id == itemId)));

            if (plan.HasViolations)
            {
                throw new TransactionEditConflictException(plan.AllViolations);
            }

            if (plan.Lines.Count == 0)
            {
                // Nothing changed — skip the audit entry and save entirely.
                return await GetTransactionFullInfo(transactionId);
            }

            // --- Apply the plan: rewrite only what this transaction created itself ---

            List<InboundTransactionPartEditRequest.InboundDraftItemEdit> newLines = edit.DraftItems.Where(d => d.DraftItemId == null).ToList();
            int newLineIndex = 0;

            // Tracks the ids of items created while applying, so the audit entry can record them per line.
            List<List<Guid>> addedItemIdsPerLine = plan.Lines.Select(_ => new List<Guid>()).ToList();

            for (int lineIndex = 0; lineIndex < plan.Lines.Count; lineIndex++)
            {
                InboundPartEditPlanner.PlannedLine line = plan.Lines[lineIndex];
                switch (line.Action)
                {
                    case InboundPartEditPlanner.LineAction.Update:
                    {
                        InboundDraftItem draft = part.DraftItems.First(d => d.Id == line.DraftItemId!.Value);

                        // Field rewrites apply to every created item of the line; the line is the unit of editing.
                        foreach (Guid itemId in line.CreatedItemIds)
                            ApplyInboundLineUpdate(part, transaction, itemById[itemId], line, customersById, schemasById, warehousesById);

                        // Count increase: create fresh items at the event date and link them to the line.
                        for (int i = 0; i < line.ItemsToAdd; i++)
                        {
                            Item item = CreateItemForDraftItem(part, draft, transaction.EventDate);
                            await ctx.Items.AddAsync(item);
                            part.NewItemStates ??= [];
                            part.NewItemStates.AddRange(item.ItemStates);
                            addedItemIdsPerLine[lineIndex].Add(item.Id);
                        }

                        if (addedItemIdsPerLine[lineIndex].Count > 0)
                            draft.CreatedItemIds = [.. draft.CreatedItemIds, .. addedItemIdsPerLine[lineIndex]];

                        // Count decrease: delete the created items selected by the planner.
                        foreach (Guid itemId in line.ItemIdsToRemove)
                            RemoveCreatedItem(part, transaction, ctx, itemById[itemId]);
                        break;
                    }

                    case InboundPartEditPlanner.LineAction.Add:
                    {
                        InboundTransactionPartEditRequest.InboundDraftItemEdit desired = newLines[newLineIndex++];
                        InboundDraftItem draft = new()
                        {
                            Id = Guid.NewGuid(),
                            CustomerId = desired.CustomerId,
                            Customer = customersById[desired.CustomerId],
                            ItemSchemaId = desired.ItemSchemaId,
                            ItemSchema = schemasById[desired.ItemSchemaId],
                            WarehouseId = desired.WarehouseId,
                            Warehouse = warehousesById[desired.WarehouseId],
                            ItemNumber = desired.ItemNumber,
                            Location = desired.Location,
                            Count = desired.Count,
                            IsQuarantined = desired.IsQuarantined,
                            Comments = desired.Comments.Trim(),
                            AdditionalProperties = BuildAdditionalPropertiesDocument(desired.AdditionalProperties.Select(p => (p.Name, p.Value))),
                        };
                        part.DraftItems.Add(draft);

                        // One physical item per count unit.
                        List<Guid> createdIds = [];
                        for (int i = 0; i < desired.Count; i++)
                        {
                            Item item = CreateItemForDraftItem(part, draft, transaction.EventDate);
                            await ctx.Items.AddAsync(item);
                            part.NewItemStates ??= [];
                            part.NewItemStates.AddRange(item.ItemStates);
                            createdIds.Add(item.Id);
                        }
                        draft.CreatedItemIds = [.. createdIds];
                        addedItemIdsPerLine[lineIndex] = createdIds;
                        break;
                    }

                    case InboundPartEditPlanner.LineAction.Remove:
                    {
                        InboundDraftItem draft = part.DraftItems.First(d => d.Id == line.DraftItemId!.Value);
                        part.DraftItems.Remove(draft);

                        // Removing the line removes all of its created items (the planner verified each is removable).
                        foreach (Guid itemId in line.CreatedItemIds)
                            RemoveCreatedItem(part, transaction, ctx, itemById[itemId]);
                        break;
                    }
                }
            }

            // This edit rewrites part data without changing any scalar of the transaction itself, so force the
            // entry to Modified: the RowVersion concurrency token is then checked and bumped (G3).
            ctx.Entry(transaction).State = EntityState.Modified;

            int updatedCount = plan.Lines.Count(l => l.Action == InboundPartEditPlanner.LineAction.Update);
            int addedCount = plan.Lines.Count(l => l.Action == InboundPartEditPlanner.LineAction.Add);
            int removedCount = plan.Lines.Count(l => l.Action == InboundPartEditPlanner.LineAction.Remove);

            List<(InboundPartEditPlanner.PlannedLine Line, int Index)> planLinesIndexed = plan.Lines.Select((l, i) => (l, i)).ToList();
            object changes = new
            {
                updated = planLinesIndexed.Where(t => t.Line.Action == InboundPartEditPlanner.LineAction.Update)
                    .Select(t => new { draftItemId = t.Line.DraftItemId, itemNumber = t.Line.ItemNumber, createdItemIds = t.Line.CreatedItemIds,
                                       changes = t.Line.ChangedFields.ToDictionary(kv => kv.Key, kv => new { @old = kv.Value.Old, @new = kv.Value.New }),
                                       itemIdsAdded = addedItemIdsPerLine[t.Index],
                                       itemIdsRemoved = t.Line.ItemIdsToRemove }),
                added = planLinesIndexed.Where(t => t.Line.Action == InboundPartEditPlanner.LineAction.Add)
                    .Select(t => new { draftItemId = t.Line.DraftItemId, itemNumber = t.Line.ItemNumber, createdItemIds = addedItemIdsPerLine[t.Index],
                                       itemIdsAdded = addedItemIdsPerLine[t.Index] }),
                removed = planLinesIndexed.Where(t => t.Line.Action == InboundPartEditPlanner.LineAction.Remove)
                    .Select(t => new { draftItemId = t.Line.DraftItemId, itemNumber = t.Line.ItemNumber, createdItemIds = t.Line.CreatedItemIds,
                                       itemIdsRemoved = t.Line.CreatedItemIds }),
            };

            await WriteEditLogEntryAsync(ctx, transaction, $"Inbound part edited: {updatedCount} updated, {addedCount} added, {removedCount} removed.", changes, TransactionEditPartType.INBOUND);

            await ctx.SaveChangesAsync();

            return (await GetTransactionFullInfo(transactionId))!;
        }

        /// <summary>
        /// Edits the outbound part of a booked transaction: the desired selection is the FULL list of item ids
        /// to ship. Items missing from it are unshipped (this transaction's own SHIPPED state is deleted); items
        /// not selected before get a new SHIPPED state at the transaction's EventDate. All conflict checks run
        /// before anything is mutated; on any violation nothing is saved (see .notes/edit-booked-transactions.md,
        /// sections 5.3 and 5.4).
        /// </summary>
        public async Task<InventoryTransactionFullInfoDto> UpdateBookedOutboundPart(Guid transactionId, IEnumerable<Guid> selectedItemIds)
        {
            await EnsureCanEditBookedTransactionPartsAsync();

            ArgumentNullException.ThrowIfNull(selectedItemIds);

            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            if (transaction.State != TransactionState.BOOKED)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} is not in BOOKED state.");
            }

            OutboundTransactionPart? part = await ctx.OutboundTransactionParts
                .AsTracking()
                .Include(p => p.DraftSelectedItems!)
                    .ThenInclude(i => i.ItemStates)
                        .ThenInclude(st => st.Warehouse)
                .Include(p => p.NewItemStates)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (part == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} has no outbound transaction part.");
            }

            List<Guid> desiredIds = selectedItemIds.ToList();

            HashSet<Guid> seenItemIds = [];
            foreach (Guid itemId in desiredIds)
            {
                if (!seenItemIds.Add(itemId))
                {
                    throw new InvalidOperationException($"Duplicate item ID {itemId} in the edit request.");
                }
            }

            part.DraftSelectedItems ??= [];
            List<Guid> currentIds = part.DraftSelectedItems.Select(i => i.Id).ToList();

            // Items newly selected are not loaded yet; fetch them with their full state history.
            List<Guid> addedItemIds = desiredIds.Except(currentIds).ToList();
            Dictionary<Guid, Item> addedItemsById = addedItemIds.Count == 0 ? [] : await ctx.Items
                .AsTracking() // must be tracked: the new SHIPPED state is appended to the item's states in place
                .Where(i => addedItemIds.Contains(i.Id))
                .Include(i => i.ItemStates)
                    .ThenInclude(st => st.Warehouse)
                .ToDictionaryAsync(i => i.Id);

            if (addedItemsById.Count != addedItemIds.Count)
            {
                throw new InvalidOperationException($"Items not found: {string.Join(", ", addedItemIds.Except(addedItemsById.Keys))}.");
            }

            Dictionary<Guid, Item> itemById = part.DraftSelectedItems.ToDictionary(i => i.Id);
            foreach (KeyValuePair<Guid, Item> entry in addedItemsById)
            {
                itemById[entry.Key] = entry.Value;
            }

            OutboundPartEditPlanner.OutboundPartEditPlan plan = OutboundPartEditPlanner.Plan(
                currentIds,
                desiredIds,
                transaction.EventDate,
                itemId => itemById[itemId].ItemStates.Where(st => st.RelatedTransactionId != transaction.Id),
                itemId => itemById[itemId].ItemNumber);

            if (plan.HasViolations)
            {
                throw new TransactionEditConflictException(plan.AllViolations);
            }

            if (plan.Items.Count == 0)
            {
                // Nothing changed — skip the audit entry and save entirely.
                return await GetTransactionFullInfo(transactionId);
            }

            // --- Apply the plan: rewrite only what this transaction created itself ---

            foreach (OutboundPartEditPlanner.PlannedItem planned in plan.Items)
            {
                switch (planned.Action)
                {
                    case OutboundPartEditPlanner.ItemAction.Remove:
                    {
                        Item item = itemById[planned.ItemId];
                        // Booking recorded exactly one SHIPPED state per selected item in NewItemStates.
                        ItemState? shippedState = (part.NewItemStates ?? []).FirstOrDefault(st => st.ItemId == item.Id && st.Location == SpecialLocations.SHIPPED);
                        if (shippedState == null)
                        {
                            throw new InvalidOperationException($"No SHIPPED state created by transaction {transaction.Id} was found for item {item.ItemNumber}.");
                        }

                        ctx.Remove(shippedState);
                        part.NewItemStates!.Remove(shippedState);
                        part.DraftSelectedItems.Remove(item);
                        break;
                    }

                    case OutboundPartEditPlanner.ItemAction.Add:
                    {
                        Item item = itemById[planned.ItemId];
                        // The planner's booking-consistent check guarantees an in-warehouse state strictly before the
                        // event date exists, so the latest state always exists here.
                        ItemState currentState = item.ItemStates.OrderByDescending(s => s.Date).First();

                        ItemState newItemState = new()
                        {
                            Date = transaction.EventDate,
                            Location = SpecialLocations.SHIPPED,
                            WarehouseId = currentState.WarehouseId,
                            IsQuarantined = currentState.IsQuarantined,
                            RelatedTransactionId = transaction.Id,
                            Item = item,
                        };

                        item.ItemStates.Add(newItemState);
                        part.NewItemStates ??= [];
                        part.NewItemStates.Add(newItemState);
                        part.DraftSelectedItems.Add(item);
                        break;
                    }
                }
            }

            // This edit rewrites part data without changing any scalar of the transaction itself, so force the
            // entry to Modified: the RowVersion concurrency token is then checked and bumped (G3).
            ctx.Entry(transaction).State = EntityState.Modified;

            int addedCount = plan.Items.Count(i => i.Action == OutboundPartEditPlanner.ItemAction.Add);
            int removedCount = plan.Items.Count(i => i.Action == OutboundPartEditPlanner.ItemAction.Remove);

            object changes = new
            {
                added = plan.Items.Where(i => i.Action == OutboundPartEditPlanner.ItemAction.Add)
                    .Select(i => new { itemId = i.ItemId, itemNumber = i.ItemNumber }),
                removed = plan.Items.Where(i => i.Action == OutboundPartEditPlanner.ItemAction.Remove)
                    .Select(i => new { itemId = i.ItemId, itemNumber = i.ItemNumber }),
            };

            await WriteEditLogEntryAsync(ctx, transaction, $"Outbound part edited: {addedCount} added, {removedCount} removed.", changes, TransactionEditPartType.OUTBOUND);

            await ctx.SaveChangesAsync();

            return (await GetTransactionFullInfo(transactionId))!;
        }

        /// <summary>
        /// Booked part editing is restricted to users with the Admin role (no finer-grained role model exists
        /// yet; see .notes/edit-booked-transactions.md, section 8). The audit log records the user regardless.
        /// </summary>
        private async Task EnsureCanEditBookedTransactionPartsAsync()
        {
            AuthenticationState authState = await authenticationStateProvider.GetAuthenticationStateAsync();

            if (authState.User is null || !authState.User.IsInRole(AppRoles.Admin))
            {
                throw new UnauthorizedAccessException($"Only users with the {AppRoles.Admin} role can edit booked transaction parts.");
            }
        }

        /// <summary>
        /// Deletes one created item of a booked inbound line: detaches this transaction's own states from the
        /// part record, marks the item's comments deleted explicitly (the Comment FK has no ON DELETE clause),
        /// and removes the item — its state rows are removed by the cascade on Item.ItemStates.
        /// </summary>
        private static void RemoveCreatedItem(
            InboundTransactionPart part,
            InventoryTransaction transaction,
            WarehouseDbContext ctx,
            Item item)
        {
            // NewItemStates may be null for legacy parts.
            part.NewItemStates?.RemoveAll(st => st.RelatedTransactionId == transaction.Id && st.ItemId == item.Id);

            foreach (Comment comment in item.Comments.ToList())
            {
                ctx.Entry(comment).State = EntityState.Deleted;
            }

            ctx.Items.Remove(item);
        }

        /// <summary>
        /// Applies one planned field update to both the draft line (the part's source of truth) and one created
        /// item of the line and its initial state, keeping the Item and ItemState GUIDs. Only this
        /// transaction's own state is touched (G1); the initial state keeps its Date. The caller invokes this
        /// once per created item; count changes are handled separately by adding/removing items.
        /// </summary>
        private static void ApplyInboundLineUpdate(
            InboundTransactionPart part,
            InventoryTransaction transaction,
            Item item,
            InboundPartEditPlanner.PlannedLine line,
            Dictionary<Guid, Customer> customersById,
            Dictionary<Guid, ItemSchema> schemasById,
            Dictionary<Guid, Warehouse> warehousesById)
        {
            InboundDraftItem draft = part.DraftItems.First(d => d.Id == line.DraftItemId!.Value);
            ItemState initialState = item.ItemStates.First(st => st.RelatedTransactionId == transaction.Id);

            if (line.ChangedFields.TryGetValue("ItemNumber", out var itemNumber))
            {
                string newValue = (string)itemNumber.New!;
                draft.ItemNumber = newValue;
                item.ItemNumber = newValue;
            }

            if (line.ChangedFields.TryGetValue("CustomerId", out var customerId))
            {
                Guid newId = (Guid)customerId.New!;
                Customer customer = customersById[newId];
                draft.CustomerId = newId;
                draft.Customer = customer;
                item.CustomerId = newId;
                item.Customer = customer;
            }

            if (line.ChangedFields.TryGetValue("ItemSchemaId", out var schemaId))
            {
                Guid newId = (Guid)schemaId.New!;
                ItemSchema schema = schemasById[newId];
                draft.ItemSchemaId = newId;
                draft.ItemSchema = schema;
                item.ItemSchemaId = newId;
                item.ItemSchema = schema;
            }

            if (line.ChangedFields.TryGetValue("WarehouseId", out var warehouseId))
            {
                Guid newId = (Guid)warehouseId.New!;
                Warehouse warehouse = warehousesById[newId];
                draft.WarehouseId = newId;
                draft.Warehouse = warehouse;
                initialState.WarehouseId = newId;
                initialState.Warehouse = warehouse;
            }

            if (line.ChangedFields.TryGetValue("Location", out var location))
            {
                string newValue = (string)location.New!;
                draft.Location = newValue;
                initialState.Location = newValue;
            }

            if (line.ChangedFields.TryGetValue("IsQuarantined", out var isQuarantined))
            {
                bool newValue = (bool)isQuarantined.New!;
                draft.IsQuarantined = newValue;
                initialState.IsQuarantined = newValue;
            }

            if (line.ChangedFields.TryGetValue("Comments", out var comments))
            {
                string newComments = (string)comments.New!;
                draft.Comments = newComments;

                // Retract the booking-time comments this transaction authored, then add one for the new text.
                foreach (Comment comment in item.Comments.Where(c => c.AuthorId == transaction.CreatedByUserId && c.Date == transaction.EventDate && !c.Retracted).ToList())
                {
                    comment.Retracted = true;
                }

                if (!string.IsNullOrEmpty(newComments))
                {
                    item.Comments.Add(new Comment()
                    {
                        AuthorId = transaction.CreatedByUserId,
                        Date = transaction.EventDate,
                        Text = newComments,
                        Retracted = false,
                    });
                }
            }

            if (line.ChangedFields.TryGetValue("AdditionalProperties", out var additionalProperties))
            {
                Dictionary<string, object?> desiredProperties = (Dictionary<string, object?>)additionalProperties.New!;
                JsonDocument document = BuildAdditionalPropertiesDocument(desiredProperties.Select(kv => (kv.Key, kv.Value)));
                draft.AdditionalProperties = document;
                item.AdditionalProperties = document;
            }
        }

        /// <summary>
        /// Serializes the given additional property values into the JsonDocument shape stored on draft lines and items.
        /// </summary>
        private static JsonDocument BuildAdditionalPropertiesDocument(IEnumerable<(string Name, object? Value)> properties)
        {
            JsonObject json = [];
            foreach ((string name, object? value) in properties)
            {
                json[name] = JsonSerializer.SerializeToNode(value);
            }

            return json.Deserialize<JsonDocument>()!;
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

            string oldTitle = transaction.Title;
            transaction.Title = title;

            await WriteEditLogEntryAsync(ctx, transaction, $"Title changed from \"{TruncateForSummary(oldTitle)}\" to \"{TruncateForSummary(title)}\".", new { title = new { @old = oldTitle, @new = title } });

            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Sets the event date of a DRAFT transaction — when the transaction's physical events actually
        /// happened. All item states created at booking are stamped with this date.
        /// </summary>
        public async Task SetTransactionEventDate(Guid transactionId, DateTime eventDate)
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
                throw new InvalidOperationException(
                    $"Only DRAFT transactions can have their event date changed. " +
                    $"Transaction with ID {transactionId} is in state {transaction.State}.");
            }

            transaction.EventDate = eventDate;

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
            await WriteEditLogEntryAsync(ctx, transaction, $"Attachment {attachmentId} added.", new { attachmentAdded = attachmentId });
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
            await WriteEditLogEntryAsync(ctx, transaction, $"Attachment {attachmentId} removed.", new { attachmentRemoved = attachmentId });
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Writes an audit entry for an edit made while the transaction is booked. No-op for DRAFT
        /// transactions — only edits to booked data are audited (see .notes/edit-booked-transactions.md, 5.5).
        /// </summary>
        private async Task WriteEditLogEntryAsync(WarehouseDbContext ctx, InventoryTransaction transaction, string changeSummary, object changes, TransactionEditPartType partType = TransactionEditPartType.METADATA)
        {
            if (transaction.State != TransactionState.BOOKED)
            {
                return;
            }

            Guid userId = await authenticationStateProvider.GetUserIdAsync();

            ctx.TransactionEditLogEntries.Add(new TransactionEditLogEntry()
            {
                Id = Guid.NewGuid(),
                TransactionId = transaction.Id,
                Transaction = transaction,
                PartType = partType,
                EditedDate = DateTime.UtcNow,
                EditedByUserId = userId,
                ChangeSummary = changeSummary.Length > 255 ? changeSummary[..252] + "..." : changeSummary,
                ChangesJson = JsonDocument.Parse(JsonSerializer.Serialize(changes)),
            });
        }

        private static string TruncateForSummary(string value)
        {
            const int maxLength = 60;
            return value.Length <= maxLength ? value : value[..maxLength] + "…";
        }

        /// <summary>
        /// Adds a comment to the transaction (allowed in both DRAFT and BOOKED state) authored by the
        /// currently signed-in user. Booked transactions get an audit entry.
        /// </summary>
        public async Task<CommentDto> AddTransactionComment(Guid transactionId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Comment text must not be empty.");
            }

            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .Include(t => t.Comments)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            Guid userId = await authenticationStateProvider.GetUserIdAsync();

            Comment comment = new()
            {
                AuthorId = userId,
                Date = DateTime.UtcNow,
                Text = text.Trim(),
                Retracted = false,
            };

            transaction.Comments.Add(comment);

            await WriteEditLogEntryAsync(ctx, transaction, $"Comment added by user {userId}.", new { commentAdded = new { id = comment.Id, authorId = userId, date = comment.Date, text = comment.Text } });

            await ctx.SaveChangesAsync();

            return new CommentDto()
            {
                Id = comment.Id,
                AuthorId = comment.AuthorId,
                Date = comment.Date,
                Text = comment.Text,
                Retracted = comment.Retracted,
            };
        }

        /// <summary>
        /// Retracts a comment of the transaction (allowed in both DRAFT and BOOKED state). Any signed-in user may retract
        /// any comment — including their own. Booked transactions get an audit entry. Retracting an already retracted
        /// comment is a no-op.
        /// </summary>
        public async Task RetractTransactionComment(Guid transactionId, Guid commentId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();
            InventoryTransaction? transaction = await ctx.InventoryTransactions
                .AsTracking()
                .Include(t => t.Comments)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            Comment? comment = transaction.Comments.FirstOrDefault(c => c.Id == commentId);

            if (comment == null)
            {
                throw new InvalidOperationException($"Comment with ID {commentId} not found on transaction with ID {transactionId}.");
            }

            if (comment.Retracted)
            {
                return;
            }

            comment.Retracted = true;

            Guid userId = await authenticationStateProvider.GetUserIdAsync();

            await WriteEditLogEntryAsync(ctx, transaction, $"Comment retracted by user {userId}.", new { commentRetracted = new { id = comment.Id, authorId = comment.AuthorId, date = comment.Date } });

            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Gets all comments of a transaction ordered by date.
        /// </summary>
        public async Task<List<CommentDto>> GetTransactionComments(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            if (!await ctx.InventoryTransactions.AnyAsync(t => t.Id == transactionId))
            {
                throw new InvalidOperationException($"Transaction with ID {transactionId} not found.");
            }

            return await ctx.InventoryTransactions
                .Where(t => t.Id == transactionId)
                .SelectMany(t => t.Comments)
                .OrderBy(c => c.Date)
                .Select(c => new CommentDto()
                {
                    Id = c.Id,
                    AuthorId = c.AuthorId,
                    Date = c.Date,
                    Text = c.Text,
                    Retracted = c.Retracted,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Gets the audit history of edits made to the transaction while booked, newest first.
        /// </summary>
        public async Task<List<TransactionEditLogEntryDto>> GetTransactionEditHistory(Guid transactionId)
        {
            await using WarehouseDbContext ctx = await warehouseDbContextFactory.CreateDbContextAsync();

            return ctx.TransactionEditLogEntries
                .Where(e => e.TransactionId == transactionId)
                .OrderByDescending(e => e.EditedDate)
                .AsEnumerable()
                .Select(e => new TransactionEditLogEntryDto()
                {
                    Id = e.Id,
                    PartType = e.PartType,
                    EditedDate = e.EditedDate,
                    EditedByUserId = e.EditedByUserId,
                    ChangeSummary = e.ChangeSummary,
                    ChangesJson = e.ChangesJson.RootElement.GetRawText(),
                })
                .ToList();
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
            transaction.BookedDate = await BookedDateHelper.GetNextBookedDate(ctx);

            InboundTransactionPartDto? inboundPart = await TryBookInboundTransactionPart(ctx, transactionId, transaction.EventDate);
            OutboundTransactionPartDto? outboundPart = await TryBookOutboundTransactionPart(ctx, transactionId, transaction.EventDate);
            RelocationTransactionPartDto? relocationPart = await TryBookRelocationTransactionPart(ctx, transactionId);

            await SaveWithBookedDateRetryAsync(ctx, transaction);

            return new InventoryTransactionFullInfoDto()
            {
                Id = transaction.Id,
                Title = transaction.Title,
                CreatedDate = transaction.CreatedDate,
                EventDate = transaction.EventDate,
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

        /// <summary>
        /// Saves the booking, retrying with a fresh (higher) BookedDate if a concurrent booking claimed
        /// the same value first — enforced by the unique index on InventoryTransactions.BookedDate.
        /// </summary>
        private static async Task SaveWithBookedDateRetryAsync(WarehouseDbContext ctx, InventoryTransaction transaction)
        {
            const int maxAttempts = 3;

            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    await ctx.SaveChangesAsync();
                    return;
                }
                catch (DbUpdateException ex) when (attempt < maxAttempts && IsUniqueViolation(ex))
                {
                    transaction.BookedDate = await BookedDateHelper.GetNextBookedDate(ctx);
                }
            }
        }

        private static bool IsUniqueViolation(Exception ex)
        {
            for (Exception? inner = ex; inner != null; inner = inner.InnerException)
            {
                if (inner is Npgsql.PostgresException { SqlState: "23505" })
                {
                    return true;
                }
            }

            return false;
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

        public async Task AddRelocationPartToTransaction(Guid transactionId)
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

            transaction.RelocationTransactionPart = new RelocationTransactionPart()
            {
                Transaction = transaction,
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
                    EventDate = t.EventDate,
                    BookedDate = t.BookedDate,
                    CreatedByUserId = t.CreatedByUserId,
                    HandledByWorker = t.HandledByWorker,
                    State = t.State,
                    HasInboundPart = ctx.InboundTransactionParts.Any(p => p.TransactionId == t.Id),
                    HasOutboundPart = ctx.OutboundTransactionParts.Any(p => p.TransactionId == t.Id),
                    HasRelocationPart = ctx.RelocationTransactionParts.Any(p => p.TransactionId == t.Id),
                    HasEditHistory = ctx.TransactionEditLogEntries.Any(e => e.TransactionId == t.Id),
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

            DateTime createdDate = DateTime.UtcNow;

            InventoryTransaction transaction = new()
            {
                CreatedDate = createdDate,
                EventDate = createdDate,
                CreatedByUserId = createdById,
                HandledByWorker = handledByWorker,
                AttachmentIds = []
            };

            ctx.InventoryTransactions.Add(transaction);
            await ctx.SaveChangesAsync();

            return transaction.Id;
        }

        private async Task<InboundTransactionPartDto?> TryBookInboundTransactionPart(WarehouseDbContext ctx, Guid transactionId, DateTime eventDate)
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
                    // One physical item per count unit: each gets its own history from the event date on.
                    List<Guid> createdItemIds = [];
                    for (int i = 0; i < draftItem.Count; i++)
                    {
                        Item item = CreateItemForDraftItem(transactionPart, draftItem, eventDate);
                        createdItemIds.Add(item.Id);
                        await ctx.Items.AddAsync(item);
                        transactionPart.NewItemStates.AddRange(item.ItemStates);
                    }
                    draftItem.CreatedItemIds = [.. createdItemIds];
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

        private async Task<OutboundTransactionPartDto?> TryBookOutboundTransactionPart(WarehouseDbContext ctx, Guid transactionId, DateTime eventDate)
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

            List<string> violations = [];

            foreach (Item item in transactionPart.DraftSelectedItems)
            {
                string? violation = BookingTimelineValidator.GetOutboundBookingViolation(item.ItemStates, eventDate);
                if (violation != null)
                {
                    violations.Add($"Item {item.ItemNumber}: {violation}");
                }
            }

            if (violations.Count > 0)
            {
                throw new BookingException(
                    $"Cannot book outbound transaction part with Transaction Id {transactionId}: " + string.Join(" ", violations));
            }

            foreach (Item item in transactionPart.DraftSelectedItems)
            {
                // The pre-pass above guarantees a state strictly before the event date exists and that no
                // state is dated at or after it, so the latest state always exists here.
                ItemState currentState = item.ItemStates.OrderByDescending(s => s.Date).First();

                ItemState newItemState = new()
                {
                    Date = eventDate,
                    Location = SpecialLocations.SHIPPED,
                    WarehouseId = currentState.WarehouseId,
                    IsQuarantined = currentState.IsQuarantined,
                    RelatedTransactionId = transactionId,
                    Item = item,
                };

                item.ItemStates.Add(newItemState);
                transactionPart.NewItemStates.Add(newItemState);
            }

            return transactionPart.ToDto();
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

        private static Item CreateItemForDraftItem(InboundTransactionPart transactionPart, InboundDraftItem draftItem, DateTime eventDate)
        {
            Item item = new()
            {
                Id = Guid.NewGuid(),
                ItemSchemaId = draftItem.ItemSchemaId,
                CustomerId = draftItem.CustomerId,
                ItemNumber = draftItem.ItemNumber,
                AdditionalProperties = draftItem.AdditionalProperties.DeepClone(),
                Comments = new SortedSet<Comment>(IEnumerable<string>.Of(draftItem.Comments)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Select(c => new Comment()
                    {
                        Text = c,
                        AuthorId = transactionPart.Transaction.CreatedByUserId,
                        Date = eventDate,
                        Retracted = false,
                    })),
                ItemStates = [],
            };
            item.ItemStates.Add(new ItemState()
            {
                Date = eventDate,
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
