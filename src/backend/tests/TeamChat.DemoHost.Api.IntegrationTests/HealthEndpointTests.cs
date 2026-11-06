using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TeamChat.AspNetCore;

namespace TeamChat.DemoHost.Api.IntegrationTests;

public sealed class HealthEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
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
    public void AddTeamChat_rejects_null_services()
    {
        Assert.Throws<ArgumentNullException>(() => TeamChatServiceCollectionExtensions.AddTeamChat(null!));
    }

    [Fact]
    public void MapTeamChat_rejects_null_endpoints()
    {
        Assert.Throws<ArgumentNullException>(() => TeamChatEndpointRouteBuilderExtensions.MapTeamChat(null!));
    }

    [Fact]
    public void AddTeamChat_returns_the_same_collection()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddTeamChat());
    }

    private sealed record HealthResponse(string Status);
}
