using Microsoft.AspNetCore.Components.Routing;

namespace LogiEdge.WebUI.Shared
{
    /// <summary>
    /// A single entry in the sidebar nav menu. Modules declare their entries via
    /// IWebUIModuleConfiguration.MenuItems; Href is always obtained via RouteTo&lt;TPage&gt;()
    /// (LogiEdge.Shared.Utility.LinkHelpers) so route renames are caught at compile time.
    /// </summary>
    public sealed record NavMenuItem
    {
        /// <summary>Display text in the menu.</summary>
        public required string Label { get; init; }

        /// <summary>Route path for this item, e.g. "/warehouse/schemas".</summary>
        public required string Href { get; init; }

        /// <summary>Material Symbols icon name (e.g. "table_view"); null = no icon.</summary>
        public string? Icon { get; init; }

        /// <summary>Sort key within its level; lower first, ties broken by Label. Default 100.</summary>
        public int Order { get; init; } = 100;

        /// <summary>NavLink match mode. Prefix (default) keeps a parent highlighted while on child pages.</summary>
        public NavLinkMatch Match { get; init; } = NavLinkMatch.Prefix;

        /// <summary>Optional ASP.NET Core policy name; item hidden unless the user passes it.</summary>
        public string? Policy { get; init; }

        /// <summary>Optional role names (e.g. [AppRoles.Admin]); item hidden unless the user holds one.</summary>
        public string[]? Roles { get; init; }

        /// <summary>Nested items; rendered collapsed by default. Recursion depth is unlimited.</summary>
        public IReadOnlyList<NavMenuItem> Children { get; init; } = [];
    }
}
