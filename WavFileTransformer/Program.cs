using System;
using System.IO;
using HelpersLibrary;

namespace WavFileTransformer
{
    static class Program
    {
        private const string ProcessingHeader = "WAVE";
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[0].ToLower())
                {
                    case "process":
                        var reader = new WavSimpleReader(args[1]);
                        var settings = FileSettings.LoadSettings(args[1]);
                        settings.DictorName = Path.GetFileNameWithoutExtension(args[1]);
                        var writer = new RawDataFiles(Path.Combine(settings.FileMetaDataFolder, "raw_wav.sbd"));
                        int sampleFrequency;
                        writer.WriteShortArray(reader.ReadFileData(out sampleFrequency), ProcessingHeader);
                        settings.SampleFrequency = sampleFrequency;
                        settings.Save();
                        break;
                }
            }
            else
            {
                Console.WriteLine("Не верный формат команды! Проверьте параметры и повторите попытку.");
            }
        }
    }
}
