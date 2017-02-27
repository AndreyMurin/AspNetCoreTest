using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;

namespace AspNetCoreTest.Data.Models
{
    public class SendActivity
    {
        public NCoords Coords;
        public int State;
    }
    public class QueueNeuron
    {
        public Neuron Neuron;
        public NCoords Coords;
    }

    // 32+64=96 (12 байт)
    public class NRelation
    {
        [JsonProperty("w")] // минимизируем названия
        // вес связи в диапазаоне думаю от -1 до 1 (возможно от -2 до 2 для повышения эффективности связи)
        // 32 бита
        public float Weight { get; set; }

        public float WeightSum
        {
            get
            {
                // не надо потокообезопасить! так как это делается в одном потоке (1 нейрон не может быть активным в 2 потоках)
                return Weight + WeightChange;
            }
        }

        private float _weightChange;
        // изменение веса связи за период (например за день) во время сна будем либо принимать суточные изменения веса либо отклонять их. пока не знаю по каким принципам
        [JsonProperty("с")] // минимизируем названия
        public float WeightChange
        {
            get
            {
                return _weightChange;
            }
            set
            {
                _weightChange = value;
            }
        }

        // индекс нейрона для сохранения-загрузки (long хватит за глаза)
        // 64 бита
        [JsonProperty("n")] // минимизируем названия
        public long Neuron { get; set; }
        
        // ссылка на нейрон (экономим оперативку)
        //private Neuron _neuron;

        public void SetNeuron(Neuron neuron) // никаких ref в параметрах не надо!
        {
            //_neuron = neuron;
        }

        // функция изменения веса связи
        public float IncWeight(float addend)
        {
            // не надо потокообезопасить! так как это делается в одном потоке (вот тут не уверен насчет 1 потока)
            //WeightChange += addend;
            
            // копипизда отсюда https://msdn.microsoft.com/en-us/library/cd0811yf(v=vs.110).aspx
            float initialValue, computedValue;
            do
            {
                // Save the current running total in a local variable.
                initialValue = _weightChange;

                // Add the new value to the running total.
                computedValue = initialValue + addend;

                // CompareExchange compares totalValue to initialValue. If
                // they are not equal, then another thread has updated the
                // running total since this loop started. CompareExchange
                // does not update totalValue. CompareExchange returns the
                // contents of totalValue, which do not equal initialValue,
                // so the loop executes again.
            }
            while (initialValue != Interlocked.CompareExchange(ref _weightChange,
                computedValue, initialValue));
            // If no other thread updated the running total, then 
            // totalValue and initialValue are equal when CompareExchange
            // compares them, and computedValue is stored in totalValue.
            // CompareExchange returns the value that was in totalValue
            // before the update, which is equal to initialValue, so the 
            // loop ends.

            // The function returns computedValue, not totalValue, because
            // totalValue could be changed by another thread between
            // the time the loop ends and the function returns.
            return computedValue;
        }

        public void AproveWeight()
        {
            Weight += WeightChange;
        }

        public void RejectWeight()
        {
            WeightChange = 0;
        }
    }

    public class Neuron
    {
        // заряд нейрона при превышении определеного порга происходят разряды (spike)
        // нельзя делать свойством (иначе не получится использовать атомарные операции)
        private int _state;// { get; set; }
        public int State { get { return _state; } set { _state = value; } }

        // запущены таймеры и потоки
        private int _isStarted = 0;

        public List<NRelation> Output { get; set; }

        // время между разрядами нейрона
        public int SpikePeriod { get; set; }

        // индекс нейрона. нужен при сохранении(загрузке) очереди в(из) файла (доп нагрузка на память но всего лишь 8 гигов на милиард нейронов)
        // при загрузке если _isActive то сразу херчаим в очередь (так как у нас не все активные нейроны в очереди!!!)
        //public long Index { get; set; }

        // время последней активации нейрона (для алгоритма обучения) вычисление потери заряда должно по идее происходить с момента последнего изменения заряда а не разряда
        // это параметр надо перевычислять при каждом считывании сети из файла 
        public DateTime LastActive;

