using System;

namespace HelpersLibrary.DspAlgorithms.Filters
{
    public class Lpf
    {
        /// <summary>
        /// Количество звеньев 2-го порядка
        /// </summary>
        private const byte NumberOfLinks = 4;

        /// <summary>
        /// Частота опроса сигнала в Гц
        /// </summary>
        private readonly int _samplesFrequency = 8000;

        /// <summary>
        /// Граница среза
        /// </summary>
        private float _frequencyBorder;

        /// <summary>
        /// Коэффициенты фильтра
        /// </summary>
        private float[] a, b, c;
        private float d;

        /// <summary>
        /// Массивы промежуточных переменных
        /// </summary>
        private float[] w0, w1, w2;

        /// <summary>
        /// Входная переменная фильтра
        /// </summary>
        private float X;

        /// <summary>
        /// Выходные переменные фильтра
        /// </summary>
        private float y1;

        /// <summary>
        /// Выходные переменные фильтра
        /// </summary>
        private float y2;

        /// <summary>
        /// Выходные переменные фильтра
        /// </summary>
        private float y3;

        /// <summary>
        /// Выходные переменные фильтра
        /// </summary>
        private float y4;

        /// <summary>
        /// Нормирующий коэффициент
        /// </summary>
        private const float Normalization = 1.0f;

        /// <summary>
        /// Инициализируем фильтр высоких частот, с частотой среза 70Гц по умолчанию
        /// </summary>
        public Lpf()
        {
            d = 0;
            a = new float[NumberOfLinks];
            b = new float[NumberOfLinks];
            c = new float[NumberOfLinks];

            w0 = new float[NumberOfLinks];
            w1 = new float[NumberOfLinks];
            w2 = new float[NumberOfLinks];
            for (int i = 0; i < NumberOfLinks; i++)
            {
                w0[i] = 0;
                w1[i] = 0;
                w2[i] = 0;
            }

            _frequencyBorder = 300;

            SetParapmeters();
        }

        /// <summary>
        /// Инициализируем фильтр высоких частот с известной частотой среза
        /// </summary>
        /// <param name="frequencyBorder">Частота среза для фильтра</param>
        /// <param name="samplesFrequency"></param>
        public Lpf(float frequencyBorder, int samplesFrequency)
        {
            d = 0;
            a = new float[NumberOfLinks];
            b = new float[NumberOfLinks];
            c = new float[NumberOfLinks];

            w0 = new float[NumberOfLinks];
            w1 = new float[NumberOfLinks];
            w2 = new float[NumberOfLinks];
            for (int i = 0; i < NumberOfLinks; i++)
            {
                w0[i] = 0;
                w1[i] = 0;
                w2[i] = 0;
            }

           _frequencyBorder = frequencyBorder;
           _samplesFrequency = samplesFrequency;

           SetParapmeters();
        }

        /// <summary>
        /// Расчёт параметров фильтра
        /// </summary>
        private void SetParapmeters()
        {
            var tangens = (float)(2.0 * Math.Sin(Math.PI * _frequencyBorder * (1.0 / (double)_samplesFrequency)) / Math.Cos(Math.PI * _frequencyBorder * (1.0 / (double)_samplesFrequency)));
            d = (float)Math.Pow(tangens,2);
            for (int i = 0; i < NumberOfLinks; i++)
            {
                double sinus = tangens * Math.Sin(Math.PI * (0.5 + (2.0 * (double)(i + 1) - 1.0) / (2.0 * (double)NumberOfLinks)));
                double cosinus = tangens * Math.Cos(Math.PI * (0.5 + (2.0 * (double)(i + 1) - 1.0) / (2.0 * (double)NumberOfLinks)));
                a[i] = (float) (4.0 + 4.0 * cosinus + Math.Pow(cosinus,2) + Math.Pow(sinus,2));
                b[i] = (float) (-8.0 + 2.0 * (Math.Pow(cosinus, 2) + Math.Pow(sinus, 2)));
                c[i] = (float) (4.0 - 4.0 * cosinus + Math.Pow(cosinus, 2) + Math.Pow(sinus, 2));
            }
        }

        /// <summary>
        /// Задаёт частоту среза фильтра и вызывает перерасчёт коэффициентов фильтра
        /// </summary>
        /// <param name="frequencyBorder">Частота среза</param>
        public void SetFrequencyBorder(float frequencyBorder)
        {
            _frequencyBorder = frequencyBorder;
            SetParapmeters();
        }

        /// <summary>
        /// К-ое звено фильтра высоких частот Баттреворта
        /// </summary>
        /// <param name="k">Номер звена</param>
        /// <param name="x">Входное значение сигнала</param>
        /// <param name="y">Выходное значение сигнала</param>
        private void Filter(int k, float x, ref float y)
        {
            w0[k] = (1.0f * x - (a[k] * w2[k]) - (b[k] * w1[k])) / c[k];
            y = d * (w0[k] + w2[k] + (2.0f * w1[k]));
            w2[k] = w1[k];
            w1[k] = w0[k];
        }

        /// <summary>
        /// Запускает фильтрацию сигнала с помощью ФВЧ
        /// </summary>
        /// <param name="inputSignal">Входной сигнал</param>
        public short[] StartFilter(short[] inputSignal)
        {
            for(long i = 0; i < inputSignal.LongLength; i++)
            {
                X = inputSignal[i];
                Filter(0, X, ref y1);
                Filter(1, y1, ref y2);
                Filter(2, y2, ref y3);
                Filter(3, y3, ref y4);
                y4 *= Normalization;
                X =  (float) Math.Round(y4);
                inputSignal[i] = (short)X;
            }
            return inputSignal;
        }

        public float[] StartFilter(float[] inputSignal)
        {
            for (long i = 0; i < inputSignal.LongLength; i++)
            {
                X = inputSignal[i];
                Filter(0, X, ref y1);
                Filter(1, y1, ref y2);
                Filter(2, y2, ref y3);
                Filter(3, y3, ref y4);
                y4 *= Normalization;
                inputSignal[i] = y4;
            }
            return inputSignal;
        }
    }
}
