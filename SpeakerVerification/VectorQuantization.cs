using System;
using System.Linq;

namespace SpeakerVerification
{
    /// <summary>
    /// Производит векторное квантование вектора признаков для диктора. Использует алгоритм ЛБГ
    /// </summary>
    class VectorQuantization
    {
        /// <summary>
        /// Кодовая книга, на основе которой будет происходить квантование
        /// </summary>
        public double[][] CodeBook;

        /// <summary>
        /// Выборка векторов признаков, на основе которых будет производиться обучение кодовой книги
        /// </summary>
        public readonly double[][] TrainingSet;

        /// <summary>
        /// Размер кодовой книги, по которой будем выявлять диктора
        /// </summary>
        private readonly int _codeBookSize;

        /// <summary>
        /// Погрешность квантования
        /// </summary>
        private const double E = 0.05;

        private double _averageDistortionMeasure;

        private readonly double _distortionDispertion;

        private readonly double[] _trainingSetMax;
        private readonly double[] _trainingSetMin;

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="SpeakerVerification.VectorQuantization"/>.
        /// </summary>
        /// <param name="traningSet">Обущающая выборка</param>
        /// <param name="lpcNumber"></param>
        /// <param name="codeBookSize"></param>
        public VectorQuantization(double[][] traningSet, int lpcNumber, int codeBookSize = 64)
        {
            _codeBookSize = codeBookSize;
			TrainingSet = traningSet;
            _trainingSetMax = new double[TrainingSet[0].Length];
            _trainingSetMin = new double[TrainingSet[0].Length];
            for (int i = 0; i < _trainingSetMax.Length; i++)
            {
                _trainingSetMax[i] = TrainingSet.Max(x => x[i]);
                _trainingSetMin[i] = TrainingSet.Min(x => x[i]);
            }
            CodeBookInit(lpcNumber);
            _distortionDispertion = DistortionMeasureDispersion();
		}

        private double DistortionMeasureDispersion()
        {
            double msquare = 0.0;
            for(int i = 0; i < TrainingSet.Length; i++)
            {
                msquare += Math.Pow(QuantizationError(TrainingSet[i], Quantazation(TrainingSet[i])) - _averageDistortionMeasure, 2);
            }
            msquare /= (TrainingSet.Length);

            return msquare;
        }

        //public double DistortionMeasureEnergy(ref double[][] lpc1, ref double[][] lpc2, int intervalNumber, int totalIntervals)
        //{
        //    double sum = 0;
        //    if (lpc1.Length < lpc2.Length)
        //    {
        //        int intervalSize = (int)Math.Ceiling((double)lpc1.Length / totalIntervals);//размер интервала
        //        for (int i = intervalSize * intervalNumber; i < lpc1.Length && i < (intervalNumber + 1) * intervalSize; i++)
        //            sum += QuantizationErrorNormal(lpc1[i], Quantazation(lpc2[i]));
        //        return Math.Abs(sum / intervalSize);
        //    }
        //    else
        //    {
        //        int intervalSize = (int)Math.Ceiling((double)lpc2.Length / totalIntervals);//размер интервала
        //        for (int i = intervalSize * intervalNumber; i < lpc2.Length && i < (intervalNumber + 1) * intervalSize; i++)
        //            sum += QuantizationErrorNormal(lpc1[i], Quantazation(lpc2[i]));
        //        return Math.Abs(sum / intervalSize);
        //    }
        //}

		/// <summary>
		/// Инициализирует кодовую книгу для заданного набора обучающих значений
		/// </summary>
        private void CodeBookInit(int lpcNumber)
        {
            int iteration = 1;//текущая итерация обучения
            //Что-то вроде инициализации начальных условий
            CodeBook = new double[iteration][];
            CodeBook[0] = new double[lpcNumber];
            for (int j = 0; j < lpcNumber; j++)
            {//идём по каждому элементу вектора
                for (int i = 0; i < TrainingSet.Length; i++)
                    CodeBook[0][j] += TrainingSet[i][j];//добавляем значения из обучающей выборки
                CodeBook[0][j] /= TrainingSet.Length;
            }
            var averageQuantError = AverageQuantizationError();

		    while (iteration < _codeBookSize)
            {
                var newCodeBook = new double[iteration*2][];
                for (int i = 0; i < iteration; i++ )
                {
                    //yi = y(i) + p
                    newCodeBook[i] = new double[CodeBook[i].Length];
                    for (int j = 0; j < CodeBook[i].Length; j++)
                    {
                        newCodeBook[i][j] = (CodeBook[i][j] + _trainingSetMax[j]) / 2;
                        //newCodeBook[i][j] = CodeBook[i][j] * 1.1;
                    }
                }
                for (int i = iteration; i < iteration * 2; i++ )
                {
                    //yi = y(i-k) - p
                    newCodeBook[i] = new double[CodeBook[i - iteration].Length];
                    for (int j = 0; j < CodeBook[i - iteration].Length; j++)
                    {
                        newCodeBook[i][j] = (CodeBook[i - iteration][j] + _trainingSetMin[j]) / 2;
                        //newCodeBook[i][j] = CodeBook[i - iteration][j] * 0.9;
                    }
                }
                iteration *= 2;
                CodeBook = newCodeBook;

                //D(m-1) - Dm > E?
                var averageQuantErrorOld = averageQuantError;
                averageQuantError = AverageQuantizationError();
                while(Math.Abs(averageQuantErrorOld - averageQuantError) > E)//abs here
                {
                    //yi = total_sum(xi)/N
					var tmpCodeBook = new double[CodeBook.Length][];
                    var vectorsCount = new int[CodeBook.Length];
					for (int i = 0; i < TrainingSet.Length; i++)
					{
						int codeBookIndex = QuantazationIndex (TrainingSet [i]);
						if (tmpCodeBook [codeBookIndex] == null)
                            tmpCodeBook[codeBookIndex] = new double[lpcNumber];
                        for (int j = 0; j < lpcNumber; j++)
                        {
                            tmpCodeBook[codeBookIndex][j] += TrainingSet[i][j];
                            vectorsCount[codeBookIndex]++;
                        }
					}
					for (int i = 0; i < tmpCodeBook.Length; i++)
                    {
                        if(tmpCodeBook[i] == null)
                            tmpCodeBook[i] = new double[CodeBook[i].Length];
						for (int j = 0; j < tmpCodeBook[i].Length; j++)
                            tmpCodeBook[i][j] /= vectorsCount[i];////-----------------------------------------------------
                    }
                    averageQuantErrorOld = averageQuantError;
                    averageQuantError = AverageQuantizationError();
                }
            }
            _averageDistortionMeasure = AverageQuantizationError();
        }

