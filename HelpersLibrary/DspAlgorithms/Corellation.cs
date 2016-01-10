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
            var resultImg = new List<double[]>(inputSignal.Length);
            var prevStop = 0;
            foreach (var curentMark in speechMarks)
            {
                for (int i = prevStop; i < curentMark.Item1; i++)
                {
                    if (i%jump == 0)
                    {
                        resultImg.Add(new[] {0.0});
                        acfsImg.Add(new double[128].Select(x => double.NaN).ToArray());
                        acfImg.Add(new double[size].Select(x => double.NaN).ToArray());
                    }
                }

                var prevAcfsCandidate = -1;
                var pieceImg = new List<double[]>();
                var globalCandidates = new List<List<Tuple<int, double>>>();
                var globalCandidatesMinimums = new List<List<Tuple<int, double>>>();
                for (int samples = curentMark.Item1; samples < inputSignal.Length && samples < curentMark.Item2; samples += jump)
                {
                    var candidates = new List<Tuple<int, double>>();//int = position, double = amplitude
                    var candidatesMins = new List<Tuple<int, double>>();//int = position, double = amplitude
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

                    var acfsCandidates = new List<Tuple<int, double>>();
                    for (int i = 1; i < acfsSample.Length-1; i++)
                    {
                        if (acfsSample[i] > acfsSample[i - 1] && acfsSample[i] > acfsSample[i + 1])
                        {
                            acfsCandidates.Add(new Tuple<int, double>(i, acfsSample[i]));
                        }
                    }

                    for (int i = 2; i < acf.Length; i++)
                    {
                        if ((acf[i - 1] > acf[i - 2] && acf[i - 1] > acf[i]))
                        {
                            candidates.Add(new Tuple<int, double>(i - 1, acf[i - 1]));//add each maximum of function
                        }
                        else if (acf[i - 1] < acf[i - 2] && acf[i - 1] < acf[i])
                        {
                            candidatesMins.Add(new Tuple<int, double>(i - 1, acf[i - 1]));
                        }
                    }

                    if (acfsCandidates.Count > 3)
                    {
                        var aproximatedPosition = acfsCandidates[0].Item1;
                        var controlMax = acfsCandidates.Max(x => x.Item2);

//                        if (acfsCandidates[0].Item2 < controlMax/10.0)
//                            aproximatedPosition = acfsCandidates.First(x => x.Item2 == controlMax).Item1;
//
//                        if ((aproximatedPosition < prevAcfsCandidate - 2 || aproximatedPosition > prevAcfsCandidate + 2) && prevAcfsCandidate > -1)
//                        {
//                            controlMax = acfsCandidates.Min(x => Math.Abs(x.Item1 - prevAcfsCandidate));
//                            aproximatedPosition = acfsCandidates.First(x => Math.Abs(x.Item1 - prevAcfsCandidate) == controlMax).Item1;
//                        }

                        var freqPosition = (sampleFrequency/furieSize)*aproximatedPosition;//aproximated frequency value

                        if (aproximatedPosition > -1 && freqPosition > 60 && freqPosition < 600)
                        {
                            var acfPosition = sampleFrequency/freqPosition; //aproximated time value

                            if (candidates.Count > 2)
                            {
                                var maxCandidate = candidates.Max(x => x.Item2)*0.2;
                                candidates.RemoveAll(x => x.Item2 < maxCandidate);

                                pieceImg.Add(new[] { acfPosition });
                                acfsImg.Add(acfsSample);
                                acfImg.Add(acf);
                                globalCandidates.Add(candidates);
                                globalCandidatesMinimums.Add(candidatesMins);
                                prevAcfsCandidate = aproximatedPosition;
                            }
                            else
                            {
                                pieceImg.Add(new[] { 0.0 });
                                acfsImg.Add(acfsSample);
                                acfImg.Add(acf);
                                globalCandidates.Add(candidates);
                                globalCandidatesMinimums.Add(candidatesMins);
                                prevAcfsCandidate = -1;
                            }
                        }
                        else
                        {
                            pieceImg.Add(new[] { 0.0 });
                            acfsImg.Add(acfsSample);
                            acfImg.Add(acf);
                            globalCandidates.Add(candidates);
                            globalCandidatesMinimums.Add(candidatesMins);
                            prevAcfsCandidate = -1;
                        }
                    }
                    else
                    {
                        pieceImg.Add(new[] {0.0});
                        acfsImg.Add(acfsSample);
                        acfImg.Add(acf);
                        globalCandidates.Add(candidates);
                        globalCandidatesMinimums.Add(candidatesMins);
                        prevAcfsCandidate = -1;
                    }
                }
                ExtractPitch(pieceImg, globalCandidates, sampleFrequency, furieSize, globalCandidatesMinimums);

                foreach (var doublese in pieceImg)
                {
//                    for (int i = 0; i < jump; i++)
//                    {
                        resultImg.Add(doublese);
//                    }
                }
//                img.AddRange(pieceImg);
                prevStop = curentMark.Item2 + 1;
            }

