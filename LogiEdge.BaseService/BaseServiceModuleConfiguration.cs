using System.Reflection;
using LogiEdge.BaseService.Persistence;
using LogiEdge.BaseService.Services;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LogiEdge.BaseService
{
    public class BaseServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
            string applicationConnectionString = builder.Configuration.GetConnectionString("DatabaseConnection")
                                                 ?? throw new InvalidOperationException("Connection string 'ApplicationConnection' not found.");
            builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseNpgsql(applicationConnectionString));
            builder.Services.AddSingleton<SettingsService>(provider =>
            {
                IDbContextFactory<ApplicationDbContext> ctxFactory = provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                return new SettingsService(ctxFactory);
            });
            builder.Services.AddSingleton<FileAttachmentService>(provider =>
            {
                IDbContextFactory<ApplicationDbContext> ctxFactory =
                    provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                return new FileAttachmentService(ctxFactory);
            });

            builder.Services.AddControllers().AddApplicationPart(Assembly);
        }

        public void OnAppBuilt(WebApplication app)
        {
            using ApplicationDbContext dbContext =
                app.Services.GetService<IDbContextFactory<ApplicationDbContext>>()!.CreateDbContext();
            dbContext.Database.Migrate();
        }

        public Assembly Assembly => typeof(BaseServiceModuleConfiguration).Assembly;
    }
}
