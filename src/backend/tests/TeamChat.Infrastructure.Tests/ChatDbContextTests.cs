namespace TeamChat.Infrastructure.Tests;

public sealed class ChatDbContextTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Saves_and_reads_an_invitation()
    {
        var invitation = new Invitation("user-1", "user-2", "Hello", Now);
        invitation.Accept(Now.AddMinutes(5));
        await using (var db = _database.CreateContext())
        {
            db.Invitations.Add(invitation);
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            var loaded = await db.Invitations.SingleAsync();

            Assert.Equal(invitation.Id, loaded.Id);
            Assert.Equal("Hello", loaded.Message);
            Assert.Equal(InvitationStatus.Accepted, loaded.Status);
            Assert.Equal(DateTimeKind.Utc, loaded.CreatedAtUtc.Kind);
            Assert.Equal(Now.AddMinutes(5), loaded.ClosedAtUtc);
        }
    }

    [Fact]
    public async Task Only_one_invitation_per_pair_is_pending()
    {
        var declined = new Invitation("user-1", "user-2", null, Now);
        declined.Decline(Now);
        await using (var db = _database.CreateContext())
        {
            db.Invitations.AddRange(declined, new Invitation("user-1", "user-2", null, Now));
            await db.SaveChangesAsync();
        }

        await using var context = _database.CreateContext();
        context.Invitations.Add(new Invitation("user-1", "user-2", null, Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task A_pair_of_users_has_one_contact()
    {
        await using (var db = _database.CreateContext())
        {
            db.Contacts.Add(new Contact("user-1", "user-2", Now));
            await db.SaveChangesAsync();
        }

        await using (var db = _database.CreateContext())
        {
            Assert.Equal("user-2", (await db.Contacts.SingleAsync()).OtherUserId("user-1"));
        }

        await using var context = _database.CreateContext();
        context.Contacts.Add(new Contact("user-2", "user-1", Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Migrations_leave_no_pending_change()
    {
        await using var db = _database.CreateContext();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public void Design_time_factory_uses_the_default_prefix()
    {
        using var db = new ChatDbContextDesignTimeFactory().CreateDbContext([]);

        Assert.Equal(ChatStorageOptions.DefaultTablePrefix, db.TablePrefix);
        Assert.Equal("Data Source=design-time.db", db.Database.GetConnectionString());
        Assert.Equal("ChatInvitations", db.Model.FindEntityType(typeof(Invitation))!.GetTableName());
    }
}
