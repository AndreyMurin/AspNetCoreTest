﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreTest.Data.Models
{
    public class NCoords
    {
        // все координаты начинаются с нуля (для оптимизации не будем юзать проперти)
        public int X;// { get; set; }
        public int Y;// { get; set; }
        public int Z;// { get; set; }

        public override string ToString()
        {
            return "(" + X + "," + Y + "," + Z + ")";
        }

        public NCoords(int x, int y, int z)
        {
            X = x;Y = y;Z = z;
        }

        // создает кординаты по индексу нейрона
        public NCoords(long i, int lenX, int lenY, int lenZ)
        {
            // вычисляем z
            Z = (int)(i / (lenX * lenY));

            i = i - (Z * lenX * lenY);
            Y = (int)(i / lenX);

            X = (int)(i - (Y * lenX));
        }

        // lenZ не обязательный параметр (по сути он нужен тока для проверки границ)
        // сделаем перегрузку с проверкой и без проверки
        // захерачить бы для оптимизации в инлайн, так как вызов данной функции довольно часто будет надо оптимизировать!
        // хотя таким способом мы будем тока инициализировать связи и задавать входные параметры а внутри везде юзаем одиночную координату!
        public long ToSingle(int lenX, int lenY)
        {
            return (X + lenX * Y + lenX * lenY * Z);
        }
        // эту прегрузку наверное не будем вызывать!!! так как тут проверки которые занимают время
        public long ToSingle(int lenX, int lenY, int lenZ)
        {
            // делать ли проверку на выход за пределы?
            if (X >= lenX) throw new Exception("Выход за пределы массива");
            if (Y >= lenY) throw new Exception("Выход за пределы массива");
            if (Z >= lenZ) throw new Exception("Выход за пределы массива");
            /**/
            return ToSingle(lenX, lenY);
        }
        /*
         lenX=3, lenY=4, lenZ=2
         (0,0,0) => 0
         (1,0,0) => 1
         (2,0,0) => 2
         (0,1,0) => 3
         (1,1,0) => 4
         (2,1,0) => 5
         */
    }

    // области
    public class NRange
    {
        // все координаты начинаются с нуля (для оптимизации не будем юзать проперти)
        public int MinX;// { get; set; }
        public int MinY;// { get; set; }
        public int MinZ;// { get; set; }

        public int MaxX;// { get; set; }
        public int MaxY;// { get; set; }
        public int MaxZ;// { get; set; }

        public bool Test(NCoords coords)
        {
            if (
                    MinX <= coords.X && coords.X <= MaxX 
                &&  MinY <= coords.Y && coords.Y <= MaxY
                &&  MinZ <= coords.Z && coords.Z <= MaxZ
                ) return true;
            return false;
        }

        
    }
}
