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
            return energy > 0.0 ? autoCorrelation/energy : 0.0;
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

        public void PitchImage(ref float[] inputSignal, int size, float offset, out double[][] image,
            WindowFunctions.WindowType windowFunction, int sampleFrequency, Tuple<int,int>[] speechMarks)
        {
            UsedWindowSize = size;
            UsedWindowType = windowFunction;

            //preprocessing
            var hpf = new Hpf(60.0f, sampleFrequency);
            var filtredSignal = hpf.Filter(inputSignal);
            var lpf = new Lpf(600.0f, sampleFrequency);
            filtredSignal = lpf.StartFilter(filtredSignal);

            //analysis variables
            var jump = (int) Math.Round(size*offset);
            var acfImg = new List<double[]>();
            var acfsImg = new List<double[]>();
            var furieSize = Math.Pow(2, Math.Ceiling(Math.Log(size, 2) + 1));
            var resultImg = new List<double[]>(inputSignal.Length);
            var prevStop = 0;
            foreach (var curentMark in speechMarks)
            {
                for (int i = prevStop; i < curentMark.Item1; i++)
                    if (i%jump == 0)
                    {
                        resultImg.Add(new[] {0.0});
                        acfsImg.Add(new double[128].Select(x => double.NaN).ToArray());
                        acfImg.Add(new double[size].Select(x => double.NaN).ToArray());
                    }

                var pieceImg = new List<double[]>();
                var globalCandidates = new List<List<Tuple<int, double>>>();
                for (int samples = curentMark.Item1; samples+size < inputSignal.Length && samples < curentMark.Item2; samples += jump)
                {
                    var candidates = new List<Tuple<int, double>>();//int = position, double = amplitude
                    
                    //generate acfs and acf on analysis interval
                    var data = new float[size];
                    Array.Copy(inputSignal, samples, data, 0, size);
                    var filterdData = new float[size];
                    Array.Copy(filtredSignal, samples, filterdData, 0, size);

                    var window = new WindowFunctions();
                    data = window.PlaceWindow(data, UsedWindowType);
                    filterdData = window.PlaceWindow(filterdData, UsedWindowType);

                    var maxSignal = Math.Abs(filterdData.Max() * 0.3);
                    filterdData = filterdData.Select(x => Math.Abs(x) > maxSignal ? x : 0.0f).ToArray();//provide central cut

                    double[] acf;
                    FFT.AutoCorrelation(size, filterdData, out acf);

                    double[] acfsSample;
                    FFT.SpectrumAutoCorrelation(size, data, out acfsSample);

                    //extract candidates
                    var acfsCandidates = new List<Tuple<int, double>>();
                    var maxEnergy = 0.0;
                    var minEnergy = 0.0;
                    for (int i = 1; i < acfsSample.Length-1; i++)
                    {
                        if (acfsSample[i] > acfsSample[i - 1] && acfsSample[i] > acfsSample[i + 1])
                        {
                            acfsCandidates.Add(new Tuple<int, double>(i, acfsSample[i]));
                            maxEnergy += Math.Pow(acfsSample[i], 2);
                        }
                        else if (acfsSample[i] < acfsSample[i - 1] && acfsSample[i] < acfsSample[i + 1])
                        {
                            minEnergy += Math.Pow(acfsSample[i], 2);
                        }
                    }

                    for (int i = 17; i < acf.Length && i < 184; i++)
                    {
                        if (acf[i - 1] > acf[i - 2] && acf[i - 1] > acf[i])
                        {
                            candidates.Add(new Tuple<int, double>(i - 1, acf[i - 1]));//add each maximum of function from 60 to 600 Hz
                        }
                    }

                    var aproximatedPosition = acfsCandidates.Any()?acfsCandidates[0].Item1:-1;
                    var freqPosition = (sampleFrequency / furieSize) * aproximatedPosition;//aproximated frequency value

                    if (acfsCandidates.Count > 3 && aproximatedPosition > -1 && freqPosition > 60 && freqPosition < 600 &&
                        candidates.Count > 2 && maxEnergy > minEnergy*2)
                    {
                        var acfPosition = sampleFrequency/freqPosition; //aproximated time value
                        var maxCandidate = candidates.Max(x => x.Item2)*0.2;
                        candidates.RemoveAll(x => x.Item2 < maxCandidate);

                        pieceImg.Add(new[] {acfPosition});
#if DEBUG
                        acfsImg.Add(acfsSample);
                        acfImg.Add(acf);
#endif
                        globalCandidates.Add(candidates);
                    }
                    else
                    {
                        pieceImg.Add(new[] {0.0});
#if DEBUG
                        acfsImg.Add(acfsSample);
                        acfImg.Add(acf);
#endif
                        globalCandidates.Add(candidates);
                    }
                }
                ExtractPitch(pieceImg, globalCandidates, sampleFrequency, furieSize);

                resultImg.AddRange(pieceImg);
                prevStop = curentMark.Item2 + 1;
            }
#if DEBUG
            Acf = acfImg.ToArray();
            Acfs = acfsImg.ToArray();
#endif
            image = resultImg.ToArray();
        }

        private void ExtractPitch(IReadOnlyList<double[]> img, IReadOnlyList<List<Tuple<int, double>>> globalCandidates, int sampleRate, double furieSize)
        {
            var searchWindow = Math.Ceiling(sampleRate*1.2/furieSize);
            var prevVal = 0.0;
            for (int i = 0; i < img.Count; i++)
            {
                var acfsCandidate = img[i][0];
                if (img[i][0] > 0.0 && globalCandidates[i].Any(x => Math.Abs(x.Item1 - img[i][0]) < searchWindow))
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
                if (prevVal > 0.0 && Math.Abs(prevVal - img[i][0]) > 5 && Math.Abs(prevVal - acfsCandidate) > 5 && globalCandidates[i].Any())
                {
                    var max = globalCandidates[i].Max(x => x.Item2);
                    var newCandidate = globalCandidates[i].First(x => x.Item2 >= max).Item1;
                    if (Math.Abs(newCandidate - prevVal) <= 5)
                    {
                        img[i][0] = newCandidate;
                    }
                }
                prevVal = img[i][0];
            }
            var filterRadius = 9;
            for (int i = filterRadius; i < img.Count-filterRadius; i++)
            {
                //use median filter to cath the errors
                var itemsToSort = new List<double>(filterRadius*2 + 1);
                for (int j = -filterRadius; j <= filterRadius; j++)
                {
                    itemsToSort.Add(img[i+j][0]);
                }
                var arr = itemsToSort.ToArray();
                Array.Sort(arr);
                if (globalCandidates[i].Count > 0)
                {
                    var selectedCandidates =
                        globalCandidates[i].Where(
                            x => Math.Abs(x.Item1 - arr[filterRadius]) < searchWindow)
                            .ToArray();
                
                    if (selectedCandidates.Any())
                    {
                        var nearest = selectedCandidates.Max(x => x.Item2);
                        img[i][0] = selectedCandidates.First(x => x.Item2 >= nearest).Item1;
                    }
                    else
                    {
                        img[i][0] = arr[filterRadius];
                    }
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
