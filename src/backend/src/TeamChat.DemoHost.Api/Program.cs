var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

builder.Services.AddControllers();
builder.Services.AddTeamChat(options => options.ConnectionString = connectionString);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.ApplyTeamChatMigrationsAsync();
}

app.MapControllers();
app.MapTeamChat();

await app.RunAsync();
