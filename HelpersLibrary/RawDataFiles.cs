using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace HelpersLibrary
{
    /// <summary>
    ///     Читает и записывает матрицы промежуточных результатов на диск
    /// </summary>
    public class RawDataFiles
    {
        /// <summary>
        ///     Поддерживаемые типы хранисых данных
        /// </summary>
        public enum PointTypes
        {
            /// <summary>
            ///     Числа с плавающей запятой двойной точности
            /// </summary>
            Double,

            /// <summary>
            ///     32-битные целые числа
            /// </summary>
            Int,

            /// <summary>
            ///     Байты
            /// </summary>
            Byte,

            /// <summary>
            ///     16-битные целые числа
            /// </summary>
            Short,

            /// <summary>
            ///     Числа с плавающей запятой одинарной точности
            /// </summary>
            Float
        }

        /// <summary>
        ///     Заголовок файла
        /// </summary>
        public FileHeader Header;

        /// <summary>
        ///     Имя файла, который нужно прочитать
        /// </summary>
        private readonly string _fileName;

        /// <summary>
        ///     Создаёт объект, готовый к чтению или записи данных в файл
        /// </summary>
        /// <param name="fileName">Имя файла</param>
        public RawDataFiles(string fileName)
        {
            _fileName = fileName;
            if (File.Exists(_fileName))
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    Header = new FileHeader
                    {
                        Type = Encoding.UTF8.GetString(sourceReader.ReadBytes(4)),
                        PointType = (PointTypes) sourceReader.ReadByte(),
                        VectorsNumber = sourceReader.ReadInt32(),
                        VectorLenght = sourceReader.ReadInt32()
                    };
                }
            }
        }

        /// <summary>
        ///     Читает матрицу байт из файла
        /// </summary>
        /// <returns>Матрицу из чисел, хранящихся в файле</returns>
        public byte[][] GetBytesMatrix()
        {
            try
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    var dataBuffer = new byte[Header.VectorsNumber][];
                    for (var i = 0; i < dataBuffer.Length; i++)
                    {
                        dataBuffer[i] = new byte[Header.VectorLenght];
                        for (var j = 0; j < dataBuffer[i].Length; j++)
                        {
                            dataBuffer[i][j] = sourceReader.ReadByte();
                        }
                    }
                    return dataBuffer;
                }
            }
            catch (Exception e)
            {
                Logger.SetLog(-1, e.ToString(), Process.GetCurrentProcess().ProcessName);
                return null;
            }
        }

        /// <summary>
        ///     Читает матрицу чисел с плавающей запятой двойной точности из файла
        /// </summary>
        /// <returns>Матрицу из чисел, хранящихся в файле</returns>
        public double[][] GetDoubleMatrix()
        {
            try
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    sourceReader.ReadBytes(13); //skip header
                    var dataBuffer = new double[Header.VectorsNumber][];
                    for (var i = 0; i < dataBuffer.Length; i++)
                    {
                        dataBuffer[i] = new double[Header.VectorLenght];
                        for (var j = 0; j < dataBuffer[i].Length; j++)
                        {
                            dataBuffer[i][j] = sourceReader.ReadDouble();
                        }
                    }
                    return dataBuffer;
                }
            }
            catch (Exception e)
            {
                Logger.SetLog(-1, e.ToString(), Process.GetCurrentProcess().ProcessName);
                return null;
            }
        }

        /// <summary>
        ///     Читает матрицу чисел с плавающей запятой из файла
        /// </summary>
        /// <returns>Матрицу из чисел, хранящихся в файле</returns>
        public float[][] GetFloatMatrix()
        {
            try
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    sourceReader.ReadBytes(13); //skip header
                    var dataBuffer = new float[Header.VectorsNumber][];
                    for (var i = 0; i < dataBuffer.Length; i++)
                    {
                        dataBuffer[i] = new float[Header.VectorLenght];
                        for (var j = 0; j < dataBuffer[i].Length; j++)
                        {
                            dataBuffer[i][j] = sourceReader.ReadSingle();
                        }
                    }
                    return dataBuffer;
                }
            }
            catch (Exception e)
            {
                Logger.SetLog(-1, e.ToString(), Process.GetCurrentProcess().ProcessName);
                return null;
            }
        }

        /// <summary>
        ///     Читает матрицу 32-битных целых чисел из файла
        /// </summary>
        /// <returns>Матрицу из чисел, хранящихся в файле</returns>
        public int[][] GetIntMatrix()
        {
            try
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    sourceReader.ReadBytes(13); //skip header
                    var dataBuffer = new int[Header.VectorsNumber][];
                    for (var i = 0; i < dataBuffer.Length; i++)
                    {
                        dataBuffer[i] = new int[Header.VectorLenght];
                        for (var j = 0; j < dataBuffer[i].Length; j++)
                        {
                            dataBuffer[i][j] = sourceReader.ReadInt32();
                        }
                    }
                    return dataBuffer;
                }
            }
            catch (Exception e)
            {
                Logger.SetLog(-1, e.ToString(), Process.GetCurrentProcess().ProcessName);
                return null;
            }
        }

        /// <summary>
        ///     Читает матрицу 16-битныхцелых чисел из файла
        /// </summary>
        /// <returns>Матрицу из чисел, хранящихся в файле</returns>
        public short[][] GetShortMatrix()
        {
            try
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    sourceReader.ReadBytes(13); //skip header
                    var dataBuffer = new short[Header.VectorsNumber][];
                    for (var i = 0; i < dataBuffer.Length; i++)
                    {
                        dataBuffer[i] = new short[Header.VectorLenght];
                        for (var j = 0; j < dataBuffer[i].Length; j++)
                        {
                            dataBuffer[i][j] = sourceReader.ReadInt16();
                        }
                    }
                    return dataBuffer;
                }
            }
            catch (Exception e)
            {
                Logger.SetLog(-1, e.ToString(), Process.GetCurrentProcess().ProcessName);
                return null;
            }
        }

        /// <summary>
        ///     Записывает матрицу байт в файл
        /// </summary>
        /// <param name="data">Данные, которые необходимо записать</param>
        /// <param name="type">Тип характеристики хранимый в файле</param>
        public void WriteBytesArray(byte[][] data, string type)
        {
            using (var writer = new BinaryWriter(new FileStream(_fileName, FileMode.OpenOrCreate)))
            {
                Header = new FileHeader
                {
                    Type = type.Substring(0, 4),
                    PointType = PointTypes.Byte,
                    VectorLenght = data[0].Length,
                    VectorsNumber = data.Length
                };
                //------------write header
                writer.Write(Encoding.UTF8.GetBytes(Header.Type));
                writer.Write((byte) Header.PointType);
                writer.Write(Header.VectorsNumber);
                writer.Write(Header.VectorLenght);
                //-------------write data
                foreach (var t1 in data.SelectMany(t => t))
                {
                    writer.Write(t1);
                }
            }
        }

        /// <summary>
        ///     Записывает матрицу чисел с плавающей запятой двойной точности в файл
        /// </summary>
        /// <param name="data">Данные, которые необходимо записать</param>
        /// <param name="type">Тип характеристики хранимый в файле</param>
        public void WriteDoublesArray(double[][] data, string type)
        {
            using (var writer = new BinaryWriter(new FileStream(_fileName, FileMode.OpenOrCreate)))
            {
                Header = new FileHeader
                {
                    Type = type.Substring(0, 4),
                    PointType = PointTypes.Double,
                    VectorLenght = data[0].Length,
                    VectorsNumber = data.Length
                };
                //------------write header
                writer.Write(Encoding.UTF8.GetBytes(Header.Type));
                writer.Write((byte) Header.PointType);
                writer.Write(Header.VectorsNumber);
                writer.Write(Header.VectorLenght);
                //-------------write data
                foreach (var t1 in data.SelectMany(t => t))
                {
                    writer.Write(t1);
                }
            }
        }

        /// <summary>
        ///     Записывает матрицу 32-битных целых чисел в файл
        /// </summary>
        /// <param name="data">Данные, которые необходимо записать</param>
        /// <param name="type">Тип характеристики хранимый в файле</param>
        public void WriteIntArray(int[][] data, string type)
        {
            using (var writer = new BinaryWriter(new FileStream(_fileName, FileMode.OpenOrCreate)))
            {
                Header = new FileHeader
                {
                    Type = type.Substring(0, 4),
                    PointType = PointTypes.Int,
                    VectorLenght = data[0].Length,
                    VectorsNumber = data.Length
                };
                //------------write header
                writer.Write(Encoding.UTF8.GetBytes(Header.Type));
                writer.Write((byte) Header.PointType);
                writer.Write(Header.VectorsNumber);
                writer.Write(Header.VectorLenght);
                //-------------write data
                foreach (var t1 in data.SelectMany(t => t))
                {
                    writer.Write(t1);
                }
            }
        }

        /// <summary>
        ///     Записывает матрицу 16-битных целых чиел в файл
        /// </summary>
        /// <param name="data">Данные, которые необходимо записать</param>
        /// <param name="type">Тип характеристики хранимый в файле</param>
        public void WriteShortArray(short[][] data, string type)
        {
            var dir = _fileName.Replace(Path.GetFileName(_fileName), "");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            using (var writer = new BinaryWriter(new FileStream(_fileName, FileMode.OpenOrCreate)))
            {
                Header = new FileHeader
                {
                    Type = type.Substring(0, 4),
                    PointType = PointTypes.Short,
                    VectorLenght = data[0].Length,
                    VectorsNumber = data.Length
                };
                //------------write header
                writer.Write(Encoding.UTF8.GetBytes(Header.Type));
                writer.Write((byte) Header.PointType);
                writer.Write(Header.VectorsNumber);
                writer.Write(Header.VectorLenght);
                //-------------write data
                foreach (var t1 in data.SelectMany(t => t))
                {
                    writer.Write(t1);
                }
            }
        }

        /// <summary>
        ///     Записывает матрицу чисел с плавающей запятой одинарной точности в файл
        /// </summary>
        /// <param name="data">Данные, которые необходимо записать</param>
        /// <param name="type">Тип характеристики хранимый в файле</param>
        public void WriteFloatsArray(float[][] data, string type)
        {
            using (var writer = new BinaryWriter(new FileStream(_fileName, FileMode.OpenOrCreate)))
            {
                Header = new FileHeader
                {
                    Type = type.Substring(0, 4),
                    PointType = PointTypes.Float,
                    VectorLenght = data[0].Length,
                    VectorsNumber = data.Length
                };
                //------------write header
                writer.Write(Encoding.UTF8.GetBytes(Header.Type));
                writer.Write((byte) Header.PointType);
                writer.Write(Header.VectorsNumber);
                writer.Write(Header.VectorLenght);
                //-------------write data
                foreach (var t1 in data.SelectMany(t => t))
                {
                    writer.Write(t1);
                }
            }
        }

        /// <summary>
        ///     Описание загаловка файла
        /// </summary>
        public struct FileHeader
        {
            /// <summary>
            ///     Тип хранимой в файле информации, не более 4-х символов в UTF-8
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            ///     Длина каждого вектора в файле
            /// </summary>
            public int VectorLenght { get; set; }

            /// <summary>
            ///     Количество векторов в файле
            /// </summary>
            public int VectorsNumber { get; set; }

            /// <summary>
            ///     Тип данных, содержащихся в файле
            /// </summary>
            public PointTypes PointType { get; set; }
        }
    }
}