        // нейрон в активном состоянии идут разряды
        private int _isActive;// { get; set; }
        public int IsActive { get { return _isActive; } set { _isActive = value; } }

        // для сериалиции объекта
        public Neuron()
        {
            //Output = new List<NOutput>();
        }

        public static NNet Net;

        public Neuron(IRnd rand, long index)
        {
            //Index = index;
            Output = new List<NRelation>();
            State = rand.Next(NNet.MIN_INIT_STATE, NNet.MAX_INIT_STATE);
            SpikePeriod = rand.Next(NNet.MIN_SPIKE_PERIOD, NNet.MAX_SPIKE_PERIOD);
        }

        // для отладки асинхронного чтения записи
        /*public Neuron(long index)
        {
            //float tmp = index;
            Output = new List<NRelation>();
            State = (int)index;
        }*/

        public void SetOutput(List<NRelation> output)
        {
            Output = output;
        }

        // функция уменьшающая состояние нейрона при разряде
        private void _decreaseState(int dec)
        {
            // если нейрон с положительным зарядом то dec всегда будет >0, так как связи всегда положительные. и наоборот
            // главное чтобы нейрон не потерял свой свой знак. очень внезапно к обучению

            if (_state > 0)
            {
                var newState = Interlocked.Add(ref _state, -dec);
                // тут _state могло изменится, но пока на это забьем
                if (newState < 0)
                {
                    _state = 0; // не очень удачно так как нарушаем закон сохранения энергии
                }
            }
            else
            {
                var newState = Interlocked.Add(ref _state, -dec);
                // тут _state могло изменится, но пока на это забьем
                if (newState > 0)
                {
                    _state = 0; // не очень удачно так как нарушаем закон сохранения энергии
                }
            }
        }

        // тупой вариант расчета новых состояний (нарушает закон сохранения - сеть быстро "взрывается")
        private void _evalNewStates()
        {
            // пропускаем ток
            var s = 0; // суммируем скока заряда ушло (если нейрон с положительным зарядом то тут всегда будет >0, так как связи всегда положительные. и наоборот)
            foreach (var o in Output)
            {
                var coord = new NCoords(o.Neuron, Net.LenX, Net.LenY, Net.LenZ);
                var state = (int)(_state * o.WeightSum);
                s += state;
                Net.Neurons[coord.Z][coord.Y][coord.X].IncState(state, coord);
            }

            // уменьшаем свое состояние
            _decreaseState(s);
        }

        // вариант с распределением существующего заряда равномерно в зависимости от веса
        private void _evalNewStates2()
        {
            var s = _state; // _state может меняться в процессе
            // если выход всего 1, то результат (state0+state1) / 2 для обоих состояний (оно выравнивается всегда)
            #region one output
            if (Output.Count == 1)
            {
                var o = Output[0];
                var coord = new NCoords(o.Neuron, Net.LenX, Net.LenY, Net.LenZ);
                var n = Net.Neurons[coord.Z][coord.Y][coord.X];
                var ts = n.State;
                if (s > 0)
                {
                    if (ts > 0) // + => +
                    {
                        if (s <= ts) // заряд активного нейрона меньше целевого, тут ничего не должно происходить (возможно уменьшение веса связи???)
                        {
                            return;
                        }
                        //var ns = (s + ts) / 2;
                        //_state -= s - (s + ts) / 2 => _state -= (s-ts)/2
                        Interlocked.Add(ref _state, -(s - ts) / 2);
                        n.IncState((s - ts) / 2, coord);
                    }
                    else        // + => -
                    {

                    }
                }
                else
                {

                }
                return;
            }
            #endregion

            // если выходов больше, то 
            foreach (var o in Output)
            {

            }

        }

        // проверка состояния нейрона активировался или нет
        private bool _checkState()
        {
            return ((_state > 0 && _state > NNet.MAX_STATE) || (_state < 0 && _state < NNet.MIN_STATE));
        }

