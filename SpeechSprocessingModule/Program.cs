using System;
using HelpersLibrary;

namespace SpeechSprocessingModule
{
    static class Program
    {
        private const string ProcessingHeader = "";
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[0].ToLower())
                {
                    case "process":
                        var reader = new RawDataFiles(args[1]);
                        if (reader.Header.PointType == RawDataFiles.PointTypes.Double)
                        {
                            var result = ProcessSpeechData(reader.GetDoubleMatrix());
                            reader.WriteDoublesArray(result,ProcessingHeader);
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

        static double[][] ProcessSpeechData(double[][] speechFileData)
        {
            return null;
        }
    }
}
