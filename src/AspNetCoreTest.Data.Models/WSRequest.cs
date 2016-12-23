using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Models
{
    public class WSRequest
    {
        public string Action { get; set; }
        public List<string> Args { get; set; }
    }
}
