using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LogiEdge.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.Warehouse
{
    public class WarehouseModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(IServiceCollection services)
        {
        }

        public Assembly Assembly => typeof(WarehouseModuleConfiguration).Assembly;
    }
}
