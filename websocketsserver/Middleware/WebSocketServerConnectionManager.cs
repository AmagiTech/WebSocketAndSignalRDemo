using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.WebSockets;


namespace websocketsserver.Middleware
{
    public class WebSocketServerConnectionManager
    {
        private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public ConcurrentDictionary<string, WebSocket> GetAllScokets()
        {
            return _sockets;
        }

        public string AddSocket(WebSocket socket)
        {
            var connId = Guid.NewGuid().ToString();
            _sockets.TryAdd(connId, socket);
            Console.WriteLine("Connection Added: "+ connId);
            return connId;
        }
    }
}
