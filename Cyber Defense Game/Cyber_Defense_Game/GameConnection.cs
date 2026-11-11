using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using System.Threading.Tasks;

namespace Cyber_Defense_Game;

public static class GameConnection
{
    private static HubConnection? _connection;

    public static async Task ConnectAsync(string serverUrl)
    {
        if (_connection != null)
            return;

        _connection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl.TrimEnd('/')}/gamehub", options =>
            {
                options.Transports = HttpTransportType.WebSockets;
            })
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