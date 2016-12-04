using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace AspNetCoreTest.Data.Models
{
    public class NNet
    {
        private List<Neuron> Neurons;
        private string FileName;

        public void init()
        {

        }

        NNet(int size, string filename) {
            //System.Reflection.Assembly.GetExecutingAssembly().Location
            if (string.IsNullOrWhiteSpace(filename)) filename = Path.GetTempFileName();
            FileName = filename;
            Neurons = new List<Neuron>(size);
            init();
        }

        NNet(string filename)
        {
            FileName = filename;
            Neurons = new List<Neuron>();
            load(filename);
        }

        public void save()
        {

        }

        public void load(string filename)
        {

        }
    }
}
