namespace TeamChat.DemoHost.Api.Auth;

/// <summary>Sign-up joins the organization with this name, or creates it.</summary>
public sealed record RegisterRequest(
    string OrganizationName,
    string DisplayName,
    string Email,
    string Password,
    string ConfirmPassword);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, CurrentUserResponse User);

public sealed record CurrentUserResponse(Guid Id, string Email, string DisplayName, Guid OrganizationId, string OrganizationName);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword, string ConfirmPassword);

public enum ChangePasswordStatus
{
    Changed = 0,
    UserNotFound = 1,
    Invalid = 2,
}

/// <param name="Errors">Messages by request field, in camelCase, when the status is <see cref="ChangePasswordStatus.Invalid"/>.</param>
public sealed record ChangePasswordResult(ChangePasswordStatus Status, IReadOnlyDictionary<string, string[]> Errors)
{
    public static ChangePasswordResult Changed { get; } = new(ChangePasswordStatus.Changed, new Dictionary<string, string[]>());

    public static ChangePasswordResult UserNotFound { get; } = new(ChangePasswordStatus.UserNotFound, new Dictionary<string, string[]>());

    public static ChangePasswordResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(ChangePasswordStatus.Invalid, errors);
}

/// <param name="Response">The new session, or <c>null</c> when <paramref name="Errors"/> is not empty.</param>
public sealed record RegisterResult(LoginResponse? Response, IReadOnlyDictionary<string, string[]> Errors)
{
    public static RegisterResult Registered(LoginResponse response) => new(response, new Dictionary<string, string[]>());

    public static RegisterResult Invalid(IReadOnlyDictionary<string, string[]> errors) => new(null, errors);
}
