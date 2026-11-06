namespace TeamChat.AspNetCore;

public static class TeamChatEndpointRouteBuilderExtensions
{
    /// <summary>Maps the chat endpoints under <see cref="TeamChatOptions.RoutePrefix"/>.</summary>
    /// <remarks>Every endpoint but the health check needs a user authenticated by the host and known by its adapter.</remarks>
    public static RouteGroupBuilder MapTeamChat(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<TeamChatOptions>();
        var chat = endpoints.MapGroup(options.RoutePrefix);

        chat.MapGet("/health", () => Results.Ok(new { status = "ok" }));
        ChatEndpoints.Map(chat.MapGroup("").RequireAuthorization().AddEndpointFilter<CurrentChatUserFilter>());

        return chat;
    }
}
