namespace TeamChat.DemoHost.Api.IntegrationTests;

public sealed class DemoDataStartupTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Fact]
    public async Task Development_startup_seeds_demo_data_once()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DemoHostDbContext>();

        Assert.Equal(DemoDataSeeder.Accounts.Count, await db.Users.CountAsync());
        Assert.Equal(3, await db.Organizations.CountAsync());
        Assert.All(DemoDataSeeder.Accounts, a => Assert.EndsWith("@example.com", a.Email));
        Assert.False(await factory.Services.SeedDemoDataAsync());
    }
}

public sealed class ProductionDemoDataStartupTests(ProductionDemoDataApiFactory factory)
    : IClassFixture<ProductionDemoDataApiFactory>
{
    [Fact]
    public async Task Startup_outside_development_never_seeds_demo_data()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DemoHostDbContext>();

        Assert.Equal(0, await db.Users.CountAsync());
    }
}
