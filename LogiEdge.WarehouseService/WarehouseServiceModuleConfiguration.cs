using System.Reflection;
using LogiEdge.Shared;
using LogiEdge.WarehouseService.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.WarehouseService
{
    public class WarehouseServiceModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(IServiceCollection services)
        {
            services.AddSingleton<WarehouseItemService>();
        }

        public Assembly Assembly => typeof(WarehouseServiceModuleConfiguration).Assembly;
    }
}
