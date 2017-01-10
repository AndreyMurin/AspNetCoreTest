using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using Microsoft.Extensions.FileProviders;

namespace AspNetCoreTest
{
    public class Startup
    {
        private IHostingEnvironment _hostingEnvironment;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                // setup default config
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "NNet:FileName", "test.murin" }, { "NNet:LenX", "10" }, { "NNet:LenY", "10" }, { "NNet:LenZ", "4" }
                })
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            _hostingEnvironment = env;
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Setup options with DI
            services.AddOptions();

            // Configure MySubOptions using a sub-section of the appsettings.json file
            services.Configure<NNetConfig>(Configuration.GetSection("NNet"));

            // Add framework services.
            services.AddMvc();

            // Uncomment to use mock storage
            //services.AddScoped(typeof(IStorage), typeof(AspNetCoreTest.Data.Mock.Storage));
            services.AddScoped<IStorage, AspNetCoreTest.Data.Mock.Storage>();
            // Uncomment to use SQLite storage
            //services.AddScoped(typeof(IStorage), typeof(AspNetCoreTest.Data.Sqlite.Storage));

            /*
            // -------------------------------------
            var physicalProvider = _hostingEnvironment.ContentRootFileProvider;
            var embeddedProvider = new EmbeddedFileProvider(Assembly.GetEntryAssembly());
            var compositeProvider = new CompositeFileProvider(physicalProvider, embeddedProvider);
            
            // choose one provider to use for the app and register it
            services.AddSingleton<IFileProvider>(physicalProvider);
            //services.AddSingleton<IFileProvider>(embeddedProvider);
            //services.AddSingleton<IFileProvider>(compositeProvider);
            // -------------------------------------
            */
            services.AddSingleton<IRnd, Rnd>();

            services.AddSingleton<NNetServer, NNetServer>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            loggerFactory.AddNLog();
            //needed for non-NETSTANDARD platforms: configure nlog.config in your project root
            env.ConfigureNLog("nlog.config");

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
