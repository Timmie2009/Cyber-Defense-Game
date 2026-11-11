using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Cyber_Defense_Game;

public static class GameConnection
{
    private static HubConnection? _connection;

    public static async Task ConnectAsync(string serverUrl)
    {
        if (_connection != null)
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl.TrimEnd('/')}/gamehub")
            .WithAutomaticReconnect()
            .Build();

        _connection.On<int>("ImageChanged", imageNumber =>
        {
            GameState.SelectedImage = imageNumber;
        });

        await _connection.StartAsync();
    }

    public static async Task ChangeImageAsync(int imageNumber)
    {
        if (_connection?.State != HubConnectionState.Connected)
            throw new InvalidOperationException(
                "Geen verbinding met de gameserver.");

        await _connection.InvokeAsync("ChangeImage", imageNumber);
    }
}