namespace TeamChat.Domain.Tests;

public sealed class InvitationTests
{
    private static readonly DateTime Later = TestDates.Now.AddHours(2);

    [Fact]
    public void A_new_invitation_is_pending()
    {
        var invitation = new Invitation(" user-1 ", "user-2", "  Hello from Bluebird Design ", TestDates.Now);

        Assert.NotEqual(Guid.Empty, invitation.Id);
        Assert.Equal("user-1", invitation.SenderId);
        Assert.Equal("user-2", invitation.RecipientId);
        Assert.Equal("Hello from Bluebird Design", invitation.Message);
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Equal(TestDates.Now, invitation.CreatedAtUtc);
        Assert.Null(invitation.ClosedAtUtc);
    }

    [Fact]
    public void Message_is_optional()
    {
        Assert.Null(new Invitation("user-1", "user-2", " ", TestDates.Now).Message);
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("senderId", Assert.Throws<ArgumentException>(
            () => new Invitation("", "user-2", null, TestDates.Now)).ParamName);
        Assert.Equal("recipientId", Assert.Throws<ArgumentException>(
            () => new Invitation("user-1", " ", null, TestDates.Now)).ParamName);
        Assert.Equal("message", Assert.Throws<ArgumentException>(
            () => new Invitation("user-1", "user-2", new string('a', Invitation.MessageMaxLength + 1), TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new Invitation("user-1", "user-2", null, TestDates.Local)).ParamName);
    }

    [Fact]
    public void A_user_cannot_invite_themselves()
    {
        var error = Assert.Throws<ArgumentException>(() => new Invitation("user-1", " user-1", null, TestDates.Now));

        Assert.Equal("recipientId", error.ParamName);
    }

    [Fact]
    public void Accept_closes_the_invitation()
    {
        var invitation = new Invitation("user-1", "user-2", null, TestDates.Now);

        invitation.Accept(Later);

        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.Equal(Later, invitation.ClosedAtUtc);
    }

    [Fact]
    public void Decline_and_cancel_close_the_invitation()
    {
        var declined = new Invitation("user-1", "user-2", null, TestDates.Now);
        var cancelled = new Invitation("user-1", "user-2", null, TestDates.Now);

        declined.Decline(Later);
        cancelled.Cancel(Later);

        Assert.Equal(InvitationStatus.Declined, declined.Status);
        Assert.Equal(InvitationStatus.Cancelled, cancelled.Status);
        Assert.Equal(Later, cancelled.ClosedAtUtc);
    }

    [Fact]
    public void A_closed_invitation_cannot_change_again()
    {
        var invitation = new Invitation("user-1", "user-2", null, TestDates.Now);
        invitation.Decline(Later);

        Assert.Throws<InvalidOperationException>(() => invitation.Accept(Later));
        Assert.Throws<InvalidOperationException>(() => invitation.Cancel(Later));
        Assert.Equal(InvitationStatus.Declined, invitation.Status);
    }

    [Fact]
    public void Closing_needs_a_utc_date()
    {
        var invitation = new Invitation("user-1", "user-2", null, TestDates.Now);

        Assert.Throws<ArgumentException>(() => invitation.Accept(TestDates.Local));
        Assert.Equal(InvitationStatus.Pending, invitation.Status);
    }
}
