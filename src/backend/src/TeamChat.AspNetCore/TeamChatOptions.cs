namespace TeamChat.AspNetCore;

/// <summary>What a host sets when it adds the chat.</summary>
public sealed class TeamChatOptions
{
    public const string DefaultRoutePrefix = "/api/chat";

    /// <summary>SQLite connection string. The chat can share the host's database thanks to its table prefix.</summary>
    public string? ConnectionString { get; set; }

    public string TablePrefix { get; set; } = ChatStorageOptions.DefaultTablePrefix;

    /// <summary>Where <c>MapTeamChat()</c> puts the chat endpoints.</summary>
    public string RoutePrefix { get; set; } = DefaultRoutePrefix;
}
