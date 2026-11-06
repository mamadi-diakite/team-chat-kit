namespace TeamChat.Infrastructure.Persistence;

/// <summary>Where the chat stores its tables.</summary>
public sealed partial class ChatStorageOptions
{
    public const string DefaultTablePrefix = "Chat";
    public const int TablePrefixMaxLength = 30;

    /// <param name="connectionString">SQLite connection string, usually the host's own database.</param>
    /// <param name="tablePrefix">Added before every chat table name, so the chat tables never clash with the host's.</param>
    public ChatStorageOptions(string connectionString, string tablePrefix = DefaultTablePrefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(tablePrefix);
        if (!TablePrefixPattern().IsMatch(tablePrefix))
        {
            throw new ArgumentException(
                $"Table prefix must use only letters, digits and underscores, at most {TablePrefixMaxLength} characters.",
                nameof(tablePrefix));
        }

        ConnectionString = connectionString;
        TablePrefix = tablePrefix;
    }

    public string ConnectionString { get; }

    public string TablePrefix { get; }

    public string MigrationsHistoryTable => TablePrefix + "MigrationsHistory";

    [GeneratedRegex("^[A-Za-z0-9_]{0,30}$")]
    private static partial Regex TablePrefixPattern();
}
