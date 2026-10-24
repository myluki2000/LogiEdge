namespace LogiEdge.Shared.ApiAuth
{
    /// <summary>Display-safe view of a stored token (no hash/value). User names are resolved by the caller.</summary>
    public sealed record ApiTokenInfo(
        Guid Id,
        string Name,
        string? Description,
        Guid UserId,
        Guid CreatedByUserId,
        DateTime CreatedAtUtc,
        DateTime? ExpiresAtUtc,
        DateTime? RevokedAtUtc,
        DateTime? LastUsedAtUtc);
}
