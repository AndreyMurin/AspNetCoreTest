using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Abstractions
{
    public interface IStorage
    {
        T GetRepository<T>() where T : IRepository;
        void Save();
    }
}
