namespace TeamChat.DemoHost.Api.Auth;

public sealed class AuthService(
    DemoHostDbContext db,
    IdentityPasswordHasher passwordHasher,
    JwtAccessTokenIssuer tokenIssuer,
    TimeProvider timeProvider)
{
    public async Task<RegisterResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.OrganizationName))
        {
            errors["organizationName"] = ["Organization is required."];
        }
        else if (request.OrganizationName.Trim().Length > Organization.NameMaxLength)
        {
            errors["organizationName"] = [$"Use at most {Organization.NameMaxLength} characters."];
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            errors["displayName"] = ["Name is required."];
        }
        else if (request.DisplayName.Trim().Length > UserAccount.DisplayNameMaxLength)
        {
            errors["displayName"] = [$"Use at most {UserAccount.DisplayNameMaxLength} characters."];
        }

        string? email = null;
        if (!UserAccount.IsValidEmail(request.Email))
        {
            errors["email"] = ["Enter a valid email address."];
        }
        else
        {
            email = request.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email, cancellationToken))
            {
                errors["email"] = ["An account already uses this email."];
            }
        }

        AddNewPasswordErrors(errors, "password", request.Password, request.ConfirmPassword);

        if (errors.Count > 0)
        {
            return RegisterResult.Invalid(errors);
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedName = Organization.Normalize(request.OrganizationName);
        var organization = await db.Organizations.SingleOrDefaultAsync(o => o.NormalizedName == normalizedName, cancellationToken);
        if (organization is null)
        {
            organization = new Organization(request.OrganizationName, now);
            db.Organizations.Add(organization);
        }

        var user = new UserAccount(email!, request.DisplayName, passwordHasher.Hash(request.Password), organization.Id, now);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return RegisterResult.Registered(CreateSession(user, organization));
    }

    /// <returns><c>null</c> when the email or the password is wrong, without telling which.</returns>
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return null;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.Include(u => u.Organization).SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            // Hash anyway, so an unknown email takes as long as a wrong password.
            passwordHasher.Hash(request.Password);
            return null;
        }

        return passwordHasher.Verify(user.PasswordHash, request.Password) ? CreateSession(user, user.Organization!) : null;
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.Include(u => u.Organization).SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? null : ToResponse(user, user.Organization!);
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return ChangePasswordResult.UserNotFound;
        }

        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(request.CurrentPassword) || !passwordHasher.Verify(user.PasswordHash, request.CurrentPassword))
        {
            errors["currentPassword"] = ["Current password is not correct."];
        }

        AddNewPasswordErrors(errors, "newPassword", request.NewPassword, request.ConfirmPassword);
        if (!errors.ContainsKey("newPassword") && request.NewPassword == request.CurrentPassword)
        {
            errors["newPassword"] = ["New password must be different from the current one."];
        }

        if (errors.Count > 0)
        {
            return ChangePasswordResult.Invalid(errors);
        }

        user.ChangePassword(passwordHasher.Hash(request.NewPassword));
        await db.SaveChangesAsync(cancellationToken);

        return ChangePasswordResult.Changed;
    }

    private static void AddNewPasswordErrors(
        Dictionary<string, string[]> errors,
        string passwordField,
        string password,
        string confirmPassword)
    {
        var policyErrors = PasswordPolicy.Check(password);
        if (policyErrors.Count > 0)
        {
            errors[passwordField] = [.. policyErrors];
        }

        if (confirmPassword != password)
        {
            errors["confirmPassword"] = ["Confirmation does not match the password."];
        }
    }

    private LoginResponse CreateSession(UserAccount user, Organization organization)
    {
        var token = tokenIssuer.Issue(user);

        return new LoginResponse(token.Value, token.ExpiresAtUtc, ToResponse(user, organization));
    }

    private static CurrentUserResponse ToResponse(UserAccount user, Organization organization) =>
        new(user.Id, user.Email, user.DisplayName, organization.Id, organization.Name);
}
