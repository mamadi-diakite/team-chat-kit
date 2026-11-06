namespace TeamChat.Application.Tests;

public sealed class DirectoryServiceTests
{
    private readonly FakeIdentityAdapter _identity = new();
    private readonly DirectoryService _service;

    public DirectoryServiceTests() => _service = new DirectoryService(_identity);

    [Fact]
    public async Task Lists_the_other_members_of_my_organization_by_name()
    {
        _identity.Users.Add(new ChatUser("u-amy", "amy Stone", "amy@example.com", "org-1", "Maple Works"));

        var directory = await _service.GetDirectoryAsync(FakeIdentityAdapter.Ada, search: null);

        Assert.Equal(["amy Stone", "Ben Okafor"], directory.Select(u => u.DisplayName));
    }

    [Theory]
    [InlineData("  OKA ", new[] { "Ben Okafor" })]
    [InlineData("", new[] { "Ben Okafor" })]
    [InlineData("nobody", new string[0])]
    public async Task Searches_by_name_without_case(string search, string[] expected)
    {
        var directory = await _service.GetDirectoryAsync(FakeIdentityAdapter.Ada, search);

        Assert.Equal(expected, directory.Select(u => u.DisplayName));
    }

    [Fact]
    public void Filter_leaves_out_people_of_other_organizations_and_orders_homonyms_by_id()
    {
        ChatUser[] members =
        [
            FakeIdentityAdapter.Ada,
            FakeIdentityAdapter.Cleo,
            new("u-2", "Sam Lee", "sam2@example.com", "org-1", "Maple Works"),
            new("u-1", "Sam Lee", "sam1@example.com", "org-1", "Maple Works"),
        ];

        var filtered = DirectoryService.Filter(members, FakeIdentityAdapter.Ada, "sam");

        Assert.Equal(["u-1", "u-2"], filtered.Select(u => u.Id));
    }

    [Fact]
    public async Task Rejects_a_missing_user()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.GetDirectoryAsync(null!, null));
    }
}
