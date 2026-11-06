namespace TeamChat.DemoHost.Api.Accounts;

internal static class Guard
{
    public static string Required(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        var trimmed = value.Trim();

        return trimmed.Length <= maxLength
            ? trimmed
            : throw new ArgumentException($"Value must be at most {maxLength} characters.", paramName);
    }

    public static string Email(string? value, string paramName, int maxLength)
    {
        var email = Required(value, paramName, maxLength).ToLowerInvariant();

        return HasEmailShape(email) ? email : throw new ArgumentException("Email is not valid.", paramName);
    }

    public static bool IsEmail([NotNullWhen(true)] string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Trim().Length <= maxLength
        && HasEmailShape(value.Trim().ToLowerInvariant());

    public static Guid NotEmpty(Guid value, string paramName) =>
        value == Guid.Empty ? throw new ArgumentException("Id is required.", paramName) : value;

    public static DateTime Utc(DateTime value, string paramName) =>
        value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Date must be in UTC.", paramName);

    private static bool HasEmailShape(string email)
    {
        var at = email.IndexOf('@');
        var domain = at < 0 ? string.Empty : email[(at + 1)..];

        return at > 0
            && at == email.LastIndexOf('@')
            && !email.Any(char.IsWhiteSpace)
            && domain.Contains('.')
            && !domain.StartsWith('.')
            && !domain.EndsWith('.');
    }
}
