using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AspNetCoreTest.Data.Abstractions;
using AspNetCoreTest.Data.Models;
using NLog.Extensions.Logging;
using System.IO;

namespace AspNetCoreTest
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

            // Setup options with DI
            //services.AddOptions();

            // Uncomment to use mock storage
            services.AddScoped(typeof(IStorage), typeof(AspNetCoreTest.Data.Mock.Storage));
            // Uncomment to use SQLite storage
            //services.AddScoped(typeof(IStorage), typeof(AspNetCoreTest.Data.Sqlite.Storage));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            loggerFactory.AddNLog();
            //needed for non-NETSTANDARD platforms: configure nlog.config in your project root
            env.ConfigureNLog("nlog.config");

            //loggerFactory.AddDebug((category, loglevel) => category.Contains("MyController") && loglevel >= LogLevel.Trace);
            //loggerFactory.AddProvider(new RollingFileSink());
            //loggerFactory.AddSerilog(dispose: true);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseWebSockets();
            //app.UseMiddleware<WebsocketsMiddleware>();
            app.UseWebsocketsMiddleware();
            /*app.Use(async (http, next) =>
            {
                if (http.WebSockets.IsWebSocketRequest)
                {

                    var path = http.Request.Path;
                    var proto = http.Request.Protocol;
                    //Handle WebSocket Requests here.
                    switch (path)
                    {
                        case "/chat":
                            await Chat.NewClient(http);
                            break;
                        default:
                            break;
                            //await next();
                            //throw new Exception("Not founded");
                    }
                }
                else
                {
                    await next();
                }
            });*/

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
