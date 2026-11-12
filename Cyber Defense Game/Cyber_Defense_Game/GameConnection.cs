using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Cyber_Defense_Game;

public static class GameConnection
{
    public const string DefaultServerUrl = "https://cyberquiz.tail9822f1.ts.net";

    private static HubConnection? _connection;
    private static Task? _connecting;

    public static Task ConnectAsync(string? serverUrl = null)
    {
        if (_connection?.State == HubConnectionState.Connected)
            return Task.CompletedTask;

        return _connecting ??= StartAsync(serverUrl ?? DefaultServerUrl);
    }

    private static async Task StartAsync(string serverUrl)
    {
        try
        {
            serverUrl = serverUrl.Trim().TrimEnd('/');

            if (serverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                serverUrl = "https://" + serverUrl.Substring("http://".Length);
            else if (!serverUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                serverUrl = "https://" + serverUrl;

            var connection = new HubConnectionBuilder()
                .WithUrl($"{serverUrl}/gamehub", options =>
                {
                    options.Transports = HttpTransportType.WebSockets;
                })
                .WithAutomaticReconnect()
                .Build();

            connection.On<int>("ImageChanged", imageNumber =>
            {
                GameState.SelectedImage = imageNumber;
            });

            await connection.StartAsync();

            _connection = connection;   // only stored after a successful start
        }
        finally
        {
            _connecting = null;         // allows a retry if it failed
        }
    }

    public static async Task ChangeImageAsync(int imageNumber)
    {
        await ConnectAsync();           // connects first if needed

        if (_connection == null)
            return;

        await _connection.InvokeAsync("ChangeImage", imageNumber);
    }
}