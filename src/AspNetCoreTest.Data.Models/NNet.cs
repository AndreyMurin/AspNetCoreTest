using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using System.Security.Cryptography;

namespace AspNetCoreTest.Data.Models
{
    public class NNetConfig
    {
        public string FileName { get; set; }
        public int Size { get; set; }
    }

    public class NNet
    {
        private readonly ILogger<NNet> _logger;
        private readonly IOptions<NNetConfig> _optionsAccessor;
        private readonly IFileProvider _provider;

        private List<Neuron> _neurons;
        private string _filename;
        private int _size;

        public NNet (ILogger<NNet> logger, IOptions<NNetConfig> optionsAccessor, IApplicationLifetime appLifetime, IFileProvider provider)
        {
            _logger = logger;
            _optionsAccessor = optionsAccessor;
            _logger.LogInformation(1111, "NNet constructor {FileName} {Size}", _optionsAccessor.Value.FileName, _optionsAccessor.Value.Size);
            _filename = _optionsAccessor.Value.FileName;
            _size = _optionsAccessor.Value.Size;
            _provider = provider;

            // Ensure any buffered events are sent at shutdown
            appLifetime.ApplicationStopped.Register(this.save);

            if (!string.IsNullOrWhiteSpace(_filename) && _provider.GetFileInfo(_filename).Exists)
            {
                load();
            }
            else
            {
                init();
            }
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
            _logger.LogInformation(1111, "NNet init");
            _neurons = new List<Neuron>();
            //RandomNumberGenerator generator = RandomNumberGenerator.Create();
            var rand = new Random();
            for (var i = 0; i < _size; ++i)
            {
                _neurons.Add(new Neuron(rand));
            }

        }

        public void save()
        {
            _logger.LogInformation(1111, "NNet save");
        }

        public void load()
        {
            _logger.LogInformation(1111, "NNet load");
            
        }
    }
}
