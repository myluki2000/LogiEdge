using System.Reflection;
using LogiEdge.WebUI.Shared;

namespace LogiEdge.WebUI.DocumentGenerator
{
    public class DocumentGeneratorWebUiModuleConfiguration : IWebUIModuleConfiguration
    {
        public Assembly Assembly => typeof(DocumentGeneratorWebUiModuleConfiguration).Assembly;

        public IReadOnlyList<NavMenuItem> MenuItems { get; } =
        [
            new NavMenuItem
            {
                Label = "Document templates",
                Href = RouteTo<Pages.Templates.IndexPage>(),
                Icon = "description",
                Order = 400,
            },
        ];
    }
}
