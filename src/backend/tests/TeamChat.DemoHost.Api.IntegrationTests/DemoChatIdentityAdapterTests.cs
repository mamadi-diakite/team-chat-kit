namespace TeamChat.DemoHost.Api.IntegrationTests;

public sealed class DemoChatIdentityAdapterTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>, IAsyncLifetime
{
    private AsyncServiceScope _scope;
    private DemoHostDbContext _db = null!;
    private DemoChatIdentityAdapter _adapter = null!;

    public Task InitializeAsync()
    {
        _ = factory.CreateClient();
        _scope = factory.Services.CreateAsyncScope();
        _db = _scope.ServiceProvider.GetRequiredService<DemoHostDbContext>();
        _adapter = Assert.IsType<DemoChatIdentityAdapter>(_scope.ServiceProvider.GetRequiredService<IChatIdentityAdapter>());

        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _scope.DisposeAsync();

    [Fact]
    public async Task Reads_the_user_id_from_the_token_subject()
    {
        var id = Guid.NewGuid();

        Assert.Equal(id.ToString(), _adapter.GetUserId(new ClaimsPrincipal(new ClaimsIdentity([new Claim(JwtClaims.Subject, id.ToString())]))));
        Assert.Null(_adapter.GetUserId(new ClaimsPrincipal(new ClaimsIdentity())));
        Assert.Empty(await _adapter.FindUsersAsync([], default));
    }

    [Fact]
    public async Task Finds_users_by_id_and_skips_unknown_or_invalid_ids()
    {
        var emma = await _db.Users.SingleAsync(u => u.Email == "emma.laurent@example.com");

        var users = await _adapter.FindUsersAsync([emma.Id.ToString(), Guid.NewGuid().ToString(), "not-a-guid"], default);

        var user = Assert.Single(users);
        Assert.Equal(new ChatUser(emma.Id.ToString(), "Emma Laurent", "emma.laurent@example.com",
            emma.OrganizationId.ToString(), "Bluebird Design"), user);
    }

    [Fact]
    public async Task Finds_a_user_by_email()
    {
        Assert.Equal("Noah Girard", (await _adapter.FindUserByEmailAsync("noah.girard@example.com", default))?.DisplayName);
        Assert.Null(await _adapter.FindUserByEmailAsync("nobody@example.com", default));
    }

    [Fact]
    public async Task Lists_the_members_of_an_organization()
    {
        var bluebird = await _db.Organizations.SingleAsync(o => o.Name == "Bluebird Design");

        var members = await _adapter.GetOrganizationMembersAsync(bluebird.Id.ToString(), default);

        Assert.Equal(["Chloe Petit", "Emma Laurent", "Hugo Bernard"], members.Select(m => m.DisplayName).Order());
        Assert.All(members, m => Assert.Equal(bluebird.Id.ToString(), m.OrganizationId));
        Assert.Empty(await _adapter.GetOrganizationMembersAsync("not-a-guid", default));
    }
}
