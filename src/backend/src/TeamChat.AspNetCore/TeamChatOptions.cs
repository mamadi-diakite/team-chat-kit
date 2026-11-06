namespace TeamChat.AspNetCore;

/// <summary>What a host sets when it adds the chat.</summary>
public sealed class TeamChatOptions
{
    /// <summary>SQLite connection string. The chat can share the host's database thanks to its table prefix.</summary>
    public string? ConnectionString { get; set; }

    public string TablePrefix { get; set; } = ChatStorageOptions.DefaultTablePrefix;
}
