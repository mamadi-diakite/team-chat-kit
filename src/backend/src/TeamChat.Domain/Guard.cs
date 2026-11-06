namespace TeamChat.Domain;

internal static class Guard
{
    public static string Required(string? value, string paramName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", paramName);
        }

        return MaxLength(value.Trim(), paramName, maxLength);
    }

    public static string? Optional(string? value, string paramName, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : MaxLength(value.Trim(), paramName, maxLength);

    public static DateTime Utc(DateTime value, string paramName) =>
        value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Date must be in UTC.", paramName);

    private static string MaxLength(string value, string paramName, int maxLength) =>
        value.Length <= maxLength
            ? value
            : throw new ArgumentException($"Value must be at most {maxLength} characters.", paramName);
}
