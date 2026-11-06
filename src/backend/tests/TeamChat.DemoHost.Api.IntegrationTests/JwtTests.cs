namespace TeamChat.DemoHost.Api.IntegrationTests;

public sealed class JwtSettingsTests
{
    [Fact]
    public void Reads_all_values_from_configuration()
    {
        var settings = JwtSettings.FromConfiguration(Configuration(
            ("Issuer", "issuer"), ("Audience", "audience"), ("AccessTokenMinutes", "15"),
            ("SigningKey", DemoHostApiFactory.TestSigningKey)));

        Assert.Equal("issuer", settings.Issuer);
        Assert.Equal("audience", settings.Audience);
        Assert.Equal(TimeSpan.FromMinutes(15), settings.AccessTokenLifetime);
        Assert.Equal(Convert.FromBase64String(DemoHostApiFactory.TestSigningKey), settings.SigningKey);
    }

    [Fact]
    public void Uses_defaults_for_everything_but_the_key()
    {
        var settings = JwtSettings.FromConfiguration(Configuration(("SigningKey", DemoHostApiFactory.TestSigningKey)));

        Assert.Equal("team-chat-kit", settings.Issuer);
        Assert.Equal("team-chat-kit", settings.Audience);
        Assert.Equal(TimeSpan.FromMinutes(60), settings.AccessTokenLifetime);
    }

    [Theory]
    [InlineData(null, "Jwt:SigningKey is missing")]
    [InlineData(" ", "Jwt:SigningKey is missing")]
    [InlineData("not base64!", "must be a base64 string")]
    [InlineData("c2hvcnQ=", "at least 32 bytes")]
    public void Refuses_a_missing_or_invalid_key(string? key, string message)
    {
        var configuration = key is null ? Configuration() : Configuration(("SigningKey", key));

        var error = Assert.Throws<InvalidOperationException>(() => JwtSettings.FromConfiguration(configuration));

        Assert.Contains(message, error.Message);
    }

    [Fact]
    public void Validation_parameters_match_the_settings()
    {
        var settings = JwtSettings.FromConfiguration(Configuration(("SigningKey", DemoHostApiFactory.TestSigningKey)));

        var parameters = settings.ToValidationParameters();

        Assert.Equal(settings.Issuer, parameters.ValidIssuer);
        Assert.Equal(settings.Audience, parameters.ValidAudience);
        Assert.Equal(JwtClaims.Name, parameters.NameClaimType);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>($"Jwt:{v.Key}", v.Value)))
            .Build();
}

public sealed class JwtAccessTokenIssuerTests
{
    private static readonly JwtSettings Settings = new()
    {
        Issuer = "issuer",
        Audience = "audience",
        AccessTokenLifetime = TimeSpan.FromMinutes(30),
        SigningKey = Convert.FromBase64String(DemoHostApiFactory.TestSigningKey),
    };

    private static readonly DateTimeOffset Now = new(2026, 11, 6, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Issued_token_is_valid_and_carries_the_user_claims()
    {
        var user = new UserAccount("emma@example.com", "Emma Laurent", "hash", Guid.NewGuid(), DateTime.UtcNow);
        var issuer = new JwtAccessTokenIssuer(Settings, TimeProvider.System);

        var token = issuer.Issue(user);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.Value, Settings.ToValidationParameters());

        Assert.True(result.IsValid);
        Assert.Equal(user.Id.ToString(), result.ClaimsIdentity.FindFirst(JwtClaims.Subject)?.Value);
        Assert.Equal("emma@example.com", result.ClaimsIdentity.FindFirst(JwtClaims.Email)?.Value);
        Assert.Equal(user.OrganizationId.ToString(), result.ClaimsIdentity.FindFirst(JwtClaims.Organization)?.Value);
        Assert.Equal("Emma Laurent", result.ClaimsIdentity.Name);
    }

    [Fact]
    public void Expiry_follows_the_configured_lifetime()
    {
        var issuer = new JwtAccessTokenIssuer(Settings, new FixedTimeProvider(Now));
        var user = new UserAccount("emma@example.com", "Emma", "hash", Guid.NewGuid(), Now.UtcDateTime);

        Assert.Equal(Now.UtcDateTime.AddMinutes(30), issuer.Issue(user).ExpiresAtUtc);
    }

    [Fact]
    public void Rejects_a_missing_user()
    {
        Assert.Throws<ArgumentNullException>(() => new JwtAccessTokenIssuer(Settings, TimeProvider.System).Issue(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public void User_id_is_null_without_a_valid_subject(string? subject)
    {
        var claims = subject is null ? [] : new[] { new Claim(JwtClaims.Subject, subject) };

        Assert.Null(new ClaimsPrincipal(new ClaimsIdentity(claims)).GetUserId());
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
