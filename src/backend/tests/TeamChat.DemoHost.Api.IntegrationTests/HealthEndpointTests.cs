namespace TeamChat.DemoHost.Api.IntegrationTests;

public sealed class HealthEndpointTests(DemoHostApiFactory factory) : IClassFixture<DemoHostApiFactory>
{
    [Theory]
    [InlineData("/api/health")]
    [InlineData("/api/chat/health")]
    public async Task Get_health_returns_ok_status(string url)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.Equal("ok", body?.Status);
    }

    [Fact]
    public void AddTeamChat_rejects_missing_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => TeamChatServiceCollectionExtensions.AddTeamChat(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTeamChat(null!));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTeamChat(_ => { }));
    }

    [Fact]
    public void MapTeamChat_rejects_null_endpoints()
    {
        Assert.Throws<ArgumentNullException>(() => TeamChatEndpointRouteBuilderExtensions.MapTeamChat(null!));
    }

    [Fact]
    public void AddTeamChat_registers_the_storage_options()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddTeamChat(options =>
        {
            options.ConnectionString = "Data Source=:memory:";
            options.TablePrefix = "tc_";
        }));
        var storage = services.BuildServiceProvider().GetRequiredService<ChatStorageOptions>();
        Assert.Equal("Data Source=:memory:", storage.ConnectionString);
        Assert.Equal("tc_", storage.TablePrefix);
    }

    private sealed record HealthResponse(string Status);
}
