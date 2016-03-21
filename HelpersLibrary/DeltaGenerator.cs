using System;
using System.Collections.Generic;

namespace HelpersLibrary
{
    public static class DeltaGenerator
    {
        public static double[][] AddDelta(double[][] data)
        {
            var delta = new List<double[]>(data.Length*2);
            var prev = data[0];
            foreach (var d in data)
            {
                var deltas = new double[prev.Length];
                for (int j = 0; j < d.Length; j++)
                {
                    deltas[j] = d[j] - prev[j];
                    prev = d;
                }
                Array.Resize(ref deltas, deltas.Length*2);
                Array.Copy(d, 0, deltas, d.Length, d.Length);
                delta.Add(deltas);
            }
            return delta.ToArray();
        }
    }
}
