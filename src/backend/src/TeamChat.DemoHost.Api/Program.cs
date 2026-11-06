using TeamChat.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddTeamChat();

var app = builder.Build();

app.MapControllers();
app.MapTeamChat();

app.Run();
