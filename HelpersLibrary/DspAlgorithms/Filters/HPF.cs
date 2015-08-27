using System;

namespace HelpersLibrary.DspAlgorithms.Filters
{
    public class Hpf
    {
        private readonly int _sampleRate;

        private readonly int _elementsNumber;
        private double[] _a;
        private double[] _b;
        private double[] _c;
        private double[] _w0;
        private double[] _w1;
        private double[] _w2;


        public Hpf(float cutFrequency, int sampleRate, int elementsNumber = 4)
        {
            _sampleRate = sampleRate;
            _elementsNumber = elementsNumber;
            InitFilter(cutFrequency);
        }

        private void InitFilter(float cutFreq)
        {
            _a = new double[_elementsNumber];
            _b = new double[_elementsNumber];
            _c = new double[_elementsNumber];
            var d = 2.0*Math.Sin(Math.PI*cutFreq*(1.0/_sampleRate))/Math.Cos(Math.PI*cutFreq*(1.0/_sampleRate));
            for (int i = 0; i < _elementsNumber; i++)
            {
                var cos = Math.Cos(Math.PI*(0.5 + (2*(i + 1) - 1)/(4.0*_elementsNumber)));
                _a[i] = d*d + 4.0*d*cos + 4.0;
                _b[i] = -8.0 + 2.0*d*d;
                _c[i] = d*d - 4.0*d*cos + 4.0;
            }
        }

        private void FilterElementPass(int k, ref float x, ref float y)
        {
            _w0[k] = (1.0*x - _a[k]*_w2[k] - _b[k]*_w1[k])/_c[k];
            y = (float)(4.0f*(_w0[k] + _w2[k] - 2.0f*_w1[k]));
            _w2[k] = _w1[k];
            _w1[k] = _w0[k];
        }

        public float[] Filter(float[] signal)
        {
            _w0 = new double[_elementsNumber];
            _w1 = new double[_elementsNumber];
            _w2 = new double[_elementsNumber];
            var resSignal = new float[signal.Length];
            for (int i = 0; i < signal.Length; i++)
            {
                var x = signal[i];
                var y = 0.0f;
                var y1 = 0.0f;
                var y2 = 0.0f;
                var y3 = 0.0f;
                FilterElementPass(0, ref x, ref y);
                FilterElementPass(0, ref y, ref y1);
                FilterElementPass(0, ref y1, ref y2);
                FilterElementPass(0, ref y2, ref y3);
                resSignal[i] = y3;
            }
            return resSignal;
        }
    }
}
