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
using Gotenberg.Sharp.API.Client.Domain.Settings;
using Gotenberg.Sharp.API.Client.Extensions;
using Microsoft.Extensions.Configuration;

namespace LogiEdge.Service.DocumentGenerator
{
    public class DocumentGeneratorServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddDbContextFactory<DocumentGeneratorDbContext>(options =>
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
            });
            builder.Services.AddSingleton<DocumentGenerationService>();

            builder.Services.AddOptions<GotenbergSharpClientOptions>()
                .Bind(builder.Configuration.GetSection("GotenbergSharpClient"));
            builder.Services.AddGotenbergSharpClient();
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
