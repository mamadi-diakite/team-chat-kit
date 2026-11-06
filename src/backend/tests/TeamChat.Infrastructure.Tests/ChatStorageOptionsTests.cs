namespace TeamChat.Infrastructure.Tests;

public sealed class ChatStorageOptionsTests
{
    [Fact]
    public void Uses_the_default_prefix()
    {
        var storage = new ChatStorageOptions("Data Source=chat.db");

        Assert.Equal("Data Source=chat.db", storage.ConnectionString);
        Assert.Equal("Chat", storage.TablePrefix);
        Assert.Equal("ChatMigrationsHistory", storage.MigrationsHistoryTable);
    }

    [Theory]
    [InlineData("")]
    [InlineData("tc_")]
    [InlineData("Team_Chat_2")]
    public void Accepts_letters_digits_and_underscores(string tablePrefix)
    {
        Assert.Equal(tablePrefix, new ChatStorageOptions("Data Source=chat.db", tablePrefix).TablePrefix);
    }

    [Theory]
    [InlineData("tc-")]
    [InlineData("team chat")]
    [InlineData("chat;drop")]
    [InlineData("abcdefghijklmnopqrstuvwxyz12345")]
    public void Rejects_other_prefixes(string tablePrefix)
    {
        var error = Assert.Throws<ArgumentException>(() => new ChatStorageOptions("Data Source=chat.db", tablePrefix));

        Assert.Equal("tablePrefix", error.ParamName);
    }

    [Fact]
    public void Rejects_missing_values()
    {
        Assert.Throws<ArgumentException>(() => new ChatStorageOptions(" "));
        Assert.Throws<ArgumentNullException>(() => new ChatStorageOptions(null!));
        Assert.Throws<ArgumentNullException>(() => new ChatStorageOptions("Data Source=chat.db", null!));
    }
}
