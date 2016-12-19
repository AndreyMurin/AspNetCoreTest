using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;

namespace AspNetCoreTest.Data.Models
{
    public class NOutput
    {
        public decimal Weight;// { get; set; }
        public long Neuron;// { get; set; }
        private Neuron _neuron;
    }

    public class Neuron
    {
        private bool _isStarted = false;

        public List<NOutput> Output { get; set; }

        public bool isActive { get; set; }
        public int State { get; set; }

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
