using System;
using System.Collections.Generic;
using System.Numerics;

namespace HelpersLibrary.DspAlgorithms
{
    public class LinearPredictCoefficient
    {
        public WindowFunctions.WindowType UsedWindowType { private get; set; }
        public int UsedNumberOfCoeficients { private get; set; }
        private int _usedAcfWindowSize;

        private double _usedAcfWindowSizeTime;
        public double UsedAcfWindowSizeTime
        {
            private get { return _usedAcfWindowSizeTime; }
            set
            {
                _usedAcfWindowSizeTime = value;
                if (SamleFrequency > 0)
                    _usedAcfWindowSize = (int)Math.Round(SamleFrequency * _usedAcfWindowSizeTime);
            }
        }

        public double SamleFrequency { private get; set; }
        public double Overlapping { get; set; }

        public LinearPredictCoefficient()
        {
            UsedWindowType = WindowFunctions.WindowType.Hamming;
            UsedNumberOfCoeficients = 10;
            _usedAcfWindowSize = 992;
        }


        /// <summary>
        /// Calcs LPC coefficients by Durbin algorythm
        /// </summary>
        /// <param name="acfVector">Auto-correlation vector from 0 to N</param>
        /// <param name="lpcCoefficients">Output LPC values vector</param>
        private void DurbinAlgLpcCoefficients(ref double[] acfVector, out double[] lpcCoefficients)
        {
            var tmp = new double[UsedNumberOfCoeficients];
            lpcCoefficients = new double[UsedNumberOfCoeficients];

            var e = acfVector[0];

            for (int i = 0; i < UsedNumberOfCoeficients; i++)
            {
                var tmp0 = acfVector[i+1];
                for (int j = 0; j < i; j++)
                    tmp0 -= lpcCoefficients[j] * acfVector[i - j];

                if (Math.Abs(tmp0) >= e) break;

                double pk;
                lpcCoefficients[i] = pk = tmp0/e;
                e -= tmp0 * pk;

                for (int j = 0; j < i; j++)
                    tmp[j] = lpcCoefficients[j];

                for (int j = 0; j < i; j++)
                    lpcCoefficients[j] -= pk * tmp[i - j - 1];
            }
        }

        /// <summary>
        /// Gets an Image of LPC of all signal
        /// </summary>
        /// <param name="inputAudio">input audio</param>
        /// <param name="lpcImage">matrix of lpc values</param>
        /// <param name="startPoint"></param>
        /// <param name="stopPoint"></param>
        public void GetLpcImage(ref float[] inputAudio, out double[][] lpcImage, int startPoint, int stopPoint)
        {
            var lpcImageList = new List<double[]>();
            var correl = new Corellation
                {
                    UsedWindowSize = _usedAcfWindowSize,
                    UsedVectorSize = UsedNumberOfCoeficients + 1,
                    UsedWindowType = UsedWindowType
                };
            var step = (int)Math.Round(_usedAcfWindowSize*(1.0 - Overlapping));
            for (int i = startPoint; i < inputAudio.Length - step && i < stopPoint; i += step)
            {
                double[] lpc;
                double[] acf;
                correl.AutoCorrelationVectorDurbin(ref inputAudio, i, out acf);
                DurbinAlgLpcCoefficients(ref acf, out lpc);
                lpcImageList.Add(lpc);
            }
            lpcImage = lpcImageList.ToArray();
        }

        /// <summary>
        /// Provide an reconstruted by LPC spectrum of signal
        /// </summary>
        /// <param name="lpcVector">LPC vector</param>
        /// <param name="spectr">output values of spectrum</param>
        /// <param name="spectrSize">Number of values in spectrum</param>
        private void GetAproximatedSpectr(double[] lpcVector, out double[] spectr, int spectrSize)
        {
            spectr = new double[spectrSize];
            for (var i = 0; i < spectrSize; i++)
            {
                var tmp = Complex.Zero;
                var freq = Math.PI * ((i + 1.0) / spectrSize);
                for (var k = 0; k < lpcVector.Length; k++)
                {
                    var cm = Complex.Exp(-Complex.ImaginaryOne * freq * (k + 1.0));
                    tmp += lpcVector[k] * cm;
                }
                spectr[i] = (1.0 / (1.0 - tmp)).Magnitude;
            }
        }

        /// <summary>
        /// Gets aproximation of spectrogram of audio through LPC
        /// </summary>
        /// <param name="lpc">Matrix of LPC values</param>
        /// <param name="spectrogramm">Output values</param>
        /// <param name="spectrSize">Number of values in spectrum vector</param>
        public void GetAproximatedSpectrogramm(ref double[][] lpc, out double[][] spectrogramm, int spectrSize)
        {
            spectrogramm = new double[lpc.Length][];
            for (int i = 0; i < lpc.Length; i++)
            {
                GetAproximatedSpectr(lpc[i], out spectrogramm[i], spectrSize);
            }
        }

