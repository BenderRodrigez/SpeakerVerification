using System;
using System.Linq;

namespace HelpersLibrary.DspAlgorithms
{
    public class SpeechSearch
    {
        private readonly byte _histogramBags;
        private readonly int _windowSize;
        private readonly float _overlapping;

        public SpeechSearch(byte histogramBags, float windowSize, float overlapping, int sampleFrequency)
        {
            _histogramBags = histogramBags;
            _windowSize = (int) Math.Round(windowSize*sampleFrequency);
            _overlapping = overlapping;
        }

        public void GetMarks(float[] speechWave, out int startPosition, out int stopPosition)
        {
            var energy = CalculateEnergyFunction(speechWave);
            double border;
            CalcHistogramm(energy, out border);
            SearchSpeech(energy, out startPosition, out stopPosition, border);
        }


        private void SearchSpeech(double[] energy, out int startPoint, out int endPoint, double speechDetectorBorder)
        {
            startPoint = int.MinValue;
            endPoint = int.MinValue;
            for (int i = 0; i < energy.Length; i++)
            {
                if (startPoint < 0 && energy[i] > speechDetectorBorder)
                {
                    startPoint = i;
                }
                if (endPoint < 0 && energy[energy.Length - i - 1] > speechDetectorBorder)
                {
                    endPoint = energy.Length - i - 1;
                }
                if (startPoint >= 0 && endPoint > 0 && endPoint < energy.Length)
                    break;
            }
        }

        private float[] CalcHistogramm(double[] energy, out double speechDetectorBorder)
        {
            var signalRange = energy.Max() - energy.Min();
            var minValue = energy.Min();
            var rangeStep = signalRange / _histogramBags;
            var prevBagValue = double.NegativeInfinity;
            speechDetectorBorder = double.NegativeInfinity;
            var bags = new float[_histogramBags];
            for (int i = 0; i < _histogramBags; i++)
            {
                for (int j = 0; j < energy.Length; j++)
                    bags[i] += (energy[j] > prevBagValue && energy[j] <= (minValue + rangeStep * i)) ? 1 : 0;

                bags[i] /= energy.Length;
                prevBagValue = minValue + rangeStep * i;
                if (i > 1)
                {
                    if (bags[i - 2] > bags[i - 1] && bags[i] > bags[i - 1] && double.IsInfinity(speechDetectorBorder))
                        speechDetectorBorder = ((minValue + rangeStep * (i - 1.0)) + (minValue + rangeStep * (i - 2.0))) / 2.0;
                }
            }
            return bags;
        }

        private double[] CalculateEnergyFunction(float[] sound)
        {
            var tmp = new double[sound.Length - _windowSize];
            var jump = (int)Math.Round(_windowSize * (1.0 - _overlapping));
            for (int i = _windowSize / 2; i < sound.Length - _windowSize; i += jump)
            {
                for (int j = 0; j < _windowSize; j++)
                {
                    tmp[i] += Math.Pow(sound[i + j], 2);
                }
                for (int k = i + 1; k < i + jump && k < tmp.Length; k++)
                    tmp[k] = tmp[i];
            }
            var min = tmp.Min();
            for (int i = _windowSize/2; i < tmp.Length; i++)
            {
                tmp[i] -= min;
            }
            return tmp;
        }
    }
}
