namespace TeamChat.AspNetCore;

/// <summary>Answers 401 when the host's user is unknown, and keeps the resolved user for the endpoint.</summary>
internal sealed class CurrentChatUserFilter(CurrentChatUserResolver resolver) : IEndpointFilter
{
    private static readonly object ItemKey = new();

    public static ChatUser Get(HttpContext httpContext) => (ChatUser)httpContext.Items[ItemKey]!;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = await resolver.ResolveAsync(httpContext.User, httpContext.RequestAborted);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        httpContext.Items[ItemKey] = user;

        return await next(context);
    }
}
