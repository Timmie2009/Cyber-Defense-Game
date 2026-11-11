using Microsoft.AspNetCore.SignalR;

namespace Cyber_Defense_Game.Server.Hubs;

public class GameHub : Hub
{
    public async Task ChangeImage(int imageNumber)
    {
        await Clients.All.SendAsync("ImageChanged", imageNumber);
    }
}