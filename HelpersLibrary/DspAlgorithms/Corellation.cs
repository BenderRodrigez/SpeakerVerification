using System;
using System.Collections.Generic;
using HelpersLibrary.DspAlgorithms.Filters;

namespace HelpersLibrary.DspAlgorithms
{
    /// <summary>
    /// Занимается сравнением сигналов и анализом подобия двух сигналов
    /// </summary>
    public class Corellation
    {

        public WindowFunctions.WindowType UsedWindowType { private get; set; }
        public int UsedWindowSize { private get; set; }
        public int UsedVectorSize { private get; set; }

        /// <summary>
        /// Вычисляет автокорелляционную функцию на заданном участке сигнала
        /// R(offset)
        /// </summary>
        /// <param name="a">Входной сигнал</param>
        /// <param name="size">Размер участка (в отсчётах)</param>
        /// <param name="offset">Смещение в сигнале</param>
        /// <param name="ret">Результат</param>
        public void AutoCorrelation(ref float[] a, int size, int offset, out double[] ret)
        {
            double[] autoCorrelation = new double[size];
            for (int i = 0; i < size; i++)
                for (int j = offset; j < size+offset - i; j++)
                {
                    autoCorrelation[i] += a[j] * a[j + i];
                }
            ret = autoCorrelation;
        }


        /// <summary>
        /// Вычисляет кратковременную автокорелляцию. Применяется в дальнейших вычислениях КЛП.
        /// </summary>
        /// <param name="inputSignal">Входной сигнал</param>
        /// <param name="offset">Смещение в сигнале</param>
        /// <param name="k">Коэффициент задержки</param>
        /// <returns>Значение кратковременной автокорелляции</returns>
        public double AutoCorrelationPerSample(ref float[] inputSignal, int offset, int k)
        {
            var autoCorrelation = 0.0;
            var energy = 0.0;
            var tmp = new float[UsedWindowSize];

            Array.Copy(inputSignal, offset, tmp, 0, UsedWindowSize);
            var windows = new WindowFunctions();
            tmp = windows.PlaceWindow(tmp, UsedWindowType);

            for (int j = 0; j < tmp.Length; j++ )
            {
                if (j + k < tmp.Length)
                {
                    autoCorrelation += tmp[j]*tmp[j + k];
                    energy += tmp[j]*tmp[j];
                }
                else
                    autoCorrelation += 0.0;
            }
            return autoCorrelation/energy;
        }

        public double Autocorrelation(ref float[] inputSignal, int offset, int k)
        {
            var autoCorrelation = 0.0;
            var tmp = new float[UsedWindowSize];

            Array.Copy(inputSignal, offset, tmp, 0, UsedWindowSize);
            var windows = new WindowFunctions();
            tmp = windows.PlaceWindow(tmp, UsedWindowType);

            for (int j = 0; j < tmp.Length; j++)
            {
                if (j + k < tmp.Length)
                {
                    autoCorrelation += tmp[j] * tmp[j + k];
                }
                else
                    autoCorrelation += 0.0;
            }
            return autoCorrelation;
        }

        /// <summary>
        /// Вычисляет квадратную матрицу для последующего вычисления Коэффициентов линейного предсказания
        /// </summary>
        /// <param name="inputSignal">Входной сигнал</param>
        /// <param name="sizeWindow">Участок, на котором расчитываем значения автокорелляционной функции</param>
        /// <param name="size">Размер матрицы</param>
        /// <param name="offset">Смещение в сигнале</param>
        /// <param name="matrix">Матрица хранящая результат</param>
        /// <param name="useWindow"></param>
        public void AutoCorrelationSquareMatrix(ref float[] inputSignal, int sizeWindow, int size, int offset, out double[][] matrix, WindowFunctions.WindowType useWindow)
        {
            UsedWindowType = useWindow;
            UsedWindowSize = sizeWindow;

            matrix = new double[size][];
            for(int i = 0; i < size; i++)
                matrix[i] = new double[size];

            for(int i = 0; i < size; i++)
            {
                for(int j = 0; j < size; j++)
                {
                    if (i < j) continue; //нижняя половина матрицы

                    matrix[j][i] = AutoCorrelationPerSample(ref inputSignal, offset, i - j);
                    matrix[i][j] = matrix[j][i];
                }
            }
        }

