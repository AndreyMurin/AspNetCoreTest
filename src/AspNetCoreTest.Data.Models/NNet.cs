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
    // класс для загрузки с конфиг файла
    public class NNetConfig
    {
        // имя файла для сохранения сети
        public string FileName { get; set; }
        // сеть трехмерная
        // длина по оси X
        public int LenX { get; set; }
        // длина по оси Y
        public int LenY { get; set; }
        // длина по оси Z (число слоев)
        public int LenZ { get; set; }
    }

    public class Coords
    {
        // все координаты начинаются с нуля (для оптимизации не будем юзать проперти)
        public int X;// { get; set; }
        public int Y;// { get; set; }
        public int Z;// { get; set; }

        // lenZ не обязательный параметр (по сути он нужен тока для проверки границ)
        // сделаем перегрузку с проверкой и без проверки
        // захерачить бы для оптимизации в инлайн, так как вызов данной функции довольно часто будет надо оптимизировать!
        // хотя таким способом мы будем тока инициализировать связи и задавать входные параметры а внутри везде юзаем одиночную координату!
        public long ToSingle(int lenX, int lenY)
        {
            return (X + lenX * Y + lenX * lenY * Z);
        }
        // эту прегрузку наверное не будем вызывать!!! так как тут проверки которые занимают время
        public long ToSingle(int lenX, int lenY, int lenZ)
        {
            // делать ли проверку на выход за пределы?
            if (X >= lenX) throw new Exception("Выход за пределы массива");
            if (Y >= lenY) throw new Exception("Выход за пределы массива");
            if (Z >= lenZ) throw new Exception("Выход за пределы массива");
            /**/
            return ToSingle(lenX, lenY);
        }
        /*
         lenX=3, lenY=4, lenZ=2
         (0,0,0) => 0
         (1,0,0) => 1
         (2,0,0) => 2
         (0,1,0) => 3
         (1,1,0) => 4
         (2,1,0) => 5
         */
    }

    public class NNet : IDisposable
    {
        private readonly ILogger<NNet> _logger;
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
        public static int isStarted = 0;
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

        public void SetInputs(Dictionary<Coords, int> inputs)
        {

        }

        private void startThreads() {
            foreach (var n in Neurons.Where(i => i.isActive))
            {
                Task.Factory.StartNew(()=> {
                    n.tick();
                });
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
            this.Neurons = tmp.Neurons;
            this.LenX = tmp.LenX;
            this.LenY = tmp.LenY;
            this.LenZ = tmp.LenZ;

            /*foreach (var n in Neurons)
            {
                n.tick();
            }*/
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
