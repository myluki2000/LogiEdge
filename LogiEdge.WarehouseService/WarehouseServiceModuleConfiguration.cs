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
            builder.Services.AddDbContextFactory<WarehouseDbContext>(options =>
            {
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
