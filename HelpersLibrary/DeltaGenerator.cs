using System.Collections.Generic;

namespace HelpersLibrary
{
    public static class DeltaGenerator
    {
        public static double[][] AddDelta(double[][] data)//todo: it's wrong way. We have other spliting inside data :(
        {
            var delta = new List<double[]>(data.Length*2);
            foreach (var d in data)
            {
                delta.Add(d);
                var prevData = d[0];
                var deltas = new double[d.Length];
                for (int j = 0; j < d.Length; j++)
                {
                    deltas[j] = d[j] - prevData;
                    prevData = d[j];
                }
                delta.Add(deltas);
            }
            return delta.ToArray();
        }
    }
}
