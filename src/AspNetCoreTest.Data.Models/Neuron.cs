using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace AspNetCoreTest.Data.Models
{
    // 32+64=96 (12 байт)
    public class NRelation
    {
        [JsonProperty("w")] // минимизируем названия
        // вес связи в диапазаоне думаю от -1 до 1 (возможно от -2 до 2 для повышения эффективности связи)
        // 32 бита
        public float Weight { get; set; }

        // индекс нейрона для сохранения-загрузки (long хватит за глаза)
        // 64 бита
        [JsonProperty("n")] // минимизируем названия
        public long Neuron { get; set; }
        
        // ссылка на нейрон (экономим оперативку)
        //private Neuron _neuron;

        public void SetNeuron(Neuron neuron) // никаких ref в параметрах не надо!
        {
            //_neuron = neuron;
        }
    }

    public class Neuron
    {
        // заряд нейрона при превышении определеного порга происходят разряды (spike)
        // нельзя делать свойством (иначе не получится использовать атомарные операции)
        private int _state;// { get; set; }
        public int State { get { return _state; } set { _state = value; } }

        // запущены таймеры и потоки
        private bool _isStarted = false;

        public List<NRelation> Output { get; set; }

        // нейрон в активном состоянии идут разряды
        private int _isActive;// { get; set; }
        //public int IsActive { get { return _isActive; } set { _isActive = value; } }

        // для сериалиции объекта
        public Neuron()
        {
            //Output = new List<NOutput>();
        }

        public static NNet Net;

        public Neuron(IRnd rand)
        {
            Output = new List<NRelation>();
            State = rand.Next(NNet.MIN_INIT_STATE, NNet.MAX_INIT_STATE);
        }

        // для отладки асинхронного чтения записи
        public Neuron(long index)
        {
            //float tmp = index;
            Output = new List<NRelation>();
            State = (int)index;
        }

        public void SetOutput(List<NRelation> output)
        {
            Output = output;
        }

        // в этой функции мы всегда! должны быть тока одним потоком. здесь мы программируем и выполняем цепочки разрядов с затуханием силы и с определенной частотой
        private Task SpikeAsync()
        {
            // нейрон активировался. НО он может быть уже активирован другим процессом. надо как-то без блокировки проверить
            // CompareExchange возвращает старое значение
            if (0 == Interlocked.CompareExchange(ref _isActive, 1, 0)) // if (_isActive==0) _isActive=1; 
            {
                return Task.Run(() =>
                {
                    Interlocked.Increment(ref NNet.Threads);

                    // чтение 32 разрядных всегда атомарно ?

                    var tasks = new List<Task>();
                    foreach (var o in Output)
                    {
                        var coord = new NCoords(o.Neuron, Net.LenX, Net.LenY, Net.LenZ);
                        var state = (int)(_state * o.Weight);
                        tasks.Add(Task.Run(() =>
                        {
                            Net.Neurons[coord.Z][coord.Y][coord.X].IncStateAsync(state);
                        }));
                    }
                    
                    // по идее мне не надо бы ждать тех других нейронов иначе у нас задача стартует задачу и так до бесконечности
                    Task.WaitAll(tasks.ToArray());

                    Interlocked.Decrement(ref NNet.Threads);

                    // обязательно освобождаем состояние
                    Interlocked.Exchange(ref _isActive, 0);

                });
            }
            // мы уже запущены
            return Task.CompletedTask;
        }

        // увеличиваем состояние нейрона и запускаем разряды
        // не делать async Task! так как в этом случае мы тупо встанем в зависон
        public Task IncStateAsync(int state)
        {
            return Task.Run(() =>
            {
                // счетчики потоков надо ставить в самой функции SpikeAsync так как разряды, затухания, повторения все в ней
                //Interlocked.Increment(ref NNet.Threads);

                var newState = Interlocked.Add(ref _state, state);

                // в этом месте значение _state может увеличится другим потоком, но нам как бы пофиг. либо этот либо тот процесс получат в newState значение выше порога и запустят разряд

                if ((newState > 0 && newState > NNet.MAX_STATE) || (newState < 0 && newState < NNet.MIN_STATE))
                {
                    // нейрон активировался. НО он может быть уже активирован другим процессом. надо как-то без блокировки проверить
                    // CompareExchange возвращает старое значение
                    //if (0 == Interlocked.CompareExchange(ref _isActive, 1, 0)) // if (_isActive==0) _isActive=1; 
                    //{
                        // здесь мы сто пудов одним потоком строго

                        // ждем окончания разрядов чтобы освободить нейрон для след разрядов
                        //Task.WaitAll(SpikeAsync());
                        SpikeAsync().Wait();

                        // обязательно освобождаем состояние
                    //    Interlocked.Exchange(ref _isActive, 0);
                    //}
                }

                //Interlocked.Decrement(ref NNet.Threads);
            });
        }

        public void Tick()
        {
            var tid = Thread.CurrentThread.ManagedThreadId;
            var NnetStarted = Interlocked.Read(ref NNet.isStarted);
            if (NnetStarted == 0) return; // сеть остановлена

            if (_isStarted) return;
            _isStarted = true;

            if (State > 1000) // типа мы активировались надо пересчитать состяния прицепленных нейронов
            {
                foreach (var o in Output)
                {

                }
            }

            _isStarted = false;
        }


    }
}
