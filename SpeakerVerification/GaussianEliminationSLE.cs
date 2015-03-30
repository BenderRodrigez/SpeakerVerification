using System;

namespace SpeakerVerification
{
    class GaussianEliminationSle
    {
        /// <summary>
        /// Матрица описывает значения коэффициентов а[i,j] перед неизвестными в ЛУ
        /// </summary>
        private double[][] _matrix;
        /// <summary>
        /// Вектор неизвесных
        /// </summary>
        private readonly double[] _x;
        /// <summary>
        /// Вектор значений решений линейных уравнений
        /// </summary>
        private double[] _vector;

        /// <summary>
        /// Размер СЛУ
        /// </summary>
        private readonly int _size;
        
        /// <summary>
        /// Конструктор, задаёт количество ЛУ
        /// </summary>
        /// <param name="numberOfCoefficents">Количество ЛУ</param>
        public GaussianEliminationSle(int numberOfCoefficents)
        {
            _size = numberOfCoefficents;
            _x = new double[_size];
        }

        /// <summary>
        /// Распределяет ЛУ таким образом, чтобы на главной диагонали стояли нули
        /// </summary>
        private void Diagonalize()
        {
            for (int i = 0; i < _size; i++)
            {
                if (_matrix[i][i] == 0)
                {
                    for (int j = 0; j < _size; j++)
                    {
                        if (j == i) continue;
                        if (_matrix[j][i] != 0 && _matrix[i][j] != 0)
                        {
                            double tmp;
                            for (int k = 0; k < _size; k++)
                            {
                                tmp = _matrix[j][k];
                                _matrix[j][k] = _matrix[i][k];
                                _matrix[i][k] = tmp;
                            }
                            tmp = _vector[j];
                            _vector[j] = _vector[i];
                            _vector[i] = tmp;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Приводит матрицу к трапецевидному виду
        /// </summary>
        /// <returns></returns>
        private bool RowsCorrect()
        {
            for (int k = 0; k < _size; k++)
            {
                for (int i = k + 1; i < _size; i++)
                {
                    if (_matrix[k][k] == 0)
                    {
                        //Solution is not exist.
                        return false;
                    }
                    var m = _matrix[i][k] / _matrix[k][k];
                    for (int j = k; j < _size; j++)
                    {
                        _matrix[i][j] -= m * _matrix[k][j];
                    }
                    _vector[i] -= m * _vector[k];
                }
            }
            return true;
        }

        /// <summary>
        /// Устанавливает решения системы линейных уравнений
        /// </summary>
        private void Solve()
        {
            if (RowsCorrect())//Если есть решение, то ищем.
            {
                for (int i = _size - 1; i >= 0; i--)
                {
                    double s = 0;
                    for (int j = i; j < _size; j++)
                    {
                        s += _matrix[i][j] * _x[j];
                    }
                    _x[i] = (_vector[i] - s) / _matrix[i][i];
                }
            }
        }

        private void SolveLog()
        {
            if (RowsCorrect())//Если есть решение, то ищем.
            {
                for (int i = _size - 1; i >= 0; i--)
                {
                    double s = 0;
                    for (int j = i; j < _size; j++)
                    {
                        s += _matrix[i][j] * _x[j];
                    }
                    var t = 10.0*Math.Log10(Math.Abs((_vector[i] - s) / _matrix[i][i]));
                    if(t == double.NegativeInfinity)
                        t = 0;
                    _x[i] = t;
                }
            }
        }

        /// <summary>
        /// Ищет решения СЛУ
        /// </summary>
        /// <param name="matrix">Матрица коэффициентов перед неизвесными в СЛУ</param>
        /// <param name="vector">Вектор значений решений СЛУ</param>
        /// <param name="x">Выходной вектор, описывающий неизвестные</param>
        public void SolutionFind(double[][] matrix, double[] vector, out double[] x)
        {
            _matrix = matrix;
            _vector = vector;
            Diagonalize();
            Solve();
            x = _x;
        }

        public void SolutionFindLog(double[][] matrix, double[] vector, out double[] x)
        {
            _matrix = matrix;
            _vector = vector;
            Diagonalize();
            SolveLog();
            x = _x;
        }
    }
}
