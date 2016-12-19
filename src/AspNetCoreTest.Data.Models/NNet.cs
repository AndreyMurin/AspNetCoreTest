﻿using System;
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
        // константы инициализации
        // макс мин вес связи
        public const double minWeight = -1;
        public const double maxWeight = 1;
        
        // максимальная глубина проникновения связей по координатам (в каждую сторону!) 
        public const int maxDeepRelationsZ = 2;
        public const int maxDeepRelationsY = 500;
        public const int maxDeepRelationsX = 500;

        // используем статик для разработки (чтобы получить доступ из нейронов) 
        private static ILogger<NNet> _logger;
        //private readonly ILogger<NNet> _logger;

        private readonly IOptions<NNetConfig> _optionsAccessor;
        private readonly IFileProvider _provider;
        private readonly IRnd _rand;

        private string _filename;

        // 2^32 = 4 294 967 296. а нам надо ~ 100 000 000 000 нейронов
        // значит нельзя делать одним списком (индекс инт)
        //public List<Neuron> Neurons { get; set; }

        // а вот таким образом теоретически мы можем реализовать до куя нейронов. в дальнейшем пределать на массивы если накладные расходы на List будут велики
        // внутри Output используем long это 2^64 = 18 446 744 073 709 551 616 я думаю этого хватит на нейроны всего человечества в целом
        // учесть в эволюционном алгоритме при изменении по осям икс игрек и зет => пересчитать Neuron.Neuron
        // помни адресацию по координатам Neurons[z][y][x]!
        public List<List<List<Neuron>>> Neurons { get; set; }

        // сеть трехмерная (договоримся что первый слой входы, тогда MaxX и MaxY определяют число входов)
        // последний слой - двигательные нейроны! тогда слоев должно быть минимум 3
        // длина по оси X
        public int LenX { get; set; }
        // длина по оси Y
        public int LenY { get; set; }
        // длина по оси Z (число слоев)
        public int LenZ { get; set; }

        // LenX * LenY * LenZ
        //private long _size { get { return LenX * LenY * LenZ; }  }

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

            if (LenZ < 3) LenZ = 3; // 3 слоя минимум 1 входной последний выход

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
                _setRelations();
                save();
            }
            _setOutputNeurons();
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

        // активация входов (за раз сразу несколько)
        public void SetInputs(Dictionary<NCoords, int> inputs)
        {

        }

        // установка безусловного рефлекса, будем проводить тупо по прямой от начальной точки до конечной (ставим макс вес если связи нет то создадим ее)
        // надо подумать хорошо ли так. с одной стороны максимальная скрость реакции с другой очень сложно подавить такой рефлекс условным

        // договоримся безусловный рефлекс проводить по всем слоям от начальной точки до конечной. при таком варианте получаем возможность подавить на любом уровне
        // и при желании можно увеличить скорость реакции прокачав более короткую связь (через несколько слоев) обучением
        // в теории, таким способом мы можем задать очень медленный безусловный рефлекс задав более длинный путь (попетляв по слоям туда сюда)
        public void SetUnconditionedReflex(List<NCoords> path)
        {
            for (var i = 1; i < path.Count; i++)
            {
                var beginN = Neurons[path[i - 1].Z][path[i - 1].Y][path[i - 1].X];
                var destN = path[i].ToSingle(LenX, LenY);
                var founded = false;
                foreach (var output in beginN.Output)
                {
                    if (output.Neuron == destN) // связь нашли усилим ее до макс и свалим
                    {
                        output.Weight = maxWeight;
                        founded = true;
                        break;
                    }
                }
                if (!founded)
                {
                    var o = new NOutput() { Neuron = destN, Weight = maxWeight };
                    o.SetNeuron(Neurons[path[i].Z][path[i].Y][path[i].X]);
                    beginN.Output.Add(o);
                }
            }
        }

        // устанавливаем у каждого нейрона в Neuron.Output _neuron соответсвующий  Neuron.Neuron (после сохранения, инициализации)
        // довольно медленная операция, но она происходит тока после загрузки и создания
        private void _setOutputNeurons()
        {
            foreach (var z in Neurons)
                foreach (var y in z)
                    foreach (var n in y)
                    {
                        foreach (var o in n.Output)
                        {
                            //var tmp = ;
                            var coords = new NCoords(o.Neuron, LenX, LenY, LenZ);
                            try
                            {
                                o.SetNeuron(Neurons[coords.Z][coords.Y][coords.X]);
                            }
                            catch (Exception e)
                            {
                                _logger.LogInformation(1111, "NNet::_setOutputNeurons() {exc} \n{Neuron} = ({X},{Y},{Z})", e.Message, o.Neuron, coords.X, coords.Y, coords.Z);
                            }
                        }
                    }
        }

        // установка связей (максимум макс инт!!!! так что ограничимся 2-3 слоя) глубина задана константами maxDeepRelationsX(YZ).
        // кроме первого слоя (там входы) и на первый слой тоже не делаем связи
        // последний слой выходной так что там нет выходов строго (если что руками позже увеличим число выходов)
        private void _setRelations()
        {
            for (var z = 0; z < LenZ-1; z++)
            {
                for (var y = 0; y < LenY; y++)
                {
                    for (var x = 0; x < LenX; x++)
                    {
                        Neurons[z][y][x].SetOutput(_createOutputForNeuron(x, y, z));
                    }
                }
            }
        }

        private List<NOutput> _createOutputForNeuron(int x, int y, int z)
        {
            var output = new List<NOutput>();
            // по оси Z распределение норм тут не надо замыкать последний слой на первый
            var minZ = z - maxDeepRelationsZ; if (minZ < 1) minZ = 1; var maxZ = z + maxDeepRelationsZ; if (maxZ > LenZ - 1) maxZ = LenZ - 1;
            
            // а вот по икс и игрек хотелось бы замкнуть первые нейроны на последние
            //todo сделать замыкание начала с концом ...
            var minY = y - maxDeepRelationsY; if (minY < 0) minY = 0; var maxY = y + maxDeepRelationsY; if (maxY > LenY - 1) maxY = LenY - 1;
            var minX = x - maxDeepRelationsX; if (minX < 0) minX = 0; var maxX = x + maxDeepRelationsX; if (maxX > LenX - 1) maxX = LenX - 1;

            for (var zz = minZ; zz <= maxZ; zz++) // первый слой входы (входы исключительно на другие слои)
            {
                for (var yy = minY; yy <= maxY; yy++)
                {
                    for (var xx = minX; xx <= maxX; xx++)
                    {
                        // связь на себя не допускаем, тока косвенная - через другие нейроны
                        if (x == xx && yy == y && z == zz) continue;

                        var coords = new NCoords(xx, yy, zz);
                        var o = new NOutput() { Neuron = coords.ToSingle(LenX, LenY), Weight = _rand.NextDouble(minWeight, maxWeight) };
                        o.SetNeuron(Neurons[zz][yy][xx]);

                        output.Add(o);
                    }
                }
            }

            return output;
        }


        private void startThreads() {
            //foreach (var n in Neurons.Where(i => i.isActive))
            foreach (var z in Neurons)
                foreach (var y in z)
                    foreach (var n in y.Where(i => i.isActive))
                    {
                Task.Factory.StartNew(()=> {
                    n.Tick();
                });/**/
            }
        }

        private void randomize()
        {
            _logger.LogInformation(1111, "NNet randomize");
            Neurons = new List<List<List<Neuron>>>();
            //RandomNumberGenerator generator = RandomNumberGenerator.Create();
            for (var z = 0; z < LenZ; z++)
            {
                Neurons.Add(new List<List<Neuron>>());
                for (var y = 0; y < LenY; y++)
                {
                    Neurons[z].Add(new List<Neuron>());
                    for (var x = 0; x < LenX; x++)
                    {
                        Neurons[z][y].Add(new Neuron(_rand));
                    }
                }
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
