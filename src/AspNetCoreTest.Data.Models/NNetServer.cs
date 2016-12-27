using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Text;
using System.Collections.Concurrent;

namespace AspNetCoreTest.Data.Models
{
    public class NNetServer : NNet
    {
        public NNetServer(ILogger<NNet> logger, IOptions<NNetConfig> optionsAccessor, IFileProvider provider, IRnd rand) : base(logger, optionsAccessor, provider, rand)
        {
        }


        #region Websockets Server
        // Список всех клиентов
        private readonly List<WebSocket> Clients = new List<WebSocket>();
        // Блокировка для обеспечения потокабезопасности
        private readonly ReaderWriterLockSlim LockerWS = new ReaderWriterLockSlim();
        //private readonly ReaderWriterLockSlim LockerSubscribers = new ReaderWriterLockSlim();
        private readonly ConcurrentDictionary<WebSocket, List<NRange>> Subscribers = new ConcurrentDictionary<WebSocket, List<NRange>>();

        public async Task SubscribeClient(HttpContext httpContext)
        {
            var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();

            //Socket = socket;
            // Добавляем его в список клиентов
            LockerWS.EnterWriteLock();
            try
            {
                Clients.Add(webSocket);
            }
            finally
            {
                LockerWS.ExitWriteLock();
            }

            while (webSocket.State == WebSocketState.Open)
            {
                try
                {
                    //var token = CancellationToken.None;
                    var buffer = new ArraySegment<Byte>(new Byte[4096]); // не очень хорошо ограничивать вход 4 кб! расчетные модули могут посылать больше инфы за 1 раз
                    var received = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

                    switch (received.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            var request = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
                            var tmp = JsonConvert.DeserializeObject<WSRequest>(request);
                            switch (tmp.Action.ToLower())
                            {
                                // подписка на изменения в сети
                                case "subscribe":
                                    if (tmp.ArgsInt.Count < 6) { await SendError(webSocket, "Число аргументов ArgsInt должно быть кратно 6"); return; }

                                    var val = new List<NRange>();

                                    // доки https://docs.microsoft.com/ru-ru/dotnet/articles/standard/collections/threadsafe/how-to-add-and-remove-items
                                    Subscribers.AddOrUpdate(webSocket, val, (key, existingVal)=> {

                                        return existingVal;
                                    });
                                    
                                    break;
                                // полная отписка
                                case "unsubscribe":

                                    Subscribers.TryRemove(webSocket);

                                    await SendMessage(webSocket, "OK");
                                    break;
                                // запрос конфигурации сети
                                case "getnetconfig":
                                    await SendConfig(webSocket);
                                    break;
                            }
                            //var type = WebSocketMessageType.Text;
                            //var data = Encoding.UTF8.GetBytes("Echo from server :" + request);
                            //buffer = new ArraySegment<Byte>(data);
                            //await webSocket.SendAsync(buffer, type, true, token);
                            //SendToAll("Echo from server :" + request);
                            break;
                    }
                }
                catch (Exception e)
                {
                    //Error = e;
                    await SendError(webSocket, e.Message);
                }
            }/**/

            LockerWS.EnterWriteLock();
            try
            {
                Clients.Remove(webSocket);
            }
            finally
            {
                LockerWS.ExitWriteLock();
            }

            Subscribers.TryRemove(webSocket);
        }

        private async Task SendRanges(WebSocket ws, List<NRange> ranges)
        {
            var resp = new WSResponse();

            await SendResponse(ws, JsonConvert.SerializeObject(resp, Formatting.Indented));
        }

        private async Task SendConfig(WebSocket ws)
        {
            var resp = new WSResponseConfig() { LenX = LenX, LenY = LenY, LenZ = LenZ };
            await SendResponse(ws, JsonConvert.SerializeObject(resp, Formatting.Indented));
        }

        private async Task SendMessage(WebSocket ws, string message)
        {
            await SendResponse(ws, JsonConvert.SerializeObject(new WSResponse() { Message = message }, Formatting.Indented));
        }

        private async Task SendError(WebSocket ws, string message)
        {
            await SendResponse(ws, JsonConvert.SerializeObject(new WSResponse() { Error = message }, Formatting.Indented));
        }

        // отправляем строку клиенту (а в строке JSON)
        private async Task SendResponse(WebSocket ws, string resp)
        {
            try
            {
                //var len = Encoding.UTF8.GetByteCount(resp);
                //var buffer = new ArraySegment<Byte>(new Byte[len]);
                //var token = CancellationToken.None;
                //var type = WebSocketMessageType.Text;
                //var data = Encoding.UTF8.GetBytes(resp);
                //var buffer = new ArraySegment<Byte>(Encoding.UTF8.GetBytes(resp));
                await ws.SendAsync(new ArraySegment<Byte>(Encoding.UTF8.GetBytes(resp)), WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (Exception)
            {
                // подавляем все ошибки, тут сокет может быть уже закрыт или еще что, нам не важно, хотя! если отвалился расчетный модуль, то кабздец ... 
                // а в расчетный модуль отправляем ответ в отдельной функции!
            }
        }
        #endregion

    }
}
