using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Models
{
    public class NInp
    {
        public decimal weight;
        public Neuron neuron;
    }

    public class Neuron
    {

        public List<NInp> Input = new List<NInp>();

        public int state;

        public Neuron(Random rand)
        {
            state = rand.Next(0, 100);
        }

        public void init(List<Neuron> inp)
        {

        }

        void tick()
        {

        }


    }
}
