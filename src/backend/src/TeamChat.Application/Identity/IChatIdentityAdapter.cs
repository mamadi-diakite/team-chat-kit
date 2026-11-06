namespace TeamChat.Application.Identity;

/// <summary>
/// The one interface a host implements: the chat never stores users, it asks the host about them.
/// </summary>
/// <remarks>Registered as a scoped service, so an implementation can use the host's DbContext.</remarks>
public interface IChatIdentityAdapter
{
    /// <summary>Reads the host's user id from the principal authenticated by the host.</summary>
    /// <returns><c>null</c> when the principal does not carry a user id.</returns>
    string? GetUserId(ClaimsPrincipal principal);

    /// <returns>The users the host still knows, in any order. Unknown ids are left out.</returns>
    Task<IReadOnlyList<ChatUser>> FindUsersAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken);

    /// <param name="email">Normalized email: trimmed and lowercase.</param>
    Task<ChatUser?> FindUserByEmailAsync(string email, CancellationToken cancellationToken);

    /// <returns>Every member of the organization, the current user included.</returns>
    Task<IReadOnlyList<ChatUser>> GetOrganizationMembersAsync(string organizationId, CancellationToken cancellationToken);
}
