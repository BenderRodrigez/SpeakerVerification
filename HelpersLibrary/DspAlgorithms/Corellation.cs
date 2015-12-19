using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public void AutCorrelationImage(ref float[] inputSignal, int size, float offset, out double[][] image,
            WindowFunctions.WindowType windowFunction, int sampleFrequency, Tuple<int,int>[] speechMarks)
        {
            UsedWindowSize = size;
            UsedWindowType = windowFunction;

            //preprocessing
            var hpf = new Hpf(60.0f, sampleFrequency);
            var filtredSignal = hpf.Filter(inputSignal);
            var lpf = new Lpf(600.0f, sampleFrequency);
            filtredSignal = lpf.StartFilter(filtredSignal);

            var maxSignal = Math.Abs(filtredSignal.Max()*0.15);
            filtredSignal = filtredSignal.Select(x => Math.Abs(x) > maxSignal ? x : 0.0f).ToArray();//provide central cut

            //analysis variables
            var jump = (int) Math.Round(size*offset);
            var img = new List<double[]>();
            var acfImg = new List<double[]>();
            var acfsImg = new List<double[]>();
            var furieSize = Math.Pow(2, Math.Ceiling(Math.Log(size, 2) + 1));
            foreach (var curentMark in speechMarks)
            {
                var prevAcfsCandidate = -1;
                var pieceImg = new List<double[]>();
                var globalCandidates = new List<List<Tuple<int, double>>>();
                for (int samples = curentMark.Item1;
                    samples < inputSignal.Length && samples < curentMark.Item2;
                    samples += jump)
                {
                    var candidates = new List<Tuple<int, double>>();//int = position, double = amplitude
                    //extract candidates
                    var data = new float[size];
                    Array.Copy(inputSignal, samples, data, 0, size);
                    var filterdData = new float[size];
                    Array.Copy(filtredSignal, samples, filterdData, 0, size);

                    var window = new WindowFunctions();
                    data = window.PlaceWindow(data, UsedWindowType);
                    filterdData = window.PlaceWindow(filterdData, UsedWindowType);

                    double[] acf;
                    FFT.AutoCorrelation(size, filterdData, out acf);

                    double[] acfsSample;
                    FFT.SpectrumAutoCorrelation(size, data, out acfsSample);

                    for (int i = 1; i < acfsSample.Length - 1; i++)
                    {
                        acfsSample[i] = (acfsSample[i - 1] + acfsSample[i] + acfsSample[i + 1])/3;
                    }

                    var acfsCandidates = new List<Tuple<int, double>>();
                    for (int i = 1; i < acfsSample.Length-1; i++)
                    {
                        if (acfsSample[i] > acfsSample[i - 1] && acfsSample[i] > acfsSample[i + 1])
                        {
                            acfsCandidates.Add(new Tuple<int, double>(i, acfsSample[i]));
                        }
                    }

                    if (acfsCandidates.Count > 3)
                    {
                        var aproximatedPosition = acfsCandidates[0].Item1;
                        var controlMax = acfsCandidates.Max(x => x.Item2);
                        
                        if (acfsCandidates[0].Item2 < controlMax/10.0)
                            aproximatedPosition = acfsCandidates.First(x => x.Item2 == controlMax).Item1;

                        if ((aproximatedPosition < prevAcfsCandidate - 2 || aproximatedPosition > prevAcfsCandidate + 2) && prevAcfsCandidate > -1)
                        {
                            controlMax = acfsCandidates.Min(x => Math.Abs(x.Item1 - prevAcfsCandidate));
                            aproximatedPosition = acfsCandidates.First(x => Math.Abs(x.Item1 - prevAcfsCandidate) == controlMax).Item1;
                        }

                        var freqPosition = (sampleFrequency/furieSize)*aproximatedPosition;
                            //aproximated frequency value
                        if (aproximatedPosition > -1 && freqPosition > 60 && freqPosition < 600)
                        {
                            var acfPosition = 1.0/(aproximatedPosition/furieSize); //aproximated time value

                            for (int i = 2; i < acf.Length; i++)
                            {
                                if ((acf[i - 1] > acf[i - 2] && acf[i - 1] > acf[i]))
                                {
                                    candidates.Add(new Tuple<int, double>(i - 1, acf[i - 1]));
                                        //add each maximum of function
                                }
                            }
                            if (candidates.Count > 2)
                            {
                                var maxCandidate = candidates.Max(x => x.Item2)*0.2;
                                candidates.RemoveAll(x => x.Item2 < maxCandidate);
                                acfsImg.Add(acfsSample);
                                acfImg.Add(acf);
                                pieceImg.Add(new[] {acfPosition});
                                globalCandidates.Add(candidates);
                                prevAcfsCandidate = aproximatedPosition;
                            }
                        }
                    }
                }
                ExtractPitch(pieceImg, globalCandidates, sampleFrequency, furieSize);
                img.AddRange(pieceImg);
            }

//            image = img.Select(x => new []{x[0]> 0.0?sampleFrequency/x[0]:0.0}).ToArray();
            Acf = acfImg.ToArray();
            Acfs = acfsImg.ToArray();
            image = img.ToArray();
        }

        private void ExtractPitch(IReadOnlyList<double[]> img, IReadOnlyList<List<Tuple<int, double>>> globalCandidates, int sampleRate, double furieSize)
        {
            var searchWindow = Math.Ceiling(sampleRate*1.5/furieSize);
            for (int i = 0; i < img.Count; i++)
            {
                if (globalCandidates[i].Count > 0 && globalCandidates[i].Any(x => Math.Abs(x.Item1 - img[i][0]) < searchWindow))
                {
                    var nearest =
                        globalCandidates[i]
                            .Where(x => Math.Abs(x.Item1 - img[i][0]) < searchWindow)
                            .Max(x => x.Item2);
                    img[i][0] =
                        globalCandidates[i]
                            .Where(x => Math.Abs(x.Item1 - img[i][0]) < searchWindow)
                            .First(x => x.Item2 >= nearest)
                            .Item1;
                }
            }
        }

#if DEBUG
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
#endif

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
