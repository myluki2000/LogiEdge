namespace LogiEdge.Shared
{
    /// <summary>
    /// Authentication scheme names used by the app. Browsers authenticate with the Identity cookie
    /// (the default scheme); endpoints that should also accept programmatic bearer tokens opt in
    /// explicitly by declaring both schemes, e.g.
    /// <c>[Authorize(AuthenticationSchemes = [IdentityConstants.ApplicationSchemeName, AppAuthSchemes.Token])]</c>.
    /// See .notes/auth-token.md.
    /// </summary>
    public static class AppAuthSchemes
    {
        public const string Token = "ApiToken";

        /// <summary>
        /// The Identity cookie scheme and the API token scheme, comma-separated — for use in
        /// [Authorize(AuthenticationSchemes = ...)] attributes, which require constant expressions
        /// (IdentityConstants.ApplicationScheme is a static readonly field, not a const, in .NET 10).
        /// </summary>
        public const string CookieAndToken = "Identity.Application, ApiToken";
    }
}
