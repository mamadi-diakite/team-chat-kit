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

    private sealed record HealthResponse(string Status);
}
