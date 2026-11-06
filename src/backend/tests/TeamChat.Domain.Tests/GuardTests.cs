namespace TeamChat.Domain.Tests;

public sealed class GuardTests
{
    [Fact]
    public void Required_trims_the_value()
    {
        Assert.Equal("value", Guard.Required("  value ", "name", 10));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Required_rejects_blank_values(string? value)
    {
        var error = Assert.Throws<ArgumentException>(() => Guard.Required(value, "name", 10));

        Assert.Equal("name", error.ParamName);
    }

    [Fact]
    public void Required_rejects_values_over_the_limit()
    {
        Assert.Throws<ArgumentException>(() => Guard.Required("too long", "name", 3));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Optional_turns_blank_values_into_null(string? value)
    {
        Assert.Null(Guard.Optional(value, "name", 10));
    }

    [Fact]
    public void Optional_trims_and_checks_the_limit()
    {
        Assert.Equal("ok", Guard.Optional(" ok ", "name", 10));
        Assert.Throws<ArgumentException>(() => Guard.Optional("too long", "name", 3));
    }

    [Fact]
    public void Utc_rejects_local_and_unspecified_dates()
    {
        Assert.Equal(TestDates.Now, Guard.Utc(TestDates.Now, "date"));
        Assert.Throws<ArgumentException>(() => Guard.Utc(TestDates.Local, "date"));
        Assert.Throws<ArgumentException>(() => Guard.Utc(DateTime.SpecifyKind(TestDates.Now, DateTimeKind.Unspecified), "date"));
    }

    [Fact]
    public void Chat_user_id_is_required_and_limited()
    {
        Assert.Equal("user-1", ChatUserId.Required(" user-1 ", "id"));
        Assert.Throws<ArgumentException>(() => ChatUserId.Required(" ", "id"));
        Assert.Throws<ArgumentException>(() => ChatUserId.Required(new string('a', ChatUserId.MaxLength + 1), "id"));
    }
}
