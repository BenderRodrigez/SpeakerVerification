using System;
using HelpersLibrary;
using HelpersLibrary.LearningAlgorithms;
using VQ.Properties;

namespace VQ
{
    static class Program
    {
        private const IntermediateDataStorage.SpecFileTypes ProcessingType = IntermediateDataStorage.SpecFileTypes.VqCb;
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                switch (args[0].ToLower())
                {
                    case "process"://args[1] - training set file name
                        var reader = new RawDataFiles(args[1]);
                        if (reader.Header.PointType == RawDataFiles.PointTypes.Double)
                        {
                            var settings = FileSettings.LoadSettings(args[1]);
                            var result = ProcessSpeechData(reader.GetDoubleMatrix(), reader.Header, settings);
                            var writer = new RawDataFiles(IntermediateDataStorage.GetSpecFilePath(args[1], ProcessingType));
                            writer.WriteDoublesArray(result, ProcessingType.ToString());
                        }
                        else
                        {
                            Console.WriteLine("Данные в этом формате не поддерживаются программой.");
                        }
                        break;
                    case "quatize"://args[1] - training set file name (*.wav), args[2] - test set file name (*.wav), args[3] characteristic type (sbd file names)
                        var trainedDataReader = new RawDataFiles(args[1]);
                        var testDataReader = new RawDataFiles(args[2]);
                        var trainedSettings = FileSettings.LoadSettings(args[1]);
                        if (trainedDataReader.Header.PointType == RawDataFiles.PointTypes.Double && testDataReader.Header.PointType == RawDataFiles.PointTypes.Double)
                        {
                            var result = Quantize(trainedDataReader.GetDoubleMatrix(), testDataReader.GetDoubleMatrix(), trainedSettings);
                            var writer = new RawDataFiles(IntermediateDataStorage.GetSpecFilePath(args[2], IntermediateDataStorage.SpecFileTypes.VqDt));
                            writer.WriteDoublesArray(result, ProcessingType.ToString());
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

        static double[][] Quantize(double[][] codeBook, double[][] testSet, FileSettings settings)
        {
            return null;
        }

        static double[][] ProcessSpeechData(double[][] speechFileData, RawDataFiles.FileHeader fileHeader, FileSettings settings)
        {
            var vq = new VectorQuantization(speechFileData, fileHeader.VectorLenght, Settings.Default.CodeBookSize)
            {
                DistortionDelta = Settings.Default.DistortionDelta
            };
            settings.DistortionDispertion = vq.DistortionDispertion;
            settings.AverageDistortionMeasure = vq.AverageDistortionMeasure;
            settings.Save();
            return vq.CodeBook;
        }
    }
}
