using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;

namespace AspNetCoreTest.Data.Models
{
    
    public class NNet : IDisposable
    {
        // используем статик для разработки (чтобы получить доступ из нейронов)
        private static ILogger<NNet> _logger;
        //private readonly ILogger<NNet> _logger;

        private readonly IOptions<NNetConfig> _optionsAccessor;
        private readonly IFileProvider _provider;
        private readonly IRnd _rand;

        private string _filename;

        public List<Neuron> Neurons { get; set; }
        // сеть трехмерная (договоримся что первый слой входы, тогда MaxX и MaxY определяют число входов)
        // длина по оси X
        public int LenX { get; set; }
        // длина по оси Y
        public int LenY { get; set; }
        // длина по оси Z (число слоев)
        public int LenZ { get; set; }
        // LenX * LenY * LenZ
        private long _size { get { return LenX * LenY * LenZ; }  }

        // даже не знаю как удобнее через статик или каждому нейрону сделать ссылку на сеть
        public static long isStarted = 0;
        // число одновременно запущеннных задач (активных нейронов, чтоб оперативно тормозить)
        public static int Threads = 0;

        public NNet (ILogger<NNet> logger, IOptions<NNetConfig> optionsAccessor, IFileProvider provider, IRnd rand)
        {
            _logger = logger;
            _optionsAccessor = optionsAccessor;
            _filename = _optionsAccessor.Value.FileName;
            LenX = _optionsAccessor.Value.LenX;
            LenY = _optionsAccessor.Value.LenY;
            LenZ = _optionsAccessor.Value.LenZ;
            _provider = provider;
            _rand = rand;

            _logger.LogInformation(1111, "NNet constructor {FileName} {MaxX} {MaxY} {MaxZ}", _filename, LenX, LenY, LenZ);

            if (string.IsNullOrWhiteSpace(_filename)) _filename = "test.murin";

            Stop();

            if (_provider.GetFileInfo(_filename).Exists)
            {
                load(); 
            }
            else
            {
                randomize();
                save();
            }
            startThreads();
            Start();
        }

        // запускаем сеть в работу (потоки обработки нейронов не затрагиваются)
        public void Start()
        {
            // присвоение без блокировки
            Interlocked.Exchange(ref isStarted, 1);
            //isStarted = 1;
        }

        // ставим сеть на паузу (потоки обработки нейронов не затрагиваются)
        public void Stop()
        {
            // присвоение без блокировки
            Interlocked.Exchange(ref isStarted, 0);
            //isStarted = 0;
        }

        public void SetInputs(Dictionary<NCoords, int> inputs)
        {

        }

        // установка связей все ко всем
        public void SetRelations()
        {
            for (var i = 0; i < _size; i++)
            {
                var output = new List<NOutput>();

                Neurons[i].SetOutput(output);
            }

        }

        private void startThreads() {
            foreach (var n in Neurons.Where(i => i.isActive))
            {
                Task.Factory.StartNew(()=> {
                    n.Tick();
                });/**/
            }
        }

        private void randomize()
        {
            _logger.LogInformation(1111, "NNet randomize");
            Neurons = new List<Neuron>();
            //RandomNumberGenerator generator = RandomNumberGenerator.Create();
            for (var i = 0; i < _size; i++)
            {
                var n = new Neuron(_rand);
                Neurons.Add(n);
                //n.tick();
            }
        }

        private void save()
        {
            _logger.LogInformation(1111, "NNet save");
            var File = System.IO.File.Create(_filename);
            using (var Writer = new System.IO.StreamWriter(File))
            {
                Writer.WriteLine(JsonConvert.SerializeObject(this, Formatting.Indented));
            }

        }

        private void load()
        {
            _logger.LogInformation(1111, "NNet load");
            // херовая идея => слишком много данных копируется. по идее надо как то десериализовать сразу в текущий объект. пока для тестов оставлю так
            var tmp = JsonConvert.DeserializeObject<NNet>(File.ReadAllText(_filename));
            // если Neurons static то присваивания не надо
            this.Neurons = tmp.Neurons;
            this.LenX = tmp.LenX;
            this.LenY = tmp.LenY;
            this.LenZ = tmp.LenZ;

            /*foreach (var n in Neurons)
            {
                n.tick();
            }*/
        }

        /*public async Task<string> GetFirstCharactersCountAsync(string url, int count)
        {
            // Execution is synchronous here
            var client = new System.Net.Http.HttpClient();

            // Execution of GetFirstCharactersCountAsync() is yielded to the caller here
            // GetStringAsync returns a Task<string>, which is *awaited*
            var page = await client.GetStringAsync("http://www.dotnetfoundation.org");

            // Execution resumes when the client.GetStringAsync task completes,
            // becoming synchronous again.

            if (count > page.Length)
            {
                return page;
            }
            else
            {
                return page.Substring(0, count);
            }
        }/**/

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
