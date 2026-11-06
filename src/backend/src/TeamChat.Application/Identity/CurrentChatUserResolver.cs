namespace TeamChat.Application.Identity;

public sealed class CurrentChatUserResolver(IChatIdentityAdapter identity)
{
    /// <returns><c>null</c> when the principal has no user id or the host no longer knows the user.</returns>
    public async Task<ChatUser?> ResolveAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (identity.GetUserId(principal) is not { Length: > 0 } userId)
        {
            return null;
        }

        var users = await identity.FindUsersAsync([userId], cancellationToken);

        return users.SingleOrDefault(u => u.Id == userId);
    }
}
