namespace TeamChat.Application.Tests;

public sealed class CurrentChatUserResolverTests
{
    private readonly FakeIdentityAdapter _identity = new();
    private readonly CurrentChatUserResolver _resolver;

    public CurrentChatUserResolverTests() => _resolver = new CurrentChatUserResolver(_identity);

    [Fact]
    public async Task Resolves_the_user_from_the_host_principal()
    {
        var user = await _resolver.ResolveAsync(FakeIdentityAdapter.PrincipalOf("u-cleo"));

        Assert.Equal(FakeIdentityAdapter.Cleo, user);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("u-unknown")]
    public async Task Is_null_without_a_known_user(string? userId)
    {
        Assert.Null(await _resolver.ResolveAsync(FakeIdentityAdapter.PrincipalOf(userId)));
    }

    [Fact]
    public async Task Rejects_a_missing_principal()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _resolver.ResolveAsync(null!));
    }

    [Fact]
    public void Response_leaves_out_the_organization_id()
    {
        Assert.Equal(
            new ChatUserResponse("u-ada", "Ada North", "ada@example.com", "Maple Works"),
            ChatUserResponse.From(FakeIdentityAdapter.Ada));
    }

    [Fact]
    public void Registers_the_application_services()
    {
        Assert.Throws<ArgumentNullException>(() => DependencyInjection.AddTeamChatApplication(null!));

        using var provider = new ServiceCollection()
            .AddTeamChatApplication()
            .AddScoped<IChatIdentityAdapter, FakeIdentityAdapter>()
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CurrentChatUserResolver>());
    }
}
