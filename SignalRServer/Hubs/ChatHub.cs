using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace SignalRServer.Hubs
{
    public class ChatHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            Console.WriteLine("--> Connection established " + Context.ConnectionId);
            Clients.Client(Context.ConnectionId).SendAsync("ReceiveConnId", Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public async Task SendMessageAsync(string message)
        {
            var routeObj = JsonConvert.DeserializeObject<dynamic>(message);
            var toClient = routeObj.To.ToString();
            Console.WriteLine("Message received on: " + Context.ConnectionId);

            if (string.IsNullOrWhiteSpace(toClient))
            {
                await Clients.All.SendAsync("ReceiveMessage", message);
            }
            else
            {
                await Clients.Client(toClient).SendAsync("ReceiveMessage", message);
            }
        }
    }
}
