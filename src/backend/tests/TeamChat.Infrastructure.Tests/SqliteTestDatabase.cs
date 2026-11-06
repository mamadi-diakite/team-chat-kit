namespace TeamChat.Infrastructure.Tests;

/// <summary>Shared in-memory SQLite database that lives as long as its keep-alive connection stays open.</summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _keepAlive;

    public SqliteTestDatabase(string tablePrefix = ChatStorageOptions.DefaultTablePrefix)
    {
        ConnectionString = $"Data Source=chat-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keepAlive = new SqliteConnection(ConnectionString);
        _keepAlive.Open();
        Migrate(tablePrefix);
        Storage = new ChatStorageOptions(ConnectionString, tablePrefix);
    }

    public string ConnectionString { get; }

    public ChatStorageOptions Storage { get; }

    public static ChatDbContext CreateContext(ChatStorageOptions storage) =>
        new((DbContextOptions<ChatDbContext>)ChatDbContext.Configure(new DbContextOptionsBuilder<ChatDbContext>(), storage).Options, storage);

    public ChatDbContext CreateContext() => CreateContext(Storage);

    /// <summary>Adds the chat tables with another prefix to the same database.</summary>
    public void Migrate(string tablePrefix)
    {
        using var db = CreateContext(new ChatStorageOptions(ConnectionString, tablePrefix));
        db.Database.Migrate();
    }

    /// <summary>Tables and indexes, without the SQLite ones and the migration lock that EF Core shares between contexts.</summary>
    public List<string> TableNames()
    {
        using var command = _keepAlive.CreateCommand();
        command.CommandText = """
            SELECT name FROM sqlite_master
            WHERE type IN ('table', 'index') AND name NOT LIKE 'sqlite_%' AND name <> '__EFMigrationsLock'
            ORDER BY name
            """;
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read())
        {
            names.Add(reader.GetString(0));
        }

        return names;
    }

    public void Dispose() => _keepAlive.Dispose();
}
