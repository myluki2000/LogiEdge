using LogiEdge.Data;
using LogiEdge.Shared.ApiAuth;
using LogiEdge.Shared.Utility;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogiEdge.ApiAuth
{
    /// <inheritdoc cref="IAuthTokenService"/>
    public class AuthTokenService(
        IDbContextFactory<AppIdentityDbContext> dbContextFactory,
        AuthenticationStateProvider authenticationStateProvider,
        IOptions<ApiTokensOptions> options) : IAuthTokenService
    {
        public async Task<CreatedToken> CreatePermanentTokenAsync(Guid userId, string name, string? description = null)
        {
            ValidateName(name);
            Guid createdByUserId = await authenticationStateProvider.GetUserIdAsync();

            await using AppIdentityDbContext ctx = await dbContextFactory.CreateDbContextAsync();
            await EnsureUserExistsAsync(ctx, userId);

            string tokenValue = ApiTokenValue.Generate();
            ApiToken token = new()
            {
                Name = name,
                Description = description,
                UserId = userId.ToString(),
                HashedToken = ApiTokenValue.Hash(tokenValue),
                CreatedByUserId = createdByUserId.ToString(),
                CreatedAtUtc = DateTime.UtcNow,
            };

            ctx.ApiTokens.Add(token);
            await ctx.SaveChangesAsync();

            return new CreatedToken(token.Id, tokenValue, token.ExpiresAtUtc);
        }

        public async Task<CreatedToken> CreateEphemeralTokenAsync(Guid userId, TimeSpan lifetime, string name = "ephemeral")
        {
            if (lifetime <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "Lifetime must be positive.");
            if (lifetime > options.Value.EphemeralMaxLifetime)
                throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime,
                    $"Lifetime exceeds the configured maximum of {options.Value.EphemeralMaxLifetime}.");

            ValidateName(name);
            Guid createdByUserId = await authenticationStateProvider.GetUserIdAsync();

            await using AppIdentityDbContext ctx = await dbContextFactory.CreateDbContextAsync();
            await EnsureUserExistsAsync(ctx, userId);

            string tokenValue = ApiTokenValue.Generate();
            DateTime expiresAtUtc = DateTime.UtcNow + lifetime;
            ApiToken token = new()
            {
                Name = name,
                UserId = userId.ToString(),
                HashedToken = ApiTokenValue.Hash(tokenValue),
                CreatedByUserId = createdByUserId.ToString(),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = expiresAtUtc,
            };

            ctx.ApiTokens.Add(token);
            await ctx.SaveChangesAsync();

            return new CreatedToken(token.Id, tokenValue, token.ExpiresAtUtc);
        }

        public async Task RevokeTokenAsync(Guid tokenId)
        {
            await using AppIdentityDbContext ctx = await dbContextFactory.CreateDbContextAsync();

            ApiToken? token = await ctx.ApiTokens.FirstOrDefaultAsync(t => t.Id == tokenId);
            if (token is null || token.RevokedAtUtc is not null)
                return; // no-op: unknown or already revoked

            token.RevokedAtUtc = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<ApiTokenInfo>> GetTokensAsync()
        {
            await using AppIdentityDbContext ctx = await dbContextFactory.CreateDbContextAsync();

            List<ApiToken> tokens = await ctx.ApiTokens
                .AsNoTracking()
                .OrderByDescending(token => token.CreatedAtUtc)
                .ToListAsync();

            return tokens.Select(token => new ApiTokenInfo(
                token.Id,
                token.Name,
                token.Description,
                Guid.Parse(token.UserId),
                Guid.Parse(token.CreatedByUserId),
                token.CreatedAtUtc,
                token.ExpiresAtUtc,
                token.RevokedAtUtc,
                token.LastUsedAtUtc))
                .ToList();
        }

        private static void ValidateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Token name must not be empty.", nameof(name));
        }

        private static async Task EnsureUserExistsAsync(AppIdentityDbContext ctx, Guid userId)
        {
            bool exists = await ctx.Users.AnyAsync(user => user.Id == userId.ToString());
            if (!exists)
                throw new InvalidOperationException($"No Identity user with id '{userId}'.");
        }
    }
}
