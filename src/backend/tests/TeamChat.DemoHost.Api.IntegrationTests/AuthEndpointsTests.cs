namespace TeamChat.DemoHost.Api.IntegrationTests;

public sealed class AuthEndpointsTests(DemoDataApiFactory factory) : IClassFixture<DemoDataApiFactory>
{
    private const string DemoPassword = DemoDataSeeder.DemoPassword;

    [Fact]
    public async Task Register_creates_an_account_in_a_new_organization_and_returns_a_session()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Willow Books", "Jade Roux", "jade.roux@example.com", "NewPass456", "NewPass456"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/auth/me", response.Headers.Location?.AbsolutePath);
        var session = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("jade.roux@example.com", session?.User.Email);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);
        var me = await client.GetFromJsonAsync<CurrentUserResponse>("/api/auth/me");
        Assert.Equal("Jade Roux", me?.DisplayName);
        Assert.Equal("Willow Books", me?.OrganizationName);
    }

    [Fact]
    public async Task Register_joins_an_existing_organization_whatever_the_case()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(" harbor LOGISTICS ", "Tom Leroy", "tom.leroy@example.com", "NewPass456", "NewPass456"));

        var session = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.Equal("Harbor Logistics", session?.User.OrganizationName);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DemoHostDbContext>();
        Assert.Equal(1, await db.Organizations.CountAsync(o => o.NormalizedName == "harbor logistics"));
    }

    [Fact]
    public async Task Register_with_errors_returns_a_validation_problem()
    {
        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("", " ", "emma.laurent@example.com", "short", "other"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["confirmPassword", "displayName", "email", "organizationName", "password"], problem!.Errors.Keys.Order());
        Assert.Equal(["An account already uses this email."], problem.Errors["email"]);
    }

    [Fact]
    public async Task Register_limits_lengths_and_checks_the_email_shape()
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            new string('o', Organization.NameMaxLength + 1),
            new string('n', UserAccount.DisplayNameMaxLength + 1),
            "not-an-email",
            "NewPass456",
            "NewPass456"));

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal([$"Use at most {Organization.NameMaxLength} characters."], problem!.Errors["organizationName"]);
        Assert.Equal([$"Use at most {UserAccount.DisplayNameMaxLength} characters."], problem.Errors["displayName"]);
        Assert.Equal(["Enter a valid email address."], problem.Errors["email"]);
    }

    [Fact]
    public async Task Login_with_a_demo_account_returns_a_token()
    {
        var response = await factory.CreateClient()
            .PostAsJsonAsync("/api/auth/login", new LoginRequest("Emma.Laurent@Example.com", DemoPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.False(string.IsNullOrEmpty(body?.AccessToken));
        Assert.Equal("emma.laurent@example.com", body!.User.Email);
        Assert.Equal("Bluebird Design", body.User.OrganizationName);
    }

    [Theory]
    [InlineData("emma.laurent@example.com", "WrongPass123")]
    [InlineData("nobody@example.com", DemoPassword)]
    [InlineData("", "")]
    [InlineData("emma.laurent@example.com", "")]
    public async Task Login_with_bad_credentials_returns_401(string email, string password)
    {
        var response = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Email or password is not correct.", problem?.Title);
    }

    [Fact]
    public async Task Protected_endpoints_need_a_token()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/chat/me")).StatusCode);
    }

    [Fact]
    public async Task A_token_without_a_user_id_is_refused()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenWithoutSubject());

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/chat/me")).StatusCode);
    }

    [Fact]
    public async Task A_token_of_a_deleted_user_is_refused()
    {
        const string email = "temporary@example.com";
        var client = await RegisteredClientAsync(email);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DemoHostDbContext>();
            db.Users.Remove(db.Users.Single(u => u.Email == email));
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/chat/me")).StatusCode);
    }

    [Fact]
    public async Task The_chat_knows_the_user_of_a_host_token()
    {
        var client = await LoggedInClientAsync("liam.moreau@example.com");

        var user = await client.GetFromJsonAsync<ChatUserResponse>("/api/chat/me");

        Assert.Equal("Liam Moreau", user?.DisplayName);
        Assert.Equal("Harbor Logistics", user?.OrganizationName);
    }

    [Fact]
    public async Task Change_password_with_errors_returns_a_validation_problem()
    {
        var client = await LoggedInClientAsync("sara.dubois@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest("WrongPass789", "short", "other"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Equal(["confirmPassword", "currentPassword", "newPassword"], problem!.Errors.Keys.Order());
    }

    [Fact]
    public async Task New_password_must_differ_from_the_current_one()
    {
        var client = await LoggedInClientAsync("sara.dubois@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest("", DemoPassword, DemoPassword));
        var same = await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, DemoPassword, DemoPassword));

        Assert.Equal(["currentPassword"], (await response.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys);
        Assert.Equal(
            ["New password must be different from the current one."],
            (await same.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors["newPassword"]);
    }

    [Fact]
    public async Task Changed_password_replaces_the_old_one()
    {
        var client = await RegisteredClientAsync("lea.marchand@example.com");

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password", new ChangePasswordRequest(DemoPassword, "NewPass456", "NewPass456"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("lea.marchand@example.com", DemoPassword))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await anonymous.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("lea.marchand@example.com", "NewPass456"))).StatusCode);
    }

    [Fact]
    public async Task Service_rejects_missing_requests()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AuthService>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => service.RegisterAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.LoginAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.ChangePasswordAsync(Guid.NewGuid(), null!));
    }

    private async Task<HttpClient> RegisteredClientAsync(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest("Test Organization", "Test User", email, DemoPassword, DemoPassword));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    private async Task<HttpClient> LoggedInClientAsync(string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, DemoPassword));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);

        return client;
    }

    private string TokenWithoutSubject()
    {
        var settings = factory.Services.GetRequiredService<JwtSettings>();

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Claims = new Dictionary<string, object> { [JwtClaims.Email] = "ghost@example.com" },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
        });
    }
}
