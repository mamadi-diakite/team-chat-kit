namespace TeamChat.Infrastructure.Persistence;

public sealed class ContactRepository(ChatDbContext db) : IContactRepository
{
    public Task<Invitation?> FindInvitationAsync(Guid invitationId, CancellationToken cancellationToken) =>
        db.Invitations.SingleOrDefaultAsync(i => i.Id == invitationId, cancellationToken);

    public Task<Invitation?> FindPendingInvitationAsync(string userId, string otherUserId, CancellationToken cancellationToken) =>
        db.Invitations
            .Where(i => i.Status == InvitationStatus.Pending)
            .Where(i => (i.SenderId == userId && i.RecipientId == otherUserId)
                || (i.SenderId == otherUserId && i.RecipientId == userId))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Invitation>> ListPendingInvitationsAsync(string userId, CancellationToken cancellationToken) =>
        await db.Invitations
            .Where(i => i.Status == InvitationStatus.Pending && (i.SenderId == userId || i.RecipientId == userId))
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void AddInvitation(Invitation invitation) => db.Invitations.Add(invitation);

    public async Task<IReadOnlyList<Contact>> ListContactsAsync(string userId, CancellationToken cancellationToken) =>
        await db.Contacts.Where(c => c.FirstUserId == userId || c.SecondUserId == userId).ToListAsync(cancellationToken);

    public Task<Contact?> FindContactAsync(string userId, string otherUserId, CancellationToken cancellationToken)
    {
        var (first, second) = Contact.Pair(userId, otherUserId);

        return db.Contacts.SingleOrDefaultAsync(c => c.FirstUserId == first && c.SecondUserId == second, cancellationToken);
    }

    public void AddContact(Contact contact) => db.Contacts.Add(contact);

    public void RemoveContact(Contact contact) => db.Contacts.Remove(contact);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
