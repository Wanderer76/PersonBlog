using Conference.Domain.Services;
using Microsoft.AspNetCore.SignalR;

namespace Conference.Service.Hubs
{
    public class ChatHub : Hub<IChatHub>
    {
        public async Task Send(string message)
        {
            await Clients.All.OnConferenceConnect(message);
        }
    }
}
