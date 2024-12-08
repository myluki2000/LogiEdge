using LogiEdge.CustomerService.Persistence;
using LogiEdge.CustomerService.Services;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace LogiEdge.CustomerService
{
    public class CustomerServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public Assembly Assembly => typeof(CustomerServiceModuleConfiguration).Assembly;

        public void OnAppBuilt(WebApplication app)
        {
            using CustomerDbContext dbContext =
                app.Services.GetService<IDbContextFactory<CustomerDbContext>>()!.CreateDbContext();
            dbContext.Database.Migrate();
        }

        public void RegisterServices(WebApplicationBuilder builder)
        {
            string connectionString = builder.Configuration.GetConnectionString("DatabaseConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
           
            builder.Services.AddDbContextFactory<CustomerDbContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddSingleton<CustomerManagementService>();
        }
    }
}