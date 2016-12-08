using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;

namespace AspNetCoreTest.Data.Models
{
    public class MyOptions
    {
        public string Option1 { get; set; }
        public int Option2 { get; set; }
    }

    public class NNet
    {
        private readonly ILogger<NNet> _logger;
        private readonly IOptions<MyOptions> _optionsAccessor;

        private List<Neuron> Neurons;
        private string FileName;

        /*~NNet()
        {
            save();
            _logger.LogInformation(1111, "NNet destructor");
        }*/

        public NNet (ILogger<NNet> logger, IOptions<MyOptions> optionsAccessor, IApplicationLifetime appLifetime)
        {
            //var config = Microsoft.Extensions.Configuration.GetSection("NNet");
            /*if(logger == null)
            {
                logger = LogManager.GetLogger(GetType().FullName);
            }*/
            _logger = logger;
            _optionsAccessor = optionsAccessor;
            _logger.LogInformation(1111, "NNet constructor {Option1} {Option2}", _optionsAccessor.Value.Option1, _optionsAccessor.Value.Option2);

            // Ensure any buffered events are sent at shutdown
            appLifetime.ApplicationStopped.Register(this.save);
        }

        /*NNet(ILogger<NNet> logger, int size, string filename) {
            //System.Reflection.Assembly.GetExecutingAssembly().Location
            if (string.IsNullOrWhiteSpace(filename)) filename = Path.GetTempFileName();
            FileName = filename;
            Neurons = new List<Neuron>(size);
            init();
        }

        NNet(ILogger<NNet> logger, string filename)
        {
            FileName = filename;
            Neurons = new List<Neuron>();
            load(filename);
        }*/

        public void init()
        {
            _logger.LogInformation(1111, "NNet init {Option1} {Option2}", _optionsAccessor.Value.Option1, _optionsAccessor.Value.Option2);
        }

        public void save()
        {
            _logger.LogInformation(1111, "NNet save");
        }

        public void load(string filename)
        {

        }
    }
}