        /// <summary>
        /// Amplitude-response curve aproximation by LPC
        /// </summary>
        /// <param name="lpcVector"></param>
        /// <param name="spectr"></param>
        /// <param name="spectrSize"></param>
        private void GetArc(double[] lpcVector, out double[] spectr, int spectrSize)//Amplitude-response curve
        {
            spectr = new double[spectrSize];
            for (int i = 0; i < spectrSize; i++)
            {
                var tmp = Complex.Zero;
                var freq = Math.PI * ((i + 1.0) / spectrSize);
                for (int k = 0; k < lpcVector.Length; k++)
                {
                    var cm = Complex.Exp(-Complex.ImaginaryOne * freq * (k + 1.0));
                    tmp += lpcVector[k] * cm;
                }
                spectr[i] = Complex.Abs(1.0 / (1.0 - tmp));
            }
        }

        public void GetArcImage(ref float[] sound, out double[][] arcImage, int startPosition, int endPosition, int vectorSize)
        {
            var arcImageList = new List<double[]>();
            var correl = new Corellation
            {
                UsedWindowSize = _usedAcfWindowSize,
                UsedVectorSize = UsedNumberOfCoeficients + 1,
                UsedWindowType = UsedWindowType
            };
            var step = (int)Math.Round(_usedAcfWindowSize * (1.0 - Overlapping));
            for (int i = startPosition; i < sound.Length - step && i < endPosition; i += step)
            {
                double[] lpc;
                double[] acf;
                correl.AutoCorrelationVectorDurbin(ref sound, i, out acf);
                DurbinAlgLpcCoefficients(ref acf, out lpc);
                double[] arc;
                GetArc(lpc, out arc, vectorSize);
                arcImageList.Add(arc);
            }
            arcImage = arcImageList.ToArray();
        }

        public void GetArcVocalTractImage(ref float[] inputSignal, int sampleRate, int vectorSize, out double[][] vtcImage, int startPosition, int endPosition)
        {
            var vocalTractImageList = new List<double[]>();
            var furieSize = (int)Math.Pow(2.0, Math.Round(Math.Log(_usedAcfWindowSizeTime * sampleRate, 2.0)));
            var correl = new Corellation
            {
                UsedWindowSize = _usedAcfWindowSize,
                UsedVectorSize = UsedNumberOfCoeficients + 1,
                UsedWindowType = UsedWindowType
            };
            var furieTransform = new FurieTransform();
            var step = (int)Math.Round(_usedAcfWindowSize * (1.0 - Overlapping));
            var windowFunc = new WindowFunctions();
            for (int i = startPosition; i < inputSignal.Length - step && i < endPosition && i < inputSignal.Length - furieSize; i += step)
            {
                double[] lpc;
                double[] acf;
                correl.AutoCorrelationVectorDurbin(ref inputSignal, i, out acf);
                DurbinAlgLpcCoefficients(ref acf, out lpc);
                double[] arc;
                GetArc(lpc, out arc, vectorSize);
                var tmp = new float[furieSize];
                Array.Copy(inputSignal, i, tmp, 0, tmp.Length);
                tmp = windowFunc.PlaceWindow(tmp, UsedWindowType);
                var tmpSpectrum = Array.ConvertAll(tmp, x => (Complex)x);

                tmpSpectrum = FurieTransform.FastFurieTransform(tmpSpectrum);

                var spectrum = SmoothAndWrapSpectrum(tmpSpectrum, vectorSize);
                for (int j = 0; j < spectrum.Length; j++)
                    spectrum[j] /= arc[j];

                vocalTractImageList.Add(spectrum);
            }
            vtcImage = vocalTractImageList.ToArray();
        }

        private double[] SmoothAndWrapSpectrum(Complex[] spectrum, int arcLenght)
        {
            var halfSpectrum = new Complex[spectrum.Length/2];
            Array.Copy(spectrum, 0, halfSpectrum, 0, spectrum.Length/2);
            var magnitudeSpectrum = Array.ConvertAll(halfSpectrum, input => input.Magnitude);

            var wrapedSpectrum = new List<double>();
            var wrappingCefficient = (int)Math.Round(((magnitudeSpectrum.Length/(double) arcLenght)*2.0 + 1.0)/2.0);
            var smoothPower = (int)Math.Floor(wrappingCefficient/2.0);
            for (int i = smoothPower; i < magnitudeSpectrum.Length - smoothPower; i+=wrappingCefficient)
            {
                var tmp = 0.0;
                for (int j = i - smoothPower; j < i + smoothPower; j++)
                    tmp += magnitudeSpectrum[j];
                wrapedSpectrum.Add(tmp/wrappingCefficient);
            }
            return wrapedSpectrum.ToArray();
        }
    }
}

