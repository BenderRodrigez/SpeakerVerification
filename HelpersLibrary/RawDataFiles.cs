using System;
using System.IO;
using System.Text;

namespace HelpersLibrary
{
    public class RawDataFiles
    {
        public struct FileHeader
        {
            public string Type { get; set; }
            public int VectorLenght { get; set; }
            public int VectorsNumber { get; set; }
            public PointTypes PointType { get; set; }
        }

        public enum PointTypes
        {
            Double, Int, Byte, Short, Float
        }

        public FileHeader Header;

        public byte[][] GetNextByteDataVector()
        {
            try
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    var dataBuffer = new byte[Header.VectorsNumber][];
                    for (int i = 0; i < dataBuffer.Length; i++)
                    {
                        dataBuffer[i] = new byte[Header.VectorLenght];
                        for (int j = 0; j < dataBuffer[i].Length; j++)
                        {
                            dataBuffer[i][j] = sourceReader.ReadByte();
                        }
                    }
                    return dataBuffer;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public double[][] GetNextDoubleDataVector()
        {
            try
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    sourceReader.ReadBytes(13);//skip header
                    var dataBuffer = new double[Header.VectorsNumber][];
                    for (int i = 0; i < dataBuffer.Length; i++)
                    {
                        dataBuffer[i] = new double[Header.VectorLenght];
                        for (int j = 0; j < dataBuffer[i].Length; j++)
                        {
                            dataBuffer[i][j] = sourceReader.ReadDouble();
                        }
                    }
                    return dataBuffer;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public float[][] GetNextFloatDataVector()
        {
            try
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    sourceReader.ReadBytes(13);//skip header
                    var dataBuffer = new float[Header.VectorsNumber][];
                    for (int i = 0; i < dataBuffer.Length; i++)
                    {
                        dataBuffer[i] = new float[Header.VectorLenght];
                        for (int j = 0; j < dataBuffer[i].Length; j++)
                        {
                            dataBuffer[i][j] = sourceReader.ReadSingle();
                        }
                    }
                    return dataBuffer;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public int[][] GetNextIntDataVector()
        {
            try
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    sourceReader.ReadBytes(13);//skip header
                    var dataBuffer = new int[Header.VectorsNumber][];
                    for (int i = 0; i < dataBuffer.Length; i++)
                    {
                        dataBuffer[i] = new int[Header.VectorLenght];
                        for (int j = 0; j < dataBuffer[i].Length; j++)
                        {
                            dataBuffer[i][j] = sourceReader.ReadInt32();
                        }
                    }
                    return dataBuffer;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public int[][] GetNextShortDataVector()
        {
            try
            {
                using (var sourceReader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
                {
                    sourceReader.ReadBytes(13);//skip header
                    var dataBuffer = new int[Header.VectorsNumber][];
                    for (int i = 0; i < dataBuffer.Length; i++)
                    {
                        dataBuffer[i] = new int[Header.VectorLenght];
                        for (int j = 0; j < dataBuffer[i].Length; j++)
                        {
                            dataBuffer[i][j] = sourceReader.ReadInt16();
                        }
                    }
                    return dataBuffer;
                }
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public void WriteBytesArray(byte[][] data, string type)
        {
            using (var writer = new BinaryWriter(new FileStream(_fileName, FileMode.CreateNew)))
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
                writer.Write((byte)Header.PointType);
                writer.Write(Header.VectorsNumber);
                writer.Write(Header.VectorLenght);
                //-------------write data
                for (int i = 0; i < data.Length; i++)
                {
                    for (int j = 0; j < data[i].Length; j++)
                    {
                        writer.Write(data[i][j]);
                    }
                }
            }
        }

        public void WriteDoublesArray(double[][] data, string type)
        {
            using (var writer = new BinaryWriter(new FileStream(_fileName, FileMode.CreateNew)))
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
                writer.Write((byte)Header.PointType);
                writer.Write(Header.VectorsNumber);
                writer.Write(Header.VectorLenght);
                //-------------write data
                for (int i = 0; i < data.Length; i++)
                {
                    for (int j = 0; j < data[i].Length; j++)
                    {
                        writer.Write(data[i][j]);
                    }
                }
            }
        }

        public void WriteIntArray(int[][] data, string type)
        {
            using (var writer = new BinaryWriter(new FileStream(_fileName, FileMode.CreateNew)))
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
                writer.Write((byte)Header.PointType);
                writer.Write(Header.VectorsNumber);
                writer.Write(Header.VectorLenght);
                //-------------write data
                for (int i = 0; i < data.Length; i++)
                {
                    for (int j = 0; j < data[i].Length; j++)
                    {
                        writer.Write(data[i][j]);
                    }
                }
            }
        }

        public void WriteShortArray(short[][] data, string type)
        {
            using (var writer = new BinaryWriter(new FileStream(_fileName, FileMode.CreateNew)))
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
                writer.Write((byte)Header.PointType);
                writer.Write(Header.VectorsNumber);
                writer.Write(Header.VectorLenght);
                //-------------write data
                for (int i = 0; i < data.Length; i++)
                {
                    for (int j = 0; j < data[i].Length; j++)
                    {
                        writer.Write(data[i][j]);
                    }
                }
            }
        }

        public void WriteFloatsArray(float[][] data, string type)
        {
            using (var writer = new BinaryWriter(new FileStream(_fileName, FileMode.CreateNew)))
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
                writer.Write((byte)Header.PointType);
                writer.Write(Header.VectorsNumber);
                writer.Write(Header.VectorLenght);
                //-------------write data
                for (int i = 0; i < data.Length; i++)
                {
                    for (int j = 0; j < data[i].Length; j++)
                    {
                        writer.Write(data[i][j]);
                    }
                }
            }
        }

        private readonly string _fileName;

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
    }
}
