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
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;

namespace AspNetCoreTest.Data.Models
{
    
    public abstract class NNet : IDisposable
    {
        // константы инициализации
        // макс мин вес связи (по идее инициализация должна быть очень слабой! в пределах 0.1 и даже возможно меньше)
        // отрицательных связей не делаем - связь это проводимость тока. она, по сути, обратна сопротивлению
        // макс вес по той же причине не должен быть выше 1 (иначе идет по пизде закон сохранения энергии, при тупом расчете новых состояний он и так идет по пизде) 
        public const float MIN_INIT_WEIGHT = 0;//-0.5F;
        public const float MAX_INIT_WEIGHT = 0.5F;

        // вес связи для безусловного рефлекса
        public const float UNCONDITIONED_REFLEX_WEIGHT = 1;

        // макс и мин для состояний нейронов
        // пока не решил будут ли отрицательные состояния нейрнов в купе со связями или ограничимся тока отрицательными связями
        public const int MIN_INIT_STATE = -50;
        public const int MAX_INIT_STATE = 50;

        // пороговые значения состояний при которых нейрон преходит в активное состояние
        public const int MIN_STATE = -256;
        public const int MAX_STATE = 256;
        
        // а здесь будем хранить реальные минимум и максимум по весу связи (пока для отрисовки)
        public float MinWeight { get; set; }
        public float MaxWeight { get; set; }

        public const int MAX_SEND_ACTIVITIES = 10;
        public const int MAX_SPIKE_PERIOD = 3000; // милисекунд
        public const int MIN_SPIKE_PERIOD = 100; // милисекунд

        // максимум и минимум состояний нейронов
        //public double MinState { get; set; }
        //public double MaxState { get; set; }

        // нужно ли вести статистку по весам? в продакшене отключим я думаю (посмотрим по ресурсам) (пока делаю константой, в дальнейшем может поменяю на переменную из конфига!)
        public const bool NEED_STAT_WEIGHT = true;

        // максимальная глубина проникновения связей по координатам (в каждую сторону!) 
        public const int MAX_DEEP_RELATIONS_Z = 2;
        private const int MAX_DEEP_RELATIONS_Y = 10;
        private const int MAX_DEEP_RELATIONS_X = 10;
        [JsonIgnore]
        public int maxDeepRelationsY
        {
            get
            {
                var res = (LenY-1)/2;
                if (res > MAX_DEEP_RELATIONS_Y) return MAX_DEEP_RELATIONS_Y;
                return res;
            }
        }
        [JsonIgnore]
        public int maxDeepRelationsX
        {
            get
            {
                var res = (LenX-1)/2;
                if (res> MAX_DEEP_RELATIONS_X) return MAX_DEEP_RELATIONS_X;
                return res;
            }
        }

        // используем статик для разработки (чтобы получить доступ из нейронов) у нейрона вся сеть в статике
        [JsonIgnore]
        public readonly ILogger<NNet> _logger;
        //private readonly ILogger<NNet> _logger;
        public void LogInformation(int code, string message, params object[] args)
        {
            _logger.LogInformation(code, message, args);
        }

        protected readonly IOptions<NNetConfig> _optionsAccessor;
        protected readonly IRnd _rand;

        private string _filename;

        // 2^32 = 4 294 967 296. а нам надо ~ 100 000 000 000 нейронов
        // значит нельзя делать одним списком (индекс инт)
        //public List<Neuron> Neurons { get; set; }

        // а вот таким образом теоретически мы можем реализовать до куя нейронов. в дальнейшем пределать на массивы если накладные расходы на List будут велики
        // внутри Output используем long это 2^64 = 18 446 744 073 709 551 616 я думаю этого хватит на нейроны всего человечества в целом
        // учесть в эволюционном алгоритме при изменении по осям икс игрек и зет => пересчитать Neuron.Neuron
        // помни адресацию по координатам Neurons[z][y][x]!
        //[JsonObjectAttribute]
        public List<List<List<Neuron>>> Neurons { get; set; }

