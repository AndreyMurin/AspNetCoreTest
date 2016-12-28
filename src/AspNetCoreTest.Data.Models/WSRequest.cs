using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Models
{
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

    public class WSResponseConfig : WSResponse
    {
        // длина по оси X
        public int LenX { get; set; }
        // длина по оси Y
        public int LenY { get; set; }
        // длина по оси Z (число слоев)
        public int LenZ { get; set; }
    }
}
