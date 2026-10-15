namespace LogiEdge.Shared
{
    /// <summary>
    /// Resolves the id of the well-known system user (see <see cref="AppUsers.System"/>).
    /// Implemented in the host, where the Identity database lives.
    /// </summary>
    public interface ISystemUserProvider
    {
        Task<Guid> GetSystemUserIdAsync();
    }
}
