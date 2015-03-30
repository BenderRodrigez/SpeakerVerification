using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

namespace SpeakerVerification
{
    class LinearPredictCoefficient
    {
        public Corellation.WindowType UsedWindowType { get; set; }
        public byte UsedNumberOfCoeficients { get; set; }
        public int UsedAcfWindowSize { get; set; }
        public double UsedAcfWindowSizeTime { get; set; }
        public double SamleFrequency { get; set; }
        public int ImageLenght { get; set; }

        public LinearPredictCoefficient()
        {
            UsedWindowType = Corellation.WindowType.Hamming;
            UsedNumberOfCoeficients = 10;
            UsedAcfWindowSize = 992;
        }

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

        public void GetLpcImage(ref float[] inputAudio, out double[][] lpcImage)
        {
            lpcImage = new double[ImageLenght][];
            for (int i = 0; i < lpcImage.Length; i++)
            {
                var inputAudioIndex = (int)Math.Round((i / (double)ImageLenght) * (inputAudio.Length - UsedAcfWindowSize));
                double[] lpc;
                double[] acf;
                Corellation.AutoCorrelationVectorDurbin(ref inputAudio, UsedAcfWindowSize, UsedNumberOfCoeficients + 1, inputAudioIndex,
                    out acf, UsedWindowType);
                DurbinAlgLpcCoefficients(ref acf, out lpc);
                lpcImage[i] = lpc;
            }
        }

        public void GetAproximatedSpectr(double[] lpcVector, out double[] spectr, int spectrSize)
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

        private double GetImpulseCharacteristic(ref double[] lpc, ref double[] impulse, int sampleNumber, ref double[] result, int k = 0)
        {
            if (sampleNumber > -1)
            {
                if (k == 0)
                {
                    double tmp = 0;
                    for (int i = 0; i < lpc.Length; i++)
                    {
                        tmp += lpc[i] * GetImpulseCharacteristic(ref lpc, ref impulse, sampleNumber - i, ref result, i) + impulse[sampleNumber];//Sadness, too much computing
                    }
                    return tmp;
                }
                return result[sampleNumber];
            }
            return 0;

        }

        public double[] GetImpulseCharacteristicImage(ref double[][] lpc, ref double[] impulse)
        {
            double[] res = new double[lpc.Length];
            for(int i = 0; i < lpc.Length; i++)
            {
                res[i] = GetImpulseCharacteristic(ref lpc[i], ref impulse, i, ref res);
            }
            return res;
        }
    }
}

