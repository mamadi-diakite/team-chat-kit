namespace TeamChat.Application.Directory;

/// <summary>The members of my organization, whom I can talk to without an invitation.</summary>
public sealed class DirectoryService(IChatIdentityAdapter identity)
{
    public async Task<IReadOnlyList<ChatUserResponse>> GetDirectoryAsync(
        ChatUser me,
        string? search,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(me);

        var members = await identity.GetOrganizationMembersAsync(me.OrganizationId, cancellationToken);

        return [.. Filter(members, me, search).Select(ChatUserResponse.From)];
    }

    /// <summary>Other members of my organization whose name contains the search text, ordered by name.</summary>
    public static IEnumerable<ChatUser> Filter(IEnumerable<ChatUser> members, ChatUser me, string? search)
    {
        var text = search?.Trim() ?? string.Empty;

        return members
            .Where(m => m.Id != me.Id && m.OrganizationId == me.OrganizationId)
            .Where(m => m.DisplayName.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Id, StringComparer.Ordinal);
    }
}
