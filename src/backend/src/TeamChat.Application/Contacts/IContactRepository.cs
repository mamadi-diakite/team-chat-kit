namespace TeamChat.Application.Contacts;

/// <summary>Invitations and contacts, saved together.</summary>
public interface IContactRepository
{
    Task<Invitation?> FindInvitationAsync(Guid invitationId, CancellationToken cancellationToken);

    /// <summary>The pending invitation between two users, whoever sent it.</summary>
    Task<Invitation?> FindPendingInvitationAsync(string userId, string otherUserId, CancellationToken cancellationToken);

    /// <returns>The pending invitations the user sent or received, newest first.</returns>
    Task<IReadOnlyList<Invitation>> ListPendingInvitationsAsync(string userId, CancellationToken cancellationToken);

    void AddInvitation(Invitation invitation);

    Task<IReadOnlyList<Contact>> ListContactsAsync(string userId, CancellationToken cancellationToken);

    Task<Contact?> FindContactAsync(string userId, string otherUserId, CancellationToken cancellationToken);

    void AddContact(Contact contact);

    void RemoveContact(Contact contact);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
