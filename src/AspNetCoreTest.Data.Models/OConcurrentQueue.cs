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

        // здесь будем хранить инфу о всех токенах на которые мы уже отписались (в теории тут будет тока 1 токен)
        private Dictionary<CancellationToken, bool> tokens;
        public void ClearTokens() {
            tokens = new Dictionary<CancellationToken, bool>();
        }

        private Task<bool> completedTaskTrue;
        private Task<bool> completedTaskFalse;

        public OConcurrentQueue() {
            var tmp = new TaskCompletionSource<bool>();
            tmp.SetResult(true);
            completedTaskTrue = tmp.Task;

            tmp = new TaskCompletionSource<bool>();
            tmp.SetResult(false);
            completedTaskFalse = tmp.Task;

            ClearTokens();
        }

        //public event ConcurrentQueueChangedEventHandler<T> ContentChanged;
        //private event Action<TaskCompletionSource<object>> handler = (TaskCompletionSource<object> _tcs) => {
        //        //if (tcs == null) return;
        //        _tcs?.SetCanceled(); _tcs = null;
        //    };

        public new void Enqueue(T item)
        {
            base.Enqueue(item);

            // Raise event added event
            this.OnContentChanged();
        }

        private void OnContentChanged()
        {
            //Console.WriteLine("Content changed");
            //this.ContentChanged?.Invoke(this);
            if (tcs == null) return;

            if (!this.IsEmpty)
            {
                // очень важно сначала tcs = null;! потмоу что как тока мы делаем SetResult будет вызван след WaitAsync а tcs у нас еще не нулл и будет блокирование 
                var t = tcs; tcs = null;
                t?.SetResult(true);
            }
        }

         
        public Task<bool> WaitAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                Console.WriteLine("IsCancellationRequested...");
                return completedTaskFalse;
            }
            // если задача уже создана вернем ее (так как чтение задач будет идти в одном потоке то повторный вызов очень маловероятен)
            if (tcs != null) return tcs.Task;
            //Console.WriteLine("Create wait task...");

            // надо проверить может уже есть что-то в очереди и елси есть то вернуть выполненную задачу
            if (!this.IsEmpty)
            {
                //Console.WriteLine("Queue not empty ...");
                return completedTaskTrue;
            }

            // https://docs.microsoft.com/ru-ru/dotnet/standard/asynchronous-programming-patterns/implementing-the-task-based-asynchronous-pattern
            tcs = new TaskCompletionSource<bool>();
            var res = tcs.Task; // ибо при первом запуске пока делаем регистрацию на токене задача уже выполнена и tcs == null

            if (!tokens.ContainsKey(ct))
            {
                tokens[ct] = true;
                // не удачный вариант. так мы нарегистрируем кучу абсолютно одинаковых делегатов на один и тот же токен. токен будет жить долго а задачи на ожидание мы будем создавать на каждый чих
                // исправлено! теперь мы ведем коллекцию уже отбработанных токенов и второй раз не подписываем
                ct.Register(() =>
                {
                    Console.WriteLine("Cancel...");
                    //if (tcs == null) return;
                    var t = tcs; tcs = null;

                    //t?.SetCanceled();
                    // вместо SetCanceled будем делать SetResult(false) при false мы прервем цикл и не будет прокинуто исключение (в теории если исключение это ресурсоемкая операция то мы выиграем)
                    t?.SetResult(false);
                });
            }
            //try { tcs.SetResult(new List<T>()); tcs = null; }
            //catch (Exception exc) { tcs.SetException(exc); tcs = null; }

            return res;
        }
    }
}
