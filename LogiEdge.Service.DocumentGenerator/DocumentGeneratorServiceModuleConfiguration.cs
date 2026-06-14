using LogiEdge.BaseService.Services;
using LogiEdge.Service.DocumentGenerator.Persistence;
using LogiEdge.Service.DocumentGenerator.Services;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace LogiEdge.Service.DocumentGenerator
{
    public class DocumentGeneratorServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
            string connectionString = builder.Configuration.GetConnectionString("DatabaseConnection")
                                      ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContextFactory<DocumentGeneratorDbContext>(options =>
            {
                options.UseNpgsql(connectionString);
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            });
            builder.Services.AddSingleton<DocumentGenerationService>();
        }

        public void OnAppBuilt(WebApplication app)
        {
            using DocumentGeneratorDbContext dbContext =
                app.Services.GetService<IDbContextFactory<DocumentGeneratorDbContext>>()!.CreateDbContext();
            dbContext.Database.Migrate();
        }

        public Assembly Assembly => typeof(DocumentGeneratorServiceModuleConfiguration).Assembly;
    }
}
