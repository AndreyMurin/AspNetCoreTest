using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Models
{
    // класс для загрузки с конфиг файла
    public class NNetConfig
    {
        // имя файла для сохранения сети
        public string FileName { get; set; }
        // сеть трехмерная
        // длина по оси X
        public int LenX { get; set; }
        // длина по оси Y
        public int LenY { get; set; }
        // длина по оси Z (число слоев)
        public int LenZ { get; set; }
    }

}
