using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
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
        public double[][] Acfs { get; private set; }
        public double[][] Acf { get; private set; }


        public void AutoCorrelationFast(ref float[] inputSignal, int size, int offset, out double[] result)
        {
            result = null;
        }

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
            }
            return energy != 0.0 ? autoCorrelation/energy : 0.0;
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

        public double[] SpectrumAutocorellationFunction(ref float[] inputSignal, int size, int offset,
            WindowFunctions.WindowType windowType)
        {
            var furieSize = (int)Math.Pow(2, Math.Ceiling(Math.Log(size, 2)));
            var windowedPart = new float[size];
            var currentSample = new float[furieSize];
            Array.Copy(inputSignal, offset, windowedPart, 0, size);
            var window = new WindowFunctions();
            var avg = windowedPart.Average();
            windowedPart = windowedPart.Select(x => x/avg).ToArray();
            windowedPart = window.PlaceWindow(windowedPart, windowType);
            Array.Copy(windowedPart, 0, currentSample, 0, size);
            var complexSample = Array.ConvertAll(currentSample, input => (Complex)input);
            var transform = new FurieTransform();
            var furieSample = transform.FastFurieTransform(complexSample);
            currentSample = Array.ConvertAll(furieSample, input => (float)input.Magnitude);

            var acf = new List<double>(furieSample.Length / 4);
            for (int i = 0; i < furieSample.Length / 4; i++)
            {
                acf.Add(AutoCorrelationPerSample(ref currentSample, 0, i + 1));
            }
            for (int i = 2; i < acf.Count; i++)
            {
                acf[i - 1] = (acf[i] + acf[i - 1] + acf[i - 2]) / 3;
            }
            return acf.ToArray();
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
            WindowFunctions.WindowType windowFunction, int sampleFrequency, Tuple<int,int>[] speechMarks, bool debug = false)
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
            var acfImg = new List<double[]>();
            var acfsImg = new List<double[]>();
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
                    double[] acfsSample;
                    var data = new float[size];
                    Array.Copy(inputSignal, samples, data, 0, size);
                    var window = new WindowFunctions();
                    data = window.PlaceWindow(data, UsedWindowType);
                    FFT.AutocorrelationAndSpectrumAutocorrelation(size, data, out acf, out acfsSample);

//                    var aproximatedPosition = AproximatedPitchPosition(ref inputSignal, size, samples, windowFunction, out acfsSample);
                    var aproximatedPosition = -1;
                    for (int i = 1; i < acfsSample.Length-1; i++)
                    {
                        if (acfsSample[i] > acfsSample[i - 1] && acfsSample[i] > acfsSample[i + 1])
                        {
                            aproximatedPosition = i;
                            break;
                        }
                    }
                    var freqPosition = (sampleFrequency / furieSize) * aproximatedPosition;//aproximated frequency value
                    if (aproximatedPosition > -1 && freqPosition > 60 && freqPosition < 600)
                    {
                        var acfPosition = 2.0 / (aproximatedPosition / furieSize);//aproximated time value
                        img.Add(new[] {acfPosition});
                        acfsImg.Add(acfsSample);
                        /*acf[0] = Autocorrelation(ref inputSignal, samples, 0);
                        acf[1] = Autocorrelation(ref inputSignal, samples, 1);
                        for (var i = 2; i < size; i++)
                        {
                            acf[i] = Autocorrelation(ref inputSignal, samples, i);
                        }*/

                        /*if (debug)*/ acfImg.Add(acf);

                        var max = acf.Max()*0.2; //get central cut

                        for (int i = 0; i < acf.Length; i++)
                        {//cut unreliable data
                            acf[i] = acf[i] > max ? acf[i] - max : 0.0;
                        }

                        for (int i = 2; i < acf.Length; i++)
                        {
                            if ((acf[i - 1] > acf[i - 2] && acf[i - 1] > acf[i]))
                            {
                                candidates.Add(new Tuple<int, double>(i - 1, acf[i - 1]));//add each maximum of function
                            }
                        }

                        if (candidates.Count < 1)
                        {
                            candidates.Add(new Tuple<int, double>((int) Math.Round(acfPosition), double.NegativeInfinity));
                        }
                        globalCandidates.Add(candidates);
                    }
                }
            }

            Acf = acfImg.ToArray();
            Acfs = acfsImg.ToArray();

            //now we should process candidates to select one of them on each sample
            /*foreach (var currentSample in globalCandidates)
            {
                if (currentSample.Count < 1 || currentSample[0].Item1 <= 0.0)
                {
                    //no pitch in this sample
                    img.Add(new []{0.0});
                    continue;
                }
                var maxVal = currentSample.Max(x => x.Item2);
                img.Add(new[] {(double) currentSample.FirstOrDefault(x => x.Item2 == maxVal).Item1});
            }*/

            for (int i = 0; i < globalCandidates.Count; i++)
            {
                var max = globalCandidates[i].Max(x => Math.Abs(x.Item1 - img[i][0]));
                img[i][0] = globalCandidates[i].FirstOrDefault(x => Math.Abs(x.Item1 - img[i][0]) == max).Item1;
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
                        //search candidate that will be closer to img[i-2][0] than img[i-1][0]
                        var candidate =
                            globalCandidates[i - 1].Where(
                                x => x.Item1 != img[i - 1][0] && Math.Abs(x.Item1 - img[i - 2][0]) <= 5)
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

                    if (img[i - 2][0] > 0.0 && img[i - 1][0] <= 0.0 && img[i][0] > 0.0)
                    {
                        var candidate =
                            globalCandidates[i - 1].Where(
                                x => x.Item1 != img[i - 1][0] && Math.Abs(x.Item1 - img[i - 2][0]) <= 5)
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

//            image = img.Select(x => new []{x[0]> 0.0?sampleFrequency/x[0]:0.0}).ToArray();
            image = img.ToArray();
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

        public void AutoCorrelationVectorDurbin(ref float[] inputSignal, int offset, out double[] vector)
        {
            vector = new double[UsedVectorSize];
            for (int i = 0; i < UsedVectorSize; i++)
            {
                vector[i] = AutoCorrelationPerSample(ref inputSignal, offset, i);
            }
        }
    }
}
