using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Cyber_Defense_Game.Server.Hubs;

public class GameHub : Hub
{
    // ConnectionId -> Name (static so it is shared across hub instances)
    private static readonly ConcurrentDictionary<string, string> Players = new();

    public async Task JoinLobby(string name)
    {
        Players[Context.ConnectionId] = name;
        await BroadcastPlayers();
    }

    public async Task ChangeImage(int imageNumber)
    {
        await Clients.All.SendAsync("ImageChanged", imageNumber);
    }

    public async Task SubmitAnswer(int answer)
    {
        var name = Players.GetValueOrDefault(Context.ConnectionId, "Unknown");
        // Send only to the host (see the host group below), tagged with who answered
        await Clients.Group("hosts").SendAsync("AnswerReceived", Context.ConnectionId, name, answer);
    }

    public async Task RegisterAsHost()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "hosts");
        await Clients.Caller.SendAsync("PlayersUpdated", GetPlayerList());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Players.TryRemove(Context.ConnectionId, out _);
        await BroadcastPlayers();
        await base.OnDisconnectedAsync(exception);
    }
    public async Task StartGame()
    {
        await Clients.All.SendAsync("GameStarted");
    }

    private static List<string> GetPlayerList() =>
        Players.Values.ToList();

    private Task BroadcastPlayers() =>
        Clients.All.SendAsync("PlayersUpdated", GetPlayerList());
}
