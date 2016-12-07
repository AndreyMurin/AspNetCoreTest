using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AspNetCoreTest.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace AspNetCoreTest.Controllers
{
    public class HomeController : Controller
    {
        private IStorage storage;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IStorage storage, ILogger<HomeController> logger)
        {
            this.storage = storage;
            _logger = logger;
        }

        /*private void testCriticalError() {
            testCriticalError();
        }*/

        public IActionResult Index()
        {
            _logger.LogInformation(11, "Home index loading");
            _logger.LogCritical(111, "Test Critical Error");
            //testCriticalError();
            return this.View(this.storage.GetRepository<IItemRepository>().All());
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
