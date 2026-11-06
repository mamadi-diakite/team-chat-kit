namespace TeamChat.Infrastructure.Persistence;

/// <summary>The chat's own tables, kept apart from the host's model and migrations.</summary>
public sealed class ChatDbContext(DbContextOptions<ChatDbContext> options, ChatStorageOptions storage) : DbContext(options)
{
    public string TablePrefix => storage.TablePrefix;

    public DbSet<Invitation> Invitations => Set<Invitation>();

    public DbSet<Contact> Contacts => Set<Contact>();

    /// <summary>Applies the SQLite connection and the services that make the table prefix work.</summary>
    public static DbContextOptionsBuilder Configure(DbContextOptionsBuilder builder, ChatStorageOptions storage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(storage);

        return builder
            .UseSqlite(storage.ConnectionString, sqlite => sqlite.MigrationsHistoryTable(storage.MigrationsHistoryTable))
            .ReplaceService<IModelCacheKeyFactory, ChatModelCacheKeyFactory>()
            .ReplaceService<IMigrationsAssembly, ChatMigrationsAssembly>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new InvitationConfiguration(TablePrefix));
        modelBuilder.ApplyConfiguration(new ContactConfiguration(TablePrefix));
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}
