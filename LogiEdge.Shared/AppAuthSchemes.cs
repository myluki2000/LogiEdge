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
    }
}
