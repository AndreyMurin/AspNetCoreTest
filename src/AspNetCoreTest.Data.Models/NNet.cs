using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Models
{
    public class NNet
    {
        private List<Neuron> Neurons;

        NNet(int size) {
            Neurons = new List<Neuron>(size);
        }
    }
}
