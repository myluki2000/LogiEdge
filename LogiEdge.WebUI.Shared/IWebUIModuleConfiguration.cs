using System.Reflection;

namespace LogiEdge.WebUI.Shared
{
    /// <summary>
    /// A Blazor UI module. Implementing classes are listed in the webUiModules array in
    /// LogiEdge/Program.cs; their assemblies feed the Router (route discovery) and their
    /// MenuItems feed the sidebar nav menu.
    /// </summary>
    public interface IWebUIModuleConfiguration
    {
        /// <summary>The module's assembly (Blazor route discovery).</summary>
        Assembly Assembly { get; }

        /// <summary>Root-level nav menu items this module exposes (empty = no nav presence).</summary>
        IReadOnlyList<NavMenuItem> MenuItems { get; }
    }
}
