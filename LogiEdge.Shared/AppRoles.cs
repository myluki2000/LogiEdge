namespace LogiEdge.Shared
{
    /// <summary>
    /// Role names used by the app. A user can hold any number of roles (many-to-many via
    /// AspNetUserRoles); each role unlocks its own surfaces, e.g. "Admin" unlocks /Settings
    /// and /RunExcelImport.
    /// </summary>
    public static class AppRoles
    {
        public const string Admin = "Admin";
    }
}
