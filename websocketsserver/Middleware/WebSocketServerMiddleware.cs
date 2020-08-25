using Microsoft.AspNetCore.Http;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;


namespace websocketsserver.Middleware
{
    public class WebSocketServerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly WebSocketServerConnectionManager _manager;
        public WebSocketServerMiddleware(RequestDelegate next, WebSocketServerConnectionManager manager)
        {
            _next = next;
            _manager = manager;
        }

        private async Task SendConnIdAsync(WebSocket socket, string connId)
        {
            var buffer = Encoding.UTF8.GetBytes($"ConnId: {connId}");
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task ReceiveMesasage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                    cancellationToken: CancellationToken.None);
                handleMessage(result, buffer);
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine("WebSocket connected");

                string connId = _manager.AddSocket(socket);
                await SendConnIdAsync(socket, connId);

                await ReceiveMesasage(socket, async (result, buffer) =>
                {
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        Console.WriteLine("Message Received");
                        Console.WriteLine($"Message: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
                        await RouteJSONMessageAsync(Encoding.UTF8.GetString(buffer, 0, result.Count));
                        return;
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("Received Close Message");
                        var id = _manager.GetAllScokets().FirstOrDefault(q => q.Value == socket).Key;

                        _manager.GetAllScokets().TryRemove(id, out WebSocket webSocket);

                        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

                        return;
                    }
                });
            }
            else
            {
                Console.WriteLine("Http request from second delegate");
                await _next(context);
            }
        }

        public async Task RouteJSONMessageAsync(string message)
        {
            var routeObj = JsonConvert.DeserializeObject<dynamic>(message);
            if (Guid.TryParse(routeObj.To.ToString(), out Guid guidOutput))
            {
                Console.WriteLine("Targeted");
                var sock = _manager.GetAllScokets().FirstOrDefault(q => q.Key == routeObj.To.ToString());
                if (sock.Value != null)
                {
                    if (sock.Value.State == WebSocketState.Open)
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(routeObj.Message.ToString()),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None);
                }
                Console.WriteLine("Invalid receipent");
            }
            else
            {
                Console.WriteLine("Broadcast");
                foreach (var s in _manager.GetAllScokets())
                {
                    if (s.Value.State == WebSocketState.Open)
                    {
                        await s.Value.SendAsync(Encoding.UTF8.GetBytes(routeObj.Message.ToString()),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None);
                    }
                }
            }
        }

    }
}
