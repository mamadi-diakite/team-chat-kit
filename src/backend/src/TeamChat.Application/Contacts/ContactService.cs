namespace TeamChat.Application.Contacts;

/// <summary>Invitations between organizations and the contacts they create.</summary>
public sealed class ContactService(IContactRepository repository, IChatIdentityAdapter identity, TimeProvider timeProvider)
{
    public async Task<SendInvitationResult> SendInvitationAsync(
        ChatUser me,
        SendInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(me);
        ArgumentNullException.ThrowIfNull(request);

        if (request.Message?.Trim().Length > Invitation.MessageMaxLength)
        {
            return SendInvitationResult.Invalid("message", $"Use at most {Invitation.MessageMaxLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return SendInvitationResult.Invalid("email", "Email is required.");
        }

        var recipient = await identity.FindUserByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        var problem = recipient switch
        {
            null => "No account uses this email.",
            _ when recipient.Id == me.Id => "You cannot invite yourself.",
            _ when recipient.OrganizationId == me.OrganizationId =>
                "This person is in your organization: find them in the directory.",
            _ => await FindInvitationProblemAsync(me, recipient, cancellationToken),
        };
        if (problem is not null)
        {
            return SendInvitationResult.Invalid("email", problem);
        }

        var invitation = new Invitation(me.Id, recipient!.Id, request.Message, timeProvider.GetUtcNow().UtcDateTime);
        repository.AddInvitation(invitation);
        await repository.SaveChangesAsync(cancellationToken);

        return SendInvitationResult.Sent(ToResponse(invitation, recipient));
    }

    public async Task<InvitationsResponse> ListInvitationsAsync(ChatUser me, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(me);

        var invitations = await repository.ListPendingInvitationsAsync(me.Id, cancellationToken);
        var users = await FindUsersAsync(
            invitations.Select(i => i.SenderId == me.Id ? i.RecipientId : i.SenderId), cancellationToken);

        // People the host no longer knows are left out.
        InvitationResponse[] Responses(Func<Invitation, bool> mine, Func<Invitation, string> other) =>
            [.. invitations
                .Where(mine)
                .Where(i => users.ContainsKey(other(i)))
                .Select(i => ToResponse(i, users[other(i)]))];

        return new InvitationsResponse(
            Received: Responses(i => i.RecipientId == me.Id, i => i.SenderId),
            Sent: Responses(i => i.SenderId == me.Id, i => i.RecipientId));
    }

    /// <summary>The recipient accepts: both people become contacts.</summary>
    public Task<InvitationActionResult> AcceptAsync(ChatUser me, Guid invitationId, CancellationToken cancellationToken = default) =>
        CloseAsync(me, invitationId, asRecipient: true, (invitation, now) =>
        {
            invitation.Accept(now);
            repository.AddContact(new Contact(invitation.SenderId, invitation.RecipientId, now));
        }, cancellationToken);

    public Task<InvitationActionResult> DeclineAsync(ChatUser me, Guid invitationId, CancellationToken cancellationToken = default) =>
        CloseAsync(me, invitationId, asRecipient: true, (invitation, now) => invitation.Decline(now), cancellationToken);

    public Task<InvitationActionResult> CancelAsync(ChatUser me, Guid invitationId, CancellationToken cancellationToken = default) =>
        CloseAsync(me, invitationId, asRecipient: false, (invitation, now) => invitation.Cancel(now), cancellationToken);

    /// <returns>My contacts the host still knows, ordered by name.</returns>
    public async Task<IReadOnlyList<ContactResponse>> ListContactsAsync(ChatUser me, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(me);

        var contacts = await repository.ListContactsAsync(me.Id, cancellationToken);
        var users = await FindUsersAsync(contacts.Select(c => c.OtherUserId(me.Id)), cancellationToken);

        return
        [
            .. contacts
                .Where(c => users.ContainsKey(c.OtherUserId(me.Id)))
                .Select(c => new ContactResponse(ChatUserResponse.From(users[c.OtherUserId(me.Id)]), c.CreatedAtUtc))
                .OrderBy(c => c.User.DisplayName, StringComparer.OrdinalIgnoreCase),
        ];
    }

    /// <returns><c>false</c> when the other person is not one of my contacts.</returns>
    public async Task<bool> RemoveContactAsync(ChatUser me, string otherUserId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(me);
        ArgumentNullException.ThrowIfNull(otherUserId);

        var contact = await repository.FindContactAsync(me.Id, otherUserId, cancellationToken);
        if (contact is null)
        {
            return false;
        }

        repository.RemoveContact(contact);
        await repository.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<string?> FindInvitationProblemAsync(ChatUser me, ChatUser recipient, CancellationToken cancellationToken)
    {
        if (await repository.FindContactAsync(me.Id, recipient.Id, cancellationToken) is not null)
        {
            return "You are already contacts.";
        }

        var pending = await repository.FindPendingInvitationAsync(me.Id, recipient.Id, cancellationToken);

        return pending switch
        {
            null => null,
            _ when pending.SenderId == me.Id => "You already invited this person.",
            _ => "This person already invited you: accept their invitation.",
        };
    }

    private async Task<InvitationActionResult> CloseAsync(
        ChatUser me,
        Guid invitationId,
        bool asRecipient,
        Action<Invitation, DateTime> close,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(me);

        var invitation = await repository.FindInvitationAsync(invitationId, cancellationToken);
        if (invitation is null || (asRecipient ? invitation.RecipientId : invitation.SenderId) != me.Id)
        {
            return InvitationActionResult.NotFound;
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return InvitationActionResult.NotPending;
        }

        close(invitation, timeProvider.GetUtcNow().UtcDateTime);
        await repository.SaveChangesAsync(cancellationToken);

        return InvitationActionResult.Done;
    }

    private async Task<Dictionary<string, ChatUser>> FindUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken)
    {
        var users = await identity.FindUsersAsync([.. userIds.Distinct()], cancellationToken);

        return users.ToDictionary(u => u.Id);
    }

    private static InvitationResponse ToResponse(Invitation invitation, ChatUser otherUser) =>
        new(invitation.Id, ChatUserResponse.From(otherUser), invitation.Message, invitation.CreatedAtUtc);
}
