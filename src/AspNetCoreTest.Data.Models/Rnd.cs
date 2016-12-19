using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Models
{
    public interface IRnd
    {
        int Next(int minValue, int maxValue);
        double NextDouble(double minValue, double maxValue);
    }

    public class Rnd : IRnd
    {
        private readonly Random _rand;

        public Rnd()
        {
            //RandomNumberGenerator generator = RandomNumberGenerator.Create();
            _rand = new Random((int)DateTime.Now.Ticks);
        }

        public int Next(int minValue, int maxValue)
        {
            return _rand.Next(minValue, maxValue);
        }

        public double NextDouble(double minValue, double maxValue)
        {
            return _rand.NextDouble() * (maxValue - minValue) + minValue;
        }

    }
}
