using LogiEdge.Shared;
using Microsoft.AspNetCore.Components.Routing;

namespace LogiEdge.WebUI.Shared
{
    /// <summary>
    /// Nav menu items for the host app itself. These use string literals deliberately: they point
    /// at pages in the host project (LogiEdge/Pages) which WebUI.Shared cannot reference — the
    /// dependency direction is host → WebUI.Shared. Documented exception to RouteTo&lt;TPage&gt;()
    /// (see .notes/navmenu.md §4).
    /// </summary>
    public static class BaseNavMenuItems
    {
        public static IReadOnlyList<NavMenuItem> All { get; } =
        [
            new NavMenuItem
            {
                Label = "Home",
                Href = "/",
                Icon = "home",
                Order = 0,
                Match = NavLinkMatch.All,
            },
            new NavMenuItem
            {
                Label = "Settings",
                Href = "/Settings",
                Icon = "settings",
                Order = 900,
                Roles = [AppRoles.Admin],
                Children =
                [
                    new() { Label = "API Tokens", Href = "/Settings/Tokens", Icon = "vpn_key", Order = 100, Roles = [AppRoles.Admin] },
                ],
            },
        ];
    }
}