		/// <summary>
		/// Вычисляет среднюю ошибку квантования для кодовой книги
		/// </summary>
		/// <returns>Средняя ошибка квантования</returns>
        private double AverageQuantizationError()
        {//D=(total_sum(d(x, Q(x))))/N
            double errorRate = 0;
            for (int i = 0; i < TrainingSet.Length; i++)
                errorRate += QuantizationError(TrainingSet[i], Quantazation(TrainingSet[i]));
            errorRate /= TrainingSet.Length;
            return errorRate;
        }

		/// <summary>
		/// Оператор векторного квантования
		/// </summary>
		/// <param name="x">Вектор входных значений</param>
        public double[] Quantazation(double[] x)
        {//Оператор квантования
		    var minError = double.PositiveInfinity;
            int min = 0;
            for (int i = 0; i < CodeBook.Length; i++)
            {
                var error = QuantizationError(x, CodeBook[i]);
                if (error < minError)
                {
                    min = i;
                    minError = error;
                }
            }
		    return CodeBook[min];
        }

		/// <summary>
		/// Возвращает индекс центроида для данного вектора
		/// </summary>
		/// <returns>Индекс в кодовой книге</returns>
		/// <param name="x">Вектор сходных значений</param>
		private int QuantazationIndex(double[] x)
		{//Тоже, что и оператор квантования, но возвращает индекс в книге
		    var minError = double.PositiveInfinity;
			int min = 0;
			for (int i = 0; i < CodeBook.Length; i++)
			{
			    var error = QuantizationError(x, CodeBook[i]);

			    if (!(error < minError)) continue;

			    min = i;
			    minError = error;
			}
		    return min;
		}

		/// <summary>
		/// Расчитывает ошибку между двумя векторами
		/// </summary>
		/// <returns>The error.</returns>
		/// <param name="a">The alpha component.</param>
		/// <param name="b">The blue component.</param>
        public double QuantizationError(double[] a, double[] b)
        {//d=total_sum(a^2-b^2)
		    if (a.Length == b.Length)
            {
                double error = a.Select((t, i) => Math.Pow(t - b[i], 2)).Sum();
                return error;
            }
		    throw new Exception("Вектора разной длины!");
        }

        public double DistortionMeasureEnergy(ref double[][] testImage)
        {
            double res = 0;
            for(int i = 0; i < testImage.Length; i++)
            {
                res += Math.Pow(QuantizationError(testImage[i], Quantazation(testImage[i])), 2);
            }
            res /= testImage.Length;
            return res;
        }

        public double QuantizationErrorNormal(double[] a, double[] b)
        {//d=total_sum(a^2-b^2)
            double error = 0;
            if (a.Length == b.Length)
            {
                for (int i = 0; i < a.Length; i++)
                {
                    error += Math.Pow(a[i] - b[i], 2);
                }
                return (error - _averageDistortionMeasure)/_distortionDispertion;
            }
            else
                throw new Exception("Вектора разной длины!");
        }

        public double[] CodeBookDistances(double[][] cb1, double[][] cb2)
        {
            if (cb1.Length == cb2.Length)
            {
                double[] distance = new double[cb1.Length];
                for (int i = 0; i < cb1.Length; i++)
                {
                    distance[i] = QuantizationError(cb1[i], cb2[i]);
                }
                return distance;
            }
            else
                throw new Exception("Вектора разной длины!");
        }

        public double AverageCodeBookDistance(double[][] _cb1, double[][] _cb2)
        {
            return CodeBookDistances(_cb1, _cb2).Average();
        }
    }
}
