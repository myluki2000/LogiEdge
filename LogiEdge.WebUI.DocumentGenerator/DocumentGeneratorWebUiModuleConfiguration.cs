using System.Reflection;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Builder;

namespace LogiEdge.WebUI.DocumentGenerator
{
    public class DocumentGeneratorWebUiModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
        }

        public void OnAppBuilt(WebApplication app)
        {
        }

        public Assembly Assembly => typeof(DocumentGeneratorWebUiModuleConfiguration).Assembly;
    }
}
