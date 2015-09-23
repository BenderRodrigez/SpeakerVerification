using System;
using System.Collections.Generic;
using System.Numerics;

namespace HelpersLibrary.DspAlgorithms
{
    public class Cepstrum
    {
        private readonly List<List<double>> _melFilterStart = new List<List<double>>();
        private readonly List<double> _filterCenters = new List<double>();
        private readonly List<int> _filterFrequencesIndex = new List<int>();
        private readonly int _filterNumber;
        private readonly int _furieSize;
        private readonly double _sampleRate;
        private readonly float _overlapping;

        public Cepstrum(int filterNumber, double windowSize, double sampleRate, float overlapping)
        {
            var furiePower = (int) Math.Round(Math.Log(windowSize*sampleRate, 2.0));
            _filterNumber = filterNumber;
            _furieSize = (int) Math.Pow(2.0, furiePower);
            _sampleRate = sampleRate;
            _overlapping = overlapping;

            MelFilterInit();
        }

        private void MelFilterInit()
        {
            var binToFreq = new Dictionary<int, double>();

            for (int f = 0; f < 1 + _furieSize/2; f++)
            {
                binToFreq[f] = (f*_sampleRate)/(_furieSize);
            }

            var maxMel = 1125.0*Math.Log(1.0 + (_sampleRate/2.0)/700.0);
            var minMel = 1125.0*Math.Log(1.0 + 300.0/700.0);
            var dMel = (maxMel - minMel)/(_filterNumber + 1);
            for (int n = 0; n < _filterNumber + 2; n++)
            {
                var mel = minMel + n*dMel;
                var hz = 700.0*(Math.Exp(mel/1125.0) - 1.0);
                _filterCenters.Add(hz);
                var bin = (int) Math.Floor((_furieSize)*hz/_sampleRate);
                _filterFrequencesIndex.Add(bin);
            }

            for (var it1 = 1; it1 < _filterFrequencesIndex.Count - 1; it1++)
            {
                var fBank = new List<double>();

                var fBelow = binToFreq[_filterFrequencesIndex[it1 - 1]];
                var fCentre = binToFreq[_filterFrequencesIndex[it1]];
                var fAbove = binToFreq[_filterFrequencesIndex[it1 + 1]];

                for (int n = 0; n < 1 + _furieSize/2; n++)
                {
                    var freq = binToFreq[n];
                    double val;

                    if ((freq <= fCentre) && (freq >= fBelow))
                    {
                        val = ((freq - fBelow)/(fCentre - fBelow));
                    }
                    else if ((freq > fCentre) && (freq <= fAbove))
                    {
                        val = ((fAbove - freq)/(fAbove - fCentre));
                    }
                    else
                    {
                        val = 0.0;
                    }
                    fBank.Add(val);
                }
                _melFilterStart.Add(fBank);
            }
        }

        private double[] GetMelScaledCepstrum(double[] spectrum)
        {

            var preDct = new List<double>(); // Initilise pre-discrete cosine transformation vetor array
            var postDct = new List<double>();
            // Initilise post-discrete cosine transformation vetor array / MFCC Coefficents

            foreach (var d in _melFilterStart)
            {
                var cel = 0.0;
                int n = 0;
                foreach (var filt in d)
                {
                    cel += filt*spectrum[n];
                    n++;
                }
                preDct.Add(Math.Log(cel)); // Compute the log of the spectrum
            }

            // Perform the Discrete Cosine Transformation
            for (int i = 0; i < _melFilterStart.Count; i++)
            {
                double val = 0;
                int n = 0;
                foreach (var d in preDct)
                {
                    val += d*Math.Cos(i*(n - 0.5)*Math.PI/_melFilterStart.Count);
                    n++;
                }
                val /= _melFilterStart.Count;
                postDct.Add(val);
            }
            return postDct.ToArray();
        }

        public void GetCepstrogram(ref float[] inputAudio, WindowFunctions.WindowType windowType, int startPoint, int stopPoint, out double[][] mfccCepstrogram)
        {
            var mfccImageList = new List<double[]>();
            var window = new WindowFunctions();
            var furieTransform = new FurieTransform();
            var step = (int)Math.Round(_furieSize * (1.0 - _overlapping));
            for (int i = startPoint; i < inputAudio.Length - step && i < stopPoint && i < inputAudio.Length - 512; i += step)
            {
                var signalSpan = new float[_furieSize];

                Array.Copy(inputAudio, i, signalSpan, 0, _furieSize);

                signalSpan = window.PlaceWindow(signalSpan, windowType); //place window

                var complexSignal = Array.ConvertAll(signalSpan, input => (Complex)input);

                complexSignal = furieTransform.FastFurieTransform(complexSignal);

                var doubleSpectrum = Array.ConvertAll(complexSignal,
                    input => Math.Pow(input.Magnitude, 2)); //Go to Magnitude

                var mfcc = GetMelScaledCepstrum(doubleSpectrum); //find cepstrums
                mfccImageList.Add(mfcc);
            }
            mfccCepstrogram = mfccImageList.ToArray();
        }
    }
}
