namespace TeamChat.AspNetCore;

internal static class ChatEndpoints
{
    public static async Task<Results<Ok<ChatUserResponse>, UnauthorizedHttpResult>> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        CurrentChatUserResolver resolver,
        CancellationToken cancellationToken)
    {
        var user = await resolver.ResolveAsync(principal, cancellationToken);

        return user is null ? TypedResults.Unauthorized() : TypedResults.Ok(ChatUserResponse.From(user));
    }
}
