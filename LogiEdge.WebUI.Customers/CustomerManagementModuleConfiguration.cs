using System.Reflection;
using LogiEdge.WebUI.Shared;

namespace LogiEdge.WebUI.Customers
{
    public class CustomerManagementModuleConfiguration : IWebUIModuleConfiguration
    {
        public Assembly Assembly => typeof(CustomerManagementModuleConfiguration).Assembly;

        public IReadOnlyList<NavMenuItem> MenuItems { get; } =
        [
            new NavMenuItem
            {
                Label = "Customers",
                Href = RouteTo<Pages.CustomersPage>(),
                Icon = "group",
                Order = 100,
            },
        ];
    }
}
