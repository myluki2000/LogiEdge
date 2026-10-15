namespace LogiEdge.Shared
{
    /// <summary>
    /// Resolves Identity user names for user ids (cross-database lookup: business data holds
    /// plain Guids, users live in the Identity database). Implemented in the host.
    /// </summary>
    public interface IUserDirectory
    {
        /// <summary>
        /// Gets the display name of a single user, or null if no such user exists.
        /// </summary>
        Task<string?> GetUserNameAsync(Guid userId);

        /// <summary>
        /// Gets the display names of all given users in one lookup; ids without a matching
        /// user are simply absent from the result.
        /// </summary>
        Task<IReadOnlyDictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds);
    }
}
