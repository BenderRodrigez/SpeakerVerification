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
        public double MaxFrequencyJumpPercents;
        public double FrequencyEnergyLineBorder;
        public int FilterDiameter;
        public double SignalCentralLimitationBorder;
        public float HighPassFilterBorder;
        public float LowPassFilterBorder;

        public Corellation()
        {
            MaxFrequencyJumpPercents = 0.27;
            FrequencyEnergyLineBorder = 0.00005;
            FilterDiameter = 5;
            SignalCentralLimitationBorder = 0.3;
            HighPassFilterBorder = 60.0f;
            LowPassFilterBorder = 600.0f;
        }

        public WindowFunctions.WindowType UsedWindowType { private get; set; }
        public int UsedWindowSize { private get; set; }
        public int UsedVectorSize { private get; set; }
        public double[][] Acfs { get; private set; }
        public double[][] Acf { get; private set; }

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
            var hpf = new Hpf(HighPassFilterBorder, sampleFrequency);
            var filtredSignal = hpf.Filter(inputSignal);
            var lpf = new Lpf(LowPassFilterBorder, sampleFrequency);
            filtredSignal = lpf.StartFilter(filtredSignal);

            //analysis variables
            var jump = (int) Math.Round(size*offset);
            var acfImg = new List<double[]>();
            var acfsImg = new List<double[]>();
            var furieSize = Math.Pow(2, Math.Ceiling(Math.Log(size, 2) + 1));
            var resultImg = new List<double[]>(inputSignal.Length);
            var prevStop = 0;
            var rOneList = new List<double>();
            var prevCandidate = 0.0;
            var globalCandidates = new List<List<Tuple<int, double>>>();
            foreach (var curentMark in speechMarks)
            {
                for (int i = prevStop; i < curentMark.Item1; i++)
                    if (i%jump == 0)
                    {
                        resultImg.Add(new[] {0.0});
                        acfsImg.Add(new double[(int)furieSize/8].Select(x => double.NaN).ToArray());
                        acfImg.Add(new double[size].Select(x => double.NaN).ToArray());
                        globalCandidates.Add(new List<Tuple<int, double>>());
                    }

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

                    var maxSignal = filterdData.Max(x=> Math.Abs(x)) * SignalCentralLimitationBorder;
                    filterdData = filterdData.Select(x => Math.Abs(x) > maxSignal ? x : 0.0f).ToArray();//provide central cut

                    double[] acf;
                    double rOne;
                    FFT.AutoCorrelation(size, filterdData, out acf, out rOne);
                    rOneList.Add(rOne);

                    double[] acfsSample;
                    FFT.SpectrumAutoCorrelation(size, data, out acfsSample);

                    //extract candidates
                    var acfsCandidates = new List<Tuple<int, double>>();
                    for (int i = 1; i < acfsSample.Length-1; i++)
                    {
                        if (acfsSample[i] > acfsSample[i - 1] && acfsSample[i] > acfsSample[i + 1])
                        {
                            acfsCandidates.Add(new Tuple<int, double>(i, acfsSample[i]));
                        }
                    }

                    for (int i = 17; i < acf.Length && i < 184; i++)
                    {
                        if (acf[i - 1] > acf[i - 2] && acf[i - 1] > acf[i])
                        {
                            candidates.Add(new Tuple<int, double>(i - 1, acf[i - 1]));//add each maximum of function from 60 to 600 Hz
                        }
                    }

                    var aproximatedPosition = acfsCandidates.Any() ? acfsCandidates[0].Item1 : -1;
                    var freqPosition = (sampleFrequency / furieSize) * aproximatedPosition;//aproximated frequency value

                    if (acfsCandidates.Count > 3 && aproximatedPosition > -1 && freqPosition > 60 && freqPosition < 600 &&
                        candidates.Count > 2 && (rOne > FrequencyEnergyLineBorder && (Math.Abs(prevCandidate - freqPosition) / prevCandidate < MaxFrequencyJumpPercents || prevCandidate <= 0.0)))
                    {
                        var acfPosition = sampleFrequency/freqPosition; //aproximated time value

                        resultImg.Add(new[] { acfPosition });
#if DEBUG
                        acfsImg.Add(acfsSample);
                        acfImg.Add(acf);
#endif
                        globalCandidates.Add(candidates);
                    }
                    else
                    {
                        resultImg.Add(new[] { 0.0 });
#if DEBUG
                        acfsImg.Add(acfsSample);
                        acfImg.Add(acf);
#endif
                        globalCandidates.Add(candidates);
                    }
                    prevCandidate = freqPosition;
                }
                prevStop = curentMark.Item2 + 1;
            }
            ExtractPitch(resultImg, globalCandidates, sampleFrequency, furieSize, jump, speechMarks);
#if DEBUG
            Acf = acfImg.ToArray();
            Acfs = acfsImg.ToArray();
#endif
            image = resultImg.ToArray();

            SaveTmpImage(rOneList.Select(x=> new[]{x}).ToArray());
        }

        private void ExtractPitch(IReadOnlyList<double[]> img, IReadOnlyList<List<Tuple<int, double>>> globalCandidates, int sampleRate, double furieSize, int jumpSize, Tuple<int,int>[] voicedSpeechMarks)
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
                if (prevVal > 0.0 && Math.Abs(prevVal - img[i][0])/prevVal > MaxFrequencyJumpPercents && Math.Abs(prevVal - acfsCandidate)/prevVal > MaxFrequencyJumpPercents && globalCandidates[i].Any())
                {
                    var max = globalCandidates[i].Max(x => x.Item2);
                    var newCandidate = globalCandidates[i].First(x => x.Item2 >= max).Item1;
                    if (Math.Abs(newCandidate - prevVal)/prevVal <= MaxFrequencyJumpPercents)
                    {
                        img[i][0] = newCandidate;
                    }
                }
                prevVal = img[i][0];
            }

            prevVal = 0.0;
            for (int i = FilterDiameter/2; i < img.Count-FilterDiameter/2; i++)
            {
                //use median filter to cath the errors
                var itemsToSort = new List<double>(FilterDiameter);
                for (int j = -FilterDiameter/2; j <= FilterDiameter/2; j++)
                {
                    itemsToSort.Add(img[i+j][0]);
                }
                var arr = itemsToSort.ToArray();
                Array.Sort(arr);
                if (Math.Abs(arr[FilterDiameter/2] - prevVal)/prevVal > MaxFrequencyJumpPercents && prevVal > 0.0)
                {
                    var nearestSpeechEnd = voicedSpeechMarks.OrderBy(x=> x.Item2).First(x => x.Item2/jumpSize >= i);
                    for (int j = i; j < nearestSpeechEnd.Item2/jumpSize && j < img.Count; j++)
                    {
                        img[j][0] = 0.0;
                    }
                }
                else
                {
                    img[i][0] = arr[FilterDiameter/2];
                }
                prevVal = img[i][0];
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