//            image = img.Select(x => new []{x[0]> 0.0?sampleFrequency/x[0]:0.0}).ToArray();
            Acf = acfImg.ToArray();
            Acfs = acfsImg.ToArray();
            image = resultImg.ToArray();
//            image = img.ToArray();
        }

        private void ExtractPitch(IReadOnlyList<double[]> img, IReadOnlyList<List<Tuple<int, double>>> globalCandidates, int sampleRate, double furieSize, IReadOnlyList<List<Tuple<int, double>>> globalCandidatesMinimums)
        {
            var searchWindow = Math.Ceiling(sampleRate*1.2/furieSize);
            var prevVal = 0.0;
            for (int i = 0; i < img.Count; i++)
            {
//                if (img[i][0] <= 0.0 && globalCandidates[i].Any())
//                {
//                    var candidate = globalCandidates[i].OrderByDescending(x => x.Item2).First();
//                    var mins = globalCandidatesMinimums[i].OrderBy(x => Math.Abs(x.Item1 - img[i][0])).Take(2).ToArray();
//                    if (mins.Length == 2 && candidate.Item1 > 18 && candidate.Item1 < 183 && candidate.Item2 > 0.1)
//                    {
//                        var dist = mins[0].Item1 - mins[1].Item1;
//                        var amp = candidate.Item2 - (mins[0].Item2 + mins[1].Item2)/2;
//                        if (amp > 0.2)
//                        {
//                            img[i][0] = candidate.Item1;
//                        }
//                    }
//                }

                if (globalCandidates[i].Count > 0 && img[i][0] > 0.0 && globalCandidates[i].Any(x => Math.Abs(x.Item1 - img[i][0]) < searchWindow && x.Item1 > 18 && x.Item1 < 183))
                {
                    var nearest =
                        globalCandidates[i]
                            .Where(x => Math.Abs(x.Item1 - img[i][0]) < searchWindow && x.Item1 > 18 && x.Item1 < 183)
                            .Max(x => x.Item2);
                    img[i][0] =
                        globalCandidates[i]
                            .Where(x => Math.Abs(x.Item1 - img[i][0]) < searchWindow && x.Item1 > 18 && x.Item1 < 183)
                            .First(x => x.Item2 >= nearest)
                            .Item1;
                }
            }

            for (int cnts = 0; cnts < 0; cnts++)
            {
                for (int i = 0; i < img.Count - 1; i++)
                {
                    var delta = img[i][0] - img[i + 1][0];
                    if (Math.Abs(delta) > 5)
                    {
                        var size = 0;
                        while (i + size + 2 < img.Count && (Math.Abs(img[i+size][0] - img[i+size+1][0]) < 5 || delta* img[i + size][0] - img[i + size + 1][0] < 0.0))
                        {
                            size++;
                        }

                        for (int j = 0; j < size; j++)
                        {
                            if(!globalCandidates[i+j].Any())
                                continue;

                            var linearApprox = FunctionBetwenTwoPoints(i, i + size + 1, img[i][0], img[i + size + 1][0],
                                i + j);
                            var minDist = globalCandidates[i + j].Min(x => Math.Abs(x.Item1 - linearApprox));
                            var candidate =
                                globalCandidates[i + j].First(x => Math.Abs(x.Item1 - linearApprox) <= minDist);
                            img[i + j][0] = candidate.Item1;
                        }

//                        var max = globalCandidates[i].Max(x => x.Item2);
//                        var candidate = globalCandidates[i].First(x => x.Item2 >= max);
//                        if (Math.Abs(candidate.Item1 - img[i + 1][0]) < Math.Abs(img[i][0] - img[i + 1][0]))
//                            img[i][0] = candidate.Item1;

//                        //jump
//                        var jumpStart = i+1;
//                        var t = 1;
//                        var jumpCnt = 0;
//                        while (i + t + 1 < img.Count && Math.Abs(img[i + t][0] - img[i + t + 1][0]) < 5)
//                        {
//                            jumpCnt++;
//                            t++;
//                        }
//
//                        for (int j = 0; j < jumpCnt + 1; j++)
//                        {
//                            if (globalCandidates[jumpStart + j].Count < 1)
//                                continue;
//
//                            var pos = img[jumpStart+jumpCnt][0] > 0.0?FunctionBetwenTwoPoints(jumpStart, jumpStart + jumpCnt, img[jumpStart][0],
//                                img[jumpStart + jumpCnt][0], jumpStart + j):img[jumpStart + j][0];
//
//                            var nearest =
//                                globalCandidates[jumpStart + j].Where(x => Math.Abs(x.Item1 - pos) < 2.5* searchWindow && x.Item1 > 18 && x.Item1 < 183)
//                                    .ToArray();
//                            if (!nearest.Any())
//                                continue;
//
//                            var nearestPoint = nearest.Max(x => x.Item2);
//
//                            img[jumpStart + j][0] = nearest.First(x => x.Item2 >= nearestPoint).Item1;
//                        }
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
