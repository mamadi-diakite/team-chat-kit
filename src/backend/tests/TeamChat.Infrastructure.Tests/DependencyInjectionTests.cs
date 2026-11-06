namespace TeamChat.Infrastructure.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void Rejects_missing_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => DependencyInjection.AddTeamChatInfrastructure(null!, new ChatStorageOptions("Data Source=:memory:")));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddTeamChatInfrastructure(null!));
        Assert.Throws<ArgumentNullException>(() => ChatDbContext.Configure(null!, new ChatStorageOptions("Data Source=:memory:")));
        Assert.Throws<ArgumentNullException>(() => ChatDbContext.Configure(new DbContextOptionsBuilder(), null!));
    }

    [Fact]
    public void Registers_the_context_with_the_storage_options()
    {
        var storage = new ChatStorageOptions("Data Source=:memory:", "tc_");
        using var provider = new ServiceCollection().AddTeamChatInfrastructure(storage).BuildServiceProvider();
        using var scope = provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        Assert.Same(storage, provider.GetRequiredService<ChatStorageOptions>());
        Assert.Equal("tc_", db.TablePrefix);
        Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public async Task Creates_missing_data_folder_and_applies_migrations()
    {
        var root = Path.Combine(Path.GetTempPath(), "team-chat-kit-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(root, "data", "app.db");

        try
        {
            await using var provider = new ServiceCollection()
                .AddTeamChatInfrastructure(new ChatStorageOptions($"Data Source={databasePath}"))
                .BuildServiceProvider();

            await provider.ApplyTeamChatMigrationsAsync();

            Assert.True(File.Exists(databasePath));
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("Data Source=:memory:")]
    [InlineData("Data Source=")]
    [InlineData("Data Source=in-memory-test;Mode=Memory;Cache=Shared")]
    public async Task Applies_migrations_without_creating_folders_for_memory_or_temporary_databases(string connectionString)
    {
        await using var provider = new ServiceCollection()
            .AddTeamChatInfrastructure(new ChatStorageOptions(connectionString))
            .BuildServiceProvider();

        await provider.ApplyTeamChatMigrationsAsync();

        Assert.False(File.Exists("in-memory-test"));
    }
}
