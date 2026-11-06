namespace TeamChat.AspNetCore;

internal static class ChatEndpoints
{
    public static void Map(RouteGroupBuilder chat)
    {
        chat.MapGet("/me", GetCurrentUser);
        chat.MapGet("/directory", GetDirectoryAsync);

        chat.MapGet("/invitations", ListInvitationsAsync);
        chat.MapPost("/invitations", SendInvitationAsync);
        chat.MapPost("/invitations/{invitationId:guid}/accept",
            (HttpContext http, Guid invitationId, ContactService contacts, CancellationToken cancellationToken) =>
                ToResultAsync(contacts.AcceptAsync(CurrentChatUserFilter.Get(http), invitationId, cancellationToken)));
        chat.MapPost("/invitations/{invitationId:guid}/decline",
            (HttpContext http, Guid invitationId, ContactService contacts, CancellationToken cancellationToken) =>
                ToResultAsync(contacts.DeclineAsync(CurrentChatUserFilter.Get(http), invitationId, cancellationToken)));
        chat.MapPost("/invitations/{invitationId:guid}/cancel",
            (HttpContext http, Guid invitationId, ContactService contacts, CancellationToken cancellationToken) =>
                ToResultAsync(contacts.CancelAsync(CurrentChatUserFilter.Get(http), invitationId, cancellationToken)));

        chat.MapGet("/contacts", ListContactsAsync);
        chat.MapDelete("/contacts/{userId}", RemoveContactAsync);
    }

    private static Ok<ChatUserResponse> GetCurrentUser(HttpContext http) =>
        TypedResults.Ok(ChatUserResponse.From(CurrentChatUserFilter.Get(http)));

    private static async Task<Ok<IReadOnlyList<ChatUserResponse>>> GetDirectoryAsync(
        HttpContext http,
        string? search,
        DirectoryService directory,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await directory.GetDirectoryAsync(CurrentChatUserFilter.Get(http), search, cancellationToken));

    private static async Task<Ok<InvitationsResponse>> ListInvitationsAsync(
        HttpContext http,
        ContactService contacts,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await contacts.ListInvitationsAsync(CurrentChatUserFilter.Get(http), cancellationToken));

    private static async Task<Results<Created<InvitationResponse>, ValidationProblem>> SendInvitationAsync(
        HttpContext http,
        SendInvitationRequest request,
        ContactService contacts,
        CancellationToken cancellationToken)
    {
        var result = await contacts.SendInvitationAsync(CurrentChatUserFilter.Get(http), request, cancellationToken);

        return result.Invitation is { } invitation
            ? TypedResults.Created((string?)null, invitation)
            : TypedResults.ValidationProblem(result.Errors);
    }

    private static async Task<Ok<IReadOnlyList<ContactResponse>>> ListContactsAsync(
        HttpContext http,
        ContactService contacts,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await contacts.ListContactsAsync(CurrentChatUserFilter.Get(http), cancellationToken));

    private static async Task<Results<NoContent, NotFound>> RemoveContactAsync(
        HttpContext http,
        string userId,
        ContactService contacts,
        CancellationToken cancellationToken) =>
        await contacts.RemoveContactAsync(CurrentChatUserFilter.Get(http), userId, cancellationToken)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> ToResultAsync(Task<InvitationActionResult> action) =>
        await action switch
        {
            InvitationActionResult.Done => TypedResults.NoContent(),
            InvitationActionResult.NotFound => TypedResults.NotFound(),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status409Conflict, title: "This invitation is no longer pending."),
        };
}