        /// <summary>
        /// Строит вектор значений автокорреляционной функции для последующего применения в расчётах КЛП.
        /// </summary>
        /// <param name="inputSignal">Входной сигнал</param>
        /// <param name="sizeWindow">Размер области вычисления АКФ</param>
        /// <param name="size">Длина вектора</param>
        /// <param name="offset">Смещение в сигнале</param>
        /// <param name="vector">Выходной вектор</param>
        /// <param name="useWindow"></param>
        public void AutoCorrelationVector(ref float[] inputSignal, int sizeWindow, int size, int offset, out double[] vector, WindowFunctions.WindowType useWindow)
        {
            UsedWindowType = useWindow;
            UsedWindowSize = sizeWindow;
            vector = new double[size];
            for(int i = 0; i < size; i++)
            {
                vector[i] = AutoCorrelationPerSample(ref inputSignal, offset, i + 1);
            }
        }

        public void AutCorrelationImage(ref float[] inputSignal, int size, float offset, out double[][] image,
            WindowFunctions.WindowType windowFunction, int sampleFrequency, int spechStart, int speechStop)
        {
            UsedWindowSize = size;
            UsedWindowType = windowFunction;

            var hpf = new Hpf(60.0f, sampleFrequency);
            inputSignal = hpf.Filter(inputSignal);
            var lpf = new Lpf(600.0f, sampleFrequency);
            inputSignal = lpf.StartFilter(inputSignal);
            var jump = (int) Math.Round(size*offset);
            var img = new List<double[]>();
            var prevMax = 0.0;
            for (int samples = spechStart; samples < inputSignal.Length && samples < speechStop; samples+= jump)
            {
                var max = double.NegativeInfinity;
                var prev = Autocorrelation(ref inputSignal, samples, 2);
                var prev2 = Autocorrelation(ref inputSignal, samples, 1);
                var maxValue = 0.0;
                for (int i = 2; i < size; i++)
                {
                    var func = Autocorrelation(ref inputSignal, samples, i + 1);
                    if (prev > prev2 && prev > func && prev > maxValue)
                    {
                        maxValue = prev;
                        max = i-1;
                    }
                    prev2 = prev;
                    prev = func;
                }
//                var delta = max - prevMax;
//                if (delta > 20 && samples != spechStart)
//                {
//                    max = prevMax;
//                    delta = 0;
//                }
                prevMax = max;
//                img.Add(new []{max, delta});
                img.Add(new []{max});
            }
            image = img.ToArray();
        }

        public void AutoCorrelationVectorDurbin(ref float[] inputSignal, int sizeWindow, int size, int offset, out double[] vector, WindowFunctions.WindowType useWindow)
        {
            UsedWindowType = useWindow;
            UsedWindowSize = sizeWindow;
            UsedVectorSize = size;
            vector = new double[size];
            for (int i = 0; i < size; i++)
            {
                vector[i] = AutoCorrelationPerSample(ref inputSignal, offset, i);
            }
        }

        public void AutoCorrelationVectorDurbin(ref float[] inputSignal, int offset, out double[] vector)
        {
            vector = new double[UsedVectorSize];
            for (int i = 0; i < UsedVectorSize; i++)
            {
                vector[i] = AutoCorrelationPerSample(ref inputSignal, offset, i);
            }
        }

        /// <summary>
        /// Вычисляет двухмерную кореллограмму для сигнала
        /// </summary>
        /// <param name="a">Входной сигнал</param>
        /// <param name="sizeWindow">Размер окна (в секундах)</param>
        /// <param name="sampleFrequency">Частота опроса</param>
        /// <param name="totalLenght">Общая длина кореллограммы</param>
        /// <returns>Двумерный массив значений кореллограммы, в котором представлено множество функций АКФ в пределах окна, для всего сигнала</returns>
        public double[][] AutoCorrelationStart(float[] a, double sizeWindow, int sampleFrequency, int totalLenght)
        {
            int size = (int)Math.Round(sampleFrequency * sizeWindow);//Переводим размер окна в отсчёты
            double[][] corel = new double[totalLenght][];
            int x = 0;
            for(int i = 0; i < a.Length-size; i++)
            {
                if (i % ((a.Length - size) / totalLenght) == 0)//"Прореживаем" множество функций, для того, чтобы оно уместилось в totalLenght масивов
                {
                    AutoCorrelation(ref a, size, i, out corel[x]);//Считаем АКФ для текущего участка
                    if (x < 1023)
                        x++;
                    else
                        break;
                }
            }
            return corel;
        }
    }
}
