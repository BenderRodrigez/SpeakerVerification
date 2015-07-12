using System;
using System.Numerics;

namespace HelpersLibrary.DspAlgorithms
{
    public class FurieTransform
    {
        /// <summary>
        /// Вычисление поворачивающего модуля e^(-i*2*PI*k/N)
        /// </summary>
        /// <param name="k"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private Complex W(int k, int n)
        {
            if (k % n == 0) return 1.0;
            var arg = -2.0 * Math.PI * k / n;
            return new Complex(Math.Cos(arg), Math.Sin(arg));
        }

        /// <summary>
        /// Возвращает спектр сигнала
        /// </summary>
        /// <param name="x">Массив значений сигнала. Количество значений должно быть степенью 2</param>
        /// <returns>Массив со значениями спектра сигнала</returns>
        public Complex[] FastFurieTransform(Complex[] x)
        {
            Complex[] transform;
            int n = x.Length;
            if (n == 2)
            {
                transform = new Complex[2];
                transform[0] = x[0] + x[1];
                transform[1] = x[0] - x[1];
            }
            else
            {
                var xEven = new Complex[n / 2];
                var xOdd = new Complex[n / 2];
                for (int i = 0; i < n / 2; i++)
                {
                    xEven[i] = x[2 * i];
                    xOdd[i] = x[2 * i + 1];
                }
                var even = FastFurieTransform(xEven);
                var odd = FastFurieTransform(xOdd);
                transform = new Complex[n];
                for (int i = 0; i < n / 2; i++)
                {
                    transform[i] = even[i] + W(i, n) * odd[i];
                    transform[i + n / 2] = even[i] - W(i, n) * odd[i];
                }
            }
            return transform;
        }
    }
}
