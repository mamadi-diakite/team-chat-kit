namespace TeamChat.DemoHost.Api.IntegrationTests;

public sealed class AccountTests
{
    private static readonly DateTime Now = new(2026, 11, 6, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Local = DateTime.SpecifyKind(Now, DateTimeKind.Local);
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void Creates_an_organization_with_a_normalized_name()
    {
        var organization = new Organization("  Bluebird Design ", Now);

        Assert.NotEqual(Guid.Empty, organization.Id);
        Assert.Equal("Bluebird Design", organization.Name);
        Assert.Equal("bluebird design", organization.NormalizedName);
        Assert.Equal(Now, organization.CreatedAtUtc);
    }

    [Fact]
    public void Organization_rejects_invalid_values()
    {
        Assert.Equal("name", Assert.Throws<ArgumentException>(() => new Organization(" ", Now)).ParamName);
        Assert.Equal("name", Assert.Throws<ArgumentException>(
            () => new Organization(new string('a', Organization.NameMaxLength + 1), Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(() => new Organization("Bluebird", Local)).ParamName);
    }

    [Fact]
    public void Creates_a_user_in_an_organization()
    {
        var user = new UserAccount("  Emma.Laurent@Example.com ", " Emma Laurent ", "hash", OrganizationId, Now);

        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("emma.laurent@example.com", user.Email);
        Assert.Equal("Emma Laurent", user.DisplayName);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Equal(OrganizationId, user.OrganizationId);
        Assert.Equal(Now, user.CreatedAtUtc);
    }

    [Fact]
    public void User_rejects_invalid_values()
    {
        Assert.Equal("email", Assert.Throws<ArgumentException>(
            () => new UserAccount("emma@", "Emma", "hash", OrganizationId, Now)).ParamName);
        Assert.Equal("displayName", Assert.Throws<ArgumentException>(
            () => new UserAccount("emma@example.com", " ", "hash", OrganizationId, Now)).ParamName);
        Assert.Equal("passwordHash", Assert.Throws<ArgumentException>(
            () => new UserAccount("emma@example.com", "Emma", "", OrganizationId, Now)).ParamName);
        Assert.Equal("organizationId", Assert.Throws<ArgumentException>(
            () => new UserAccount("emma@example.com", "Emma", "hash", Guid.Empty, Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new UserAccount("emma@example.com", "Emma", "hash", OrganizationId, Local)).ParamName);
    }

    [Theory]
    [InlineData("no-at-sign.example.com")]
    [InlineData("@example.com")]
    [InlineData("emma@")]
    [InlineData("emma@example")]
    [InlineData("emma@.example.com")]
    [InlineData("emma@example.com.")]
    [InlineData("emma@@example.com")]
    [InlineData("em ma@example.com")]
    public void User_rejects_invalid_email_shapes(string email)
    {
        Assert.Throws<ArgumentException>(() => new UserAccount(email, "Emma", "hash", OrganizationId, Now));
    }

    [Theory]
    [InlineData(" Emma@Example.com ", true)]
    [InlineData("emma@", false)]
    [InlineData(null, false)]
    [InlineData(" ", false)]
    public void Is_valid_email_matches_the_constructor_rule(string? email, bool expected)
    {
        Assert.Equal(expected, UserAccount.IsValidEmail(email));
        Assert.False(UserAccount.IsValidEmail(new string('a', UserAccount.EmailMaxLength) + "@example.com"));
    }

    [Fact]
    public void Change_password_replaces_the_hash()
    {
        var user = new UserAccount("emma@example.com", "Emma", "old-hash", OrganizationId, Now);

        user.ChangePassword("new-hash");

        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Throws<ArgumentException>(() => user.ChangePassword(" "));
        Assert.Equal("new-hash", user.PasswordHash);
    }

    [Theory]
    [InlineData("TestDemo123!", new string[0])]
    [InlineData(null, new[] { "Password is required." })]
    [InlineData("short1", new[] { "Use at least 8 characters." })]
    [InlineData("onlyletters", new[] { "Use at least one digit." })]
    [InlineData("12345678", new[] { "Use at least one letter." })]
    public void Password_policy_lists_every_problem(string? password, string[] expected)
    {
        Assert.Equal(expected, PasswordPolicy.Check(password));
    }

    [Fact]
    public void Password_policy_limits_the_length()
    {
        Assert.Equal(["Use at most 128 characters."], PasswordPolicy.Check(new string('a', 128) + "1"));
    }

    [Fact]
    public void Identity_hasher_verifies_its_own_hashes()
    {
        var hasher = new IdentityPasswordHasher();

        var hash = hasher.Hash("TestDemo123!");

        Assert.NotEqual("TestDemo123!", hash);
        Assert.True(hasher.Verify(hash, "TestDemo123!"));
        Assert.False(hasher.Verify(hash, "Wrong123!"));
        Assert.Throws<ArgumentException>(() => hasher.Hash(""));
        Assert.Throws<ArgumentException>(() => hasher.Verify("", "TestDemo123!"));
        Assert.Throws<ArgumentException>(() => hasher.Verify(hash, ""));
    }
}
