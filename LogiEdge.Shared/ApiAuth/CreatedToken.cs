namespace LogiEdge.Shared.ApiAuth
{
    /// <summary>A freshly created token. <see cref="Value"/> is the plaintext bearer token — shown exactly once.</summary>
    public sealed record CreatedToken(Guid Id, string Value, DateTime? ExpiresAtUtc);
}
