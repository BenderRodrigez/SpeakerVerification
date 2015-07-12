using System;
using System.IO;
using System.Linq;
using HelpersLibrary;
using HelpersLibrary.DspAlgorithms;
using Lpc.Properties;

namespace Lpc
{
    static class Program
    {
        private const string ProcessingHeader = "LPC_";
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
                            var writer = new RawDataFiles(Path.Combine(settings.FileMetaDataFolder, "lpc.sbd"));
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
            if(!Enum.TryParse(Settings.Default.WindowType, true, out windowType))
                windowType = WindowFunctions.WindowType.Blackman;

            var lpc = new LinearPredictCoefficient
            {
                SamleFrequency = settings.SampleFrequency,
                UsedNumberOfCoeficients = Settings.Default.NumberOfCeficients,
                UsedAcfWindowSizeTime = Settings.Default.AcfWindowLenght,
                Overlapping = Settings.Default.Overlapping,
                UsedWindowType = windowType
            };

            double[][] res;
            var sound = speechFileData.Select(x => x[0]).ToArray();
            lpc.GetLpcImage(ref sound, out res, settings.SpeechStartPosition, settings.SpeechEndPosition);
            return res;
        }
    }
}
