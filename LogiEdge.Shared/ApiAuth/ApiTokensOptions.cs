namespace LogiEdge.Shared.ApiAuth
{
    /// <summary>Configuration for API tokens (appsettings.json section "ApiTokens").</summary>
    public class ApiTokensOptions
    {
        public const string SectionName = "ApiTokens";

        /// <summary>Maximum lifetime an ephemeral token may be created with. Defaults to 1 hour.</summary>
        public TimeSpan EphemeralMaxLifetime { get; set; } = TimeSpan.FromHours(1);
    }
}
