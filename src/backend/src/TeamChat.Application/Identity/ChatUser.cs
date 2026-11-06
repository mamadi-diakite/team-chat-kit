namespace TeamChat.Application.Identity;

/// <summary>What the chat needs to know about a host user.</summary>
public sealed record ChatUser(string Id, string DisplayName, string Email, string OrganizationId, string OrganizationName);

public sealed record ChatUserResponse(string Id, string DisplayName, string Email, string OrganizationName)
{
    public static ChatUserResponse From(ChatUser user) => new(user.Id, user.DisplayName, user.Email, user.OrganizationName);
}
