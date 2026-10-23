namespace LogiEdge.Shared.ApiAuth
{
    /// <summary>
    /// Creates and manages API bearer tokens (see .notes/auth-token.md). A token acts as its owning
    /// Identity user, so all existing authorization (roles, fallback policy) applies unchanged.
    /// Callers must run in an authenticated request/circuit scope: creation attributes to the
    /// currently signed-in user, and only that user or an admin may create tokens for a given user
    /// or revoke a token.
    /// </summary>
    public interface IAuthTokenService
    {
        /// <summary>Creates a permanent (non-expiring) token for the given user. The value is returned exactly once.</summary>
        Task<CreatedToken> CreatePermanentTokenAsync(Guid userId, string name, string? description = null);

        /// <summary>Creates an ephemeral (short-lived) token; lifetime must be positive and ≤ ApiTokens:EphemeralMaxLifetime.</summary>
        Task<CreatedToken> CreateEphemeralTokenAsync(Guid userId, TimeSpan lifetime, string name = "ephemeral");

        /// <summary>
        /// Revokes a token immediately when the acting user is its owner or an admin. Returns false (no-op)
        /// when the token does not exist, is already revoked, or the acting user has no right to revoke it.
        /// </summary>
        Task<bool> RevokeTokenAsync(Guid tokenId);

        /// <summary>Lists all tokens (admin view; never includes hashes or values), newest first.</summary>
        Task<IReadOnlyList<ApiTokenInfo>> GetTokensAsync();

        /// <summary>Lists the tokens owned by the given user (never includes hashes or values), newest first.</summary>
        Task<IReadOnlyList<ApiTokenInfo>> GetTokensForUserAsync(Guid userId);
    }
}