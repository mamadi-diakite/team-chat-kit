namespace TeamChat.Application.Contacts;

public sealed record SendInvitationRequest(string Email, string? Message);

/// <param name="User">The other person: the sender of a received invitation, the recipient of a sent one.</param>
public sealed record InvitationResponse(Guid Id, ChatUserResponse User, string? Message, DateTime CreatedAtUtc);

public sealed record InvitationsResponse(IReadOnlyList<InvitationResponse> Received, IReadOnlyList<InvitationResponse> Sent);

public sealed record ContactResponse(ChatUserResponse User, DateTime SinceUtc);

/// <param name="Invitation">The new invitation, or <c>null</c> when <paramref name="Errors"/> is not empty.</param>
/// <param name="Errors">Messages by request field, in camelCase.</param>
public sealed record SendInvitationResult(InvitationResponse? Invitation, IReadOnlyDictionary<string, string[]> Errors)
{
    public static SendInvitationResult Sent(InvitationResponse invitation) => new(invitation, new Dictionary<string, string[]>());

    public static SendInvitationResult Invalid(string field, string message) =>
        new(null, new Dictionary<string, string[]> { [field] = [message] });
}

public enum InvitationActionResult
{
    Done = 0,

    /// <summary>No such invitation, or the user is not the one who may act on it.</summary>
    NotFound = 1,

    /// <summary>The invitation was already accepted, declined or cancelled.</summary>
    NotPending = 2,
}