        // очередь активированных нейронов
        public ConcurrentQueue<QueueNeuron> Queue;

        // очередь отправки инфы об активных нейронах
        public ConcurrentQueue<SendActivity> SendActiveQueue;

        // тестируем сериализацию в лонг
        public long LongTest { get; set; }

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
        public static int isStarted = 0;
        // число одновременно запущеннных задач (активных нейронов, чтоб оперативно тормозить)
        public static int Threads = 0;

        // пустой конструктор для сериалиции
        //public NNet() { }

        public NNet (ILogger<NNet> logger, IOptions<NNetConfig> optionsAccessor, IRnd rand)
        {
            Neuron.Net = this;
            // тестируем сериализацию в лонг
            LongTest = 100123123123; // > 100 000 000 000
            _logger = logger;
            _optionsAccessor = optionsAccessor;
            _filename = _optionsAccessor.Value.FileName.TrimEnd(new char[] { '/', ' ' });
            LenX = _optionsAccessor.Value.LenX;
            LenY = _optionsAccessor.Value.LenY;
            LenZ = _optionsAccessor.Value.LenZ;
            _rand = rand;

            if (LenZ < 3) LenZ = 3; // 3 слоя минимум 1 входной последний выход

            _logger.LogInformation(1111, "NNet constructor {FileName} {MaxX} {MaxY} {MaxZ}", _filename, LenX, LenY, LenZ);

            if (string.IsNullOrWhiteSpace(_filename)) _filename = "test.murin";

            Stop();
            
            // инициализируем очередь задач
            Queue = new ConcurrentQueue<QueueNeuron>();

            if (System.IO.Directory.Exists(_filename) || System.IO.File.Exists(_filename + ".zip"))
            {
                load();
            }
            else
            {
                randomize();
                _setRelations();
                save();
            }

            //_initStatWeight();

            //_setOutputNeurons();
            //startThreads();
            Start();
        }

        private bool checkNeurons()
        {
            if (Neurons == null)
            {
                _logger.LogInformation(1111, "NNet checkNeurons:: Neurons is null");
                return false;
            }
            return true;
        }

        // цикл отправки данных другим частям и подписчикам
        private Task _sendActivities()
        {
            return Task.Run(() =>
            {
                List<SendActivity> list;
                SendActivity a;
                while (isStarted == 1)
                {
                    var kol = 0;
                    list = new List<SendActivity>();
                    while (SendActiveQueue.TryDequeue(out a) && kol<MAX_SEND_ACTIVITIES)
                    {
                        list.Add(a);
                    }
                    if (list.Any()) Task.WaitAll(SendActiveNeuronAsync(list));
                }
                // досылаем остатки и выходим. если много в остатке будет то будет отправлен большой пакет. это не очень хорошо так как неконтролируемо
                list = new List<SendActivity>();
                while (SendActiveQueue.TryDequeue(out a))
                {
                    list.Add(a);
                }
                if (list.Any()) Task.WaitAll(SendActiveNeuronAsync(list));
            });
        }

