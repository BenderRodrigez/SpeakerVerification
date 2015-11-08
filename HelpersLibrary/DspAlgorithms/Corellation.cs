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
            var furieSize = Math.Pow(2, Math.Ceiling(Math.Log(size, 2)));
            var globalCandidates = new List<List<Tuple<int, double>>>();
            foreach (var curentMark in speechMarks)
            {
                for (int samples = curentMark.Item1;
                    samples < inputSignal.Length && samples < curentMark.Item2;
                    samples += jump)
                {
                    var candidates = new List<Tuple<int, double>>();//int = position, double = amplitude
                    //extract candidates
                    var acf = new double[size];
                    acf[1] = Autocorrelation(ref inputSignal, samples, 1);
                    acf[0] = Autocorrelation(ref inputSignal, samples, 0);
                    for (var i = 2; i < size; i++)
                    {
                        acf[i] = Autocorrelation(ref inputSignal, samples, i);
                    }

                    var max = acf.Max()*0.2;//get central cut

                    for (int i = 0; i < acf.Length; i++)
                    {//cut unreliable data
                        acf[i] = Math.Abs(acf[i]) > max ? acf[i] > 0?acf[i] - max:acf[i]+max : 0.0;
                    }

                    for (int i = 2; i < acf.Length; i++)
                    {
                        if ((acf[i - 1] > acf[i - 2] && acf[i - 1] > acf[i]))
                        {
                            candidates.Add(new Tuple<int, double>(i - 1, acf[i - 1]));//add each maximum of function
                        }
                    }
                    
                    if(candidates.Count < 1)
                    {
                        double[] acfs;
                        var spectrumPosition = AproximatedPitchPosition(ref inputSignal, size, samples, windowFunction,
                            out acfs);
                        if (spectrumPosition > -1)
                        {
                            var acfPosition = 2.0 / (spectrumPosition / furieSize);
                            if (sampleFrequency/acfPosition > 60 && sampleFrequency/acfPosition < 600)
                                candidates.Add(new Tuple<int, double>((int) Math.Round(acfPosition),
                                    double.NegativeInfinity));
                        }
                    }
                    globalCandidates.Add(candidates);
                }
            }

            //now we should process candidates to select one of them on each sample
            foreach (var currentSample in globalCandidates)
            {
                if (currentSample.Count < 1 || currentSample[0].Item1 < 0)
                {
                    //no pitch in this sample
                    img.Add(new []{0.0});
                    continue;
                }
                var maxVal = currentSample.Max(x => x.Item2);
                img.Add(new[] {(double) currentSample.FirstOrDefault(x => x.Item2 == maxVal).Item1});
            }

            for (int iteration = 0; iteration < 2; iteration++)
            {
                for (int i = 2; i < img.Count; i++)
                {
                    if (Math.Abs(img[i - 1][0] - img[i - 2][0]) > 2 && Math.Abs(img[i - 1][0] - img[i][0]) > 2 &&
                        img[i][0] > 0.0 && img[i - 2][0] > 0.0)
                    {
                        img[i - 1][0] = (img[i - 2][0] + img[i][0])/2;
                    }
                    else if (img[i - 1][0] <= 0.0 && img[i - 2][0] > 0.0 && Math.Abs(img[i - 1][0] - img[i - 2][0]) < 5)
                    {
                        var pos = 1;
                        for (int j = 1; j < img.Count - i - 1 && img[i - 1 + j][0] <= 0.0; j++)
                        {
                            pos = j;
                        }
                        if (pos < 6)
                        {
                            img[i - 1][0] = FunctionBetwenTwoPoints(i - 2, i + pos, img[i - 2][0], img[i + pos][0],
                                i - 1);
                        }
                    }

                    if (Math.Abs(img[i - 1][0] - img[i - 2][0]) > 5 && img[i][0] <= 0.0)
                    {
                        img[i - 1][0] = 0.0;
                    }
                    if (Math.Abs(img[i - 1][0] - img[i][0]) > 5 && img[i - 2][0] <= 0.0)
                    {
                        img[i - 1][0] = 0.0;
                    }

                    if (Math.Abs(img[i - 1][0] - img[i - 2][0]) > 5 && img[i - 1][0] > 0.0 && img[i - 2][0] > 0.0)
                    {
                        var candidate =
                            globalCandidates[i].Where(x => x.Item1 != img[i - 1][0] && Math.Abs(x.Item1 - img[i - 2][0]) <= 5)
                                .OrderByDescending(x => Math.Abs(x.Item1 - img[i - 1][0]))
                                .FirstOrDefault();

                        if (candidate != null)
                            img[i - 1][0] = candidate.Item1;
                        else
                        {
                            //try approximation
                            img[i - 1][0] = FunctionBetwenTwoPoints(i - 2, i, img[i - 2][0], img[i][0], i - 1);
                        }
                    }
                }
            }

            image = img.Select(x => new []{x[0]> 0.0?sampleFrequency/x[0]:0.0}).ToArray();
        }

        private void SaveTmpImage(double[][] img)
        {
            using (var writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tmp.txt")))
            {
                foreach (var s in img.Select(d => d.Aggregate(string.Empty, (current, d1) => current + (d1 + " "))))
                {
                    writer.WriteLine(s);
                }
            }
        }

        private double FunctionBetwenTwoPoints(int t1, int t2, double x1, double x2, int pos)
        {
            return -((x1 - x2)*pos + (t1*x2 - t2*x1))/(t2 - t1);
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
