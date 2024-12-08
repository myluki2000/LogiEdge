using System.Diagnostics;
using LogiEdge.CustomerService.Persistence;
using LogiEdge.CustomerService.Services;
using LogiEdge.ExcelImporterService.Data;
using LogiEdge.ExcelImporterService.Services;
using LogiEdge.Test.Shared.Utility;
using LogiEdge.WarehouseService.Data;
using LogiEdge.WarehouseService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using Testcontainers.PostgreSql;

namespace LogiEdge.Test.ExcelImporterService
{
    public class ExcelImportingServiceTest(DatabaseFixture dbFixture) : IClassFixture<DatabaseFixture>
    {
        [Fact]
        public void TestSingleFile()
        {
            CustomerManagementService customerService = new(dbFixture.CustomerDbContextFactory);

            ExcelImportingService service = new(null!, dbFixture.WarehouseDbContextFactory, customerService);

            ExcelImporterConfig config = service.LoadConfigFromPath("./Resources/TestSingleFileConfig.json");

            service.RunImportUsingConfig(config);

            using CustomerDbContext customerCtx = dbFixture.CustomerDbContextFactory.CreateDbContext();
            using WarehouseDbContext warehouseCtx = dbFixture.WarehouseDbContextFactory.CreateDbContext();

            // assert that customer was created with the correct name
            Assert.Equal("ExampleCustomer", customerCtx.Customers.First().Name);

            // assert warehouse was created with the correct name
            Assert.Equal("ExampleWarehouse", warehouseCtx.Warehouses.First().Name);

            List<Item> items = warehouseCtx.Items
                .Include(x => x.ItemStates)
                .ToList();

            // assert all items have exactly 1 item state
            Assert.All(items, x => Assert.Single(x.ItemStates));

            // assert there are 2 items with number 1140213 in the warehouse
            Assert.Equal(2, items.Count(x => x.ItemNumber == "1140213"));
            // assert that one item is stored at location "F 7-1" and the other at "E 3-3"
            List<ItemState> matchingItems = items
                .Where(x => x.ItemNumber == "1140213")
                .SelectMany(x => x.ItemStates)
                .ToList();

            Assert.Contains(matchingItems, x => x.Location == "F 7-1" && x.Date == new DateTime(2022, 1, 5).ToUniversalTime());
            Assert.Contains(matchingItems, x => x.Location == "E 3-3" && x.Date == new DateTime(2022, 1, 6).ToUniversalTime());

            // assert there are 20 items with number 1300055 in the warehouse
            Assert.Equal(20, items.Count(x => x.ItemNumber == "1300055"));
            // assert that 9 are stored at TR 7-2, another 9 at TR 7-4, 1 at TR 5-1, and 1 at TML 1
            List<ItemState> matchingItems2 = items
                .Where(x => x.ItemNumber == "1300055")
                .SelectMany(x => x.ItemStates)
                .ToList();
            Assert.Equal(9, matchingItems2.Count(x => x.Location == "TR 7-2"));
            Assert.Equal(9, matchingItems2.Count(x => x.Location == "TR 7-4"));
            Assert.Equal(1, matchingItems2.Count(x => x.Location == "TR 5-1"));
            Assert.Equal(1, matchingItems2.Count(x => x.Location == "TML 1"));
            // assert that all arrived on 2022-01-27
            Assert.All(matchingItems2, x => Assert.Equal(new DateTime(2022, 1, 27).ToUniversalTime(), x.Date));
        }

        [Fact]
        public void TestShippedAndArrived()
        {
            CustomerManagementService customerService = new(dbFixture.CustomerDbContextFactory);

            ExcelImportingService service = new(null!, dbFixture.WarehouseDbContextFactory, customerService);

            ExcelImporterConfig config = service.LoadConfigFromPath("./Resources/TestShippedAndArrivedConfig.json");

            service.RunImportUsingConfig(config);

            using CustomerDbContext customerCtx = dbFixture.CustomerDbContextFactory.CreateDbContext();
            using WarehouseDbContext warehouseCtx = dbFixture.WarehouseDbContextFactory.CreateDbContext();

            // assert that customer was created with the correct name
            Assert.Equal("ExampleCustomer", customerCtx.Customers.First().Name);

            // assert warehouse was created with the correct name
            Assert.Equal("ExampleWarehouse", warehouseCtx.Warehouses.First().Name);

            List<Item> itemsInWarehouse = warehouseCtx.Items
                .Include(x => x.ItemStates)
                .AsEnumerable()
                .Where(x => x.InWarehouse)
                .ToList();

            // assert correct final count of items
            Assert.Equal(37, itemsInWarehouse.Count);
        }
    }

    public class DatabaseFixture : IDisposable, IAsyncLifetime
    {
        public IDbContextFactory<WarehouseDbContext> WarehouseDbContextFactory { get; private set; }
        public IDbContextFactory<CustomerDbContext> CustomerDbContextFactory { get; private set; }

        private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithUsername("root")
            .WithPassword("root")
            .Build();

        public void Dispose()
        {
        }

        public async Task InitializeAsync()
        {
            await postgres.StartAsync();


            WarehouseDbContextFactory = new TestContextFactory<WarehouseDbContext>(
                () => new WarehouseDbContext(new DbContextOptionsBuilder<WarehouseDbContext>()
                    .UseNpgsql(CreateConnection("logiedge")).Options));
            CustomerDbContextFactory = new TestContextFactory<CustomerDbContext>(
                () => new CustomerDbContext(new DbContextOptionsBuilder<CustomerDbContext>()
                    .UseNpgsql(CreateConnection("logiedge")).Options));

            await using CustomerDbContext customerDbContext = await CustomerDbContextFactory.CreateDbContextAsync();
            IRelationalDatabaseCreator customerDbCreator = customerDbContext.Database.GetService<IRelationalDatabaseCreator>();
            await customerDbCreator.CreateAsync();
            await customerDbCreator.CreateTablesAsync();

            await using WarehouseDbContext warehouseDbContext = await WarehouseDbContextFactory.CreateDbContextAsync();
            IRelationalDatabaseCreator warehouseDbCreator = warehouseDbContext.Database.GetService<IRelationalDatabaseCreator>();
            await warehouseDbCreator.CreateTablesAsync();

        }

        private NpgsqlConnection CreateConnection(string dbName)
        {
            return new NpgsqlConnection(
                new NpgsqlConnectionStringBuilder(postgres.GetConnectionString())
                {
                    Database = dbName
                }.ConnectionString
            );
        }

        public async Task DisposeAsync()
        {
            await postgres.DisposeAsync().AsTask();
        }
    }
}