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
        // максимальное число нейронов для отправки
        private const int MaxNeuronsForSend = 3;

        // Список всех клиентов
        private readonly List<WebSocket> Clients = new List<WebSocket>();
        // Блокировка для обеспечения потокабезопасности
        private readonly ReaderWriterLockSlim LockerWS = new ReaderWriterLockSlim();
        //private readonly ReaderWriterLockSlim LockerSubscribers = new ReaderWriterLockSlim();
        private readonly ConcurrentDictionary<WebSocket, List<NRange>> Subscribers = new ConcurrentDictionary<WebSocket, List<NRange>>();

        public async Task SubscribeClient(HttpContext httpContext)
        {
            //_logger.LogInformation(1113, "NNetServer SubscribeClient start");

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
                //_logger.LogInformation(1113, "NNetServer SubscribeClient begin action");
                try
                {
                    //var token = CancellationToken.None;
                    var buffer = new ArraySegment<Byte>(new Byte[4096]); // не очень хорошо ограничивать вход 4 кб! расчетные модули могут посылать больше инфы за 1 раз
                    var received = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                    
                    switch (received.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            var request = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count);
                            //_logger.LogInformation(1113, "NNetServer SubscribeClient request = {request}", request);
                            var tmp = JsonConvert.DeserializeObject<WSRequest>(request);
                            switch (tmp.Action.ToLower())
                            {
                                // подписка на изменения в сети
                                case "subscribe":
                                    if (tmp.ArgsInt.Count < 6) { await SendError(webSocket, "Число аргументов ArgsInt должно быть >= 6", tmp.Action); return; }
                                    if (tmp.ArgsInt.Count % 6 != 0) { await SendError(webSocket, "Число аргументов ArgsInt должно быть кратно 6", tmp.Action); return; }

                                    var val = new List<NRange>();
                                    for (var i = 0; i < tmp.ArgsInt.Count; i=i+6)
                                    {
                                        // обязательно проверить кто раньше первый или второй и это важно
                                        var min = new NCoords(tmp.ArgsInt[i + 0], tmp.ArgsInt[i + 1], tmp.ArgsInt[i + 2]);
                                        var max = new NCoords(tmp.ArgsInt[i + 3], tmp.ArgsInt[i + 4], tmp.ArgsInt[i + 5]);
                                        if (max.ToSingle(LenX, LenY) < min.ToSingle(LenX, LenY))
                                        {
                                            var t = min; min = max; max = t;
                                        }
                                        val.Add(new NRange()
                                        {
                                            MinX = min.X, MinY = min.Y, MinZ = min.Z,
                                            MaxX = max.X, MaxY = max.Y, MaxZ = max.Z
                                        });
                                    }

                                    // доки https://docs.microsoft.com/ru-ru/dotnet/articles/standard/collections/threadsafe/how-to-add-and-remove-items
                                    // очень маловероятно что 1 клиент 2 раза сможет вызвать подписку больше 1 раза в одно и то же время!
                                    Subscribers.AddOrUpdate(webSocket, val, (key, existingVal)=> {

                                        // If this delegate is invoked, then the key already exists.
                                        // Here we make sure the city really is the same city we already have.
                                        // (Support for multiple cities of the same name is left as an exercise for the reader.)
                                        existingVal.AddRange(val);

                                        return existingVal;
                                    });

                                    // сразу отправим полные данные о выбранных нейронах
                                    await SendNeurons(webSocket, val, tmp.Action);

                                    break;
                                // полная отписка
                                case "unsubscribe":
                                    List<NRange> tmp1;
                                    Subscribers.TryRemove(webSocket, out tmp1);

                                    await SendMessage(webSocket, "OK", tmp.Action);
                                    break;
                                // запрос конфигурации сети
                                case "getnetconfig":
                                    await SendConfig(webSocket, tmp.Action);
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
                    //_logger.LogInformation(1113, "NNetServer SubscribeClient error {error}.", e.Message);
                    await SendError(webSocket, e.Message, null);
                }
                //_logger.LogInformation(1113, "NNetServer SubscribeClient end action");
            }/**/

            //_logger.LogInformation(1113, "NNetServer SubscribeClient before removing");

            LockerWS.EnterWriteLock();
            try
            {
                Clients.Remove(webSocket);
            }
            finally
            {
                LockerWS.ExitWriteLock();
            }
            List<NRange> tmp2;
            Subscribers.TryRemove(webSocket, out tmp2);
            //_logger.LogInformation(1113, "NNetServer SubscribeClient end");
        }

        private Task createTaskSendNeurons(WebSocket ws, List<NeuronForDraw> neurons, string action) {
            return Task.Run(() =>
            {
                return SendResponse(ws, JsonConvert.SerializeObject(new WSResponseNeurons { Action = action, Neurons = neurons }, Formatting.Indented));
            });
        }

        // отправляем данные по выбранным нейронам
        private async Task SendNeurons(WebSocket ws, List<NRange> ranges, string action)
        {
            //var resp = new WSResponseNeurons { Action = action, Neurons = _getOutputNeurons(ranges) };
            var tasks = new List<Task>();
            var neurons = new List<NeuronForDraw>();
            foreach (var range in ranges)
            {
                for (int z = range.MinZ; z <= range.MaxZ; z++)
                {
                    for (int y = Math.Min(range.MinY, range.MaxY); y <= Math.Max(range.MinY, range.MaxY); y++)
                    {
                        for (int x = Math.Min(range.MinX, range.MaxX); x <= Math.Max(range.MinX, range.MaxX); x++)
                        {
                            neurons.Add(new NeuronForDraw() { x = x, y = y, z = z, Neuron = Neurons[z][y][x], Input = findNeuronInputs(x, y, z) });

                            if (neurons.Count >= MaxNeuronsForSend)
                            {
                                tasks.Add(createTaskSendNeurons(ws, neurons, action));
                                neurons = new List<NeuronForDraw>();
                            }
                        }
                    }
                }
            }
            if (neurons.Any())
            {
                tasks.Add(createTaskSendNeurons(ws, neurons, action));
            }

            await Task.WhenAll(tasks);
            //await SendResponse(ws, JsonConvert.SerializeObject(resp, Formatting.Indented));
        }

        /*private async Task SendRanges(WebSocket ws, List<NRange> ranges, string action)
        {
            var resp = new WSResponse { Action = action };

            await SendResponse(ws, JsonConvert.SerializeObject(resp, Formatting.Indented));
        }*/

        private async Task SendConfig(WebSocket ws, string action)
        {
            var resp = new WSResponseConfig {Action = action, LenX = LenX, LenY = LenY, LenZ = LenZ };
            await SendResponse(ws, JsonConvert.SerializeObject(resp, Formatting.Indented));
        }

        private async Task SendMessage(WebSocket ws, string message, string action)
        {
            await SendResponse(ws, JsonConvert.SerializeObject(new WSResponse { Action = action, Message = message }, Formatting.Indented));
        }

        private async Task SendError(WebSocket ws, string message, string action)
        {
            await SendResponse(ws, JsonConvert.SerializeObject(new WSResponse { Action = action, Error = message }, Formatting.Indented));
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
