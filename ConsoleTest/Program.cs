using System;
using System.Threading.Tasks;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("Hello World!");
            var t = new OConcurrentQueueTests();

            //Task.WaitAll(t.TestZero());
            //Task.WaitAll(t.TestPipeReadFirst());

            //Task.WaitAll(t.TestPipeReadSpeed());

            Task.WaitAll(t.TestPipeWriteFirst());
        }

    }
}
