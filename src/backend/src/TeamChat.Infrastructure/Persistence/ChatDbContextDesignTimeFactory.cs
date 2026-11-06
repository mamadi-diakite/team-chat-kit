namespace TeamChat.Infrastructure.Persistence;

/// <summary>Used by <c>dotnet ef</c> only: migrations are always generated with the default table prefix.</summary>
/// <remarks>The prefix services are left out: before the first migration there is no model snapshot to rename.</remarks>
public sealed class ChatDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ChatDbContext>
{
    public ChatDbContext CreateDbContext(string[] args)
    {
        var storage = new ChatStorageOptions("Data Source=design-time.db");
        var options = new DbContextOptionsBuilder<ChatDbContext>()
            .UseSqlite(storage.ConnectionString, sqlite => sqlite.MigrationsHistoryTable(storage.MigrationsHistoryTable))
            .Options;

        return new ChatDbContext(options, storage);
    }
}
