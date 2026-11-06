namespace TeamChat.Infrastructure.Tests;

public sealed class TablePrefixTests
{
    [Fact]
    public void Default_prefix_names_every_chat_table()
    {
        using var database = new SqliteTestDatabase();

        Assert.Equal(
            [
                "ChatContacts", "ChatInvitations", "ChatMigrationsHistory",
                "IX_ChatContacts_SecondUserId", "IX_ChatInvitations_RecipientId_Status", "IX_ChatInvitations_SenderId_RecipientId",
            ],
            database.TableNames());
    }

    [Fact]
    public async Task A_custom_prefix_names_tables_and_indexes_and_leaves_no_pending_change()
    {
        using var database = new SqliteTestDatabase("tc_");

        Assert.Equal(
            [
                "IX_tc_Contacts_SecondUserId", "IX_tc_Invitations_RecipientId_Status", "IX_tc_Invitations_SenderId_RecipientId",
                "tc_Contacts", "tc_Invitations", "tc_MigrationsHistory",
            ],
            database.TableNames());
        await using var db = database.CreateContext();
        Assert.False(db.Database.HasPendingModelChanges());
        db.Contacts.Add(new Contact("user-1", "user-2", DateTime.UtcNow));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Two_prefixes_share_one_database_without_mixing_data()
    {
        using var database = new SqliteTestDatabase();
        database.Migrate("Other");
        var other = new ChatStorageOptions(database.ConnectionString, "Other");

        await using (var db = SqliteTestDatabase.CreateContext(other))
        {
            db.Contacts.Add(new Contact("user-1", "user-2", DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        await using var defaultContext = database.CreateContext();
        await using var otherContext = SqliteTestDatabase.CreateContext(other);
        Assert.Equal(0, await defaultContext.Contacts.CountAsync());
        Assert.Equal(1, await otherContext.Contacts.CountAsync());
        Assert.Equal("OtherContacts", otherContext.Model.FindEntityType(typeof(Contact))!.GetTableName());
        Assert.Contains("OtherInvitations", database.TableNames());
    }

    [Fact]
    public void Migrations_can_be_rolled_back_with_a_prefix()
    {
        using var database = new SqliteTestDatabase("tc_");
        using var db = database.CreateContext();

        db.GetService<IMigrator>().Migrate(Migration.InitialDatabase);

        Assert.Equal(["tc_MigrationsHistory"], database.TableNames());
    }
}
