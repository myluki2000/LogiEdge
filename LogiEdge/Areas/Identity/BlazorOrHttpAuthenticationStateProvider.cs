using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace LogiEdge.Areas.Identity
{
    /// <summary>
    /// Scoped AuthenticationStateProvider bridging plain HTTP requests and Blazor circuits: inside an
    /// HTTP request it reports HttpContext.User (cookie or API token), otherwise it delegates to the
    /// revalidating Identity provider (Blazor circuit state, unchanged). This keeps the Phase 3 pattern -
    /// services resolve the acting user themselves via AuthenticationStateProvider - working for
    /// token-authenticated API calls. See .notes/auth-token.md \u00a74.3.
    /// </summary>
    public class BlazorOrHttpAuthenticationStateProvider(
        IHttpContextAccessor httpContextAccessor,
        RevalidatingIdentityAuthenticationStateProvider<IdentityUser> revalidating)
        : AuthenticationStateProvider
    {
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
                return new AuthenticationState(user); // HTTP request: cookie or token

            return await revalidating.GetAuthenticationStateAsync(); // Blazor circuit: unchanged
        }
    }
}
