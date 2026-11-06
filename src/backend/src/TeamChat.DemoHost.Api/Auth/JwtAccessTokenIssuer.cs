namespace TeamChat.DemoHost.Api.Auth;

public sealed class JwtAccessTokenIssuer(JwtSettings settings, TimeProvider timeProvider)
{
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken Issue(UserAccount user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expiresAt = now + settings.AccessTokenLifetime;

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAt,
            Claims = new Dictionary<string, object>
            {
                [JwtClaims.Subject] = user.Id.ToString(),
                [JwtClaims.Email] = user.Email,
                [JwtClaims.Name] = user.DisplayName,
                [JwtClaims.Organization] = user.OrganizationId.ToString(),
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(settings.SigningKey), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }
}

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
