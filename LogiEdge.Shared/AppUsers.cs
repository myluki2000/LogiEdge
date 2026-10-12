namespace LogiEdge.Shared
{
    /// <summary>
    /// Well-known user names. The "system" user is a locked-out account (no password) that
    /// background actions outside of a UI circuit (e.g. the Excel import) are attributed to,
    /// so CreatedByUserId/AuthorId columns always hold real ids (see .notes/auth-impl.md, D4).
    /// </summary>
    public static class AppUsers
    {
        public const string System = "system";
    }
}
