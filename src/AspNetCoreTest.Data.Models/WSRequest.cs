using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Models
{
    public class NeuronForDraw
    {
        public Neuron Neuron { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public int z { get; set; }
        
        // массив входных нейронов. надо оптимизировать! здесь будет нул если выбрана вся сеть
        public List<NRelation> Input { get; set; }
    }

    public class WSRequest
    {
        public string Action { get; set; }
        public List<string> ArgsString { get; set; }
        public List<int> ArgsInt { get; set; }
    }

    public class WSResponse
    {
        public string Action { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
        //public List<string> Args { get; set; }
    }

    public class WSResponseNeurons : WSResponse
    {
        public List<NeuronForDraw> Neurons { get; set; }
    }

    public class WSResponseConfig : WSResponse
    {
        // длина по оси X
        public int LenX { get; set; }
        // длина по оси Y
        public int LenY { get; set; }
        // длина по оси Z (число слоев)
        public int LenZ { get; set; }

        // статистика по весам (если есть конечно)
        public double MinWeight { get; set; }
        public double MaxWeight { get; set; }
    }
}
