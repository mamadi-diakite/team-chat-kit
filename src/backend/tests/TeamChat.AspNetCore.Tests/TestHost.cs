namespace TeamChat.AspNetCore.Tests;

/// <summary>
/// A host that is not the demo host: its own authentication, its own users, other route and table prefixes.
/// </summary>
public sealed class TestHost : IAsyncDisposable
{
    public const string RoutePrefix = "/team/chat";
    public const string TablePrefix = "tc_";

    private readonly SqliteConnection _keepAlive;
    private readonly WebApplication _app;

    private TestHost(WebApplication app, SqliteConnection keepAlive)
    {
        _app = app;
        _keepAlive = keepAlive;
    }

    public IServiceProvider Services => _app.Services;

    public static async Task<TestHost> StartAsync()
    {
        var connectionString = $"Data Source=host-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync();

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(HeaderAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, HeaderAuthenticationHandler>(HeaderAuthenticationHandler.SchemeName, null);
        builder.Services.AddAuthorization();
        builder.Services.AddTeamChat<TestIdentityAdapter>(options =>
        {
            options.ConnectionString = connectionString;
            options.RoutePrefix = RoutePrefix;
            options.TablePrefix = TablePrefix;
        });

        var app = builder.Build();
        await app.Services.ApplyTeamChatMigrationsAsync();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapTeamChat();
        await app.StartAsync();

        return new TestHost(app, keepAlive);
    }

    /// <param name="userId">Sent as the user id header; <c>null</c> for an anonymous client.</param>
    public HttpClient CreateClient(string? userId = null)
    {
        var client = _app.GetTestClient();
        if (userId is not null)
        {
            client.DefaultRequestHeaders.Add(HeaderAuthenticationHandler.UserHeader, userId);
        }

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();
        await _keepAlive.DisposeAsync();
    }
}

/// <summary>Trusts a header: enough to stand for any host authentication.</summary>
public sealed class HeaderAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Header";
    public const string UserHeader = "X-Test-User";
    public const string ClaimType = "uid";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = userId.ToString() == "no-id" ? [] : new[] { new Claim(ClaimType, userId.ToString()) };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}

/// <summary>A fixed set of users in three organizations.</summary>
public sealed class TestIdentityAdapter : IChatIdentityAdapter
{
    public static readonly ChatUser Ada = new("u-ada", "Ada North", "ada@example.com", "org-1", "Maple Works");
    public static readonly ChatUser Ben = new("u-ben", "Ben Okafor", "ben@example.com", "org-1", "Maple Works");
    public static readonly ChatUser Cleo = new("u-cleo", "Cleo Haddad", "cleo@example.com", "org-2", "River Labs");
    public static readonly ChatUser Dan = new("u-dan", "Dan Ruiz", "dan@example.com", "org-3", "Summit Foods");

    public static IReadOnlyList<ChatUser> Users { get; } = [Ada, Ben, Cleo, Dan];

    public string? GetUserId(ClaimsPrincipal principal) => principal.FindFirst(HeaderAuthenticationHandler.ClaimType)?.Value;

    public Task<IReadOnlyList<ChatUser>> FindUsersAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ChatUser>>([.. Users.Where(u => userIds.Contains(u.Id))]);

    public Task<ChatUser?> FindUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(u => u.Email == email));

    public Task<IReadOnlyList<ChatUser>> GetOrganizationMembersAsync(string organizationId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ChatUser>>([.. Users.Where(u => u.OrganizationId == organizationId)]);
}
