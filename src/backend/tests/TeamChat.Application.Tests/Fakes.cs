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
