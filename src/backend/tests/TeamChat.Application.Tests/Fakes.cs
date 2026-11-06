namespace TeamChat.Application.Tests;

/// <summary>Host users kept in a list; the principal carries the user id in a "uid" claim.</summary>
internal sealed class FakeIdentityAdapter : IChatIdentityAdapter
{
    public static readonly ChatUser Ada = new("u-ada", "Ada North", "ada@example.com", "org-1", "Maple Works");
    public static readonly ChatUser Ben = new("u-ben", "Ben Okafor", "ben@example.com", "org-1", "Maple Works");
    public static readonly ChatUser Cleo = new("u-cleo", "Cleo Haddad", "cleo@example.com", "org-2", "River Labs");
    public static readonly ChatUser Dan = new("u-dan", "Dan Ruiz", "dan@example.com", "org-3", "Summit Foods");

    public List<ChatUser> Users { get; } = [Ada, Ben, Cleo, Dan];

    public static ClaimsPrincipal PrincipalOf(string? userId) =>
        new(new ClaimsIdentity(userId is null ? [] : new[] { new Claim("uid", userId) }, "test"));

    public string? GetUserId(ClaimsPrincipal principal) => principal.FindFirst("uid")?.Value;

    public Task<IReadOnlyList<ChatUser>> FindUsersAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ChatUser>>([.. Users.Where(u => userIds.Contains(u.Id))]);

    public Task<ChatUser?> FindUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Email == email));

    public Task<IReadOnlyList<ChatUser>> GetOrganizationMembersAsync(string organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ChatUser>>([.. Users.Where(u => u.OrganizationId == organizationId)]);
}

internal sealed class InMemoryContactRepository : IContactRepository
{
    public List<Invitation> Invitations { get; } = [];

    public List<Contact> Contacts { get; } = [];

    public int SaveCount { get; private set; }

    public Task<Invitation?> FindInvitationAsync(Guid invitationId, CancellationToken cancellationToken) =>
        Task.FromResult(Invitations.SingleOrDefault(i => i.Id == invitationId));

    public Task<Invitation?> FindPendingInvitationAsync(string userId, string otherUserId, CancellationToken cancellationToken) =>
        Task.FromResult(Invitations.FirstOrDefault(i => i.Status == InvitationStatus.Pending
            && ((i.SenderId == userId && i.RecipientId == otherUserId) || (i.SenderId == otherUserId && i.RecipientId == userId))));

    public Task<IReadOnlyList<Invitation>> ListPendingInvitationsAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Invitation>>([.. Invitations
            .Where(i => i.Status == InvitationStatus.Pending && (i.SenderId == userId || i.RecipientId == userId))
            .OrderByDescending(i => i.CreatedAtUtc)]);

    public void AddInvitation(Invitation invitation) => Invitations.Add(invitation);

    public Task<IReadOnlyList<Contact>> ListContactsAsync(string userId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Contact>>([.. Contacts.Where(c => c.FirstUserId == userId || c.SecondUserId == userId)]);

    public Task<Contact?> FindContactAsync(string userId, string otherUserId, CancellationToken cancellationToken)
    {
        var (first, second) = Contact.Pair(userId, otherUserId);

        return Task.FromResult(Contacts.SingleOrDefault(c => c.FirstUserId == first && c.SecondUserId == second));
    }

    public void AddContact(Contact contact) => Contacts.Add(contact);

    public void RemoveContact(Contact contact) => Contacts.Remove(contact);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
