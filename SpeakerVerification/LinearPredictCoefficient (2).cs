using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace SpeakerVerification
{
    static class LinearPredictCoefficient
    {
        private static int TotalLenght = 1024;
        public static double[] CalcCoefficentsPerSample(ref float[] InputAudio, int ACFWindowSize, int Offset, byte NumberOfCoefficients, int useWindow)
        {
            double[][] Matrix;
            Comparer.AutoCorrelationSquareMatrix(ref InputAudio, ACFWindowSize, NumberOfCoefficients, Offset, out Matrix, useWindow);
            double[] Vector;
            Comparer.AutoCorrelationVector(ref InputAudio, ACFWindowSize, NumberOfCoefficients, Offset, out Vector, useWindow);
            GaussianEliminationSLE SLE = new GaussianEliminationSLE(NumberOfCoefficients);
            double[] X;
            SLE.SolutionFind(Matrix, Vector, out X);
            return X;
        }

        private static double[] HammingWindow(double[] x, int N)
        {
            double[] X = new double[x.LongLength];
            for (long i = 0; i < x.LongLength; i++)
            {
                X[i] = x[i] * (0.54 - 0.46 * Math.Cos(2 * Math.PI * (double)i / (double)N));
            }
            return X;
        }

        private static Complex[] HammingWindowComplex(double[] x, int N)
        {
            Complex[] X = new Complex[x.LongLength];
            for (long i = 0; i < x.LongLength; i++)
            {
                X[i] = x[i] * (0.54 - 0.46 * Math.Cos(2 * Math.PI * (double)i / (double)N));
            }
            return X;
        }

        public static void CalcLPCFunction(ref float[] InputAudio, int SampleFrequency, double ACFWindowSize,
            out double[][] LPCImage, byte NumberOfCefficients, int TotalLenght)
        {
            int windowSize = (int)Math.Round(ACFWindowSize * SampleFrequency);//участок, по которому строим в секундах
//            LPCImage = new double[InputAudio.Length - windowSize][];
            LPCImage = new double[TotalLenght][];
            for (int i = 0; i < LPCImage.Length; i++)
            {
                int inputAudioIndex = (int)Math.Round((i / (double)TotalLenght) * InputAudio.Length);
                LPCImage[i]  = CalcCoefficentsPerSample(ref InputAudio, windowSize, i, NumberOfCefficients, 1);
            }
            //Normalize(ref LPCImage);
        }

        private static void Normalize(ref double[][] LPC)
        {
            double max = LPC
                .Max(x => x
                    .Where(y => y > 0)
                    .Max());
            double min = LPC
                .Min(x => x
                    .Where(y => y < 0)
                    .Min());
            for(int i = 0; i < LPC.Length; i++)
            {
                for(int j = 0; j < LPC[i].Length; j++)
                {
                    if (LPC[i][j] > 0)
                    {
                        LPC[i][j] /= max;
                    }
                    else
                        LPC[i][j] /= Math.Abs(min);
                }
            }
        }
    }
}

