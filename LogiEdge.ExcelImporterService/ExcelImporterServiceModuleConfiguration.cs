using System.Diagnostics;
using System.Reflection;
using LogiEdge.ExcelImporterService.Services;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.ExcelImporterService
{
    public class ExcelImporterServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<ExcelImportingService>();
        }

        public void OnAppBuilt(WebApplication app)
        {
            app.MapGet("/RunExcelImport", async () => await app.Services.GetService<ExcelImportingService>()!.RunImportAsync());
        }

        public Assembly Assembly => typeof(ExcelImporterServiceModuleConfiguration).Assembly;
    }
}
