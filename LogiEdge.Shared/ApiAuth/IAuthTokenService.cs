namespace LogiEdge.Shared.ApiAuth
{
    /// <summary>
    /// Creates and manages API bearer tokens (see .notes/auth-token.md). A token acts as its owning
    /// Identity user, so all existing authorization (roles, fallback policy) applies unchanged.
    /// Token creation attributes to the currently signed-in user, so callers must run in an
    /// authenticated request/circuit scope.
    /// </summary>
    public interface IAuthTokenService
    {
        /// <summary>Creates a permanent (non-expiring) token for the given user. The value is returned exactly once.</summary>
        Task<CreatedToken> CreatePermanentTokenAsync(Guid userId, string name, string? description = null);

        /// <summary>Creates an ephemeral (short-lived) token; lifetime must be positive and ≤ ApiTokens:EphemeralMaxLifetime.</summary>
        Task<CreatedToken> CreateEphemeralTokenAsync(Guid userId, TimeSpan lifetime, string name = "ephemeral");

        /// <summary>Revokes a token immediately. No-op when the token does not exist or is already revoked.</summary>
        Task RevokeTokenAsync(Guid tokenId);

        /// <summary>Lists all tokens (admin view; never includes hashes or values), newest first.</summary>
        Task<IReadOnlyList<ApiTokenInfo>> GetTokensAsync();
    }
}
