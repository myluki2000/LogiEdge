using System.Reflection;
using LogiEdge.WebUI.Shared;

namespace LogiEdge.WebUI.PlanningBoard
{
    public class PlanningBoardWebUiModuleConfiguration : IWebUIModuleConfiguration
    {
        public Assembly Assembly => typeof(PlanningBoardWebUiModuleConfiguration).Assembly;

        public IReadOnlyList<NavMenuItem> MenuItems { get; } =
        [
            new NavMenuItem
            {
                Label = "Planning board",
                Href = RouteTo<Pages.PlanningBoardPage>(),
                Icon = "task_alt",
                Order = 300,
            },
        ];
    }
}
