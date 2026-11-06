namespace TeamChat.DemoHost.Api.IntegrationTests;

public sealed class DatabaseStartupTests(DemoHostApiFactory factory) : IClassFixture<DemoHostApiFactory>
{
    [Fact]
    public async Task Startup_applies_the_chat_migrations()
    {
        _ = factory.CreateClient();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        Assert.Equal(ChatStorageOptions.DefaultTablePrefix, db.TablePrefix);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await db.Invitations.CountAsync());
    }
}
