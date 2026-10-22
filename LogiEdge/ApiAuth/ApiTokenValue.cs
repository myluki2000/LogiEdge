using System.Security.Cryptography;
using System.Text;

namespace LogiEdge.ApiAuth
{
    /// <summary>
    /// Token value format: "lget_" + 256 random bits as base64url. The prefix allows fast-fail in the
    /// authentication handler and makes tokens recognizable in logs/configs. Only the SHA-256 hex digest
    /// is ever stored, so a database leak does not yield usable tokens. See .notes/auth-token.md §3.
    /// </summary>
    internal static class ApiTokenValue
    {
        public const string Prefix = "lget_";

        public static string Generate()
        {
            byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
            return Prefix + Convert.ToBase64String(randomBytes)
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        /// <summary>SHA-256 of the token value as lowercase hex (64 chars) - the stored lookup key.</summary>
        public static string Hash(string tokenValue) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(tokenValue))).ToLowerInvariant();
    }
}
