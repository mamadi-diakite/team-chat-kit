namespace TeamChat.Infrastructure.Persistence;

#pragma warning disable EF1001 // EF Core has no public hook to pass settings to migrations; MigrationsAssembly is the documented extension point.

/// <summary>Gives the configured table prefix to the chat migrations and to the models they were generated with.</summary>
internal sealed class ChatMigrationsAssembly(
    ICurrentDbContext currentContext,
    IDbContextOptions options,
    IMigrationsIdGenerator idGenerator,
    IDiagnosticsLogger<DbLoggerCategory.Migrations> logger)
    : Microsoft.EntityFrameworkCore.Migrations.Internal.MigrationsAssembly(currentContext, options, idGenerator, logger)
{
    private readonly string _tablePrefix = ((ChatDbContext)currentContext.Context).TablePrefix;
    private ModelSnapshot? _modelSnapshot;

    public override ModelSnapshot? ModelSnapshot => _modelSnapshot ??= WithPrefix(base.ModelSnapshot!);

    public override Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
    {
        var migration = (ChatMigration)base.CreateMigration(migrationClass, activeProvider);
        migration.TablePrefix = _tablePrefix;
        ApplyPrefix(migration.TargetModel!, _tablePrefix);

        return migration;
    }

    /// <summary>Renames the tables of a model built with the default prefix.</summary>
    internal static void ApplyPrefix(IModel model, string tablePrefix)
    {
        foreach (var entityType in ((IMutableModel)model).GetEntityTypes())
        {
            var tableName = entityType.GetTableName()!;
            entityType.SetTableName(tablePrefix + tableName[ChatStorageOptions.DefaultTablePrefix.Length..]);
        }
    }

    private ModelSnapshot WithPrefix(ModelSnapshot snapshot)
    {
        ApplyPrefix(snapshot.Model, _tablePrefix);
        return snapshot;
    }
}

#pragma warning restore EF1001
