using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HelpersLibrary.DspAlgorithms.Filters;

namespace HelpersLibrary.DspAlgorithms
{
    public class TonalSpeechSelector
    {
        private float[] _energy;
        private float[] _corellation;
        private float[] _zeroCrossings;
        private float[] _generalFeature;
        private readonly float[] _signal;
        private readonly double _border;

        public int LowPassFilterBorder { get; set; }
        public float AdditionalNoiseLevel { get; set; }
        public int HistogramBagsCout { get; set; }

        public TonalSpeechSelector(float[] speechSignal, float windowSize, float overlapping, int sampleRate)
        {
            LowPassFilterBorder = 300;
            AdditionalNoiseLevel = 0.2f;
            HistogramBagsCout = 20;
            var windowSizeSamples = (int) Math.Round(sampleRate*windowSize);
            var jumpSize = 1.0f - overlapping;
            _signal = new float[speechSignal.Length];
            Array.Copy(speechSignal, _signal, speechSignal.Length);
            GetZeroCrossings(windowSizeSamples, jumpSize);
            GetEnergy(windowSizeSamples, jumpSize, sampleRate);
            GetCorellation(windowSizeSamples, jumpSize);
            GenerateGeneralFeature(windowSizeSamples);
            CalcHistogramm(out _border);
        }

        public float[] CleanUnvoicedSpeech()
        {
            var voicedSpeech = new List<float>(_signal.Length);
            var start = -1;
            using (//TODO: remove this debug feature after it
                var writer =
                    new BinaryWriter(File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "generalFeature.txt"))))
            {
                for (int i = 0; i < _generalFeature.Length; i++)
                {
                    writer.Write((short)Math.Round(_generalFeature[i] * 10000000));
                    if (_generalFeature[i] > _border && start < 0)
                    {
                        start = i;
                    }
                    else
                    {
                        if (start > -1 && _generalFeature[i] < _border)
                        {
                            var tmp = new float[i - start];
                            Array.Copy(_signal, start, tmp, 0, i - start);
                            start = -1;
                            voicedSpeech.AddRange(tmp);
                        }
                    }
                }
            }
            return voicedSpeech.ToArray();
        }

        public Tuple<int, int>[] GetTonalSpeechMarks()
        {
            var marks = new List<Tuple<int,int>>();
            var start = -1;
            for (int i = 0; i < _generalFeature.Length; i++)
            {
                if (_generalFeature[i] > _border && start < 0)
                {
                    start = i;
                }
                else
                {
                    if (start > -1 && _generalFeature[i] < _border)
                    {
                        marks.Add(new Tuple<int,int>(start, i));
                        start = -1;
                    }
                }
            }
            if(start > -1)
                marks.Add(new Tuple<int, int>(start, _generalFeature.Length-1));
            return marks.ToArray();
        }

        private float[] CalcHistogramm(out double tonalBorder)
        {
            var signalRange = _generalFeature.Max() - _generalFeature.Min();
            var minValue = _generalFeature.Min();
            var rangeStep = signalRange / HistogramBagsCout;
            var prevBagValue = double.NegativeInfinity;
            tonalBorder = double.NegativeInfinity;
            var bags = new float[HistogramBagsCout];
            for (int i = 0; i < HistogramBagsCout; i++)
            {
                for (int j = 0; j < _generalFeature.Length; j++)
                    bags[i] += (_generalFeature[j] > prevBagValue && _generalFeature[j] <= (minValue + rangeStep * i)) ? 1 : 0;

                bags[i] /= _generalFeature.Length;
                prevBagValue = minValue + rangeStep * i;
                if (i > 1)
                {
                    if (bags[i - 2] > bags[i - 1] && bags[i] > bags[i - 1] && double.IsInfinity(tonalBorder))
                        tonalBorder = ((minValue + rangeStep * (i - 1.0)) + (minValue + rangeStep * (i - 2.0))) / 2.0;
                }
            }
            return bags;
        }

        private void GetEnergy(int windowSize, float overlapping, int sampleRate)
        {
            var file = new float[_signal.Length];
            _signal.CopyTo(file, 0);
            var filter = new Lpf(LowPassFilterBorder, sampleRate);
            file = filter.StartFilter(file);
            var tmp = new float[file.Length - windowSize];
            var jump = (int)Math.Round(windowSize * overlapping);
            for (int i = 0; i < file.Length - windowSize; i += jump)
            {
                for (int j = 0; j < windowSize; j++)
                {
                    tmp[i] += (float)Math.Pow(file[i + j], 2);
                }
                for (int k = i + 1; k < i + jump && k < tmp.Length; k++)
                    tmp[k] = tmp[i];
            }
            _energy = tmp;
        }

        private void GetCorellation(int windowSize, float overlapping)
        {
            var file = new float[_signal.Length];
            _signal.CopyTo(file, 0);
            var rand = new Random(DateTime.Now.Millisecond);

            var tmp = new float[file.Length - windowSize];

            for (int i = 0; i < file.Length; i += 20)
                file[i] *= (float)rand.NextDouble() * AdditionalNoiseLevel;

            var jump = (int)Math.Round(windowSize * overlapping);
            for (int i = 0; i < file.Length - windowSize; i += jump)
            {
                double energy = 0.0;
                for (int j = 0; j < windowSize - 1; j++)
                {
                    energy += Math.Pow(file[i + j], 2);
                    tmp[i] += file[i + j] * file[i + j + 1];
                }
                tmp[i] = (float)(50.0f * (tmp[i] / energy));
                for (int k = i + 1; k < i + jump && k < tmp.Length; k++)
                    tmp[k] = tmp[i];
            }
            var min = tmp.Min();
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] -= min;
            }
            _corellation = tmp;
        }

        private void GetZeroCrossings(int windowSize, float overlapping)
        {
            var tmp = new float[_signal.Length - windowSize];

            var jump = (int)Math.Round(windowSize * overlapping);
            for (int i = 0; i < _signal.Length - windowSize; i += jump)
            {
                int zeroes = 0;
                for (int j = 0; j < windowSize - 1; j++)
                {
                    if (_signal[i + j] * _signal[i + j + 1] < 0.0)
                        zeroes++;
                }
                tmp[i] = (float)(zeroes / 2.0 * windowSize);
                for (int k = i + 1; k < i + jump && k < tmp.Length; k++)
                    tmp[k] = tmp[i];
            }
            _zeroCrossings = tmp;
        }

        private void GenerateGeneralFeature(int windowSize)
        {
            var tmp = new List<float>(_energy.Length + windowSize / 2);
            tmp.AddRange(new float[windowSize/2]);
            for (int i = 0; i < _energy.Length; i++)
            {
                tmp.Add(_corellation[i] * _energy[i] / _zeroCrossings[i]);
            }
            _generalFeature = tmp.ToArray();
        }
    }
}
