using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace LogiEdge.Shared
{
    public interface IServiceModuleConfiguration
    {
        void RegisterServices(WebApplicationBuilder builder);
        void OnAppBuilt(WebApplication app);

        Assembly Assembly { get; }
    }
}