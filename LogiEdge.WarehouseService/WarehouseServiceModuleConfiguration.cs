using System.Reflection;
using LogiEdge.CustomerService.Persistence;
using LogiEdge.Shared;
using LogiEdge.WarehouseService.Persistence;
using LogiEdge.WarehouseService.Services.WarehouseManagement;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LogiEdge.WarehouseService
{
    public class WarehouseServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
            string connectionString = builder.Configuration.GetConnectionString("DatabaseConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContextFactory<WarehouseDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            });
            builder.Services.AddSingleton<WarehouseManagementService>();
        }

        public void OnAppBuilt(WebApplication app)
        {
            using WarehouseDbContext dbContext =
                app.Services.GetService<IDbContextFactory<WarehouseDbContext>>()!.CreateDbContext();
            dbContext.Database.Migrate();
        }

        public Assembly Assembly => typeof(WarehouseServiceModuleConfiguration).Assembly;
    }
}
