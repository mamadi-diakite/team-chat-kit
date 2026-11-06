namespace TeamChat.DemoHost.Api.Accounts;

/// <summary>A company of the demo host. Its members see each other in the chat directory.</summary>
public sealed class Organization
{
    public const int NameMaxLength = 150;

    private Organization()
    {
    }

    public Organization(string name, DateTime createdAtUtc)
    {
        Id = Guid.CreateVersion7();
        Name = Guard.Required(name, nameof(name), NameMaxLength);
        NormalizedName = Normalize(Name);
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    /// <summary>Lowercase name, unique, so "Bluebird Design" and "bluebird design" are the same organization.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public static string Normalize(string name) => name.Trim().ToLowerInvariant();
}
