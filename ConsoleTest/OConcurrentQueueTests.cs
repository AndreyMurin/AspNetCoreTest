using AspNetCoreTest.Data.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleTest
{
    public class OConcurrentQueueTests
    {
        private Task runKillTask(int t, CancellationTokenSource cTokenSource)
        {
            return Task.Run(async () =>
            {
                if (t > 0) await Task.Delay(t);
                cTokenSource.Cancel();
            });
        }

        public async Task TestZero() {
            var t = Task.Run(() =>
            {
                var tmp = new TaskCompletionSource<int>();
                tmp.SetResult(100500);

                return tmp.Task;
            });

            var res = await t;
            Console.WriteLine("zero task completed:" + res);
        }

        public Task TestPipeReadFirst()
        {
            var q = new OConcurrentQueue<int>();
            var cTokenSource = new CancellationTokenSource();

            var readTask = Task.Run(async () =>
            {
                Console.WriteLine("read task run");
                try
                {
                    while (await q.WaitAsync(cTokenSource.Token))
                    {
                        //Console.WriteLine("queue not empty");
                        int n;
                        while (q.TryDequeue(out n))
                        {
                            Console.WriteLine("readed from pipe: " + n);
                        }
                    }
                }
                catch {
                    Console.WriteLine("posible cancel?");
                }
            });

            var workTask1 = Task.Run(async () =>
            {
                for (var i = 0; i < 10; i++, i++)
                {
                    Console.WriteLine("put in pipe: " + i);
                    q.Enqueue(i);
                    await Task.Delay(10);
                }
            });

            //var killTask = runKillTask(3000, cTokenSource);

            //Task.WaitAll(readTask);
            return Task.WhenAll(readTask);
            //Assert.True(res == 30);
        }
    }
}
