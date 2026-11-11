using Cyber_Defense_Game.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// SignalR inschakelen
builder.Services.AddSignalR();

// Browser toegang toestaan tijdens lokaal testen
builder.Services.AddCors(options =>
{
    options.AddPolicy("GameClient", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                new Uri(origin).Host == "localhost" ||
                new Uri(origin).Host == "127.0.0.1")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("GameClient");

app.MapGet("/", () => "Cyber Defense Server werkt!");

app.MapHub<GameHub>("/gamehub");

app.Run();