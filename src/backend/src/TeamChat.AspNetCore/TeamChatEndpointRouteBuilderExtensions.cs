namespace TeamChat.AspNetCore;

public static class TeamChatEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapTeamChat(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var chat = endpoints.MapGroup("/api/chat");
        chat.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        return chat;
    }
}
