using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Cyber_Defense_Game;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class PlayerInfo
{
    [JsonPropertyName("connectionId")]
    public string ConnectionId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public static class GameConnection
{
    public const string ClientVersion = "v1.0.5";

    private static HubConnection? _connection;

    public static List<PlayerInfo> CurrentPlayers { get; private set; } = new();

    public static event Action<List<PlayerInfo>>? PlayersUpdated;
    public static event Action<string, string, int>? AnswerReceived;

    public static async Task ConnectAsync(string serverUrl)
    {
        if (_connection != null) return;

        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl.TrimEnd('/')}/gamehub",
                o => o.Transports = HttpTransportType.WebSockets)
            .WithAutomaticReconnect()
            .Build();

        _connection.On<int>("ImageChanged", n => GameState.SelectedImage = n);
        _connection.On<List<PlayerInfo>>("PlayersUpdated", p =>
        {
            Console.WriteLine($"PlayersUpdated received: {p?.Count ?? 0} players");
            CurrentPlayers = p ?? new List<PlayerInfo>();
            PlayersUpdated?.Invoke(CurrentPlayers);
        });
        _connection.On<string, string, int>("AnswerReceived",
            (id, name, answer) => AnswerReceived?.Invoke(id, name, answer));

        await _connection.StartAsync();
    }

    public static Task JoinLobbyAsync(string name) =>
        _connection?.InvokeAsync("JoinLobby", name) ?? Task.CompletedTask;

    public static Task RegisterAsHostAsync() =>
        _connection?.InvokeAsync("RegisterAsHost") ?? Task.CompletedTask;

    public static Task SubmitAnswerAsync(int answer) =>
        _connection?.InvokeAsync("SubmitAnswer", answer) ?? Task.CompletedTask;

    public static Task ChangeImageAsync(int imageNumber) =>
        _connection?.InvokeAsync("ChangeImage", imageNumber) ?? Task.CompletedTask;
}
