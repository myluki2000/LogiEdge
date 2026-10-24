using System.Reflection;
using LogiEdge.WebUI.Shared;

namespace LogiEdge.WebUI.Warehouse
{
    public class WarehouseModuleConfiguration : IWebUIModuleConfiguration
    {
        public Assembly Assembly => typeof(WarehouseModuleConfiguration).Assembly;

        public IReadOnlyList<NavMenuItem> MenuItems { get; } =
        [
            new NavMenuItem
            {
                Label = "Warehouse",
                Href = RouteTo<Pages.Warehouse.ItemsPage>(),
                Icon = "inventory_2",
                Order = 200,
                Children =
                [
                    new() { Label = "Item schemas", Href = RouteTo<Pages.Warehouse.SchemasPage>(), Icon = "table_view" },
                    new() { Label = "Transactions", Href = RouteTo<Pages.Warehouse.Transactions.Index>(), Icon = "swap_vert" },
                ],
            },
        ];
    }
}
