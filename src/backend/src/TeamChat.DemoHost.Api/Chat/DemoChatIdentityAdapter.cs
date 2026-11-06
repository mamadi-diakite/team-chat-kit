namespace TeamChat.DemoHost.Api.Chat;

/// <summary>Tells the chat who the demo host users are. A real host writes the same class over its own users.</summary>
public sealed class DemoChatIdentityAdapter(DemoHostDbContext db) : IChatIdentityAdapter
{
    public string? GetUserId(ClaimsPrincipal principal) => principal.GetUserId()?.ToString();

    public async Task<IReadOnlyList<ChatUser>> FindUsersAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        foreach (var userId in userIds)
        {
            if (Guid.TryParse(userId, out var id))
            {
                ids.Add(id);
            }
        }

        return await ToChatUsersAsync(db.Users.Where(u => ids.Contains(u.Id)), cancellationToken);
    }

    public async Task<ChatUser?> FindUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        (await ToChatUsersAsync(db.Users.Where(u => u.Email == email), cancellationToken)).SingleOrDefault();

    public async Task<IReadOnlyList<ChatUser>> GetOrganizationMembersAsync(string organizationId, CancellationToken cancellationToken) =>
        Guid.TryParse(organizationId, out var id)
            ? await ToChatUsersAsync(db.Users.Where(u => u.OrganizationId == id), cancellationToken)
            : [];

    // Ids are formatted in .NET: SQLite stores GUIDs in uppercase, the JWT carries them in lowercase.
    private static async Task<IReadOnlyList<ChatUser>> ToChatUsersAsync(IQueryable<UserAccount> users, CancellationToken cancellationToken) =>
        [.. (await users.Include(u => u.Organization).AsNoTracking().ToListAsync(cancellationToken))
            .Select(u => new ChatUser(
                u.Id.ToString(), u.DisplayName, u.Email, u.OrganizationId.ToString(), u.Organization!.Name))];
}
