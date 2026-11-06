namespace TeamChat.Infrastructure.Persistence;

/// <summary>Base class of the chat migrations: table, key and index names start with the configured prefix.</summary>
/// <remarks>
/// Migrations are generated with <see cref="ChatStorageOptions.DefaultTablePrefix"/>. After <c>dotnet ef migrations add</c>,
/// derive the new migration from this class and replace the generated prefix with <see cref="TablePrefix"/>.
/// </remarks>
public abstract class ChatMigration : Migration
{
    public string TablePrefix { get; internal set; } = ChatStorageOptions.DefaultTablePrefix;
}
