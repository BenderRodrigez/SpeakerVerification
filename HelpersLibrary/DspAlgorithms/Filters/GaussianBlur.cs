﻿using System;

namespace HelpersLibrary.DspAlgorithms.Filters
{
    public class GaussianBlur
    {
        public double Sigma { get; private set; }

        private double[] BlurPoint(double[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (Math.Exp(-Math.Pow(i, 2)/(2.0*Math.Pow(Sigma, 2))))/(Math.Sqrt(2*Math.PI)*Sigma);
            }
            return data;
        }

        public double[] GetBlur(double[] data, int size)
        {
            Sigma = (size - 1.0) / 6;
            var delta = Math.Floor(size/2.0);
            var sum = 0.0;
            for (int j = (int)-delta; j <= delta; j++)
            {
                sum += Math.Exp(-Math.Pow(j, 2) / (2.0 * Math.Pow(Sigma, 2))) / (Math.Sqrt(2 * Math.PI) * Sigma);
            }

            for (int i = (int)delta; i < data.Length-delta; i++)
            {
                var res = 0.0;
                for (int j = (int)-delta; j <= delta; j++)
                {
                    res += data[i+j]* (Math.Exp(-Math.Pow(j, 2)/(2.0*Math.Pow(Sigma, 2)))/(Math.Sqrt(2*Math.PI)*Sigma))/sum;
                }
                data[i] = res;
            }
            return data;
        }
    }
}
