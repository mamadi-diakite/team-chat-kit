namespace TeamChat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTeamChatInfrastructure(this IServiceCollection services, ChatStorageOptions storage)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(storage);

        services.AddSingleton(storage);
        services.AddDbContext<ChatDbContext>(options => ChatDbContext.Configure(options, storage));
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    public static async Task ApplyTeamChatMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

        EnsureDataDirectoryExists(db.Database.GetConnectionString());
        await db.Database.MigrateAsync(cancellationToken);
    }

    // SQLite creates the database file but not its parent folder.
    private static void EnsureDataDirectoryExists(string? connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (builder.Mode == SqliteOpenMode.Memory || builder.DataSource is "" or ":memory:")
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(builder.DataSource))!);
    }
}
