using System.Diagnostics;
using System.Reflection;
using LogiEdge.ExcelImporterService.Services;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.ExcelImporterService
{
    public class ExcelImporterServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
            // Scoped: resolves per-request dependencies (system user lookup for import attribution).
            builder.Services.AddScoped<ExcelImportingService>();
        }

        public void OnAppBuilt(WebApplication app)
        {
            // Admin-only (state-changing import trigger). ExcelImportingService is scoped, so resolve it from the
            // per-request service provider (each HTTP request already runs in its own DI scope).
            app.MapGet("/RunExcelImport", async (HttpContext context) =>
                await context.RequestServices.GetRequiredService<ExcelImportingService>().RunImportAsync())
                .RequireAuthorization(new AuthorizeAttribute { Roles = AppRoles.Admin, AuthenticationSchemes = AppAuthSchemes.CookieAndToken });
        }

        public Assembly Assembly => typeof(ExcelImporterServiceModuleConfiguration).Assembly;
    }
}
