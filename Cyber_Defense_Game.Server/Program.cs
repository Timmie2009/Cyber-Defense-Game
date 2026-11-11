using Cyber_Defense_Game.Server.Hubs;

var builder = WebApplication.CreateBuilder(args);

// SignalR inschakelen
builder.Services.AddSignalR();

// Browser toegang toestaan (localhost + Tailscale)
builder.Services.AddCors(options =>
{
    options.AddPolicy("GameClient", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
        {
            var host = new Uri(origin).Host;
            return host == "localhost" ||
                   host == "127.0.0.1" ||
                   host.EndsWith(".ts.net");
        })
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