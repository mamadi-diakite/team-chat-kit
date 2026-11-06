namespace TeamChat.Domain;

/// <summary>
/// Users belong to the host application, so the chat only keeps their id as the host gives it.
/// </summary>
public static class ChatUserId
{
    public const int MaxLength = 100;

    public static string Required(string? value, string paramName) => Guard.Required(value, paramName, MaxLength);
}
