using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;

namespace AspNetCoreTest.Data.Models
{
    public class NNetConfig
    {
        public string FileName { get; set; }
        public int Size { get; set; }
    }

    public class NNet : IDisposable
    {
        private readonly ILogger<NNet> _logger;
        private readonly IOptions<NNetConfig> _optionsAccessor;
        private readonly IFileProvider _provider;
        private readonly IRnd _rand;

        private string _filename;

        public List<Neuron> Neurons { get; set; }
        public int Size { get; set; }

        // даже не знаю как удобнее через статик или каждому нейрону сделать ссылку на сеть
        public static bool isStarted = false;

        public NNet (ILogger<NNet> logger, IOptions<NNetConfig> optionsAccessor, IFileProvider provider, IRnd rand)
        {
            _logger = logger;
            _optionsAccessor = optionsAccessor;
            _filename = _optionsAccessor.Value.FileName;
            Size = _optionsAccessor.Value.Size;
            _provider = provider;
            _rand = rand;

            _logger.LogInformation(1111, "NNet constructor {FileName} {Size}", _filename, Size);

            if (string.IsNullOrWhiteSpace(_filename)) _filename = "test.murin";

            stop();

            if (_provider.GetFileInfo(_filename).Exists)
            {
                load(); 
            }
            else
            {
                init();
                save();
            }

            start();
        }

        public void start()
        {
            isStarted = true;
        }

        public void stop()
        {
            isStarted = false;
        }

        public void init()
        {
            _logger.LogInformation(1111, "NNet init");
            Neurons = new List<Neuron>();
            //RandomNumberGenerator generator = RandomNumberGenerator.Create();
            for (var i = 0; i < Size; ++i)
            {
                var n = new Neuron(_rand);
                Neurons.Add(n);
                n.tick();
            }

        }

        public void save()
        {
            _logger.LogInformation(1111, "NNet save");
            var File = System.IO.File.Create(_filename);
            using (var Writer = new System.IO.StreamWriter(File))
            {
                Writer.WriteLine(JsonConvert.SerializeObject(this, Formatting.Indented));
            }

        }

        public void load()
        {
            _logger.LogInformation(1111, "NNet load");
            // херовая идея => слишком много данных копируется. по идее надо как то десериализовать сразу в текущий объект. пока для тестов оставлю так
            var tmp = JsonConvert.DeserializeObject<NNet>(File.ReadAllText(_filename));
            this.Neurons = tmp.Neurons;
            this.Size = tmp.Size;

            foreach (var n in Neurons)
            {
                n.tick();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // Для определения избыточных вызовов

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: освободить управляемое состояние (управляемые объекты).
                    save();
                }

                // TODO: освободить неуправляемые ресурсы (неуправляемые объекты) и переопределить ниже метод завершения.
                // TODO: задать большим полям значение NULL.

                disposedValue = true;
            }
        }

        // TODO: переопределить метод завершения, только если Dispose(bool disposing) выше включает код для освобождения неуправляемых ресурсов.
        // ~NNet() {
        //   // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
        //   Dispose(false);
        // }

        // Этот код добавлен для правильной реализации шаблона высвобождаемого класса.
        void IDisposable.Dispose()
        {
            // Не изменяйте этот код. Разместите код очистки выше, в методе Dispose(bool disposing).
            Dispose(true);
            // TODO: раскомментировать следующую строку, если метод завершения переопределен выше.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
