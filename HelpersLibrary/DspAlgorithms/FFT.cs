using System;
using System.Linq;
using HelpersLibrary.DspAlgorithms.Filters;

namespace HelpersLibrary.DspAlgorithms
{
    // ReSharper disable once InconsistentNaming
    public static class FFT
    {
        /// <summary>
        /// This computes an in-place complex-to-complex FFT 
        /// x and y are the real and imaginary arrays of 2^m points.
        /// </summary>
        public static void FastFurieTransform(bool forward, int m, ComplexNumber[] data)
        {
            int n, i, i1, j, k, i2, l, l1, l2;
            double c1, c2, tx, ty, t1, t2, u1, u2, z;

            // Calculate the number of points
            n = 1;
            for (i = 0; i < m; i++)
                n *= 2;

            // Do the bit reversal
            i2 = n >> 1;
            j = 0;
            for (i = 0; i < n - 1; i++)
            {
                if (i < j)
                {
                    tx = data[i].RealPart;
                    ty = data[i].ImaginaryPart;
                    data[i].RealPart = data[j].RealPart;
                    data[i].ImaginaryPart = data[j].ImaginaryPart;
                    data[j].RealPart = tx;
                    data[j].ImaginaryPart = ty;
                }
                k = i2;

                while (k <= j)
                {
                    j -= k;
                    k >>= 1;
                }
                j += k;
            }

            // Compute the FFT 
            c1 = -1.0f;
            c2 = 0.0f;
            l2 = 1;
            for (l = 0; l < m; l++)
            {
                l1 = l2;
                l2 <<= 1;
                u1 = 1.0f;
                u2 = 0.0f;
                for (j = 0; j < l1; j++)
                {
                    for (i = j; i < n; i += l2)
                    {
                        i1 = i + l1;
                        t1 = u1 * data[i1].RealPart - u2 * data[i1].ImaginaryPart;
                        t2 = u1 * data[i1].ImaginaryPart + u2 * data[i1].RealPart;
                        data[i1].RealPart = data[i].RealPart - t1;
                        data[i1].ImaginaryPart = data[i].ImaginaryPart - t2;
                        data[i].RealPart += t1;
                        data[i].ImaginaryPart += t2;
                    }
                    z = u1 * c1 - u2 * c2;
                    u2 = u1 * c2 + u2 * c1;
                    u1 = z;
                }
                c2 = (float)Math.Sqrt((1.0f - c1) / 2.0f);
                if (forward)
                    c2 = -c2;
                c1 = (float)Math.Sqrt((1.0f + c1) / 2.0f);
            }

            // Scaling for forward transform 
            if (forward)
            {
                for (i = 0; i < n; i++)
                {
                    data[i].RealPart /= n;
                    data[i].ImaginaryPart /= n;
                }
            }
        }

        public static void SpectrumForAcfs(int size, float[] data, out double[] result)
        {
            var nearestSize = (int)Math.Ceiling(Math.Log(size, 2) + 1);
            var complexData = new ComplexNumber[(int)Math.Pow(2, nearestSize)];
            for (int i = 0; i < size; i++)
            {
                complexData[i] = new ComplexNumber(data[i]);
            }
            FastFurieTransform(true, nearestSize, complexData);
            Array.Resize(ref complexData, complexData.Length / 8);
            var logSpectrum = complexData.Select(x => Math.Sqrt(x.Sqr)).ToArray();
            var avg = logSpectrum.Average();
            complexData = logSpectrum.Select(x => new ComplexNumber(x - avg)).ToArray();
            result = Array.ConvertAll(complexData, input => Math.Sqrt(input.Sqr));
        }
        
        public static void AutoCorrelation(int size, float[] data, out double[] result)
        {
            var complexData = Array.ConvertAll(data, input => new ComplexNumber(input));
            AutoCorrelation(ref complexData);
            result = new double[size];
            Array.Copy(complexData.Select(x => x.RealPart).ToArray(), result, size);
            var k = result[0];
            result = result.Select(x => x/k).ToArray();
        }

        public static void SpectrumAutoCorrelation(int size, float[] data, out double[] result)
        {
            var nearestSize = (int)Math.Ceiling(Math.Log(size, 2)+1);
            var complexData = new ComplexNumber[(int)Math.Pow(2, nearestSize)];
            for (int i = 0; i < size; i++)
            {
                complexData[i] = new ComplexNumber(data[i]);
            }

            FastFurieTransform(true, nearestSize, complexData);

            Array.Resize(ref complexData, complexData.Length / 8);
            
            var logSpectrum = complexData.Select(x => Math.Sqrt(x.Sqr)).ToArray();

//            for (int i = 1; i < logSpectrum.Length - 1; i++)
//            {
//                var tmp = new[] { logSpectrum[i - 1], logSpectrum[i], logSpectrum[i + 1] };
//                Array.Sort(tmp);
//                logSpectrum[i] = tmp[1];
//            }

            const int blurSize = 9;
//            for (int repeatings = 0; repeatings < 3; repeatings++)
//            {
//                for (int i = blurSize; i < logSpectrum.Length - blurSize; i++)
//                {
//                    var sum = 0.0;
//                    for (int j = 1; j < blurSize; j++)
//                    {
//                        sum += logSpectrum[i - j] + logSpectrum[i + j];
//                    }
//                    logSpectrum[i] = (sum + logSpectrum[i])/(blurSize*2 + 1);
//                }
//            }

            var blur = new GaussianBlur();
            logSpectrum = blur.GetBlur(logSpectrum, blurSize);

            var max = double.NegativeInfinity;
            for (int i = 1; i < logSpectrum.Length-1; i++)
            {
                if (logSpectrum[i] > logSpectrum[i - 1] && logSpectrum[i] > logSpectrum[i + 1] && logSpectrum[i] > max)
                    max = logSpectrum[i];
            }

            if (double.IsNegativeInfinity(max))
            {
                result = new double[logSpectrum.Length];
                return;
            }
            else
            {
                logSpectrum = logSpectrum.Select(x => Math.Abs(x) > max * 0.5 ? x : 0.0).ToArray();
            }

            var avg = logSpectrum.Average();
            complexData = logSpectrum.Select(x => new ComplexNumber(x-avg)).ToArray();

            AutoCorrelation(ref complexData);
            result = complexData.Select(x => x.RealPart).ToArray();
            var k = result[0];
            result = result.Select(x => x / k).ToArray();
        }

        private static void AutoCorrelation(ref ComplexNumber[] data)
        {
            var nearestSize = Math.Ceiling(Math.Log(data.Length, 2));
            var avgX = data.Average(x=> x.RealPart);
            var avgY = data.Average(x => x.ImaginaryPart);
            data = data.Select(x => new ComplexNumber(x.RealPart - avgX, x.ImaginaryPart - avgY)).ToArray();
            var newSize = (int)nearestSize + 1;
            var doubleSized = new ComplexNumber[(int) Math.Pow(2, newSize)];
            Array.Copy(data, doubleSized, data.Length);

            FastFurieTransform(true, newSize, doubleSized);
            doubleSized = doubleSized.Select(x => new ComplexNumber(x.Sqr)).ToArray();
            FastFurieTransform(false, newSize, doubleSized);
            Array.Copy(doubleSized, data, data.Length);
        }
    }
}