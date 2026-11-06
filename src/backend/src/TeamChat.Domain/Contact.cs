namespace TeamChat.Domain;

/// <summary>Two users of different organizations who accepted to talk to each other.</summary>
/// <remarks>The ids are stored in ordinal order, so a pair of users has a single row.</remarks>
public sealed class Contact
{
    private Contact()
    {
    }

    public Contact(string userId, string otherUserId, DateTime createdAtUtc)
    {
        var first = ChatUserId.Required(userId, nameof(userId));
        var second = ChatUserId.Required(otherUserId, nameof(otherUserId));
        if (first == second)
        {
            throw new ArgumentException("A user cannot be their own contact.", nameof(otherUserId));
        }

        (FirstUserId, SecondUserId) = Pair(first, second);
        CreatedAtUtc = Guard.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public string FirstUserId { get; private set; } = string.Empty;

    public string SecondUserId { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Orders two user ids the way a contact stores them.</summary>
    public static (string First, string Second) Pair(string userId, string otherUserId) =>
        string.CompareOrdinal(userId, otherUserId) <= 0 ? (userId, otherUserId) : (otherUserId, userId);

    /// <exception cref="ArgumentException">The user is not part of this contact.</exception>
    public string OtherUserId(string userId) =>
        userId == FirstUserId ? SecondUserId
        : userId == SecondUserId ? FirstUserId
        : throw new ArgumentException("The user is not part of this contact.", nameof(userId));
}
