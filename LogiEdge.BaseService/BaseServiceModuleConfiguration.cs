using System.Reflection;
using LogiEdge.BaseService.MemoryCache;
using LogiEdge.BaseService.Persistence;
using LogiEdge.BaseService.Services;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace LogiEdge.BaseService
{
    public class BaseServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddDbContextFactory<ApplicationDbContext>();
            builder.Services.AddSingleton<MemoryCacheService>();
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

            app.MapGet("/tmp/{id:guid}", (Guid id, IMemoryCache cache) =>
            {
                if (!cache.TryGetValue<CachedFile>(id, out CachedFile? file) || file == null)
                    return Results.NotFound();

                return Results.File(file.Data, file.MimeType);
            });
        }

        public Assembly Assembly => typeof(BaseServiceModuleConfiguration).Assembly;
    }
}
