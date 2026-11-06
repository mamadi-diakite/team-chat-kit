namespace TeamChat.Application.Tests;

public sealed class ContactServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);

    private readonly FakeIdentityAdapter _identity = new();
    private readonly InMemoryContactRepository _repository = new();
    private readonly ContactService _service;

    private static ChatUser Ada => FakeIdentityAdapter.Ada;

    private static ChatUser Cleo => FakeIdentityAdapter.Cleo;

    private static ChatUser Dan => FakeIdentityAdapter.Dan;

    public ContactServiceTests() => _service = new ContactService(_repository, _identity, new FixedTimeProvider(Now));

    [Fact]
    public async Task Sends_an_invitation_to_someone_of_another_organization()
    {
        var result = await _service.SendInvitationAsync(Ada, new SendInvitationRequest(" Cleo@Example.com ", " Hello Cleo "));

        Assert.Empty(result.Errors);
        var invitation = Assert.Single(_repository.Invitations);
        Assert.Equal((Ada.Id, Cleo.Id, "Hello Cleo"), (invitation.SenderId, invitation.RecipientId, invitation.Message));
        Assert.Equal(Now.UtcDateTime, invitation.CreatedAtUtc);
        Assert.Equal(1, _repository.SaveCount);
        Assert.Equal(
            new InvitationResponse(invitation.Id, ChatUserResponse.From(Cleo), "Hello Cleo", Now.UtcDateTime),
            result.Invitation);
    }

    [Theory]
    [InlineData(" ", "Email is required.")]
    [InlineData("nobody@example.com", "No account uses this email.")]
    [InlineData("ada@example.com", "You cannot invite yourself.")]
    [InlineData("ben@example.com", "This person is in your organization: find them in the directory.")]
    public async Task Refuses_invitations_that_make_no_sense(string email, string message)
    {
        var result = await _service.SendInvitationAsync(Ada, new SendInvitationRequest(email, null));

        AssertRefused(result, "email", message);
    }

    [Fact]
    public async Task Refuses_a_message_that_is_too_long()
    {
        var result = await _service.SendInvitationAsync(
            Ada, new SendInvitationRequest("cleo@example.com", new string('a', 501)));

        AssertRefused(result, "message", "Use at most 500 characters.");
    }

    [Fact]
    public async Task Refuses_to_invite_a_contact()
    {
        _repository.Contacts.Add(new Contact(Cleo.Id, Ada.Id, Now.UtcDateTime));

        var result = await _service.SendInvitationAsync(Ada, new SendInvitationRequest("cleo@example.com", null));

        AssertRefused(result, "email", "You are already contacts.");
    }

    [Fact]
    public async Task Keeps_one_pending_invitation_per_pair()
    {
        _repository.Invitations.Add(new Invitation(Ada.Id, Cleo.Id, null, Now.UtcDateTime));
        _repository.Invitations.Add(new Invitation(Dan.Id, Ada.Id, null, Now.UtcDateTime));

        var again = await _service.SendInvitationAsync(Ada, new SendInvitationRequest("cleo@example.com", null));
        var reverse = await _service.SendInvitationAsync(Ada, new SendInvitationRequest("dan@example.com", null));

        Assert.Equal(["You already invited this person."], again.Errors["email"]);
        Assert.Equal(["This person already invited you: accept their invitation."], reverse.Errors["email"]);
        Assert.Equal(2, _repository.Invitations.Count);
    }

    [Fact]
    public async Task A_closed_invitation_does_not_block_a_new_one()
    {
        var declined = new Invitation(Ada.Id, Cleo.Id, null, Now.UtcDateTime);
        declined.Decline(Now.UtcDateTime);
        _repository.Invitations.Add(declined);

        var result = await _service.SendInvitationAsync(Ada, new SendInvitationRequest("cleo@example.com", null));

        Assert.NotNull(result.Invitation);
    }

    [Fact]
    public async Task Lists_received_and_sent_invitations_with_the_other_person()
    {
        var sent = new Invitation(Ada.Id, Cleo.Id, "Hi", Now.UtcDateTime);
        var received = new Invitation(Dan.Id, Ada.Id, null, Now.UtcDateTime);
        var fromUnknown = new Invitation("u-gone", Ada.Id, null, Now.UtcDateTime);
        var notMine = new Invitation(Dan.Id, Cleo.Id, null, Now.UtcDateTime);
        _repository.Invitations.AddRange([sent, received, fromUnknown, notMine]);

        var invitations = await _service.ListInvitationsAsync(Ada);

        Assert.Equal([received.Id], invitations.Received.Select(i => i.Id));
        Assert.Equal("Dan Ruiz", invitations.Received[0].User.DisplayName);
        Assert.Equal([sent.Id], invitations.Sent.Select(i => i.Id));
        Assert.Equal(("Cleo Haddad", "Hi"), (invitations.Sent[0].User.DisplayName, invitations.Sent[0].Message));
    }

    [Fact]
    public async Task Accepting_makes_both_people_contacts()
    {
        var invitation = new Invitation(Cleo.Id, Ada.Id, null, Now.UtcDateTime.AddDays(-1));
        _repository.Invitations.Add(invitation);

        var result = await _service.AcceptAsync(Ada, invitation.Id);

        Assert.Equal(InvitationActionResult.Done, result);
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        var contact = Assert.Single(_repository.Contacts);
        Assert.Equal(Cleo.Id, contact.OtherUserId(Ada.Id));
        Assert.Equal(Now.UtcDateTime, contact.CreatedAtUtc);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Only_the_recipient_accepts_or_declines_and_only_the_sender_cancels()
    {
        var invitation = new Invitation(Cleo.Id, Ada.Id, null, Now.UtcDateTime);
        _repository.Invitations.Add(invitation);

        Assert.Equal(InvitationActionResult.NotFound, await _service.AcceptAsync(Cleo, invitation.Id));
        Assert.Equal(InvitationActionResult.NotFound, await _service.DeclineAsync(Dan, invitation.Id));
        Assert.Equal(InvitationActionResult.NotFound, await _service.CancelAsync(Ada, invitation.Id));
        Assert.Equal(InvitationActionResult.NotFound, await _service.AcceptAsync(Ada, Guid.NewGuid()));
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Equal(0, _repository.SaveCount);
    }

    [Fact]
    public async Task Decline_and_cancel_close_the_invitation_without_contact()
    {
        var declined = new Invitation(Cleo.Id, Ada.Id, null, Now.UtcDateTime);
        var cancelled = new Invitation(Ada.Id, Dan.Id, null, Now.UtcDateTime);
        _repository.Invitations.AddRange([declined, cancelled]);

        Assert.Equal(InvitationActionResult.Done, await _service.DeclineAsync(Ada, declined.Id));
        Assert.Equal(InvitationActionResult.Done, await _service.CancelAsync(Ada, cancelled.Id));

        Assert.Equal(InvitationStatus.Declined, declined.Status);
        Assert.Equal(InvitationStatus.Cancelled, cancelled.Status);
        Assert.Empty(_repository.Contacts);
    }

    [Fact]
    public async Task A_closed_invitation_cannot_be_answered_again()
    {
        var invitation = new Invitation(Cleo.Id, Ada.Id, null, Now.UtcDateTime);
        invitation.Decline(Now.UtcDateTime);
        _repository.Invitations.Add(invitation);

        Assert.Equal(InvitationActionResult.NotPending, await _service.AcceptAsync(Ada, invitation.Id));
        Assert.Equal(InvitationStatus.Declined, invitation.Status);
    }

    [Fact]
    public async Task Lists_known_contacts_by_name()
    {
        _repository.Contacts.AddRange(
        [
            new Contact(Ada.Id, Dan.Id, Now.UtcDateTime),
            new Contact(Cleo.Id, Ada.Id, Now.UtcDateTime),
            new Contact(Ada.Id, "u-gone", Now.UtcDateTime),
            new Contact(Cleo.Id, Dan.Id, Now.UtcDateTime),
        ]);

        var contacts = await _service.ListContactsAsync(Ada);

        Assert.Equal(["Cleo Haddad", "Dan Ruiz"], contacts.Select(c => c.User.DisplayName));
        Assert.Equal(Now.UtcDateTime, contacts[0].SinceUtc);
    }

    [Fact]
    public async Task Removes_a_contact_for_both_people()
    {
        _repository.Contacts.Add(new Contact(Cleo.Id, Ada.Id, Now.UtcDateTime));

        Assert.True(await _service.RemoveContactAsync(Ada, Cleo.Id));
        Assert.False(await _service.RemoveContactAsync(Cleo, Ada.Id));

        Assert.Empty(_repository.Contacts);
        Assert.Equal(1, _repository.SaveCount);
    }

    [Fact]
    public async Task Rejects_missing_arguments()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SendInvitationAsync(null!, new SendInvitationRequest("a@example.com", null)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SendInvitationAsync(Ada, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ListInvitationsAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AcceptAsync(null!, Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ListContactsAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RemoveContactAsync(null!, Cleo.Id));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.RemoveContactAsync(Ada, null!));
    }

    [Fact]
    public void Registers_the_contact_and_directory_services()
    {
        using var provider = new ServiceCollection()
            .AddTeamChatApplication()
            .AddScoped<IChatIdentityAdapter, FakeIdentityAdapter>()
            .AddScoped<IContactRepository, InMemoryContactRepository>()
            .AddSingleton(TimeProvider.System)
            .BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ContactService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<DirectoryService>());
    }

    private void AssertRefused(SendInvitationResult result, string field, string message)
    {
        Assert.Null(result.Invitation);
        Assert.Equal([message], result.Errors[field]);
        Assert.Equal(0, _repository.SaveCount);
    }
}
