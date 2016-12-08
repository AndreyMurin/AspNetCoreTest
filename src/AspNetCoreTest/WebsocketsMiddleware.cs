using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using AspNetCoreTest.Data.Models;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AspNetCoreTest
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class WebsocketsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebsocketsMiddleware> _logger;

        public WebsocketsMiddleware(RequestDelegate next, ILogger<WebsocketsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _logger.LogInformation(1112, "WebsocketsMiddleware constructor");
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.WebSockets.IsWebSocketRequest)
            {
                var path = httpContext.Request.Path;
                _logger.LogInformation(1112, "WebsocketsMiddleware Invoke {path}", path);
                //Handle WebSocket Requests here.
                switch (path)
                {
                    case "/chat":
                        await Chat.NewClient(httpContext);
                        break;
                    default:
                        _logger.LogError(1113, "Websocket path not founded! path = {path}", path);
                        var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync();
                        await webSocket.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Websocket path not founded!", CancellationToken.None);
                        break;
                        //await next();
                        //throw new Exception("Not founded");
                }
            }
            else
            {
                await _next(httpContext);
                //return _next(httpContext);
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class WebsocketsMiddlewareExtensions
    {
        public static IApplicationBuilder UseWebsocketsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebsocketsMiddleware>();
        }
    }
}
