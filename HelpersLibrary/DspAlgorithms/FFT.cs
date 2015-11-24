using System;
using System.Linq;

namespace HelpersLibrary.DspAlgorithms
{
    // ReSharper disable once InconsistentNaming
    public class FFT
    {
        /// <summary>
        /// This computes an in-place complex-to-complex FFT 
        /// x and y are the real and imaginary arrays of 2^m points.
        /// </summary>
        public static void FastFurieTransform(bool forward, int m, ComplexNumber[] data)
        {
            int n, i, i1, j, k, i2, l, l1, l2;
            float c1, c2, tx, ty, t1, t2, u1, u2, z;

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

        /// <summary>
        /// Вычисляет АКФ и АКФС сигнала с помощью БПФ
        /// </summary>
        /// <param name="size">Размер окна</param>
        /// <param name="data">Входной сигнал</param>
        /// <param name="acf">АКФ входного сигнала</param>
        /// <param name="acfs">АКФС входного сигнала</param>
        public static void AutocorrelationAndSpectrumAutocorrelation(int size, float[] data, out double[] acf, out double[] acfs)
        {
            var nearestSize = (int)Math.Ceiling(Math.Log(size, 2));
            var complexData = new ComplexNumber[(int)Math.Pow(2, nearestSize)];
            for (int i = 0; i < size; i++)
            {
                complexData[i] = new ComplexNumber(data[i]);
            }

            var complexAcf = new ComplexNumber[(int)Math.Pow(2, nearestSize)];
            Array.Copy(complexData, complexAcf, (int)Math.Pow(2, nearestSize));

            FastFurieTransform(true, nearestSize, complexData);

            AutoCorrelation(ref complexData);
            AutoCorrelation(ref complexAcf);
            acfs = new double[(int)Math.Pow(2, nearestSize - 1)];
            Array.Copy(complexData.Select(x => Math.Sqrt(x.Sqr)).ToArray(), acfs, (int)Math.Pow(2, nearestSize - 1));
            acf = new double[size];
            Array.Copy(complexAcf.Select(x => Math.Sqrt(x.Sqr)).ToArray(), acf, size);
        }

        public static void AutoCorrelation(int size, float[] data, out double[] result)
        {
            var complexData = Array.ConvertAll(data, input => new ComplexNumber(input));
            AutoCorrelation(ref complexData);
            result = Array.ConvertAll(complexData, input => Math.Sqrt(input.Sqr));
        }

        private static void AutoCorrelation(ref ComplexNumber[] data)
        {
            var nearestSize = Math.Ceiling(Math.Log(data.Length, 2));
            //bug if we try extract an average from data, it becomes corrupted
            /*var avgX = data.Average(x=> x.RealPart);
            var avgY = data.Average(x => x.ImaginaryPart);
            data = data.Select(x => new ComplexNumber(x.RealPart - avgX, x.ImaginaryPart - avgY)).ToArray();*/
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