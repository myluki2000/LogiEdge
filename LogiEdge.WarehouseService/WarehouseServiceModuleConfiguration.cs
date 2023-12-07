using System.Reflection;
using LogiEdge.Shared;
using LogiEdge.WarehouseService.Persistence;
using LogiEdge.WarehouseService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.WarehouseService
{
    public class WarehouseServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
            string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContextFactory<WarehouseDbContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddSingleton<WarehouseItemService>();
        }

        public void OnAppBuilt(WebApplication app)
        {
            app.Services.GetService<IDbContextFactory<WarehouseDbContext>>()!.CreateDbContext().Database.EnsureCreated();
        }

        public Assembly Assembly => typeof(WarehouseServiceModuleConfiguration).Assembly;
    }
}
