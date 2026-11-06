namespace TeamChat.AspNetCore.Tests;

public sealed class ContactEndpointsTests : IAsyncLifetime
{
    private const string Prefix = TestHost.RoutePrefix;

    private TestHost _host = null!;

    public async Task InitializeAsync() => _host = await TestHost.StartAsync();

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Fact]
    public async Task Directory_lists_my_organization_and_searches_by_name()
    {
        var client = _host.CreateClient(TestIdentityAdapter.Ada.Id);

        var all = await client.GetFromJsonAsync<List<ChatUserResponse>>($"{Prefix}/directory");
        var none = await client.GetFromJsonAsync<List<ChatUserResponse>>($"{Prefix}/directory?search=zzz");

        Assert.Equal(["Ben Okafor"], all!.Select(u => u.DisplayName));
        Assert.Empty(none!);
    }

    [Fact]
    public async Task Invitation_accepted_makes_contacts_on_both_sides()
    {
        var ada = _host.CreateClient(TestIdentityAdapter.Ada.Id);
        var cleo = _host.CreateClient(TestIdentityAdapter.Cleo.Id);

        var sent = await ada.PostAsJsonAsync($"{Prefix}/invitations", new SendInvitationRequest("cleo@example.com", "Hello"));
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        var invitation = await sent.Content.ReadFromJsonAsync<InvitationResponse>();

        var received = await cleo.GetFromJsonAsync<InvitationsResponse>($"{Prefix}/invitations");
        Assert.Equal(("Ada North", "Hello"), (received!.Received.Single().User.DisplayName, received.Received.Single().Message));
        Assert.Empty(received.Sent);

        var accepted = await cleo.PostAsync($"{Prefix}/invitations/{invitation!.Id}/accept", null);
        Assert.Equal(HttpStatusCode.NoContent, accepted.StatusCode);

        var adaContacts = await ada.GetFromJsonAsync<List<ContactResponse>>($"{Prefix}/contacts");
        var cleoContacts = await cleo.GetFromJsonAsync<List<ContactResponse>>($"{Prefix}/contacts");
        Assert.Equal("Cleo Haddad", adaContacts!.Single().User.DisplayName);
        Assert.Equal("Ada North", cleoContacts!.Single().User.DisplayName);
        Assert.Empty((await ada.GetFromJsonAsync<InvitationsResponse>($"{Prefix}/invitations"))!.Sent);
    }

    [Fact]
    public async Task Invitation_errors_are_validation_problems()
    {
        var ada = _host.CreateClient(TestIdentityAdapter.Ada.Id);

        var unknown = await ada.PostAsJsonAsync($"{Prefix}/invitations", new SendInvitationRequest("nobody@example.com", null));
        var colleague = await ada.PostAsJsonAsync($"{Prefix}/invitations", new SendInvitationRequest("ben@example.com", null));

        Assert.Equal(HttpStatusCode.BadRequest, unknown.StatusCode);
        Assert.Equal(["No account uses this email."], (await ReadProblemAsync(unknown)).Errors["email"]);
        Assert.Equal(
            ["This person is in your organization: find them in the directory."],
            (await ReadProblemAsync(colleague)).Errors["email"]);
    }

    [Fact]
    public async Task Decline_cancel_and_answers_to_closed_or_unknown_invitations()
    {
        var ada = _host.CreateClient(TestIdentityAdapter.Ada.Id);
        var dan = _host.CreateClient(TestIdentityAdapter.Dan.Id);
        var toDan = await SendAsync(ada, "dan@example.com");
        var toCleo = await SendAsync(ada, "cleo@example.com");

        Assert.Equal(HttpStatusCode.NoContent, (await dan.PostAsync($"{Prefix}/invitations/{toDan}/decline", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await ada.PostAsync($"{Prefix}/invitations/{toCleo}/cancel", null)).StatusCode);

        var again = await dan.PostAsync($"{Prefix}/invitations/{toDan}/accept", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ada.PostAsync($"{Prefix}/invitations/{toDan}/accept", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ada.PostAsync($"{Prefix}/invitations/{Guid.NewGuid()}/cancel", null)).StatusCode);
        Assert.Empty((await dan.GetFromJsonAsync<List<ContactResponse>>($"{Prefix}/contacts"))!);
    }

    [Fact]
    public async Task Removing_a_contact_ends_it_for_both_people()
    {
        var ada = _host.CreateClient(TestIdentityAdapter.Ada.Id);
        var cleo = _host.CreateClient(TestIdentityAdapter.Cleo.Id);
        var invitationId = await SendAsync(ada, "cleo@example.com");
        await cleo.PostAsync($"{Prefix}/invitations/{invitationId}/accept", null);

        Assert.Equal(HttpStatusCode.NoContent, (await cleo.DeleteAsync($"{Prefix}/contacts/{TestIdentityAdapter.Ada.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await ada.DeleteAsync($"{Prefix}/contacts/{TestIdentityAdapter.Cleo.Id}")).StatusCode);
        Assert.Empty((await ada.GetFromJsonAsync<List<ContactResponse>>($"{Prefix}/contacts"))!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("u-unknown")]
    public async Task Every_endpoint_needs_a_user_known_by_the_host(string? userId)
    {
        var client = _host.CreateClient(userId);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Prefix}/directory")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Prefix}/invitations")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            $"{Prefix}/invitations", new SendInvitationRequest("cleo@example.com", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync($"{Prefix}/contacts")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync($"{Prefix}/contacts/u-cleo")).StatusCode);
    }

    private static async Task<Guid> SendAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync($"{Prefix}/invitations", new SendInvitationRequest(email, null));
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<InvitationResponse>())!.Id;
    }

    private static async Task<HttpValidationProblemDetails> ReadProblemAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>())!;
}
