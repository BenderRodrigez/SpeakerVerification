using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeakerVerification
{
    class WindowFunctions
    {
        public enum WindowType { Rectangular, Hamming, Blackman };

        public float[] PlaceWindow(float[] inputs, WindowType windowType)
        {
            switch (windowType)
            {
                case WindowType.Blackman:
                    return BlackmanWindow(inputs, inputs.Length, 0.16);
                case WindowType.Hamming:
                    return HammingWindow(inputs, inputs.Length);
                case WindowType.Rectangular:
                    return inputs;
            }
            return inputs;
        }

        /// <summary>
        /// Наложение окна Хемминга на входной сигнал
        /// </summary>
        /// <param name="input">Сигнал</param>
        /// <param name="n">Размерность БПФ</param>
        /// <returns></returns>
        private float[] HammingWindow(float[] input, int n)
        {
            var x = new float[input.Length];
            for (int i = 0; i < input.Length / 2; i++)
            {
                x[i] = (float)(input[i] * (0.54 - 0.46 * Math.Cos(2.0 * Math.PI * i / n)));
            }
            return x;
        }

        private float[] BlackmanWindow(float[] input, int n, double a)
        {
            var x = new float[input.Length];
            var a0 = (1.0 - a) / 2.0;
            const double a1 = 0.5;
            var a2 = a / 2.0;
            for (var i = 0; i < x.Length; i++)
            {
                x[i] = input[i] * (float)(a0 - a1 * Math.Cos(2.0 * Math.PI * i / n) + a2 * Math.Cos(4.0 * Math.PI * i / n));
            }
            return x;
        }

        private float[] RectangularWindow(float[] input)
        {
            return input;
        }
    }
}
