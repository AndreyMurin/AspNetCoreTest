using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Models
{
    public class NInp
    {
        
        public decimal weight { get; set; }
        public int neuron { get; set; }
    }

    public class Neuron
    {
        private bool _isStarted = false;

        public List<NInp> input { get; set; }

        public bool isActive { get; set; }
        public int state { get; set; }

        public Neuron(IRnd rand)
        {
            input = new List<NInp>();
            state = rand.Next(0, 100);
        }

        public void init(List<Neuron> inp)
        {

        }

        public void tick()
        {
            if (_isStarted) return;

        }


    }
}
