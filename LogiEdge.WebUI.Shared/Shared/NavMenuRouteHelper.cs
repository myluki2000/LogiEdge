namespace LogiEdge.WebUI.Shared.Shared
{
    /// <summary>
    /// Route matching for the nav menu: decides which menu item is active for a given location
    /// and which branches must be expanded to make it visible. Hrefs are exact, parameter-less
    /// route templates (obtained via RouteTo<>()), so matching runs on stable literal paths.
    /// </summary>
    public static class NavMenuRouteHelper
    {
        /// <summary>
        /// Strips the query string and fragment from a base-relative location (callers pass
        /// NavigationManager.ToBaseRelativePath output, which has no leading slash) and normalizes
        /// the result to an absolute-style path starting with "/".
        /// </summary>
        public static string GetPath(string location)
        {
            var path = location.Split('?', 2)[0];
            path = path.Split('#', 2)[0];
            if (path.Length == 0)
                return "/";

            return path.StartsWith('/') ? path : $"/{path}";
        }

        /// <summary>
        /// Boundary-aware case-insensitive prefix match mirroring NavLink's Prefix semantics:
        /// the href must equal the path or be followed by a '/' (so "/customers" does not match
        /// "/customer-x").
        /// </summary>
        public static bool Matches(string href, string path)
        {
            var target = GetPath(href);
            if (string.Equals(target, path, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!path.StartsWith(target, StringComparison.OrdinalIgnoreCase))
                return false;

            return target.EndsWith('/') || path[target.Length] == '/';
        }

        /// <summary>
        /// Finds the deepest menu item whose Href matches the location (longest match wins, ties
        /// broken by document order) and returns the hrefs of that item and all its ancestors -
        /// i.e. exactly the branches that must be expanded for the active page to be visible.
        /// </summary>
        public static HashSet<string> GetActiveBranches(IEnumerable<NavMenuItem> roots, string location)
        {
            var path = GetPath(location);
            string? activeHref = null;
            List<string>? activeBranch = null;

            Visit(roots, []);

            return activeBranch is null ? [] : [.. activeBranch];

            void Visit(IEnumerable<NavMenuItem> items, List<string> branch)
            {
                foreach (var item in items)
                {
                    if (Matches(item.Href, path) && (activeHref is null || GetPath(item.Href).Length > activeHref.Length))
                    {
                        activeHref = item.Href;
                        activeBranch = [.. branch, item.Href];
                    }

                    Visit(item.Children, [.. branch, item.Href]);
                }
            }
        }
    }
}
