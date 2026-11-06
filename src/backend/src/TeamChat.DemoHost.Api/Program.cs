var builder = WebApplication.CreateBuilder(args);

var jwtSettings = JwtSettings.FromConfiguration(builder.Configuration);
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

builder.Services.AddControllers();
builder.Services.AddDbContext<DemoHostDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IdentityPasswordHasher>();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<JwtAccessTokenIssuer>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DemoDataSeeder>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = jwtSettings.ToValidationParameters();
    });
builder.Services.AddAuthorization();

builder.Services.AddTeamChat<DemoChatIdentityAdapter>(options =>
{
    builder.Configuration.GetSection("TeamChat").Bind(options);
    options.ConnectionString = connectionString;
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await app.Services.ApplyMigrationsAsync();
}

if (app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("Database:SeedDemoData"))
{
    await app.Services.SeedDemoDataAsync();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapTeamChat();

await app.RunAsync();
