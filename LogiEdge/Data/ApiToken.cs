using System.ComponentModel.DataAnnotations;

namespace LogiEdge.Data
{
    /// <summary>
    /// An opaque bearer token ("lget_\u2026") that authenticates programmatic HTTP calls as its owning
    /// Identity user. Only the SHA-256 hash of the token value is stored, so a database leak does
    /// not yield usable tokens. Permanent vs. time-limited is derived from <see cref="ExpiresAtUtc"/>
    /// (null = permanent). See .notes/auth-token.md.
    /// </summary>
    public class ApiToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Human label ("ERP integration", "CI job").</summary>
        public required string Name { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// Owning IdentityUser id (Guid-formatted string, matching AspNetUsers.Id). No FK -
        /// consistent with the cross-reference convention; user deletion is handled by the UI/service.
        /// </summary>
        public required string UserId { get; set; }

        /// <summary>SHA-256 hex digest (64 chars) of the token value - the unique lookup key.</summary>
        public required string HashedToken { get; set; }

        /// <summary>Id of the user who created the token.</summary>
        public required string CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        /// <summary>null = permanent; set for time-limited/ephemeral tokens.</summary>
        public DateTime? ExpiresAtUtc { get; set; }

        /// <summary>null = active.</summary>
        public DateTime? RevokedAtUtc { get; set; }

        /// <summary>Updated on successful auth (guarded against write amplification).</summary>
        public DateTime? LastUsedAtUtc { get; set; }
    }
}
