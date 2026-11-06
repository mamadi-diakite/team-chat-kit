namespace TeamChat.Domain;

/// <summary>A request from one user to another, in another organization, to become contacts.</summary>
public sealed class Invitation
{
    public const int MessageMaxLength = 500;

    private Invitation()
    {
    }

    public Invitation(string senderId, string recipientId, string? message, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        SenderId = ChatUserId.Required(senderId, nameof(senderId));
        RecipientId = ChatUserId.Required(recipientId, nameof(recipientId));
        Message = Guard.Optional(message, nameof(message), MessageMaxLength);
        Status = InvitationStatus.Pending;
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));

        if (SenderId == RecipientId)
        {
            throw new ArgumentException("A user cannot invite themselves.", nameof(recipientId));
        }
    }

    public Guid Id { get; private set; }

    public string SenderId { get; private set; } = string.Empty;

    public string RecipientId { get; private set; } = string.Empty;

    public string? Message { get; private set; }

    public InvitationStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>When the invitation was accepted, declined or cancelled.</summary>
    public DateTime? ClosedAtUtc { get; private set; }

    public void Accept(DateTime nowUtc) => Close(InvitationStatus.Accepted, nowUtc);

    public void Decline(DateTime nowUtc) => Close(InvitationStatus.Declined, nowUtc);

    public void Cancel(DateTime nowUtc) => Close(InvitationStatus.Cancelled, nowUtc);

    private void Close(InvitationStatus status, DateTime nowUtc)
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new InvalidOperationException("Only a pending invitation can be accepted, declined or cancelled.");
        }

        ClosedAtUtc = Guard.Utc(nowUtc, nameof(nowUtc));
        Status = status;
    }
}
