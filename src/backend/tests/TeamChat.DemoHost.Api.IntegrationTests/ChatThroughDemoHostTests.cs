namespace TeamChat.DemoHost.Api.IntegrationTests;

/// <summary>The chat module behind the demo host: host JWT, host users, chat tables in the host database.</summary>
public sealed class ChatThroughDemoHostTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    [Fact]
    public async Task Directory_lists_the_colleagues_of_the_signed_in_user()
    {
        var emma = await LoggedInClientAsync("emma.laurent@example.com");

        var directory = await emma.GetFromJsonAsync<List<ChatUserResponse>>("/api/chat/directory?search=e");

        Assert.Equal(["Chloe Petit", "Hugo Bernard"], directory!.Select(u => u.DisplayName));
    }

    [Fact]
    public async Task People_of_two_organizations_become_contacts_through_an_invitation()
    {
        var hugo = await LoggedInClientAsync("hugo.bernard@example.com");
        var noah = await LoggedInClientAsync("noah.girard@example.com");

        var sent = await hugo.PostAsJsonAsync("/api/chat/invitations", new { email = "Noah.Girard@example.com", message = "Hi Noah" });
        Assert.Equal(HttpStatusCode.Created, sent.StatusCode);
        var invitations = await noah.GetFromJsonAsync<InvitationsView>("/api/chat/invitations");
        var received = Assert.Single(invitations!.Received);
        Assert.Equal(("Hugo Bernard", "Bluebird Design"), (received.User.DisplayName, received.User.OrganizationName));

        Assert.Equal(HttpStatusCode.NoContent, (await noah.PostAsync($"/api/chat/invitations/{received.Id}/accept", null)).StatusCode);

        var contacts = await hugo.GetFromJsonAsync<List<ContactView>>("/api/chat/contacts");
        Assert.Equal("Noah Girard", Assert.Single(contacts!).User.DisplayName);
    }

    private async Task<HttpClient> LoggedInClientAsync(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, DemoDataSeeder.DemoPassword));
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    private sealed record InvitationView(Guid Id, ChatUserResponse User, string? Message);

    private sealed record InvitationsView(List<InvitationView> Received, List<InvitationView> Sent);

    private sealed record ContactView(ChatUserResponse User);
}
