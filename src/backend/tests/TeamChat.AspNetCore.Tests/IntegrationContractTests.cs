namespace TeamChat.AspNetCore.Tests;

public sealed class IntegrationContractTests : IAsyncLifetime
{
    private TestHost _host = null!;

    public async Task InitializeAsync() => _host = await TestHost.StartAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task Endpoints_live_under_the_configured_route_prefix()
    {
        var client = _host.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync($"{TestHost.RoutePrefix}/health")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"{TeamChatOptions.DefaultRoutePrefix}/health")).StatusCode);
    }

    [Fact]
    public async Task Chat_tables_use_the_configured_table_prefix()
    {
        await using var scope = _host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        Assert.Equal(TestHost.TablePrefix, db.TablePrefix);
        Assert.Equal("tc_Invitations", db.Model.FindEntityType(typeof(TeamChat.Domain.Invitation))!.GetTableName());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Me_returns_the_user_given_by_the_host_adapter()
    {
        var response = await _host.CreateClient(TestIdentityAdapter.Cleo.Id).GetAsync($"{TestHost.RoutePrefix}/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            new ChatUserResponse("u-cleo", "Cleo Haddad", "cleo@example.com", "River Labs"),
            await response.Content.ReadFromJsonAsync<ChatUserResponse>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-id")]
    [InlineData("u-unknown")]
    public async Task Me_needs_a_user_known_by_the_host(string? userId)
    {
        var response = await _host.CreateClient(userId).GetAsync($"{TestHost.RoutePrefix}/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
