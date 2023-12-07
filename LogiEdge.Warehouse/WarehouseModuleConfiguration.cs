using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.Warehouse
{
    public class WarehouseModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
        }

        public void OnAppBuilt(WebApplication app)
        {
        }

        public Assembly Assembly => typeof(WarehouseModuleConfiguration).Assembly;
    }
}
