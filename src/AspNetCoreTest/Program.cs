using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using AspNetCoreTest.Data.Models;
using Microsoft.AspNetCore;
using NLog.Web;
using NLog;
using System.Reflection;

namespace AspNetCoreTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                logger.Debug("init main");
                BuildWebHost(args).Run();
                LogManager.ReconfigExistingLoggers();
            }
            catch (Exception e)
            {
                //NLog: catch setup errors
                logger.Error(e, "Stopped program because of exception");
                throw;
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                //.UseShutdownTimeout(new TimeSpan(0, 10, 0)) // ждем 10 минут для завершения
                .UseStartup<Startup>()
                .UseNLog()
                .Build();

    }
}
