using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
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

        private int AproximatedPitchPosition(ref float[] inputSignal, int size, int offset,
            WindowFunctions.WindowType windowType, out double[] acfsSample)
        {
            var furieSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(size, 2)));
            var windowedPart = new float[size];
            var currentSample = new float[furieSize];
            Array.Copy(inputSignal, offset, windowedPart, 0, size);
            var window = new WindowFunctions();
            windowedPart = window.PlaceWindow(windowedPart, windowType);
            Array.Copy(windowedPart, 0, currentSample, 0, size);
            var complexSample = Array.ConvertAll(currentSample, input => (Complex) input);
            var transform = new FurieTransform();
            var furieSample = transform.FastFurieTransform(complexSample);
            currentSample = Array.ConvertAll(furieSample, input => (float)input.Magnitude);
            var acf = new List<double>(furieSample.Length/4);
            for (int i = 0; i < furieSample.Length/4; i++)
            {
                acf.Add(AutoCorrelationPerSample(ref currentSample, 0, i + 1));
            }
            for (int i = 2; i < acf.Count; i++)
            {
                acf[i - 1] = (acf[i] + acf[i - 1] + acf[i - 2])/3;
            }
            acfsSample = acf.ToArray();
            for (int i = 2; i < acf.Count; i++)
            {
                if (acf[i - 1] > acf[i - 2] && acf[i - 1] > acf[i])
                    return i - 1;
            }
            return -1;
        }

        public void AutCorrelationImage(ref float[] inputSignal, int size, float offset, out double[][] image,
            WindowFunctions.WindowType windowFunction, int sampleFrequency, Tuple<int,int>[] speechMarks)
        {
            UsedWindowSize = size;
            UsedWindowType = windowFunction;

            //preprocessing
            var hpf = new Hpf(60.0f, sampleFrequency);
            inputSignal = hpf.Filter(inputSignal);
            var lpf = new Lpf(600.0f, sampleFrequency);
            inputSignal = lpf.StartFilter(inputSignal);

            //analysis variables
            var jump = (int) Math.Round(size*offset);
            var img = new List<double[]>();
            var acfs = new List<double[]>();
            var furieSize = Math.Pow(2, Math.Ceiling(Math.Log(size, 2)));
            var frequencyResolution = sampleFrequency/furieSize;
            var acf = new List<double[]>();
            foreach (var curentMark in speechMarks)
            {
                var prevMax = 0.0;
                for (int samples = curentMark.Item1;
                    samples < inputSignal.Length && samples < curentMark.Item2;
                    samples += jump)
                {
                    //main loop
                    var max = double.NegativeInfinity;
                    var acfsSample = new double[(int)furieSize/4];
                    var aproximatedValue = AproximatedPitchPosition(ref inputSignal, size, samples, windowFunction, out acfsSample);
                    acfs.Add(acfsSample);
                    var positionInAcf = aproximatedValue > -1
                        ? aproximatedValue*sampleFrequency/(2.0*furieSize)
                        : sampleFrequency/prevMax;
                    var start = (int)Math.Round((positionInAcf - frequencyResolution * 1.1) + 2);
                    var stop = (positionInAcf + frequencyResolution * 1.1) + 1;
                    if (start < 1 || stop < 1)
                    {
                        start = 2;
                        stop = size;
                    }
                    var prev = Autocorrelation(ref inputSignal, samples, start - 2);
                    var prev2 = Autocorrelation(ref inputSignal, samples, start - 1);
                    var maxValue = 0.0;
                    var currentAcf = new List<double>();
                    currentAcf.Add(prev2);
                    currentAcf.Add(prev);
                    for (var i = start; i < size && i < stop; i++)
                    {
                        var func = Autocorrelation(ref inputSignal, samples, i);
                        currentAcf.Add(func);
                        if (prev > prev2 && prev > func && prev > maxValue /*maximum*/&&
                            (Math.Abs(prevMax - sampleFrequency/(i - 1.0)) < 50.0 || prevMax == 0.0))
                        {
                            maxValue = prev;
                            max = i - 1;
                        }
                        prev2 = prev;
                        prev = func;
                    }
                    acf.Add(currentAcf.ToArray());

                    if (double.IsNegativeInfinity(max) || sampleFrequency/max > 600 || sampleFrequency/max < 60)
                    {
                        max = aproximatedValue < 0 || aproximatedValue*sampleFrequency/furieSize > 600 ||
                              aproximatedValue*sampleFrequency/furieSize < 60
                            ? img[img.Count - 1][0]
                            : positionInAcf;
                    }
                    max = sampleFrequency/max;

//                var delta = max - prevMax;
//                if (delta > 20 && samples != spechStart)
//                {
//                    max = prevMax;
//                    delta = 0;
//                }
                    prevMax = max;
//                img.Add(new []{max, delta});
                    img.Add(new[] {max});
                }
            }
            using (
                var writer =
                    new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "ACF.txt")))
            {
                foreach (var s in acf.Select(d => d.Aggregate(string.Empty, (current, d1) => current + (d1 + " "))))
                {
                    writer.WriteLine(s);
                }
            }
            using (
                var writer =
                    new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "ACFS.txt")))
            {
                foreach (var s in acfs.Select(d => d.Aggregate(string.Empty, (current, d1) => current + (d1 + " "))))
                {
                    writer.WriteLine(s);
                }
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
