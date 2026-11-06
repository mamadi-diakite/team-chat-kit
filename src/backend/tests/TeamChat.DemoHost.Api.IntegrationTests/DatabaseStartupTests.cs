namespace TeamChat.DemoHost.Api.IntegrationTests;

public sealed class DatabaseStartupTests(DemoHostApiFactory factory) : IClassFixture<DemoHostApiFactory>
{
    [Fact]
    public async Task Startup_applies_the_chat_and_host_migrations_to_one_database()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var chat = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        var host = scope.ServiceProvider.GetRequiredService<DemoHostDbContext>();

        Assert.Equal(ChatStorageOptions.DefaultTablePrefix, chat.TablePrefix);
        Assert.Equal(host.Database.GetConnectionString(), chat.Database.GetConnectionString());
        Assert.Empty(await chat.Database.GetPendingMigrationsAsync());
        Assert.Empty(await host.Database.GetPendingMigrationsAsync());
        Assert.False(host.Database.HasPendingModelChanges());
        Assert.Equal(0, await host.Users.CountAsync());
    }

    [Fact]
    public void Design_time_factory_builds_the_host_context()
    {
        using var db = new DemoHostDbContextDesignTimeFactory().CreateDbContext([]);

        Assert.Equal("Data Source=design-time.db", db.Database.GetConnectionString());
    }
}

public sealed class MissingSigningKeyTests(MissingSigningKeyApiFactory factory) : IClassFixture<MissingSigningKeyApiFactory>
{
    [Fact]
    public void The_api_does_not_start_without_a_signing_key()
    {
        var error = Assert.Throws<InvalidOperationException>(() => factory.CreateClient());

        Assert.Contains("Jwt:SigningKey is missing", error.Message);
    }
}
