using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Models
{
    //public delegate void ConcurrentQueueChangedEventHandler<T>(object sender);
    public delegate void tmpHandler(object sender);

    public class OConcurrentQueue<T> : ConcurrentQueue<T>
    {
        private TaskCompletionSource<bool> tcs = null;
        //private int max = 1;
      
        //public event ConcurrentQueueChangedEventHandler<T> ContentChanged;
        private event Action<TaskCompletionSource<object>> handler = (TaskCompletionSource<object> _tcs) => {
                //if (tcs == null) return;
                _tcs?.SetCanceled(); _tcs = null;
            };

        public new void Enqueue(T item)
        {
            base.Enqueue(item);

            // Raise event added event
            this.OnContentChanged();
        }

        private void OnContentChanged()
        {
            Console.WriteLine("Content changed");
            //this.ContentChanged?.Invoke(this);
            if (tcs == null) return;

            if (!this.IsEmpty)
            {
                tcs.SetResult(true); tcs = null;
            }
        }

        public Task<bool> WaitAsync(CancellationToken ct)
        {
            // если задача уже создана вернем ее (так как чтение задач будет идти в одном потоке то повторный вызов очень маловероятен)
            if (tcs != null) return tcs.Task;
            Console.WriteLine("Create wait task...");

            // надо проверить может уже есть что-то в очереди и елси есть то вернуть выполненную задачу
            if (!this.IsEmpty)
            {
                Console.WriteLine("Queue not empty ...");
                //return Task.CompletedTask as Task<bool>;
                var tmp = new TaskCompletionSource<bool>();
                tmp.SetResult(true);
                
                return tmp.Task;
            }

            // https://docs.microsoft.com/ru-ru/dotnet/standard/asynchronous-programming-patterns/implementing-the-task-based-asynchronous-pattern
            tcs = new TaskCompletionSource<bool>();

            // не удачный вариант. так мы нарегистрируем кучу абсолютно одинаковых делегатов на один и тот же токен. токен будет жить долго а задачи на ожидание мы будем создавать на каждый чих
            ct.Register(() => {
                Console.WriteLine("Cancel...");
                //if (tcs == null) return;
                var t = tcs; tcs = null;
                t?.SetCanceled(); 
            });

            //try { tcs.SetResult(new List<T>()); tcs = null; }
            //catch (Exception exc) { tcs.SetException(exc); tcs = null; }

            return tcs.Task;
        }
    }
}
