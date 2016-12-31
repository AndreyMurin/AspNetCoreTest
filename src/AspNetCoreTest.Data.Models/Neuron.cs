using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace AspNetCoreTest.Data.Models
{
    public class NOutput
    {
        [JsonProperty("w")]
        // вес связи в диапазаоне думаю от -1 до 1 (возможно от -2 до 2 для повышения эффективности связи)
        public double Weight { get; set; }

        // индекс нейрона для сохранения-загрузки (long хватит за глаза)
        [JsonProperty("n")]
        public long Neuron { get; set; }
        
        // ссылка на нейрон
        private Neuron _neuron;

        public void SetNeuron(Neuron neuron) { _neuron = neuron; } // никаких ref в параметрах не надо!
    }

    public class Neuron
    {
        // запущены таймеры и потоки
        private bool _isStarted = false;

        public List<NOutput> Output { get; set; }

        // нейрон в активном состоянии идут разряды
        public bool isActive { get; set; }

        // заряд нейрона при превышении определеного порга происходят разряды (spike)
        public int State { get; set; }

        // для сериалиции объекта
        public Neuron()
        {
            //Output = new List<NOutput>();
        }

        public Neuron(IRnd rand)
        {
            Output = new List<NOutput>();
            State = rand.Next(0, 100);
        }

        public void SetOutput(List<NOutput> output)
        {
            Output = output;
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
