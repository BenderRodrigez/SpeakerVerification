using System;
using System.Collections.Generic;
using System.Numerics;

namespace SpeakerVerification
{
    class Cepstrum
    {
        private readonly List<List<double>> _melFilterStart = new List<List<double>>();
        private readonly List<double> _filterCenters = new List<double>();
        private readonly List<int> _filterFrequencesIndex = new List<int>();
        private readonly int _filterNumber;
        private readonly int _furieSize;
        private readonly double _sampleRate;

        public Cepstrum(int filterNumber, double windowSize, double sampleRate)
        {
            var furiePower = (int)Math.Round(Math.Log(windowSize*sampleRate, 2.0));
            _filterNumber = filterNumber;
            _furieSize = (int)Math.Pow(2.0, furiePower);
            _sampleRate = sampleRate;

            MelFilterInit();
        }

        private void MelFilterInit()
        {
            var binToFreq = new Dictionary<int, double>();

            for (int f = 0; f < 1 + _furieSize / 2; f++)
            {
                binToFreq[f] = (f * _sampleRate) / (_furieSize);
            }

            var maxMel = 1125.0 * Math.Log(1.0 + (_sampleRate/2.0)/700.0);
	        var minMel = 1125.0 * Math.Log(1.0 + 300.0/700.0);
	        var dMel = (maxMel - minMel) / (_filterNumber+1);
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
            var postDct = new List<double>();// Initilise post-discrete cosine transformation vetor array / MFCC Coefficents

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

        public double[][] GetCepstrogram(ref float[] inputAudio, WindowFunctions.WindowType windowType, int imageSize)
        {
            var res = new double[imageSize][];
            var window = new WindowFunctions();

            for (int i = 0; i < imageSize; i++)
            {
                var inputAudioIndex = (int)Math.Round((i / (double)imageSize) * (inputAudio.Length - _furieSize));
                var signalSpan = new float[_furieSize];

                Array.Copy(inputAudio, inputAudioIndex, signalSpan, 0, _furieSize);

                signalSpan = window.PlaceWindow(signalSpan, windowType);//place window

                var complexSignal = Array.ConvertAll(signalSpan, input => (Complex)input);

                complexSignal = FastFurieTransform(complexSignal);

                var doubleSpectrum = Array.ConvertAll(complexSignal,
                    input => Math.Pow(input.Magnitude,2));//Go to Magnitude

                res[i] = GetMelScaledCepstrum(doubleSpectrum);//find cepstrums
            }
            return res;
        }


        /// <summary>
        /// Вычисление поворачивающего модуля e^(-i*2*PI*k/N)
        /// </summary>
        /// <param name="k"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public static Complex W(int k, int n)
        {
            if (k % n == 0) return 1;
            double arg = -2 * Math.PI * k / n;
            return new Complex(Math.Cos(arg), Math.Sin(arg));
        }

        /// <summary>
        /// Возвращает спектр сигнала
        /// </summary>
        /// <param name="x">Массив значений сигнала. Количество значений должно быть степенью 2</param>
        /// <returns>Массив со значениями спектра сигнала</returns>
        public static Complex[] FastFurieTransform(Complex[] x)
        {
            Complex[] X;
            int N = x.Length;
            if (N == 2)
            {
                X = new Complex[2];
                X[0] = x[0] + x[1];
                X[1] = x[0] - x[1];
            }
            else
            {
                Complex[] x_even = new Complex[N / 2];
                Complex[] x_odd = new Complex[N / 2];
                for (int i = 0; i < N / 2; i++)
                {
                    x_even[i] = x[2 * i];
                    x_odd[i] = x[2 * i + 1];
                }
                Complex[] X_even = FastFurieTransform(x_even);
                Complex[] X_odd = FastFurieTransform(x_odd);
                X = new Complex[N];
                for (int i = 0; i < N / 2; i++)
                {
                    X[i] = X_even[i] + W(i, N) * X_odd[i];
                    X[i + N / 2] = X_even[i] - W(i, N) * X_odd[i];
                }
            }
            return X;
        }
    }
}
