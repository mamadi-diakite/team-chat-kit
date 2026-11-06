namespace TeamChat.AspNetCore;

public static class TeamChatEndpointRouteBuilderExtensions
{
    /// <summary>Maps the chat endpoints under <see cref="TeamChatOptions.RoutePrefix"/>.</summary>
    /// <remarks>Every endpoint but the health check needs an authenticated user.</remarks>
    public static RouteGroupBuilder MapTeamChat(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var options = endpoints.ServiceProvider.GetRequiredService<TeamChatOptions>();
        var chat = endpoints.MapGroup(options.RoutePrefix).RequireAuthorization();

        chat.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
        chat.MapGet("/me", ChatEndpoints.GetCurrentUserAsync);

        return chat;
    }
}
