using System;
using System.Collections.Generic;
using System.Linq;
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

        public long state;

        void init(List<Neuron> inp)
        {

        }

        void tick()
        {

        }


    }
}
