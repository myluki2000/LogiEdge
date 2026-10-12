using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace LogiEdge.Shared.Utility
{
    public static class AuthenticationStateProviderExtensions
    {
        /// <summary>
        /// Resolves the Identity user id (ClaimTypes.NameIdentifier claim) of the currently
        /// signed-in user. Throws when no user is authenticated.
        /// </summary>
        public static async Task<Guid> GetUserIdAsync(this AuthenticationStateProvider provider)
        {
            AuthenticationState state = await provider.GetAuthenticationStateAsync();
            string? rawId = state.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(rawId) || !Guid.TryParse(rawId, out Guid userId))
            {
                throw new InvalidOperationException("No authenticated user available.");
            }

            return userId;
        }
    }
}