        // рабочий цикл сети. все потоки нейронов стартуем строго отсюда
        private Task _work()
        {
            if (0 == Interlocked.CompareExchange(ref isStarted, 1, 0))
            {
                // сохраним задачу чтобы ждать ее завершения при остановке
                return Task.Run(() =>
                {
                    while (isStarted == 1)
                    {
                        try
                        {
                            //_logger.LogInformation("Try read task .... IsEmpty={IsEmpty}", Queue.IsEmpty);
                            QueueNeuron n;
                            if (/*!Queue.IsEmpty &&*/ Queue.TryDequeue(out n))
                            {
                                _logger.LogInformation("Exec task {coords}", n.Coords);
                                //_logger.LogInformation("Task exists");
                                // надо как то организовать теперь управление запущенными задачами
                                // в принципе мы можем следить за NNet.Threads там как раз счетчик запущенных потоков именно нейронов
                                var task = n.Neuron.SpikeAsync(n.Coords.X, n.Coords.Y, n.Coords.Z);
                            }
                            else
                            {
                                //_logger.LogInformation("Task not exists");
                                // засыпать или нет? стопарнем на милисекунду, чтобы не сильно нагружать процессор пока нет новых заданий
                                Thread.Sleep(1);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("----> Error: {e}", e);
                            Thread.Sleep(1000);
                        }
                        //finally
                        //{
                        //    //_logger.LogError("----> finally!!!!!!!!!!!!!!!!!!!!");
                        //    Thread.Sleep(1000);
                        //}
                    }
                    // пока тока так можно гарантирвоать окончание работы всех нейронов? но не более 2 секунд
                    var exitDelim = 0;
                    var ts = 10;
                    while (Threads > 0 && exitDelim < 5000) { exitDelim += ts; Thread.Sleep(ts); }
                    if (Threads > 0) _logger.LogError("----> Not all task was completed!!!! {t}", Threads);
                    Threads = 0;
                });
                //return _workingTask;
            }
            return Task.CompletedTask;
        }

        private Task _workingTask = Task.CompletedTask;
        private Task _workingSATask = Task.CompletedTask;
        // запускаем сеть в работу (потоки обработки нейронов не затрагиваются)
        public void Start()
        {
            Threads = 0;

            // а вот очередь отправки мы инициализируем тут
            SendActiveQueue = new ConcurrentQueue<SendActivity>();

            _workingTask = _work();

            _workingSATask = _sendActivities();
        }

        // ставим сеть на паузу (потоки обработки нейронов не затрагиваются)
        public void Stop()
        {
            // присвоение без блокировки
            Interlocked.Exchange(ref isStarted, 0);

            //_workingTask.Wait();
            Task.WaitAll( new Task[] { _workingTask, _workingSATask } );
        }

        // активация входов (за раз сразу несколько)
        public void SetInputs(Dictionary<NCoords, int> inputs)
        {
            var tasks = new List<Task>();
            foreach (var inp in inputs)
            {
                var coord = inp.Key;
                var state = inp.Value;
                //tasks.Add(Task.Run(() =>
                //{
                    Neurons[coord.Z][coord.Y][coord.X].IncState(state, coord);
                //}));
            }
            //return Task.WhenAll(tasks);
        }

        // рассылаем всем подписчикам инфу о том что нейрон активировался
        public abstract Task SendActiveNeuronAsync(List<SendActivity> list);
        /*{
            return Task.CompletedTask;
        }*/

        // установка безусловного рефлекса, будем проводить тупо по прямой от начальной точки до конечной (ставим макс вес если связи нет то создадим ее)
        // надо подумать хорошо ли так. с одной стороны максимальная скрость реакции с другой очень сложно подавить такой рефлекс условным

        // договоримся безусловный рефлекс проводить по всем слоям от начальной точки до конечной. при таком варианте получаем возможность подавить на любом уровне
        // и при желании можно увеличить скорость реакции прокачав более короткую связь (через несколько слоев) обучением
        // в теории, таким способом мы можем задать очень медленный безусловный рефлекс задав более длинный путь (попетляв по слоям туда сюда)
        public void SetUnconditionedReflex(List<NCoords> path)
        {
            if (!checkNeurons()) return;
            for (var i = 1; i < path.Count; i++)
            {
                var beginN = Neurons[path[i - 1].Z][path[i - 1].Y][path[i - 1].X];
                var destN = path[i].ToSingle(LenX, LenY);
                var founded = false;
                foreach (var output in beginN.Output)
                {
                    if (output.Neuron == destN) // связь нашли усилим ее до макс и свалим
                    {
                        output.Weight = UNCONDITIONED_REFLEX_WEIGHT;
                        founded = true;
                        break;
                    }
                }
                if (!founded)
                {
                    var o = new NRelation() { Neuron = destN, Weight = UNCONDITIONED_REFLEX_WEIGHT };
                    o.SetNeuron(Neurons[path[i].Z][path[i].Y][path[i].X]);
                    beginN.Output.Add(o);
                }
            }
        }

        // устанавливаем у каждого нейрона в Neuron.Output _neuron соответсвующий  Neuron.Neuron (после сохранения, инициализации)
        // довольно медленная операция, но она происходит тока после загрузки и создания
        // в данный момент не используется
        /*private void _setOutputNeurons()
        {
            if (!checkNeurons()) return;
            foreach (var z in Neurons)
                foreach (var y in z)
                    foreach (var n in y)
                        foreach (var o in n.Output)
                        {
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
        }*/

        // поиск всех входов для нейрона (пока исключительно для отрисовки)
        // не будем использовать
        /*protected List<NRelation> findNeuronInputs(int x, int y, int z)
        {
            var n = new NCoords(x, y, z).ToSingle(LenX, LenY);

            var res = new List<NRelation>();
            for (var zz = 0; zz < LenZ; zz++)
            {
                for (var yy = 0; yy < LenY; yy++)
                {
                    for (var xx = 0; xx < LenX; xx++)
                    {
                        foreach (var o in Neurons[zz][yy][xx].Output)
                        {
                            if (n == o.Neuron) res.Add(o);
                        }
                    }
                }
            }
            return res;
        }*/

        // очень тяжелая инициализация при большом количестве нейронов и связей, начальное значение вычисляем при инициализации и сохраняем в файле
        /*private void _initStatWeight()
        {
            #pragma warning disable CS0162 // Обнаружен недостижимый код
            if (!NEED_STAT_WEIGHT) return;
            #pragma warning restore CS0162 // Обнаружен недостижимый код
            if (!checkNeurons()) return;

            MinWeight = 0;
            MaxWeight = 0;

            foreach (var z in Neurons)
                foreach (var y in z)
                    foreach (var n in y)
                        foreach (var o in n.Output)
                        {
                            if (MinWeight > o.Weight) MinWeight = o.Weight;
                            if (MaxWeight < o.Weight) MaxWeight = o.Weight;
                        }
        }*/

        // установка связей (максимум макс инт (индекс List - int)!!!! так что ограничимся 2-3 слоя) глубина задана константами maxDeepRelationsX(YZ).
        // кроме первого слоя (там входы) и на первый слой тоже не делаем связи
        // последний слой выходной так что там нет выходов строго (если что руками позже увеличим число выходов)
        private void _setRelations()
        {
            if (!checkNeurons()) return;
            for (var z = 0; z < LenZ-1; z++)
            {
                for (var y = 0; y < LenY; y++)
                {
                    for (var x = 0; x < LenX; x++)
                    {
                        Neurons[z][y][x].SetOutput(_createOutputForNeuron(x, y, z));
                        if (x % 500 == 0)
                        {
                            _logger.LogInformation(1111, "NNet _setRelations z={z}, y={y}, x={x}. Relations count={c}", z, y, x, Neurons[z][y][x].Output.Count);
                        }
                    }
                }
            }
        }

        // создаем выходные связи для нейрона (рандомно)
        private List<NRelation> _createOutputForNeuron(int x, int y, int z)
        {
            if (NEED_STAT_WEIGHT)
            {
                MinWeight = 0;
                MaxWeight = 0;
            }

            var output = new List<NRelation>();
            // по оси Z распределение норм тут не надо замыкать последний слой на первый
            var minZ = z - MAX_DEEP_RELATIONS_Z; if (minZ < 1) minZ = 1; var maxZ = z + MAX_DEEP_RELATIONS_Z; if (maxZ > LenZ - 1) maxZ = LenZ - 1;
            
            // а вот по икс и игрек хотелось бы замкнуть первые нейроны на последние
            var minY = y - maxDeepRelationsY; /*if (minY < 0) minY = 0;*/ var maxY = y + maxDeepRelationsY;// if (maxY > LenY - 1) maxY = LenY - 1;
            var minX = x - maxDeepRelationsX; /*if (minX < 0) minX = 0;*/ var maxX = x + maxDeepRelationsX;// if (maxX > LenX - 1) maxX = LenX - 1;
            /*if (z==0 && y==1 && x==2)
                _logger.LogInformation(2111, "NNet _createOutputForNeuron  {x}-{xx} {y}-{yy} {z}-{zz}", minX, maxX, minY, maxY, minZ, maxZ);
            /**/
            for (var zz = minZ; zz <= maxZ; zz++) // первый слой входы (входы исключительно на другие слои)
            {
                for (var yy = minY; yy <= maxY; yy++)
                {
                    for (var xx = minX; xx <= maxX; xx++)
                    {
                        // замыкания по оси икс и игрек
                        var yyy = yy; var xxx = xx;
                        if (yy < 0) yyy = LenY + yy; if (yy > LenY - 1) yyy = yy - (LenY);
                        if (xx < 0) xxx = LenX + xx; if (xx > LenX - 1) xxx = xx - (LenX);

                        // связь на себя не допускаем, тока косвенная - через другие нейроны
                        if (x == xxx && yyy == y && z == zz) continue;

                        var coords = new NCoords(xxx, yyy, zz);
                        var o = new NRelation() { Neuron = coords.ToSingle(LenX, LenY), Weight = _rand.NextFloat(MIN_INIT_WEIGHT, MAX_INIT_WEIGHT), WeightChange=0 };
                        if (NEED_STAT_WEIGHT)
                        {
                            // здесь не надо юзать WeightSum так как WeightChange==0
                            if (MinWeight > o.Weight) MinWeight = o.Weight;
                            if (MaxWeight < o.Weight) MaxWeight = o.Weight;
                        }
                        /*if (z == 0 && y == 1 && x == 2)
                        {
                            _logger.LogInformation(1111, "NNet _createOutputForNeuron for ({x}, {y}, {z}) => {n} ({xx}, {yy}, {zz})", x, y, z, (new NCoords(xxx, yyy, zz)).ToSingle(LenX, LenY), xxx, yyy, zz);
                        }/**/

                        o.SetNeuron(Neurons[zz][yyy][xxx]);

                        output.Add(o);
                    }
                }
            }

            return output;
        }

        /*
        private void startThreads()
        {
            if (!checkNeurons()) return;
            
            //foreach (var n in Neurons.Where(i => i.isActive))
            foreach (var z in Neurons)
                foreach (var y in z)
                    foreach (var n in y.Where(i => i.isActive))
                    {
                        Task.Factory.StartNew(() =>
                        {
                            n.Tick();
                        });
                    }
            
        }/**/

        // создание нейронов
        private void randomize()
        {
            _logger.LogInformation(1111, "NNet randomize");
            Neurons = new List<List<List<Neuron>>>();
            //RandomNumberGenerator generator = RandomNumberGenerator.Create();
            long index = 0;
            for (var z = 0; z < LenZ; z++)
            {
                Neurons.Add(new List<List<Neuron>>());
                for (var y = 0; y < LenY; y++)
                {
                    Neurons[z].Add(new List<Neuron>());
                    for (var x = 0; x < LenX; x++)
                    {
                        // нормальный режим добавления нейрона
                        Neurons[z][y].Add(new Neuron(_rand, index++));
                        
                        // отладка асинхронного чтения записи
                        //_logger.LogInformation(1111, "NNet randomize DEBUG MODE DEBUG MODE DEBUG MODE");
                        //Neurons[z][y].Add(new Neuron((new NCoords(x,y,z)).ToSingle(LenX, LenY)));
                    }
                }
            }
        }

        #region SAVE

        private async Task _saveNeuronAsync(string filename, int x, int y, int z)
        {
            //_logger.LogInformation(1111, "NNet _saveNeuronTask save {filename} {state} {id}", filename, Neurons[z][y][x].State, Thread.CurrentThread.ManagedThreadId);
            //Thread.Sleep(_rand.Next(2000, 5000));
            var f = System.IO.File.Create(filename);
            using (var Writer = new System.IO.StreamWriter(f))
                await Writer.WriteAsync(JsonConvert.SerializeObject(
                                Neurons[z][y][x]
                                , Formatting.Indented
                            ));
            //_logger.LogInformation(1111, "NNet _saveNeuronTask saved {filename} {state}", filename, Neurons[z][y][x].State);
        }

        private void save(bool pack=false)
        {
            _logger.LogInformation(1111, "NNet save");
            if (!checkNeurons()) return;

            // удаление старых данных (переименование!) если они есть
            try
            {
                System.IO.Directory.Move(_filename, _filename + "." + DateTime.Now.ToString("s").Replace(":", "-"));
            }
            catch (Exception e)
            {
                _logger.LogError(1111, "NNet save params {source} -> {dest}", _filename, _filename + "." + DateTime.Now.ToString("s").Replace(":", "-"));
                _logger.LogError(1111, "NNet save error: {e}", e);
            }

            var dir = System.IO.Directory.CreateDirectory(_filename);
            var File = System.IO.File.Create(_filename + "/config.murin");
            using (var Writer = new System.IO.StreamWriter(File))
            {
                Writer.WriteLine(JsonConvert.SerializeObject(
                    new WSResponseConfig { LenX = LenX, LenY = LenY, LenZ = LenZ, MinWeight = MinWeight, MaxWeight = MaxWeight, MaxState = MAX_STATE, MinState = MIN_STATE }
                    , Formatting.Indented
                ));
            }
            /*File = System.IO.File.Create(_filename + "/queue.murin");
            using (var Writer = new System.IO.StreamWriter(File))
            {
                Writer.WriteLine(JsonConvert.SerializeObject(
                    Queue.Select(i=>i.Index).ToList()
                    , Formatting.Indented
                ));
            }*/

            for (var z = 0; z < LenZ; z++)
            {
                System.IO.Directory.CreateDirectory(_filename + "/" + z);

                Task[] tasksY = new Task[LenY];
                for (var y = 0; y < LenY; y++)
                {
                    System.IO.Directory.CreateDirectory(_filename + "/" + z + "/" + y);
                    var yy = y;

                    tasksY[yy] = Task.Run(() =>
                    {
                        Task[] tasksX = new Task[LenX];
                        for (var x = 0; x < LenX; x++)
                        {
                            tasksX[x] = _saveNeuronAsync(_filename + "/" + z + "/" + yy + "/" + x + ".neuron", x, yy, z);
                            /*var f = System.IO.File.Create(_filename + "/" + z + "/" + y + "/" + x + ".neuron");
                            using (var Writer = new System.IO.StreamWriter(f))
                            {
                                Writer.WriteLine(JsonConvert.SerializeObject(
                                    Neurons[z][y][x]
                                    , Formatting.Indented
                                ));
                            }*/
                        }
                        //Task.WaitAll(tasksX);
                        return Task.WhenAll(tasksX);
                    });
                    //_logger.LogInformation(1111, "NNet save end y={y}", y);
                }
                //_logger.LogInformation(1111, "NNet save Task.WaitAll(tasksY) {z}",z);
                Task.WaitAll(tasksY);
            }

            if (pack)
            {
                // архивируем данные
            }

        }

        #endregion

        #region LOAD

        private async Task _loadNeuronAsync(string filename, int x, int y, int z)
        {
            //return Task.Run(()=> { });
            //StringBuilder contents = new StringBuilder();
            using (StreamReader SourceReader = File.OpenText(filename))
            {
                /*string nextLine;
                while ((nextLine = await SourceReader.ReadLineAsync()) != null)
                {
                    contents.Append(nextLine);
                }*/
                //var res = await SourceReader.ReadToEndAsync();
                Neurons[z][y][x] = JsonConvert.DeserializeObject<Neuron>(await SourceReader.ReadToEndAsync());
                if (Neurons[z][y][x].IsActive > 0)
                {
                    Queue.Enqueue(new QueueNeuron { Neuron = Neurons[z][y][x], Coords = new NCoords(x, y, z) });
                }
            }

            //var n = JsonConvert.DeserializeObject<Neuron>(contents.ToString());
            //Neurons[z][y][x] = n;
        }/**/

        private void _loadFrom(string folder)
        {
            var tmp = JsonConvert.DeserializeObject<WSResponseConfig>(File.ReadAllText(folder + "/config.murin"));
            LenX = tmp.LenX;
            LenY = tmp.LenY;
            LenZ = tmp.LenZ;
            MinWeight = tmp.MinWeight;
            MaxWeight = tmp.MaxWeight;

            Queue = new ConcurrentQueue<QueueNeuron>();
            //var queue = JsonConvert.DeserializeObject<List<long>>(File.ReadAllText(folder + "/queue.murin"));

            Neurons = new List<List<List<Neuron>>>();
            for (var z = 0; z < LenZ; z++)
            {
                //var zz = z; // индекс z не может изменится пока выполняются задачи так что его не будем в локаль копировать
                Neurons.Add(new List<List<Neuron>>());

                Task[] tasksY = new Task[LenY];
                for (var y = 0; y < LenY; y++)
                {
                    Neurons[z].Add(new List<Neuron>(LenX));
                    var yy = y; // а вот Y меняется пока задача выполняется для 0 а у нас уже y=1 (вот тут будет проблема for (var x = 0; x < LenX; x++) Neurons[z][1].Add(null); а мы еще в моменте создания для y=1 Neurons[z].Add(new List<Neuron>(LenX));)

                    tasksY[yy] = Task.Run(()=>
                    {
                        Task[] tasksX = new Task[LenX];
                        for (var x = 0; x < LenX; x++) Neurons[z][yy].Add(null);
                        for (var x = 0; x < LenX; x++)
                        {
                            /*var n = JsonConvert.DeserializeObject<Neuron>(File.ReadAllText(folder + "/" + z + "/" + y + "/" + x + ".neuron"));
                            Neurons[z][y][x] = n;/**/
                            tasksX[x] = _loadNeuronAsync(folder + "/" + z + "/" + yy + "/" + x + ".neuron", x, yy, z);
                        }
                        //Task.WaitAll(tasksX);
                        return Task.WhenAll(tasksX);
                    });
                }
                Task.WaitAll(tasksY);
            }

            /*foreach (var i in queue)
            {
                var c = new NCoords(i, LenX, LenY, LenZ);
                Queue.Enqueue(Neurons[c.Z][c.Y][c.X]);
            }*/
        }

        private void load()
        {
            _logger.LogInformation(1111, "NNet load");
            // херовая идея => слишком много данных копируется. по идее надо как то десериализовать сразу в текущий объект. пока для тестов оставлю так
            // хотя копируются ссылки так что норм
            try
            {
                if (System.IO.Directory.Exists(_filename))
                {
                    _loadFrom(_filename);
                }
                else if (System.IO.File.Exists(_filename + ".zip"))
                {
                    // todo доделать
                }

            }
            catch (Exception e)
            {
                _logger.LogInformation(1111, "NNet load error:" + e.ToString());
            }
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // Для определения избыточных вызовов

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // обязательно стопарнуть сеть!
                    Stop();
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
