using System;

namespace SpeakerVerification
{
    /// <summary>
    /// Занимается сравнением сигналов и анализом подобия двух сигналов
    /// </summary>
    static class Corellation
    {
        public enum WindowType { Rectangular, Hamming, Blackman };

        private static WindowType _usedWindowType;
        private static int _usedWindowSize;

        /// <summary>
        /// Вычисляет автокорелляционную функцию на заданном участке сигнала
        /// R(offset)
        /// </summary>
        /// <param name="a">Входной сигнал</param>
        /// <param name="size">Размер участка (в отсчётах)</param>
        /// <param name="offset">Смещение в сигнале</param>
        /// <param name="ret">Результат</param>
        public static void AutoCorrelation(ref short[] a, int size, int offset, out double[] ret)
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
        public static double AutoCorrelationPerSample(ref float[] inputSignal, int offset, int k)
        {
            double autoCorrelation = 0;
            var tmp = new float[_usedWindowSize];

            Array.Copy(inputSignal, offset, tmp, 0, _usedWindowSize);
            //double[] temp = HammingWindow(tmp, tmp.Length);
            switch (_usedWindowType)
            {
                case WindowType.Hamming:
                    tmp = HammingWindow(tmp, tmp.Length);
                    break;
                case WindowType.Blackman:
                    tmp = BlackmanWindow(tmp, tmp.Length, 0.16, 1.73);
                    break;
                case WindowType.Rectangular://nothing to do in this case, but it's correct
                    break;
            }

            for (int j = 0; j < tmp.Length; j++ )
            {
                if (j + k < tmp.Length)
                    autoCorrelation += tmp[j] * tmp[j + k];
                else
                    autoCorrelation += 0.0;
            }
            return autoCorrelation;
        }

        /// <summary>
        /// Наложение окна Хемминга на входной сигнал
        /// </summary>
        /// <param name="input">Сигнал</param>
        /// <param name="n">Размерность БПФ</param>
        /// <returns></returns>
        private static float[] HammingWindow(float[] input, int n)
        {
            var x = new float[input.Length];
            var iteratorBorder = (input.Length/2.0) - 4.0;
            var iterator = 0;
            var iterator2 = input.Length - 1;
            const double omega = 2.0*Math.PI;

            while (iterator <= iteratorBorder)
            {
                x[iterator] = (float)(input[iterator] * (0.54 - 0.46 * Math.Cos(omega * iterator / n)));
                x[iterator + 1] = (float) (input[iterator + 1]*(0.54 - 0.46*Math.Cos(omega*(iterator + 1)/n)));
                x[iterator + 2] = (float) (input[iterator + 2]*(0.54 - 0.46*Math.Cos(omega*(iterator + 2)/n)));
                x[iterator + 3] = (float) (input[iterator + 3]*(0.54 - 0.46*Math.Cos(omega*(iterator + 3)/n)));
                x[iterator + 4] = (float) (input[iterator + 4]*(0.54 - 0.46*Math.Cos(omega*(iterator + 4)/n)));

                x[iterator2] = (float)(input[iterator2] * (0.54 - 0.46 * Math.Cos(omega * iterator2 / n)));
                x[iterator2 - 1] = (float) (input[iterator2 - 1]*(0.54 - 0.46*Math.Cos(omega*(iterator2 - 1)/n)));
                x[iterator2 - 2] = (float) (input[iterator2 - 2]*(0.54 - 0.46*Math.Cos(omega*(iterator2 - 2)/n)));
                x[iterator2 - 3] = (float) (input[iterator2 - 3]*(0.54 - 0.46*Math.Cos(omega*(iterator2 - 3)/n)));
                x[iterator2 - 4] = (float) (input[iterator2 - 4]*(0.54 - 0.46*Math.Cos(omega*(iterator2 - 4)/n)));
                iterator += 5;
                iterator2 -= 5;
            }
            x[iterator] = (float)(input[iterator] * (0.54 - 0.46 * Math.Cos(omega * iterator / n)));
            x[iterator2] = (float)(input[iterator2] * (0.54 - 0.46 * Math.Cos(omega * iterator2 / n)));
            //for (int i = 0; i < input.Length/2; i++)
            //{
            //    x[i] = (float)(input[i] * (0.54 - 0.46 * Math.Cos(2.0 * Math.PI * i / n)));
            //}
            return x;
        }

        private static float[] BlackmanWindow(float[] input, int n, double a, double b)
        {
            var x = new float[input.Length];
            var a0 = (1.0-a)/2.0;
            const double a1 = 0.5;
            var a2 = a/2.0;
            for(var i = 0; i < x.Length; i++)
            {
                x[i] = input[i] * (float)(a0 - a1 * Math.Cos(2.0 * Math.PI * i / n) + Math.Cos(4.0 * Math.PI * i / n));
            }
            return x;
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
        public static void AutoCorrelationSquareMatrix(ref float[] inputSignal, int sizeWindow, int size, int offset, out double[][] matrix, WindowType useWindow)
        {
            _usedWindowType = useWindow;
            _usedWindowSize = sizeWindow;

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
        public static void AutoCorrelationVector(ref float[] inputSignal, int sizeWindow, int size, int offset, out double[] vector, WindowType useWindow)
        {
            _usedWindowType = useWindow;
            _usedWindowSize = sizeWindow;
            vector = new double[size];
            for(int i = 0; i < size; i++)
            {
                vector[i] = AutoCorrelationPerSample(ref inputSignal, offset, i + 1);
            }
        }

        public static void AutoCorrelationVectorDurbin(ref float[] inputSignal, int sizeWindow, int size, int offset, out double[] vector, WindowType useWindow)
        {
            _usedWindowType = useWindow;
            _usedWindowSize = sizeWindow;
            vector = new double[size];
            for (int i = 0; i < size; i++)
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
        public static double[][] AutoCorrelationStart(short[] a, double sizeWindow, int sampleFrequency, int totalLenght)
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
