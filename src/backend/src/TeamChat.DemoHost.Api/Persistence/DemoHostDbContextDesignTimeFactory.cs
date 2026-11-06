namespace TeamChat.DemoHost.Api.Persistence;

/// <summary>Used by <c>dotnet ef</c>, which cannot start the API without a JWT signing key.</summary>
public sealed class DemoHostDbContextDesignTimeFactory : IDesignTimeDbContextFactory<DemoHostDbContext>
{
    public DemoHostDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<DemoHostDbContext>().UseSqlite("Data Source=design-time.db").Options);
}
