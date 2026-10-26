using LogiEdge.CustomerService.Persistence;
using LogiEdge.Test.Shared.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Testcontainers.PostgreSql;

namespace LogiEdge.Test.CustomerService
{
    /// <summary>
    /// Spins up a disposable Postgres container and exposes a CustomerDbContext factory pointing at it.
    /// Tables are created from the current EF model (no migrations in tests).
    /// </summary>
    public sealed class DatabaseFixture : IAsyncLifetime
    {
        public IDbContextFactory<CustomerDbContext> CustomerDbContextFactory { get; private set; } = null!;

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithUsername("root")
            .WithPassword("root")
            .Build();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            string connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
            {
                Database = "logiedge",
            }.ConnectionString;

            // LogiEdgeDbContext.OnConfiguring unconditionally applies UseNpgsql with the connection string from
            // IConfiguration, so the context must be constructed with a real configuration pointing at Testcontainers.
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DatabaseConnection"] = connectionString,
                })
                .Build();

            CustomerDbContextFactory = new TestContextFactory<CustomerDbContext>(
                () => new CustomerDbContext(new DbContextOptionsBuilder<CustomerDbContext>().Options, configuration));

            await using CustomerDbContext customerDbContext = await CustomerDbContextFactory.CreateDbContextAsync();
            IRelationalDatabaseCreator customerDbCreator = customerDbContext.Database.GetService<IRelationalDatabaseCreator>();
            await customerDbCreator.CreateAsync();
            await customerDbCreator.CreateTablesAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgres.DisposeAsync().AsTask();
        }
    }
}
