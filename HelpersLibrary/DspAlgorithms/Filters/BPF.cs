using System;

namespace HelpersLibrary.DspAlgorithms.Filters
{
    //bug: this implementation not working well.
    /// <summary>
    /// Полосовой фильтр
    /// </summary>
    public class Bpf
    {
        private readonly int _elementsNumber;
        private double[] _a;
        private double[] _b;
        private double[] _c;
        private double[] _d;
        private double[] _e;
        private double[] _w0;
        private double[] _w1;
        private double[] _w2;
        private double[] _w3;
        private double[] _w4;
        private double _a1;


        public Bpf(float cutFrequencyLow, float cutFrequencyHigh, int sampleRate, int elementsNumber = 4)
        {
            _elementsNumber = elementsNumber;
            var fn = 2.0*(Math.Sin(Math.PI*cutFrequencyLow/sampleRate))/Math.Cos(Math.PI*cutFrequencyLow/sampleRate);
            var fv = 2.0*(Math.Sin(Math.PI*cutFrequencyHigh/sampleRate))/Math.Cos(Math.PI*cutFrequencyHigh/sampleRate);
            InitFilter(fn, fv);
        }

        private void InitFilter(double cutFreqLow, double cutFreqHigh)
        {
            _a = new double[_elementsNumber];
            _b = new double[_elementsNumber];
            _c = new double[_elementsNumber];
            _d = new double[_elementsNumber];
            _e = new double[_elementsNumber];
            _a1 = Math.Pow(cutFreqHigh - cutFreqLow, 2.0);
            var c1 = 2.0 * cutFreqHigh * cutFreqLow + Math.Pow(cutFreqHigh - cutFreqLow, 2.0);
            var e1 = Math.Pow(cutFreqHigh * cutFreqLow, 2.0);

            for (int i = 0; i < _elementsNumber; i++)
            {
                var cos = Math.Cos(Math.PI*(0.5 + (2.0*(i+1.0) - 1.0)/(4.0*(_elementsNumber+1.0))));
                var b1 = -2.0*cos;
                var d1 = -2.0*cutFreqHigh*cutFreqLow*(cutFreqHigh - cutFreqLow)*cos;

                _a[i] = 16.0 - 8.0*b1 + 4.0*c1 - 2.0*d1 + e1;
                _b[i] = -64.0 + 16.0*b1 - 4.0*d1 + 4.0*e1;
                _c[i] = 96.0 - 8.0*c1 + 6.0*e1;
                _d[i] = -64.0 - 16.0*b1 + 4.0*d1 + 4.0*e1;
                _e[i] = 16.0 + 8.0*b1 + 4.0*c1 + 2.0*d1 + e1;
            }
        }

        private void FilterElementPass(int k, ref float x, out float y)
        {
            _w0[k] = (x - _a[k]*_w4[k] - _b[k]*_w3[k] - _c[k]*_w2[k] - _d[k]*_w1[k])/_e[k];
            y = (float) ((_w0[k] - (2.0*_w2[k]) + _w4[k])*4.0*_a1);
            _w4[k] = _w3[k];
            _w3[k] = _w2[k];
            _w2[k] = _w1[k];
            _w1[k] = _w0[k];
        }

        public float[] Filter(float[] signal)
        {
            _w0 = new double[_elementsNumber];
            _w1 = new double[_elementsNumber];
            _w2 = new double[_elementsNumber];
            _w3 = new double[_elementsNumber];
            _w4 = new double[_elementsNumber];
            var resSignal = new float[signal.Length];
            for (int i = 0; i < signal.Length; i++)
            {
                var x = signal[i];
                float y;
                float y1;
                float y2;
                float y3;
                FilterElementPass(0, ref x, out y);
                FilterElementPass(1, ref y, out y1);
                FilterElementPass(2, ref y1, out y2);
                FilterElementPass(3, ref y2, out y3);
                resSignal[i] = y3;
            }
            return resSignal;
        }
    }
}
