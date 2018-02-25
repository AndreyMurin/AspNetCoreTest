using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AspNetCoreTest.Data.Models.Tests
{
    public class OConcurrentQueueTests
    {
        private Task runKillTask(int t, CancellationTokenSource cTokenSource) {
            return Task.Run(async () =>
            {
                if (t > 0) await Task.Delay(t);
                cTokenSource.Cancel();
            });
        }

        [Fact]
        async public void TestPipeReadFirst()
        {
            var q = new OConcurrentQueue<int>();
            var cTokenSource = new CancellationTokenSource();

            var readTask = Task.Run(async () =>
            {
                try
                {
                    while (await q.WaitAsync(cTokenSource.Token))
                    {
                        int n;
                        while (q.TryDequeue(out n))
                        {
                            Console.WriteLine("readed from pipe: " + n);
                        }
                    }
                }
                catch { }
            });

            var workTask1 = Task.Run(async () =>
            {
                for (var i = 0; i < 10; i++, i++)
                {
                    Console.WriteLine("put in pipe: " + i);
                    await Task.Delay(10);
                }
            });

            var killTask = runKillTask(1000, cTokenSource);

            //Task.WaitAll(readTask);
            await Task.WhenAll(readTask);
            //Assert.True(res == 30);
        }
    }
}
