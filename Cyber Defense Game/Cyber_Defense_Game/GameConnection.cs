using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace Cyber_Defense_Game;

public static class GameConnection
{
    private static HubConnection? _connection;

    public static async Task ConnectAsync(string serverUrl)
    {
        if (_connection != null)
            return;

        serverUrl = serverUrl.Trim().TrimEnd('/');

        if (!serverUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            serverUrl = "https://" + serverUrl;

        if (serverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            serverUrl = "https://" + serverUrl.Substring("http://".Length);

        var connection = new HubConnectionBuilder()

    .WithUrl("https://ts.net")

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
        if (_connection == null)
            return;

        await _connection.InvokeAsync("ChangeImage", imageNumber);
    }
}