using System.Reflection;
using LogiEdge.Shared;
using Microsoft.AspNetCore.Builder;

namespace LogiEdge.WebUI.PlanningBoard
{
    public class PlanningBoardWebUiModuleConfiguration : IServiceModuleConfiguration
    {
        public void RegisterServices(WebApplicationBuilder builder)
        {
        }

        public void OnAppBuilt(WebApplication app)
        {
        }

        public Assembly Assembly => typeof(PlanningBoardWebUiModuleConfiguration).Assembly;
    }
}
