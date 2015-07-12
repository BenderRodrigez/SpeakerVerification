using System;
using System.Linq;
using HelpersLibrary;
using HelpersLibrary.DspAlgorithms.Filters;
using SpeechSelector.Properties;

namespace SpeechSelector
{
    static class Program
    {
        private const string ProcessingHeader = "STST";

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
                            var result = reader.GetShortMatrix();
                            ProcessSpeechData(result, FileSettings.LoadSettings(args[1]));
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
                case "windowsize":
                    int windowSize;
                    if (int.TryParse(args[2], out windowSize))
                    {
                        Settings.Default.WindowSize = windowSize;
                        Console.WriteLine("Current window size is: {0}", windowSize);
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
                case "bag":
                    byte histogrammBags;
                    if (byte.TryParse(args[2], out histogrammBags))
                    {
                        Settings.Default.HistogramBags = histogrammBags;
                        Console.WriteLine("Current bags number is: {0}", histogrammBags);
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

        static void ProcessSpeechData(short[][] speechFileData, FileSettings settings)
        {
            var filter1 = new Hpf(Settings.Default.HpfCutFrequency, settings.SampleFrequency);
            var sound = speechFileData.Select(x => x[0]).ToArray();
            var resSound = filter1.Filter(sound);
            Console.WriteLine("Sound successfully filtered by HPF with cut freq. 70 Hz");

            var searcher = new HelpersLibrary.DspAlgorithms.SpeechSearch(Settings.Default.HistogramBags, Settings.Default.WindowSize,
                Settings.Default.Overlapping, settings.SampleFrequency);
            var energy = searcher.CalculateEnergyFunction(resSound);
            Console.WriteLine("Energy successfully calculated");

            double speechDetectorBorder;
            searcher.CalcHistogramm(energy, out speechDetectorBorder);
            Console.WriteLine("Voice detector border is {0}", speechDetectorBorder);
            int startPoint1;
            int endPoint1;
            searcher.SearchSpeech(energy, out startPoint1, out endPoint1, speechDetectorBorder);

            settings.SpeechStartPosition = startPoint1;
            settings.SpeechEndPosition = endPoint1;
            Console.WriteLine("Start point is: {0}", startPoint1);
            Console.WriteLine("End point is: {0}", endPoint1);
            settings.Save();
        }
    }
}
