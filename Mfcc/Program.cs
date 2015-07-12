using System;
using System.IO;
using System.Linq;
using HelpersLibrary;
using HelpersLibrary.DspAlgorithms;
using Mfcc.Properties;

namespace Mfcc
{
    static class Program
    {
        private const string ProcessingHeader = "MFCC";
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[0].ToLower())
                {
                    case "process":
                        var reader = new RawDataFiles(args[1]);
                        if (reader.Header.PointType == RawDataFiles.PointTypes.Short)
                        {
                            var settings = FileSettings.LoadSettings(args[1]);
                            var result = ProcessSpeechData(reader.GetShortMatrix(), settings);
                            var writer = new RawDataFiles(Path.Combine(settings.FileMetaDataFolder, "mfcc.sbd"));
                            writer.WriteDoublesArray(result, ProcessingHeader);
                        }
                        else
                        {
                            Console.WriteLine("Данные в этом формате не поддерживаются программой.");
                        }
                        break;
                }
            }
            else
            {
                Console.WriteLine("Не верный формат команды! Проверьте параметры и повторите попытку.");
            }
        }

        static double[][] ProcessSpeechData(short[][] speechFileData, FileSettings settings)
        {
            WindowFunctions.WindowType windowType;
            if (!Enum.TryParse(Settings.Default.WindowType, true, out windowType))
                windowType = WindowFunctions.WindowType.Blackman;

            var mfcc = new Cepstrum(Settings.Default.CoefficientsNumber, Settings.Default.WindowSize,
                settings.SampleFrequency, Settings.Default.Overlapping);

            double[][] res;
            var sound = speechFileData.Select(x => x[0]).ToArray();
            mfcc.GetCepstrogram(ref sound,windowType, settings.SpeechStartPosition, settings.SpeechEndPosition, out res);
            return res;
        }
    }
}
