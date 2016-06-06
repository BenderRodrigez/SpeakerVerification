using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        public double DistortionDelta { get; set; }

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
            DistortionDelta = 0.05;
            CodeBookInit(vectorLength);
            ClearCodeBook();

            DistortionDispertion = DistortionMeasureDispersion();
		}

        private void ClearCodeBook()
        {
            var effecivness = new int[CodeBook.Length];
            foreach (var t in TrainingSet)
            {
                effecivness[QuantazationIndex(t)]++;
            }

            var clearCodeBook = new List<double[]>(CodeBook.Length);
            for (int i = 0; i < effecivness.Length; i++)
            {
                if(effecivness[i] > 0)
                    clearCodeBook.Add(CodeBook[i]);
            }
            CodeBook = clearCodeBook.ToArray();
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
                var j1 = j;
                var res = Parallel.For(0, TrainingSet.Length, i =>
                    CodeBook[0][j1] += TrainingSet[i][j1]);//добавляем значения из обучающей выборки
                while (!res.IsCompleted)
                {
                    
                }
                CodeBook[0][j] /= TrainingSet.Length;
            }
            var averageQuantError = AverageQuantizationError();

		    while (iteration < _codeBookSize)
            {
                var newCodeBook = new double[iteration*2][];
                Parallel.For(0, CodeBook.Length, cb =>
                {
                    var maxDistance = double.NegativeInfinity;
                    var centrOne = new double[vectorLength];
                    var centrTwo = new double[vectorLength];
                    for (int i = 0; i < TrainingSet.Length-1; i++)
                    {
                        if (QuantazationIndex(TrainingSet[i]) != cb) continue;

                        for (int j = i+1; j < TrainingSet.Length; j++)
                        {
                            if (QuantazationIndex(TrainingSet[j]) != cb) continue;

                            var distance = QuantizationError(TrainingSet[i], TrainingSet[j]);
                            if (distance > maxDistance)
                            {
                                centrOne = TrainingSet[i];
                                centrTwo = TrainingSet[j];
                                maxDistance = distance;
                            }
                        }
                    }

                    newCodeBook[(cb + 1)*2 - 1] = new double[vectorLength];
                    newCodeBook[(cb + 1)*2 - 2] = new double[vectorLength];

                    if (centrOne.Sum() == 0.0)
                    {
                        var rand = new Random();
                        for (int i = 0; i < vectorLength; i++)
                        {
                            newCodeBook[(cb + 1)*2 - 1][i] = CodeBook[cb][i] - CodeBook[cb][i]*rand.NextDouble()*0.1;
                        }
                    }
                    else
                    {
                        Array.Copy(centrOne, newCodeBook[(cb + 1)*2 - 1], vectorLength);
                    }
                    if (centrTwo.Sum() == 0.0)
                    {
                        var rand = new Random();
                        for (int i = 0; i < vectorLength; i++)
                        {
                            newCodeBook[(cb + 1)*2 - 2][i] = CodeBook[cb][i] + CodeBook[cb][i]*rand.NextDouble()*0.1;
                        }
                    }
                    else
                    {
                        Array.Copy(centrTwo, newCodeBook[(cb + 1)*2 - 2], vectorLength);
                    }
                });

                iteration *= 2;
                CodeBook = newCodeBook;

                //D(m-1) - Dm > DistortionDelta?
                var averageQuantErrorOld = averageQuantError;
                averageQuantError = AverageQuantizationError();
                var kMeansIntertionsCount = 0;
                while(Math.Abs(averageQuantErrorOld - averageQuantError) > DistortionDelta && kMeansIntertionsCount < 50)//abs here
                {
                    //yi = total_sum(xi)/N
					var tmpCodeBook = new double[CodeBook.Length][];
                    var vectorsCount = new int[CodeBook.Length];
                    var res = Parallel.For(0, TrainingSet.Length, i =>
                    {
                        int codeBookIndex = QuantazationIndex(TrainingSet[i]);
                        if (tmpCodeBook[codeBookIndex] == null)
                            tmpCodeBook[codeBookIndex] = new double[vectorLength];
                        for (int j = 0; j < vectorLength; j++)
                        {
                            tmpCodeBook[codeBookIndex][j] += TrainingSet[i][j];
                        }
                        vectorsCount[codeBookIndex]++;
                    });

                    while (!res.IsCompleted)
                    {
                        
                    }

                    var result = Parallel.For(0, tmpCodeBook.Length, i =>
                    {
                        if (tmpCodeBook[i] == null)
                        {
                            tmpCodeBook[i] = new double[vectorLength];
                            Array.Copy(CodeBook[i], tmpCodeBook[i], vectorLength);
                        }
                        else if (vectorsCount[i] > 0)
                        {
                            for (int j = 0; j < tmpCodeBook[i].Length; j++)
                                tmpCodeBook[i][j] /= vectorsCount[i];
                        }
                    });
                    while (!result.IsCompleted)
                    {
                        
                    }
                    CodeBook = tmpCodeBook;
                    averageQuantErrorOld = averageQuantError;
                    averageQuantError = AverageQuantizationError();
                    kMeansIntertionsCount++;
                }
            }
            AverageDistortionMeasure = AverageQuantizationError();
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
        public static double QuantizationError(double[] a, double[] b)
        {//d=total_sum(a^2-b^2)
		    if (a.Length == b.Length)
            {
                var error = a.Select((t, i) => Math.Pow(t - b[i], 2)).Sum();
                return error;
            }
		    throw new Exception("Вектора разной длины!");
        }

        public double DistortionMeasureEnergy(double[][] testImage)
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
