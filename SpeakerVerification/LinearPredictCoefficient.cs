using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace SpeakerVerification
{
    class LinearPredictCoefficient
    {
        public WindowFunctions.WindowType UsedWindowType { private get; set; }
        public int UsedNumberOfCoeficients { private get; set; }
        public int UsedAcfWindowSize { private get; set; }
        public double UsedAcfWindowSizeTime { private get; set; }
        public double SamleFrequency { private get; set; }
        public int ImageLenght { private get; set; }

        public LinearPredictCoefficient()
        {
            UsedWindowType = WindowFunctions.WindowType.Hamming;
            UsedNumberOfCoeficients = 10;
            UsedAcfWindowSize = 992;
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
        public void GetLpcImage(ref float[] inputAudio, out double[][] lpcImage)
        {
            lpcImage = new double[ImageLenght][];
            var correl = new Corellation
                {
                    UsedWindowSize = UsedAcfWindowSize,
                    UsedVectorSize = UsedNumberOfCoeficients + 1,
                    UsedWindowType = UsedWindowType
                };
            for (int i = 0; i < lpcImage.Length; i++)
            {
                var inputAudioIndex = (int)Math.Round((i / (double)ImageLenght) * (inputAudio.Length - UsedAcfWindowSize));
                double[] lpc;
                double[] acf;
                correl.AutoCorrelationVectorDurbin(ref inputAudio, inputAudioIndex, out acf);
                DurbinAlgLpcCoefficients(ref acf, out lpc);
                lpcImage[i] = lpc;
            }
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
            using (StreamWriter writer = new StreamWriter("spectrum.txt"))
            {
                var culture = CultureInfo.CreateSpecificCulture("en-US");
                for (int i = 0; i < spectrogramm.Length; i++)
                {
                    for (int j = 0; j < spectrogramm[i].Length; j++)
                    {
                        writer.Write(spectrogramm[i][j].ToString(culture) + " ");
                    }
                    writer.WriteLine();
                }
            }
        }

        /// <summary>
        /// Clculate the formants traectories in aproximated spectrogramm
        /// </summary>
        /// <param name="lpc"></param>
        /// <param name="formantsMax"></param>
        /// <param name="formantsLenght"></param>
        /// <param name="formantsAmps"></param>
        /// <param name="formantsEnergy"></param>
        /// <param name="spectrSize"></param>
        /// <param name="sampleFrequency"></param>
        public void GetFormants(ref double[][] lpc, out double[][] formantsMax, out double[][] formantsLenght, out double[][] formantsAmps, out double[][] formantsEnergy, int spectrSize, double sampleFrequency)
        {
            formantsMax = new double[lpc.Length][];
            formantsLenght = new double[lpc.Length][];
            formantsAmps = new double[lpc.Length][];
            formantsEnergy = new double[lpc.Length][];

            for(int i = 0;i< lpc.Length; i++)
            {
                double[] spectrum;
                GetAproximatedSpectr(lpc[i], out spectrum, spectrSize);

                formantsMax[i] = new double[3];
                formantsLenght[i] = new double[3];
                formantsAmps[i] = new double[3];
                formantsEnergy[i] = new double[3];

                int currentFormant = 0;
                int formantStart = 0;

                for (int j = 1; j < spectrSize - 1; j++)
                {
                    if(spectrum[j-1] > spectrum[j] && spectrum[j+1] >= spectrum[j])
                    {//min
                        if (formantStart == 0)
                            formantStart = j;
                        else
                        {
                            formantsLenght[i][currentFormant] = ((j - formantStart) / (double)spectrSize) * sampleFrequency / 2;
                            formantsEnergy[i][currentFormant] /= formantsLenght[i][currentFormant];
                            formantStart = j;
                            currentFormant++;
                            if (currentFormant > 2)
                                break;
                        }
                    }
                    else if(spectrum[j-1] <= spectrum[j] && spectrum[j+1] < spectrum[j])
                    {//max
                        formantsAmps[i][currentFormant] = spectrum[j];
                        formantsMax[i][currentFormant] = (j / (double)spectrSize) * sampleFrequency / 2;
                    }
                    formantsEnergy[i][currentFormant] += spectrum[j];
                }
            }

            for(int i = 1; i < formantsMax.Length; i++)
            {
                for(int j = 0; j < formantsMax[i].Length; j++)
                {//if formans changed to fast, kick it
                    if(Math.Abs(formantsMax[i][j]-formantsMax[i-1][j]) > 200.0)
                    {
                        formantsMax[i][j] = formantsMax[i - 1][j];
                        formantsLenght[i][j] = formantsLenght[i - 1][j];
                        formantsEnergy[i][j] = formantsEnergy[i - 1][j];
                        formantsAmps[i][j] = formantsAmps[i - 1][j];
                    }
                    if (Math.Abs(formantsLenght[i][j] - formantsLenght[i - 1][j]) > 400.0)
                    {
                        //FormantsMax[i][j] = FormantsMax[i - 1][j];
                        formantsLenght[i][j] = formantsLenght[i - 1][j];
                        formantsEnergy[i][j] = formantsEnergy[i - 1][j];
                        //FormantsAmps[i][j] = FormantsAmps[i - 1][j];
                    }
                }
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
                Complex tmp = 0.0;
                double freq = Math.PI * ((i + 1.0) / spectrSize);
                for (int k = 0; k < lpcVector.Length; k++)
                {
                    Complex cm = Complex.Exp(-Complex.ImaginaryOne * freq * (k + 1.0));
                    tmp += lpcVector[k] * cm;
                }
                spectr[i] = Complex.Abs(1.0 / (1.0 - tmp));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="poles"></param>
        /// <param name="zeroes"></param>
        /// <param name="pc"></param>
        /// <param name="sizeOfPc"></param>
        private void GetPhaseCurve(ref double[] poles, ref double[] zeroes, out double[] pc, int sizeOfPc)
        {
            pc = new double[sizeOfPc];
            for (int f = 0; f < sizeOfPc; f++)
            {
                Complex freq = f * Math.PI / sizeOfPc;//freq = 0..pi

                Complex tmpSum1 = 0.0, tmpSum2 = 0.0, tmpSum3 = 0.0, tmpSum4 = 0.0;

                for (int i = 1; i < zeroes.Length; i++)
                {
                    tmpSum1 += zeroes[i] * Complex.Cos(Complex.ImaginaryOne * freq);
                }

                tmpSum1 += zeroes[0];

                for (int i = 1; i < zeroes.Length; i++)
                {
                    tmpSum2 += zeroes[i] * Complex.Sin(i * freq);
                }

                for (int i = 1; i < poles.Length; i++)
                {
                    tmpSum3 += poles[i] * Complex.Cos(i * freq);
                }
                tmpSum3 += 1;

                for (int i = 1; i < poles.Length; i++)
                {
                    tmpSum4 += poles[i] * Complex.Sin(i * freq);
                }

                pc[f] = (Complex.Atan(tmpSum4 / tmpSum3) - Complex.Atan(tmpSum2 / tmpSum1)).Magnitude;
            }
        }

        public void GetArcAndPcImages(ref double[][] lpc, out double[][] arcImage, out double[][] pcImage, int sizeOfImage)
        {
            arcImage = new double[lpc.Length][];
            pcImage = new double[lpc.Length][];
            for(int i = 0; i < lpc.Length; i++)
            {
                double[] t = {1.0};
                GetArc(lpc[i], out arcImage[i], sizeOfImage);
                GetPhaseCurve(ref lpc[i], ref t, out pcImage[i], sizeOfImage);
            }
        }

        public void GetArcImage(ref double[][] lpc, out double[][] arcImage, int sizeOfImage)
        {
            arcImage = new double[lpc.Length][];
            for(int i = 0; i < lpc.Length; i++)
            {
                GetArc(lpc[i], out arcImage[i], sizeOfImage);
            }
        }

        /// <summary>
        /// Вычисление поворачивающего модуля e^(-i*2*PI*k/N)
        /// </summary>
        /// <param name="k"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private Complex W(int k, int n)
        {
            if (k % n == 0) return 1.0;
            var arg = -2.0 * Math.PI * k / n;
            return new Complex(Math.Cos(arg), Math.Sin(arg));
        }

        /// <summary>
        /// Возвращает спектр сигнала
        /// </summary>
        /// <param name="x">Массив значений сигнала. Количество значений должно быть степенью 2</param>
        /// <returns>Массив со значениями спектра сигнала</returns>
        private Complex[] FastFurieTransform(Complex[] x)
        {
            Complex[] transform;
            int n = x.Length;
            if (n == 2)
            {
                transform = new Complex[2];
                transform[0] = x[0] + x[1];
                transform[1] = x[0] - x[1];
            }
            else
            {
                var xEven = new Complex[n / 2];
                var xOdd = new Complex[n / 2];
                for (int i = 0; i < n / 2; i++)
                {
                    xEven[i] = x[2 * i];
                    xOdd[i] = x[2 * i + 1];
                }
                var even = FastFurieTransform(xEven);
                var odd = FastFurieTransform(xOdd);
                transform = new Complex[n];
                for (int i = 0; i < n / 2; i++)
                {
                    transform[i] = even[i] + W(i, n) * odd[i];
                    transform[i + n / 2] = even[i] - W(i, n) * odd[i];
                }
            }
            return transform;
        }

        public double[][] GetArcVocalTractImage(ref float[] inputSignal, double analysisInterval, int sampleRate, int imageSize, int arcImageSize, ref double[][] arcImage, WindowFunctions.WindowType window)
        {
            var vocalTractImage = new double[imageSize][];
            var windowFunc = new WindowFunctions();
            var windowSize = (int)Math.Round(sampleRate*analysisInterval);
            var furieSize = (int)Math.Pow(2.0, Math.Round(Math.Log(analysisInterval*sampleRate, 2.0)));
            var place = 0;
            for (int i = 0; i < imageSize; i++)
            {
                var startIndex = (int)Math.Round(i*(inputSignal.Length - furieSize)/(double) imageSize);
                var tmp = new float[furieSize];
                Array.Copy(inputSignal, startIndex, tmp, 0, tmp.Length);
                tmp = windowFunc.PlaceWindow(tmp, window);
                var tmpSpectrum = Array.ConvertAll(tmp, x => (Complex) x);

                tmpSpectrum = FastFurieTransform(tmpSpectrum);

                var spectrum = SmoothAndWrapSpectrum(tmpSpectrum, arcImageSize);
                for (int j = 0; j < spectrum.Length; j++)
                    spectrum[j] /= arcImage[place][j];
                vocalTractImage[place] = spectrum;
                place++;
            }
            return vocalTractImage;
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

