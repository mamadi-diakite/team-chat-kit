namespace TeamChat.Domain.Tests;

public sealed class ContactTests
{
    [Fact]
    public void Stores_the_pair_in_ordinal_order()
    {
        var contact = new Contact("user-b", " user-a ", TestDates.Now);

        Assert.Equal("user-a", contact.FirstUserId);
        Assert.Equal("user-b", contact.SecondUserId);
        Assert.Equal(TestDates.Now, contact.CreatedAtUtc);
    }

    [Theory]
    [InlineData("a", "b")]
    [InlineData("b", "a")]
    [InlineData("B", "a")]
    public void Pair_gives_the_same_order_both_ways(string userId, string otherUserId)
    {
        var expected = string.CompareOrdinal(userId, otherUserId) < 0 ? (userId, otherUserId) : (otherUserId, userId);

        Assert.Equal(expected, Contact.Pair(userId, otherUserId));
        Assert.Equal(expected, Contact.Pair(otherUserId, userId));
    }

    [Fact]
    public void Other_user_id_gives_the_other_side()
    {
        var contact = new Contact("user-a", "user-b", TestDates.Now);

        Assert.Equal("user-b", contact.OtherUserId("user-a"));
        Assert.Equal("user-a", contact.OtherUserId("user-b"));
        Assert.Throws<ArgumentException>(() => contact.OtherUserId("user-c"));
    }

    [Fact]
    public void Rejects_invalid_values()
    {
        Assert.Equal("userId", Assert.Throws<ArgumentException>(
            () => new Contact(" ", "user-b", TestDates.Now)).ParamName);
        Assert.Equal("otherUserId", Assert.Throws<ArgumentException>(
            () => new Contact("user-a", "", TestDates.Now)).ParamName);
        Assert.Equal("otherUserId", Assert.Throws<ArgumentException>(
            () => new Contact("user-a", "user-a", TestDates.Now)).ParamName);
        Assert.Equal("createdAtUtc", Assert.Throws<ArgumentException>(
            () => new Contact("user-a", "user-b", TestDates.Local)).ParamName);
    }
}
