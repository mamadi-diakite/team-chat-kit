namespace TeamChat.Infrastructure.Tests;

public sealed class ContactRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Finds_invitations_by_id_and_pending_ones_in_both_directions()
    {
        var closed = new Invitation("user-a", "user-b", null, Now);
        closed.Cancel(Now);
        var pending = new Invitation("user-b", "user-a", null, Now.AddMinutes(1));
        await SaveAsync(db => db.Invitations.AddRange(closed, pending));

        await using var context = _database.CreateContext();
        var repository = new ContactRepository(context);

        Assert.Equal(closed.Id, (await repository.FindInvitationAsync(closed.Id, default))?.Id);
        Assert.Null(await repository.FindInvitationAsync(Guid.NewGuid(), default));
        Assert.Equal(pending.Id, (await repository.FindPendingInvitationAsync("user-a", "user-b", default))?.Id);
        Assert.Equal(pending.Id, (await repository.FindPendingInvitationAsync("user-b", "user-a", default))?.Id);
        Assert.Null(await repository.FindPendingInvitationAsync("user-a", "user-c", default));
    }

    [Fact]
    public async Task Lists_the_pending_invitations_of_a_user_newest_first()
    {
        var older = new Invitation("user-a", "user-b", null, Now);
        var newer = new Invitation("user-c", "user-a", null, Now.AddHours(1));
        var accepted = new Invitation("user-a", "user-d", null, Now);
        accepted.Accept(Now);
        var other = new Invitation("user-b", "user-c", null, Now);
        await SaveAsync(db => db.Invitations.AddRange(older, newer, accepted, other));

        await using var context = _database.CreateContext();
        var invitations = await new ContactRepository(context).ListPendingInvitationsAsync("user-a", default);

        Assert.Equal([newer.Id, older.Id], invitations.Select(i => i.Id));
    }

    [Fact]
    public async Task Adds_an_invitation_and_a_contact_in_one_save()
    {
        await using (var context = _database.CreateContext())
        {
            var repository = new ContactRepository(context);
            repository.AddInvitation(new Invitation("user-a", "user-b", "Hi", Now));
            repository.AddContact(new Contact("user-a", "user-c", Now));
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        Assert.Equal("Hi", (await check.Invitations.SingleAsync()).Message);
        Assert.Equal("user-c", (await check.Contacts.SingleAsync()).OtherUserId("user-a"));
    }

    [Fact]
    public async Task Lists_finds_and_removes_contacts()
    {
        await SaveAsync(db => db.Contacts.AddRange(
            new Contact("user-a", "user-b", Now),
            new Contact("user-c", "user-a", Now),
            new Contact("user-b", "user-c", Now)));

        await using (var context = _database.CreateContext())
        {
            var repository = new ContactRepository(context);

            Assert.Equal(2, (await repository.ListContactsAsync("user-a", default)).Count);
            Assert.NotNull(await repository.FindContactAsync("user-a", "user-c", default));
            Assert.Null(await repository.FindContactAsync("user-a", "user-d", default));

            repository.RemoveContact((await repository.FindContactAsync("user-b", "user-a", default))!);
            await repository.SaveChangesAsync(default);
        }

        await using var check = _database.CreateContext();
        Assert.Equal(2, await check.Contacts.CountAsync());
    }

    [Fact]
    public void Is_registered_with_the_infrastructure()
    {
        using var provider = new ServiceCollection()
            .AddTeamChatInfrastructure(new ChatStorageOptions("Data Source=:memory:"))
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<ContactRepository>(scope.ServiceProvider.GetRequiredService<TeamChat.Application.Contacts.IContactRepository>());
    }

    private async Task SaveAsync(Action<ChatDbContext> add)
    {
        await using var db = _database.CreateContext();
        add(db);
        await db.SaveChangesAsync();
    }
}
