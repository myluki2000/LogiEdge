using System.Security.Claims;
using System.Text.Encodings.Web;
using LogiEdge.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LogiEdge.ApiAuth
{
    /// <summary>
    /// Bearer token ("lget_\u2026") authentication scheme for programmatic API access. Endpoints opt in
    /// explicitly via AuthenticationSchemes = [IdentityConstants.ApplicationSchemeName, AppAuthSchemes.Token];
    /// Blazor routes and the hub are never opted in, so a token can never open a circuit. All rejections
    /// (unknown/revoked/expired token, deleted or locked-out user) report NoResult, so callers uniformly
    /// get 401 without leaking which check failed. See .notes/auth-token.md \u00a74.2.
    /// </summary>
    public class ApiTokenAuthenticationHandler(
        IOptionsMonitor<ApiTokenSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IDbContextFactory<AppIdentityDbContext> dbContextFactory)
        : AuthenticationHandler<ApiTokenSchemeOptions>(options, loggerFactory, encoder)
    {
        /// <summary>LastUsedAtUtc is only refreshed when older than this, to avoid excessive writes.</summary>
        private static readonly TimeSpan LastUsedRefreshInterval = TimeSpan.FromMinutes(5);

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            string? tokenValue = ExtractBearerToken();
            if (tokenValue is null || !tokenValue.StartsWith(ApiTokenValue.Prefix, StringComparison.Ordinal))
                return AuthenticateResult.NoResult(); // not a bearer token / wrong prefix - let other schemes try

            await using AppIdentityDbContext ctx = await dbContextFactory.CreateDbContextAsync();

            ApiToken? token = await ctx.ApiTokens.FirstOrDefaultAsync(t => t.HashedToken == ApiTokenValue.Hash(tokenValue));
            if (token is null)
                return Reject("unknown token");
            if (token.RevokedAtUtc is not null)
                return Reject("revoked");
            if (token.ExpiresAtUtc is { } expiresAtUtc && expiresAtUtc <= DateTime.UtcNow)
                return Reject("expired");

            IdentityUser? user = await ctx.Users.FirstOrDefaultAsync(u => u.Id == token.UserId);
            if (user is null)
                return Reject("user no longer exists");
            if (IsInLockout(user))
                return Reject("user is locked out");

            DateTime nowUtc = DateTime.UtcNow;
            if (token.LastUsedAtUtc is null || token.LastUsedAtUtc < nowUtc - LastUsedRefreshInterval)
            {
                token.LastUsedAtUtc = nowUtc;
                await ctx.SaveChangesAsync();
            }

            List<Claim> claims =
            [
                new(ClaimTypes.NameIdentifier, user.Id),
            ];
            if (user.UserName is not null)
                claims.Add(new(ClaimTypes.Name, user.UserName));
            if (user.Email is not null)
                claims.Add(new(ClaimTypes.Email, user.Email));

            List<string> roleNames = await ctx.Roles
                .Where(role => role.Name != null && ctx.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == role.Id))
                .Select(role => role.Name!)
                .ToListAsync();
            claims.AddRange(roleNames.Select(roleName => new Claim(ClaimTypes.Role, roleName)));

            ClaimsPrincipal principal = new(new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.NameIdentifier, ClaimTypes.Role));
            return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
        }

        private string? ExtractBearerToken()
        {
            StringValues authorization = Request.Headers.Authorization;
            if (authorization.Count == 0)
                return null;

            const string bearerPrefix = "Bearer ";
            foreach (string value in authorization)
            {
                if (value.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
                    return value[bearerPrefix.Length..];
            }

            return null;
        }

        private static bool IsInLockout(IdentityUser user) =>
            user.LockoutEnabled && user.LockoutEnd is { } lockoutEnd && lockoutEnd.UtcDateTime > DateTime.UtcNow;

        private AuthenticateResult Reject(string reason)
        {
            Logger.LogInformation("API token rejected: {Reason}", reason);
            return AuthenticateResult.NoResult();
        }
    }
}
