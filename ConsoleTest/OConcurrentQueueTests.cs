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

        private int nn;
        private Task runReadTask(OConcurrentQueue<int>  q, CancellationTokenSource cTokenSource) {
            return Task.Run(async () =>
            {
                Console.WriteLine("read task run");
                //try
                //{
                while (await q.WaitAsync(cTokenSource.Token))
                {
                    //Console.WriteLine("queue not empty");
                    int n;
                    while (q.TryDequeue(out n))
                    {
                        //Console.WriteLine("readed from pipe: " + n);
                        nn = n; // иммитация нагрузки
                    }
                }
                Console.WriteLine("false? last:" + nn);
                //}
                //catch
                //{
                //    Console.WriteLine("posible cancel?");
                //}
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

            var readTask = runReadTask(q, cTokenSource);

            var workTask1 = Task.Run(async () =>
            {
                for (var i = 0; i < 10; i++, i++)
                {
                    Console.WriteLine("put in pipe: " + i);
                    q.Enqueue(i);
                    Console.WriteLine(".");
                    await Task.Delay(10);
                }
            });

            var killTask = runKillTask(50, cTokenSource);

            //Task.WaitAll(readTask);
            return Task.WhenAll(readTask);
        }

        // сколько интов мы прочитаем из очереди за 1 милисекунду (20 чтений за 10 милисекунд с выводом на консоль! без консоли астрономические цифры 19472, 17474 за 1 мс!)
        // 1 ms (после цикла 509918, 388412)
        // без консоли за 10 мс 21395, 17157, 23808, 24696 (после цикла 440525, 1009119, 965629)
        // 100 мс 58818, 242128, 243178
        // результаты довольно странные видимо силно зависит от прогретости проца и видимо очередь успевает расти быстрее чем с нее считывают
        // в общем за 1 мс может пройти дофига и слип даже на 1 мс делать нельзя
        public async Task TestPipeReadSpeed()
        {
            var q = new OConcurrentQueue<int>();
            var cTokenSource = new CancellationTokenSource();

            var readTask = runReadTask(q, cTokenSource);

            // время на запуск первой задачи (она медленная)
            //await Task.Delay(50);
            for (var i = 0; i < 1000000; i++) {
                var j = i * 2;
            }

            var workTask1 = Task.Run(async () =>
            {
                for (var i = 0; i < 3000000; i++)
                {
                    //Console.WriteLine("put in pipe: " + i);
                    q.Enqueue(i);
                    //Console.WriteLine(".");
                    await Task.Delay(0);
                }
            });

            var killTask = runKillTask(100, cTokenSource);

            //Task.WaitAll(readTask);
            await readTask;
        }
    }
}
