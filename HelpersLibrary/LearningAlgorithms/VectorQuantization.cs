using System;
using System.Collections.Generic;
using System.Linq;

namespace HelpersLibrary.LearningAlgorithms
{
    /// <summary>
    /// Производит векторное квантование вектора признаков для диктора. Использует алгоритм ЛБГ
    /// </summary>
    public class VectorQuantization
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
        public const double DistortionDelta = 0.0005;

        public double AverageDistortionMeasure { get; set; }

        public double DistortionDispertion { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="VectorQuantization"/>.
        /// </summary>
        /// <param name="traningSet">Обущающая выборка</param>
        /// <param name="vectorLength"></param>
        /// <param name="codeBookSize"></param>
        public VectorQuantization(double[][] traningSet, int vectorLength, int codeBookSize)
        {
            _codeBookSize = codeBookSize;
			TrainingSet = traningSet;
            CodeBookInit(vectorLength);
            DistortionDispertion = DistortionMeasureDispersion();
		}

        private double DistortionMeasureDispersion()
        {
            double msquare = 0.0;
            for(int i = 0; i < TrainingSet.Length; i++)
            {
                msquare += Math.Pow(QuantizationError(TrainingSet[i], Quantazation(TrainingSet[i])) - AverageDistortionMeasure, 2);
            }
            msquare /= (TrainingSet.Length);

            return msquare;
        }

        private double[] GetNewCodeWord(int vectorLenght)
        {
            var res = new double[vectorLenght];
            var rand = new Random();
            for (int i = 0; i < vectorLenght; i++)
            {
                res[i] = rand.NextDouble()*2.0 - 1.0;
            }
            return res;
        }

		/// <summary>
		/// Инициализирует кодовую книгу для заданного набора обучающих значений
		/// </summary>
        private void CodeBookInit(int vectorLength)
        {
            int iteration = 1;//текущая итерация обучения
            //Что-то вроде инициализации начальных условий
            CodeBook = new double[iteration][];
            CodeBook[0] = new double[vectorLength];
            for (int j = 0; j < vectorLength; j++)
            {//идём по каждому элементу вектора
                for (int i = 0; i < TrainingSet.Length; i++)
                    CodeBook[0][j] += TrainingSet[i][j];//добавляем значения из обучающей выборки
                CodeBook[0][j] /= TrainingSet.Length;
            }
            var averageQuantError = 0.0;
		    while (iteration < _codeBookSize)
            {
                var newCodeBook = new double[iteration*2][];
                var clusters = new List<double[]>[CodeBook.Length];
                for (int i = 0; i < TrainingSet.Length; i++)
                {
                    //get clusters
                    var index = QuantazationIndex(TrainingSet[i]);
                    if (clusters[index] == null)
                    {
                        clusters[index] = new List<double[]>();
                    }
                    clusters[index].Add(TrainingSet[i]);
                }

                var centrOne = GetNewCodeWord(vectorLength);
                var centrTwo = GetNewCodeWord(vectorLength);

                for(int cb = 0; cb < clusters.Length; cb++)
                {
                    if (clusters[cb] == null)
                    {
                        newCodeBook[(cb + 1) * 2 - 1] = new double[vectorLength];
                        newCodeBook[(cb + 1) * 2 - 2] = new double[vectorLength];
                        Array.Copy(centrOne, newCodeBook[(cb + 1) * 2 - 1], vectorLength);
                        Array.Copy(centrTwo, newCodeBook[(cb + 1) * 2 - 2], vectorLength);
                        continue;
                    }
                    var maxLenght = double.NegativeInfinity;
                    for (int i = 0; i < clusters[cb].Count; i++)
                    {
                        for (int j = 0; j < clusters[cb].Count; j++)
                        {
                            var lenght = QuantizationError(clusters[cb][i], clusters[cb][j]);
                            if (lenght > maxLenght)
                            {
                                centrOne = clusters[cb][i];
                                centrTwo = clusters[cb][j];
                                maxLenght = lenght;
                            }
                        }
                    }

                    newCodeBook[(cb + 1) * 2 - 1] = new double[vectorLength];
                    newCodeBook[(cb + 1) * 2 - 2] = new double[vectorLength];
                    Array.Copy(centrOne, newCodeBook[(cb + 1) * 2 - 1], vectorLength);
                    Array.Copy(centrTwo, newCodeBook[(cb + 1) * 2 - 2], vectorLength);
                }

                iteration *= 2;
                CodeBook = newCodeBook;

                //D(m-1) - Dm > DistortionDelta?
                var averageQuantErrorOld = double.PositiveInfinity;
                averageQuantError = AverageQuantizationError();
                while(Math.Abs(averageQuantErrorOld - averageQuantError) > DistortionDelta)//abs here
                {
                    //yi = total_sum(xi)/N
                    var tmpCodeBook = new double[CodeBook.Length][];
                    Array.Copy(CodeBook, tmpCodeBook, CodeBook.Length);
                    var vectorsCount = new int[CodeBook.Length];
					for (int i = 0; i < TrainingSet.Length; i++)
					{
						int codeBookIndex = QuantazationIndex (TrainingSet[i]);
                        for (int j = 0; j < vectorLength; j++)
                        {
                            tmpCodeBook[codeBookIndex][j] += TrainingSet[i][j];
                        }
                        vectorsCount[codeBookIndex]++;
                    }
					for (int i = 0; i < tmpCodeBook.Length; i++)
					{
					    for (int j = 0; j < tmpCodeBook[i].Length; j++)
					        tmpCodeBook[i][j] /= vectorsCount[i] + 1;
					}
                    Array.Copy(tmpCodeBook, CodeBook, tmpCodeBook.Length);
                    averageQuantErrorOld = averageQuantError;
                    averageQuantError = AverageQuantizationError();
                }
            }
            AverageDistortionMeasure = averageQuantError;
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

			    if (error < minError)
			    {

			        min = i;
			        minError = error;
			    }
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
		        var error = 0.0;
                for (int i = 0; i < a.Length; i++)
                {
                    error += Math.Pow(a[i] - b[i], 2);
                }
                return Math.Sqrt(error);
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
                return (error - AverageDistortionMeasure)/DistortionDispertion;
            }
            else
                throw new Exception("Вектора разной длины!");
        }

        private double[] CodeBookDistances(double[][] cb1, double[][] cb2)
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

        public double AverageCodeBookDistance(double[][] cb1, double[][] cb2)
        {
            return CodeBookDistances(cb1, cb2).Average();
        }
    }
}
