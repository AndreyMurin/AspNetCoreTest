﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using AspNetCoreTest.Data.Models;

namespace AspNetCoreTest
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class WebsocketsMiddleware
    {
        private readonly RequestDelegate _next;

        public WebsocketsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            if (httpContext.WebSockets.IsWebSocketRequest)
            {

                var path = httpContext.Request.Path;
                //Handle WebSocket Requests here.
                switch (path)
                {
                    case "/chat":
                        await Chat.NewClient(httpContext);
                        break;
                    default:
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
