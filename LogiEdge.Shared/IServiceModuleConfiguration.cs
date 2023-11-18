using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.Shared
{
    public interface IServiceModuleConfiguration
    {
        void RegisterServices(IServiceCollection services);

        Assembly Assembly { get; }
    }
}