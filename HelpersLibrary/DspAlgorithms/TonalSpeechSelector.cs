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
        private float[] _signal;
        private int _sampleRate;

        public double Border { get; set; }
        public double MinimalVoicedSpeechLength { get; set; }
        public int LowPassFilterBorder { get; set; }
        public float AdditiveNoiseLevel { get; set; }

        public TonalSpeechSelector()
        {
            InitVariables();
        }

        public void InitData(float[] speechSignal, float windowSize, float overlapping, int sampleRate)
        {
            _sampleRate = sampleRate;
            var windowSizeSamples = (int) Math.Round(sampleRate*windowSize);
            var jumpSize = 1.0f - overlapping;
            _signal = new float[speechSignal.Length];
            Array.Copy(speechSignal, _signal, speechSignal.Length);
            GetZeroCrossings(windowSizeSamples, jumpSize);
            GetEnergy(windowSizeSamples, jumpSize, sampleRate);
            GetCorellation(windowSizeSamples, jumpSize);
            GenerateGeneralFeature(windowSizeSamples);
        }

        private void InitVariables()
        {
            LowPassFilterBorder = 300;
            AdditiveNoiseLevel = 0.2f;
            MinimalVoicedSpeechLength = 0.04;
            Border = 5.0;
        }

        public Tuple<int, int>[] GetTonalSpeechMarks()
        {
            return GetTonalSpeechMarksStandart();
        }

        private Tuple<int, int>[] GetTonalSpeechMarksStandart()
        {
            var marks = new List<Tuple<int, int>>();
            var start = -1;
            for (int i = 0; i < _generalFeature.Length; i++)
            {
                if (_generalFeature[i] > Border && start < 0)
                {
                    start = i;
                }
                else
                {
                    if (start > -1 && _generalFeature[i] < Border)
                    {
                        marks.Add(new Tuple<int, int>(start, i));
                        start = -1;
                    }
                }
            }
            if (start > -1)
                marks.Add(new Tuple<int, int>(start, _generalFeature.Length - 1));
            return marks.Where(x => (x.Item2 - x.Item1) / (double)_sampleRate > MinimalVoicedSpeechLength).ToArray();
        }

        private void GetEnergy(int windowSize, float overlapping, int sampleRate)
        {
            var file = new float[_signal.Length];
            _signal.CopyTo(file, 0);
            var filter = new Lpf(LowPassFilterBorder, sampleRate);
            file = filter.StartFilter(file);
            var tmp = new float[file.Length - windowSize];
            var jump = (int) Math.Round(windowSize*overlapping);
            for (int i = 0; i < file.Length - windowSize; i += jump)
            {
                for (int j = 0; j < windowSize; j++)
                {
                    tmp[i] += (float) Math.Pow(file[i + j], 2);
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

            for (int i = 0; i < file.Length; i++)
                file[i] += (float) rand.NextDouble()*AdditiveNoiseLevel;

            var jump = (int) Math.Round(windowSize*overlapping);
            for (int i = 0; i < file.Length - windowSize; i += jump)
            {
                double energy = 0.0;
                for (int j = 0; j < windowSize - 1; j++)
                {
                    energy += Math.Pow(file[i + j], 2);
                    tmp[i] += file[i + j]*file[i + j + 1];
                }
                tmp[i] = (float) (50.0f*(tmp[i]/energy));
                for (int k = i + 1; k < i + jump && k < tmp.Length; k++)
                    tmp[k] = tmp[i];
            }
            var min = tmp.Min();
            for (int i = 0; i < tmp.Length; i++)
            {
                tmp[i] -= min;
            }
            _corellation = tmp;
#if DEBUG
            using (var writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "rone.txt")))
            {
                foreach (var x in _corellation)
                {
                    writer.WriteLine(x);
                }
            }
#endif
        }

        private void GetZeroCrossings(int windowSize, float overlapping)
        {
            var tmp = new float[_signal.Length - windowSize];

            var jump = (int) Math.Round(windowSize*overlapping);
            for (int i = 0; i < _signal.Length - windowSize; i += jump)
            {
                int zeroes = 0;
                for (int j = 0; j < windowSize - 1; j++)
                {
                    if (_signal[i + j]*_signal[i + j + 1] < 0.0)
                        zeroes++;
                }
                tmp[i] = (float) (zeroes/2.0*windowSize);
                for (int k = i + 1; k < i + jump && k < tmp.Length; k++)
                    tmp[k] = tmp[i];
            }
            _zeroCrossings = tmp;
#if DEBUG
            using (var writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "zeros.txt")))
            {
                foreach (var x in _zeroCrossings)
                {
                    writer.WriteLine(x);
                }
            }
#endif
        }

        private void GenerateGeneralFeature(int windowSize)
        {
            var tmp = new List<float>(_energy.Length + windowSize/2);
            tmp.AddRange(new float[windowSize/2]);
            for (int i = 0; i < _energy.Length; i++)
            {
                tmp.Add(_corellation[i]*_energy[i]/*/_zeroCrossings[i]*/);
            }
            _generalFeature = tmp.ToArray();
#if DEBUG
            using (var writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "generalFeature.txt")))
            {
                foreach (var x in _generalFeature)
                {
                    writer.WriteLine(x);
                }
            }
#endif
        }
    }
}
