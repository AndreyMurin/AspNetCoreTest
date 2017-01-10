using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCoreTest.Data.Models
{
    public class NNetClient : NNet
    {
        public NNetClient(ILogger<NNet> logger, IOptions<NNetConfig> optionsAccessor, IRnd rand) : base(logger, optionsAccessor, rand)
        {
        }

        public async Task Connect(string href)
        {

        }

    }
}
