using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HelpersLibrary.DspAlgorithms.Filters;

namespace HelpersLibrary.DspAlgorithms
{
    public class TonalSpeechSelector
    {
        public enum Algorithm
        {
            Standart,
            Acfs,
            Acf
        }
        private float[] _energy;
        private float[] _corellation;
        private float[] _zeroCrossings;
        private float[] _generalFeature;
        private float[] _signal;
        private readonly double _border;
        private readonly Algorithm _usedAlgorithm;
        private Tuple<int, double>[][] _acfsMaximums;
        private Tuple<int, double>[][] _acfsMinimums;
        private readonly int _windowSize;
        private readonly int _jump;

        public int LowPassFilterBorder { get; set; }
        public float AdditionalNoiseLevel { get; set; }
        public int HistogramBagsCout { get; set; }

        public TonalSpeechSelector(float[] speechSignal, float windowSize, float overlapping, int sampleRate, Algorithm usedAlgorithm = Algorithm.Standart)
        {
            _usedAlgorithm = usedAlgorithm;
            LowPassFilterBorder = 300;
            AdditionalNoiseLevel = 0.2f;
            HistogramBagsCout = 20;
            var windowSizeSamples = (int) Math.Round(sampleRate*windowSize);
            var jumpSize = 1.0f - overlapping;
            _signal = new float[speechSignal.Length];
            Array.Copy(speechSignal, _signal, speechSignal.Length);
            _windowSize = windowSizeSamples;
            switch (_usedAlgorithm)
            {
                case Algorithm.Standart:
                    GetZeroCrossings(windowSizeSamples, jumpSize);
                    GetEnergy(windowSizeSamples, jumpSize, sampleRate);
                    GetCorellation(windowSizeSamples, jumpSize);
                    GenerateGeneralFeature(windowSizeSamples);
                    CalcHistogramm(out _border);
                    break;
                case Algorithm.Acfs:
                    //init acfs algorithm
                    _jump = (int)Math.Round(_windowSize * jumpSize);
                    InitAcfsAlgorithm(windowSizeSamples, jumpSize);
                    break;
                case Algorithm.Acf:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void InitAcfsAlgorithm(int windowSize, float overlapping)
        {
            var jump = (int)Math.Round(windowSize * overlapping);
            var maximums = new List<Tuple<int, double>[]>();
            var minimums = new List<Tuple<int, double>[]>();
            for (int i = 0; i < _signal.Length - windowSize; i += jump)
            {
                double[] currentAcfs;
                var data = new float[windowSize];
                Array.Copy(_signal, data, windowSize);
                var window = new WindowFunctions();
                data = window.PlaceWindow(data, WindowFunctions.WindowType.Blackman);
                FFT.SpectrumAutoCorrelation(windowSize, data, out currentAcfs);
                var currentMax = new List<Tuple<int,double>>();
                var currentMin = new List<Tuple<int, double>>();
                for (int j = 1; j < currentAcfs.Length - 1; j++)
                {
                    if (currentAcfs[j] > currentAcfs[j - 1] && currentAcfs[j] > currentAcfs[j + 1])
                    {
                        currentMax.Add(new Tuple<int, double>(j, currentAcfs[j]));
                    }
                    else if (currentAcfs[j] < currentAcfs[j - 1] && currentAcfs[j] < currentAcfs[j + 1])
                    {
                        currentMin.Add(new Tuple<int, double>(j, currentAcfs[j]));
                    }
                }
                maximums.Add(currentMax.ToArray());
                minimums.Add(currentMin.ToArray());
            }
            _acfsMaximums = maximums.ToArray();
            _acfsMinimums = minimums.ToArray();
        }

        public float[] CleanUnvoicedSpeech()
        {
            switch (_usedAlgorithm)
            {
                case Algorithm.Standart:
                    return CleanUnvoicedSpeechStandart();
                case Algorithm.Acfs:
                    return CleanUnvoicedSpeechAcfs();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private float[] CleanUnvoicedSpeechAcfs()
        {
            var resSignal = new List<float>();
            for (int i = 0; i < _acfsMinimums.Length; i++)
            {
                if (_acfsMaximums[i].Length > 3 && Math.Abs(_acfsMaximums[i][1].Item1 / _acfsMaximums[i][0].Item1 - _acfsMaximums[i][2].Item1 / _acfsMaximums[i][1].Item1) < 2)
                {
                    //if we have multiple maximums of function and each of them is divsible to each other (in some disersion) is a tonal sound
                    resSignal.Add(_signal[i + _windowSize/2]);
                }
            }
            return resSignal.ToArray();
        }

        private float[] CleanUnvoicedSpeechStandart()
        {
            var voicedSpeech = new List<float>(_signal.Length);
            var start = -1;
            using ( //TODO: remove this debug feature after it
                var writer = new BinaryWriter(File.Create(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "generalFeature.txt"))))
            {
                for (int i = 0; i < _generalFeature.Length; i++)
                {
                    writer.Write((short) Math.Round(_generalFeature[i]*10000000));
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
            switch (_usedAlgorithm)
            {
                case Algorithm.Standart:
                    return GetTonalSpeechMarksStandart();
                case Algorithm.Acfs:
                    return GetTonalSpeechMarksAcfs();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private Tuple<int, int>[] GetTonalSpeechMarksAcfs()
        {
            var marks = new List<Tuple<int, int>>();
            var start = -1;
            for (int i = 0; i < _acfsMinimums.Length; i++)
            {
                if (_acfsMaximums[i].Length > 3 && _acfsMaximums[i].Sum(x => x.Item2)/_acfsMaximums[i].Length > _acfsMinimums[i].Sum(x=> x.Item2)/_acfsMinimums[i].Length)
                {
                    //if we have multiple maximums of function and each of them is divsible to each other (in some disersion) is a tonal sound
                    if (start < 0)
                        start = i;
                }
                else if(start > -1)
                {
                    marks.Add(new Tuple<int, int>(start*_jump, i*_jump));
                    start = -1;
                }
            }
            if (start > -1)
                marks.Add(new Tuple<int, int>(start*_jump, (_acfsMaximums.Length - 1)*_jump));
            return marks.ToArray();
        }

        private Tuple<int, int>[] GetTonalSpeechMarksStandart()
        {
            var marks = new List<Tuple<int, int>>();
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
                        marks.Add(new Tuple<int, int>(start, i));
                        start = -1;
                    }
                }
            }
            if (start > -1)
                marks.Add(new Tuple<int, int>(start, _generalFeature.Length - 1));
            return marks.ToArray();
        }

        private float[] CalcHistogramm(out double tonalBorder)
        {
            var signalRange = _generalFeature.Max() - _generalFeature.Min();
            var minValue = _generalFeature.Min();
            var rangeStep = signalRange/HistogramBagsCout;
            var prevBagValue = double.NegativeInfinity;
            tonalBorder = double.NegativeInfinity;
            var bags = new float[HistogramBagsCout];
            for (int i = 0; i < HistogramBagsCout; i++)
            {
                for (int j = 0; j < _generalFeature.Length; j++)
                    bags[i] += (_generalFeature[j] > prevBagValue && _generalFeature[j] <= (minValue + rangeStep*i)) ? 1 : 0;

                bags[i] /= _generalFeature.Length;
                prevBagValue = minValue + rangeStep*i;
                if (i > 1)
                {
                    if (bags[i - 2] > bags[i - 1] && bags[i] > bags[i - 1] && double.IsInfinity(tonalBorder))
                        tonalBorder = ((minValue + rangeStep*(i - 1.0)) + (minValue + rangeStep*(i - 2.0)))/2.0;
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

            for (int i = 0; i < file.Length; i += 20)
                file[i] *= (float) rand.NextDouble()*AdditionalNoiseLevel;

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
        }

        private void GenerateGeneralFeature(int windowSize)
        {
            var tmp = new List<float>(_energy.Length + windowSize/2);
            tmp.AddRange(new float[windowSize/2]);
            for (int i = 0; i < _energy.Length; i++)
            {
                tmp.Add(_corellation[i]*_energy[i]/_zeroCrossings[i]);
            }
            _generalFeature = tmp.ToArray();
        }
    }
}
