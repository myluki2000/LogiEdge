using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using LogiEdge.BaseService.Persistence;
using LogiEdge.BaseService.Services;
using LogiEdge.CustomerService.Data;
using LogiEdge.CustomerService.Services;
using LogiEdge.ExcelImporterService.Data;
using LogiEdge.ExcelImporterService.Internal;
using LogiEdge.Shared.Utility;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Persistence;
using LogiEdge.WarehouseService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogiEdge.ExcelImporterService.Services
{
    public class ExcelImportingService(SettingsService settingsService,
                                       IDbContextFactory<WarehouseDbContext> warehouseDbContextFactory,
                                       CustomerManagementService customerService)
    {
        public async Task RunImportAsync()
        {
            string configJson = await settingsService.GetSettingAsync("ExcelImporterConfig")
                                ?? throw new Exception("Could not find 'ExcelImporterConfig' settings entry.");

            ExcelImporterConfig config = LoadConfigFromPath(configJson);

            RunImportUsingConfig(config);
        }

        public ExcelImporterConfig LoadConfigFromPath(string path)
        {
            using FileStream fileStream = new(path, FileMode.Open);
            return LoadConfigFromStream(fileStream);
        }

        public ExcelImporterConfig LoadConfigFromStream(Stream stream)
        {
            return JsonSerializer.Deserialize<ExcelImporterConfig>(stream) ??
                   throw new Exception("Error parsing ExcelImporterConfig config json.");
        }

        public void RunImportUsingConfig(ExcelImporterConfig config)
        {
            using WarehouseDbContext warehouseDbContext = warehouseDbContextFactory.CreateDbContext();

            // create the item schemas listed in the config if they don't exist already
            foreach (InventoryItemSchema schema in config.InventoryItemSchemas)
            {
                // get schema with the given name if it exists already
                ItemSchema? existingSchema = warehouseDbContext.ItemSchemas
                    .Include(sch => sch.Customers)
                    .FirstOrDefault(x => x.Name == schema.Name);

                // check if the existing schema has the correct properties
                if (existingSchema != null)
                {
                    if (!existingSchema.AdditionalProperties.Select(x => x.Name).SequenceEqual(schema.AdditionalProperties)
                        || !existingSchema.AdditionalProperties.Select(x => x.Type.ToString()).SequenceEqual(schema.AdditionalPropertiesTypes))
                        throw new Exception("Schema with same name as defined in the excel import config already exists but has different properties or property types.");
                }
                else
                {
                    existingSchema = new ItemSchema()
                    {
                        Name = schema.Name,
                        AdditionalProperties = schema.AdditionalProperties.Zip(schema.AdditionalPropertiesTypes)
                            .Select(x => new ItemSchemaProperty()
                            {
                                Name = x.First,
                                Type = Enum.Parse<ItemSchemaProperty.SupportedTypes>(x.Second)
                            }).ToList()
                    };
                    warehouseDbContext.ItemSchemas.Add(existingSchema);
                }

                // create customers associated with this schema if they don't exist already
                foreach (string customerName in schema.Customers)
                {
                    Customer? customer = customerService.GetCustomers().FirstOrDefault(x => x.Name == customerName);
                    if (customer == null)
                    {
                        customer = customerService.CreateCustomer(new Customer()
                        {
                            Name = customerName,
                            Abbreviation = customerName.Substring(0, 2).ToUpper()
                        });
                    }
                    // add the item schema to the customer's schemas if it doesn't exist already
                    warehouseDbContext.Attach(customer);
                    if (!existingSchema.Customers.Contains(customer))
                    {
                        existingSchema.Customers.Add(customer);
                    }
                }
            }

            warehouseDbContext.SaveChanges();

            foreach (InventoryFileMatchingOptions options in config.InventoryFileMatchingOptions)
            {
                // get or create the customer entry for this import
                Customer? customer = customerService.GetCustomers().FirstOrDefault(x => x.Name == options.CustomerName);
                if (customer == null)
                {
                    throw new Exception("Could not find defined customer with the given name " + options.CustomerName);
                }

                // get or create the warehouse entry for this import
                Warehouse? warehouse = warehouseDbContext.Warehouses
                    .Include(x => x.Items)
                    .ThenInclude(x => x.ItemStates)
                    .Include(x => x.Items)
                    .ThenInclude(x => x.Comments)
                    .FirstOrDefault(x => x.Name == options.WarehouseName);
                
                if (warehouse == null)
                {
                    warehouse = warehouseDbContext.Warehouses.Add(new Warehouse()
                    {
                        Name = options.WarehouseName,
                        Items = new List<Item>(),
                    }).Entity;
                }

                // loop through all the days we have in the Excel backup
                bool firstEntry = true;
                foreach (string dayDirPath in Directory.GetDirectories(config.FolderPath))
                {
                    string dayDirName = Path.GetFileName(dayDirPath);

                    DateTime day = DateTime
                        .SpecifyKind(DateTime.ParseExact(dayDirName, "yyyy-MM-dd", CultureInfo.InvariantCulture), DateTimeKind.Local)
                        .ToUniversalTime();

                    InventoryFileMatcher matcher = new(dayDirPath, day, options);

                    List<InventoryFileMatcher.InventoryItem> items = matcher.Process();

                    if (firstEntry)
                    {
                        // just straight up save the items for the first entry of the backup
                        foreach (InventoryFileMatcher.InventoryItem item in items)
                        {
                            Item itemEntity = new()
                            {
                                ItemNumber = item.ItemNumber,
                                CustomerId = customer.Id,
                                ItemSchemaId = warehouseDbContext.ItemSchemas.First(sch => sch.Name == options.UseSchema).Id,
                                Comments = new SortedSet<Comment>(IEnumerable<string>.Of(item.Comment)
                                    .Where(c => !string.IsNullOrWhiteSpace(c))
                                    .Select(c => new Comment()
                                {
                                    AuthorId = Guid.Empty,
                                    Date = item.EntryDate,
                                    Retracted = false,
                                    Text = c,
                                })),
                                ItemStates = [
                                    new ItemState()
                                    {
                                        Date = item.EntryDate,
                                        WarehouseId = warehouse.Id,
                                        Location = item.StorageLocation,
                                    }
                                ]
                            };

                            JsonObject jsonObj = new();
                            foreach (KeyValuePair<string, string> property in item.AdditionalProperties)
                            {
                                jsonObj[property.Key] = property.Value;
                            }
                            itemEntity.AdditionalProperties = jsonObj.Deserialize<JsonDocument>();

                            if (item.ExitDate.HasValue)
                            {
                                itemEntity.ItemStates.Add(new ItemState()
                                {
                                    Date = item.ExitDate.Value,
                                    WarehouseId = warehouse.Id,
                                    Location = SpecialLocations.SHIPPED,
                                });
                            }

                            warehouse.Items.Add(itemEntity);
                        }

                        firstEntry = false;
                    }
                    else
                    {
                        // for the other ones we'll have to match them with the existing entries in the DB to find
                        // out which items were moved

                        List<Guid> unchangedItemIds = [];

                        // firstly, remove all items from our new list which have stayed the same from last time
                        items.RemoveAll(x => warehouse.Items
                            .Where(y => y.InWarehouse == x.InWarehouse)
                            .Any(y =>
                        {
                            // we can't use an item which has already been "occupied" by another unchanged item
                            if (unchangedItemIds.Contains(y.Id))
                                return false;

                            // if the item number (or other matching properties) are different, it's not the same item
                            if (!IsInventoryItemMatchingEntity(options, x, y))
                                return false;

                            // if the item properties match, but the location is different, it might be the same item
                            // which was moved, or a different item; we don't know yet - handle it later
                            if (y.ItemStates.OrderBy(state => state.Date).Last().Location != (x.ExitDate.HasValue ? SpecialLocations.SHIPPED : x.StorageLocation))
                                return false;

                            unchangedItemIds.Add(y.Id);
                            return true;
                        }));

                        List<Item> itemsToAdd = [];

                        items.ForEach(x =>
                        {
                            // see if an existing item exists, which matches the current item, is in the warehouse, not one of the unchanged items
                            // we've already accounted for, and which has not already been "used" for another moved item (i.e. no item state from today)
                            Item? existingItem = warehouse.Items.FirstOrDefault(y =>
                                IsInventoryItemMatchingEntity(options, x, y)
                                && y.InWarehouse
                                && !unchangedItemIds.Contains(y.Id)
                                && y.ItemStates.Select(st => st.Date).Max() < day);

                            if (existingItem == null)
                            {
                                // if we have no existing item, this is a newly stored item
                                Item itemEntity = new()
                                {
                                    ItemNumber = x.ItemNumber,
                                    CustomerId = customer.Id,
                                    ItemSchemaId = warehouseDbContext.ItemSchemas.First(sch => sch.Name == options.UseSchema).Id,
                                    ItemStates = [
                                        new ItemState()
                                        {
                                            Date = x.EntryDate,
                                            WarehouseId = warehouse.Id,
                                            Location = x.StorageLocation,
                                        }
                                    ],
                                    Comments = new SortedSet<Comment>(IEnumerable<string>.Of(x.Comment)
                                        .Where(c => !string.IsNullOrEmpty(c))
                                        .Select(c => new Comment()
                                        {
                                            AuthorId = Guid.Empty,
                                            Date = x.EntryDate,
                                            Retracted = false,
                                            Text = c,
                                        }))
                                };

                                JsonObject jsonObj = new();
                                foreach (KeyValuePair<string, string> property in x.AdditionalProperties)
                                {
                                    jsonObj[property.Key] = property.Value;
                                }
                                itemEntity.AdditionalProperties = jsonObj.Deserialize<JsonDocument>();

                                if (x.ExitDate.HasValue)
                                {
                                    itemEntity.ItemStates.Add(new ItemState()
                                    {
                                        Date = x.ExitDate.Value,
                                        WarehouseId = warehouse.Id,
                                        Location = SpecialLocations.SHIPPED,
                                    });
                                }

                                itemsToAdd.Add(itemEntity);

                            }
                            else
                            {
                                // otherwise we have an item which was moved (or exited the warehouse, if the spreadsheet
                                // doesn't remove items when they exit the warehouse but instead has an exit date column),
                                // so add a new item state to it
                                if (x.ExitDate.HasValue)
                                {
                                    existingItem.ItemStates.Add(new ItemState()
                                    {
                                        Date = x.ExitDate.Value,
                                        WarehouseId = warehouse.Id,
                                        Location = SpecialLocations.SHIPPED,
                                    });
                                }
                                else
                                {
                                    if (existingItem.ItemStates.Last().Location != x.StorageLocation)
                                    {
                                        existingItem.ItemStates.Add(new ItemState()
                                        {
                                            Date = day,
                                            WarehouseId = warehouse.Id,
                                            Location = x.StorageLocation,
                                        });
                                    }

                                    if (existingItem.Comments.Last().Text != x.Comment)
                                    {
                                        foreach (Comment c in existingItem.Comments)
                                        {
                                            c.Retracted = true;
                                        }

                                        existingItem.Comments.Add(new Comment()
                                        {
                                            AuthorId = Guid.Empty,
                                            Date = day,
                                            Retracted = false,
                                            Text = x.Comment,
                                        });
                                    }
                                }
                            }
                        });

                        foreach (Item itemToRemove in warehouse.Items
                                     .Where(x => x.InWarehouse && x.ItemStates.Select(st => st.Date).Max() < day &&
                                                 !unchangedItemIds.Contains(x.Id)))
                        {
                            itemToRemove.ItemStates.Add(new ItemState()
                            {
                                Date = day,
                                WarehouseId = warehouse.Id,
                                Location = SpecialLocations.SHIPPED,
                            });
                        }

                        foreach (Item item in itemsToAdd)
                        {
                            warehouse.Items.Add(item);
                        }
                    }

                    warehouseDbContext.SaveChanges();
                }
            }
        }

        private bool IsInventoryItemMatchingEntity(InventoryFileMatchingOptions options, InventoryFileMatcher.InventoryItem x, Item y)
        {
            foreach (string columnName in options.IdentifyingColumns)
            {
                switch (options.Columns[columnName])
                {
                    case InventoryFileMatchingOptions.ColumnType.ITEM_NUMBER
                        when y.ItemNumber != x.ItemNumber:
                        return false;
                    case InventoryFileMatchingOptions.ColumnType.ENTRY_DATE
                        when y.ItemStates.OrderBy(state => state.Date).First().Date != x.EntryDate:
                        return false;
                    case InventoryFileMatchingOptions.ColumnType.STORAGE_LOCATION
                        when y.ItemStates.OrderBy(state => state.Date).Last().Location != x.StorageLocation:
                        return false;
                    case InventoryFileMatchingOptions.ColumnType.OTHER
                        when y.AdditionalProperties.RootElement.GetProperty(columnName).GetString() != x.AdditionalProperties[columnName]:
                        return false;
                }
            }

            return true;
        }
    }
}
