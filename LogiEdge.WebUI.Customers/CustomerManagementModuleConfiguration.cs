using LogiEdge.Shared;
using System.Reflection;

namespace LogiEdge.WebUI.Customers
{
    public class CustomerManagementModuleConfiguration : IServiceModuleConfiguration
    {
        public Assembly Assembly => typeof(CustomerManagementModuleConfiguration).Assembly;

        public void OnAppBuilt(WebApplication app)
        {
        }

        public void RegisterServices(WebApplicationBuilder builder)
        {
        }
    }
}
