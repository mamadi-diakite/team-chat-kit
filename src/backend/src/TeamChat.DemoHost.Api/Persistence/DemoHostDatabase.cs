namespace TeamChat.DemoHost.Api.Persistence;

public static class DemoHostDatabase
{
    /// <summary>Applies the chat migrations, then the demo host ones, to the shared database.</summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // The chat migrations also create the database folder.
        await services.ApplyTeamChatMigrationsAsync(cancellationToken);

        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<DemoHostDbContext>().Database.MigrateAsync(cancellationToken);
    }

    public static async Task<bool> SeedDemoDataAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        return await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(cancellationToken);
    }
}
