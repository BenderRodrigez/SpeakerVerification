using System;
using System.IO;
using System.Linq;
using HelpersLibrary;
using HelpersLibrary.DspAlgorithms;
using VTC.Properties;

namespace VTC
{
    static class Program
    {
        private const string ProcessingHeader = "VTC_";
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
                            var writer = new RawDataFiles(Path.Combine(settings.FileMetaDataFolder, "vtc.sbd"));
                            writer.WriteDoublesArray(result, ProcessingHeader);
                        }
                        else
                        {
                            Console.WriteLine("Данные в этом формате не поддерживаются программой.");
                        }
                        break;
                    case "set":
                        SetSettings(args);
                        break;
                }
            }
            else
            {
                Console.WriteLine("Не верный формат команды! Проверьте параметры и повторите попытку.");
            }
        }

        private static void SetSettings(string[] args)
        {
            switch (args[1].ToLowerInvariant())
            {
                case "acfwindowlenght":
                    float acfWindowLenght;
                    if (float.TryParse(args[2], out acfWindowLenght))
                    {
                        Settings.Default.AcfWindowLenght = acfWindowLenght;
                        Console.WriteLine("Current window size is: {0}", acfWindowLenght);
                    }
                    else
                        Console.WriteLine("Value is incorrect");
                    break;
                case "overlapping":
                    float overlapping;
                    if (float.TryParse(args[2].Replace('.', ','), out overlapping))
                    {
                        Settings.Default.Overlapping = overlapping;
                        Console.WriteLine("Current overlapping is: {0}", overlapping);
                    }
                    else
                        Console.WriteLine("Value is incorrect");
                    break;
                case "numberofcoeficients":
                    byte numberOfCeficients;
                    if (byte.TryParse(args[2], out numberOfCeficients))
                    {
                        Settings.Default.NumberOfCeficients = numberOfCeficients;
                        Console.WriteLine("Current bags number is: {0}", numberOfCeficients);
                    }
                    else
                        Console.WriteLine("Value is incorrect");
                    break;
                case "vtcsize":
                    int vtcSize;
                    if (int.TryParse(args[2], out vtcSize))
                    {
                        Settings.Default.VtcSize = vtcSize;
                        Console.WriteLine("Current bags number is: {0}", vtcSize);
                    }
                    else
                        Console.WriteLine("Value is incorrect");
                    break;
                case "windowtype":
                    WindowFunctions.WindowType windowType;
                    if (Enum.TryParse(args[2], out windowType))
                    {
                        Settings.Default.WindowType = windowType.ToString();
                        Console.WriteLine("Current bags number is: {0}", windowType);
                    }
                    else
                        Console.WriteLine("Value is incorrect");
                    break;
                //default:
                //    Console.WriteLine("type help to show comand list");
                //    break;
            }
            Settings.Default.Save();
        }

        static double[][] ProcessSpeechData(short[][] speechFileData, FileSettings settings)
        {
            WindowFunctions.WindowType windowType;
            if (!Enum.TryParse(Settings.Default.WindowType, true, out windowType))
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
            lpc.GetArcVocalTractImage(ref sound, settings.SampleFrequency, Settings.Default.VtcSize, out res, settings.SpeechStartPosition, settings.SpeechEndPosition);
            return res;
        }
    }
}
