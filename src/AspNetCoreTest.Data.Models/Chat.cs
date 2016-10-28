using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Text;

namespace AspNetCoreTest.Data.Models
{
    public class Chat
    {
        // Список всех клиентов
        private static readonly List<WebSocket> Clients = new List<WebSocket>();

        // Блокировка для обеспечения потокабезопасности
        private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim();

        public static Exception Error { get; set; }

        //private WebSocket Socket { get; set; }

        public static async Task NewClient(HttpContext http)
        {
            var webSocket = await http.WebSockets.AcceptWebSocketAsync();

            //Socket = socket;
            // Добавляем его в список клиентов
            Locker.EnterWriteLock();
            try
            {
                Clients.Add(webSocket);
            }
            finally
            {
                Locker.ExitWriteLock();
            }

            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    var token = CancellationToken.None;
                    var buffer = new ArraySegment<Byte>(new Byte[4096]);
                    var received = await webSocket.ReceiveAsync(buffer, token);

                    switch (received.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            var request = Encoding.UTF8.GetString(buffer.Array,
                                                    buffer.Offset,
                                                    buffer.Count);
                            //var type = WebSocketMessageType.Text;
                            //var data = Encoding.UTF8.GetBytes("Echo from server :" + request);
                            //buffer = new ArraySegment<Byte>(data);
                            //await webSocket.SendAsync(buffer, type, true, token);
                            SendToAll("Echo from server :" + request);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Error = e;
                }
            }/**/

            Locker.EnterWriteLock();
            try
            {
                Clients.Remove(webSocket);

            }
            finally
            {
                Locker.ExitWriteLock();
            }
        }


        private static void SendToAll(string text)
        {
            var type = WebSocketMessageType.Text;
            var token = CancellationToken.None;
            //var buffer = new ArraySegment<Byte>(new Byte[4096]);
            var data = Encoding.UTF8.GetBytes(text);
            var buffer = new ArraySegment<Byte>(data);
            Clients.ForEach(async (socket) =>
            {
                if (socket != null && socket.State == WebSocketState.Open)
                {
                    await socket.SendAsync(buffer, type, true, token);
                }
            });
        }
    }
}