        // усыпаем на период, но проверяем NNet.isStarted и если сеть стопарнули то сразу выходим
        private void _sleep(int period)
        {
            var minsleep = 100;
            if (period <= minsleep)
            {
                Thread.Sleep(period);
            }
            else
            {
                for (var i = 0; i < (period / minsleep); i++)
                {
                    Thread.Sleep(minsleep);
                    if (NNet.isStarted == 0) return;// сеть остановлена выходим
                }
                var ostatok = period - ((period / minsleep) * minsleep);
                if (ostatok > 0) Thread.Sleep(ostatok);
            }
        }

        // в этой функции мы всегда! должны быть тока одним потоком. здесь мы программируем и выполняем цепочки разрядов с затуханием силы и с определенной частотой
        public Task SpikeAsync(int x, int y, int z)
        {
            //Net.LogInformation(321, "TRY Neuron SpikeAsync {x} {y} {z}", x, y, z);
            // нейрон активировался. НО он может быть уже активирован другим процессом. надо как-то без блокировки проверить
            // CompareExchange возвращает старое значение
            if (0 == Interlocked.CompareExchange(ref _isStarted, 1, 0)) // if (_isActive==0) _isActive=1; 
            {
                return Task.Run(() =>
                {
                    //Net.LogInformation(321, "Neuron SpikeAsync {x} {y} {z} success", x, y, z);
                    Interlocked.Increment(ref NNet.Threads);
                    try
                    {
                        while (_checkState() && NNet.isStarted == 1)
                        {
                            Net.SendActiveQueue.Enqueue(new SendActivity { Coords = new NCoords(x, y, z), State = _state });
                            LastActive = DateTime.Now;

                            _evalNewStates();

                            // не важно когда мы выйдем до засыпания или после эта инфа потеряется при остановке
                            if (NNet.isStarted == 0) break;// сеть остановлена выходим

                            if (_checkState())
                            {
                                // надо разбить успыание на периоды не больше полсекунды
                                _sleep(SpikePeriod);
                            }

                            if (NNet.isStarted == 0) break;// сеть остановлена выходим
                        }

                        // здесь нельзя снимать активность! а что если сеть остановили и мы вышли из цикла?
                        if (!_checkState()) IsActive = 0;
                    }
                    finally // при любом раскладе уменьшить число потоков иначе ждать при остановке будем вечно
                    {
                        // обязательно освобождаем состояние
                        Interlocked.Exchange(ref _isStarted, 0);

                        Interlocked.Decrement(ref NNet.Threads);
                    }
                });
            }
            // мы уже запущены
            return Task.CompletedTask;
        }/**/

        // увеличиваем состояние нейрона и запускаем разряды
        // не делать async Task! так как в этом случае мы тупо встанем в зависон
        public int IncState(int state, NCoords coords)
        {
            //return Task.Run(() =>
            //{
            // счетчики потоков надо ставить в самой функции SpikeAsync так как разряды, затухания, повторения все в ней
            //Interlocked.Increment(ref NNet.Threads);

            var newState = Interlocked.Add(ref _state, state);

            // в этом месте значение _state может увеличится другим потоком, но нам как бы пофиг. либо этот либо тот процесс получат в newState значение выше порога и запустят разряд
            //Net.LogInformation(321, "Neuron IncState {coords} + {state} = {newstate}", coords, state, _state);

            if (_checkState() && _isStarted==0)
            {
                Net.LogInformation(321, "Neuron IncState {coords} activated!!!!!", coords);

                // просто метим как активный
                IsActive = 1;

                // надо где то прописать что появился активный нейрон (пофигу елси 1 нейрон засунем в очередь несколько раз на том конце разберемся)
                Net.Queue.Enqueue(new QueueNeuron { Neuron=this, Coords=coords });
            }

            //Interlocked.Decrement(ref NNet.Threads);
            //});
            return newState;
        }


    }
}
