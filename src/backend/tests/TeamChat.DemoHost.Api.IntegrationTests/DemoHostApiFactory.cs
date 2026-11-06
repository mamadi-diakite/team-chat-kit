namespace TeamChat.DemoHost.Api.IntegrationTests;

/// <summary>Runs the demo host against its own throwaway SQLite file.</summary>
public class DemoHostApiFactory : WebApplicationFactory<Program>
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "team-chat-kit-tests", Guid.NewGuid().ToString("N"));

    protected virtual string EnvironmentName => "Development";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(EnvironmentName);
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={Path.Combine(_root, "app.db")}");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
