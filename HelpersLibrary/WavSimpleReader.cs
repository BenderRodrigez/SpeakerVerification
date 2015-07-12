using System.Collections.Generic;
using System.IO;

namespace HelpersLibrary
{
    public class WavSimpleReader
    {
        private readonly string _fileName;
        public WavSimpleReader(string fileName)
        {
            if (File.Exists(fileName))
            {
                _fileName = fileName;
            }
            else
            {
                throw new FileNotFoundException("Нет такого файла! Проверте имя файла и повторите попытку!");
            }
        }

        public short[][] ReadFileData(out int sampleFrequency)
        {
            using (var reader = new BinaryReader(new FileStream(_fileName, FileMode.Open)))
            {
                reader.ReadBytes(24);//header
                sampleFrequency = reader.ReadInt16();
                reader.ReadBytes(18);

                var data = new List<short[]>();
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    data.Add(new []{reader.ReadInt16()});
                }
                return data.ToArray();
            }
        }
    }
}