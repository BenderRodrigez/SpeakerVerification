using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Mime;
using HelpersLibrary;
using HelpersLibrary.DspAlgorithms;
using HelpersLibrary.DspAlgorithms.Filters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WavFileTransformer;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void RawDataFiles()
        {
            var rawData = new RawDataFiles("some name");
            var bytes = new byte[1024][];
            for(int i = 0; i < bytes.Length; i++)
                bytes[i] = new byte[1024];
            rawData.WriteBytesArray(bytes, "TEST");
            var bytesRes = rawData.GetBytesMatrix();

            if(!rawData.Header.Type.Equals("TEST"))
                throw new Exception();

            if (bytes.Select(x => x.SequenceEqual(bytesRes.SelectMany(y => y))).Min())
                throw new Exception();

            var doubles = new double[1024][];
            for (int i = 0; i < doubles.Length; i++)
                doubles[i] = new double[1024];
            rawData.WriteDoublesArray(doubles, "TEST");
            var doubleRes = rawData.GetDoubleMatrix();

            if (!rawData.Header.Type.Equals("TEST"))
                throw new Exception();

            if (doubles.Select(x => x.SequenceEqual(doubleRes.SelectMany(y => y))).Min())
                throw new Exception();

            var floats = new float[1024][];
            for (int i = 0; i < floats.Length; i++)
                floats[i] = new float[1024];
            rawData.WriteFloatsArray(floats, "TEST");
            var floatRes = rawData.GetFloatMatrix();

            if (!rawData.Header.Type.Equals("TEST"))
                throw new Exception();

            if (floats.Select(x => x.SequenceEqual(floatRes.SelectMany(y => y))).Min())
                throw new Exception();

            var ints = new int[1024][];
            for (int i = 0; i < ints.Length; i++)
                ints[i] = new int[1024];
            rawData.WriteIntArray(ints, "TEST");
            var intRes = rawData.GetIntMatrix();

            if (!rawData.Header.Type.Equals("TEST"))
                throw new Exception();

            if (ints.Select(x => x.SequenceEqual(intRes.SelectMany(y => y))).Min())
                throw new Exception();

            var shorts = new short[1024][];
            for (int i = 0; i < bytes.Length; i++)
                shorts[i] = new short[1024];
            rawData.WriteShortArray(shorts, "TEST");
            var shotrsRes = rawData.GetShortMatrix();

            if (!rawData.Header.Type.Equals("TEST"))
                throw new Exception();

            if (shorts.Select(x=>x.SequenceEqual(shotrsRes.SelectMany(y=> y))).Min())
                throw new Exception();
        }

        [TestMethod]
        public void SpeechModuleParams()
        {
            //SpeechSprocessingModule.Program.Main(new[] { "process", "C:\\" });
            //SpeechSprocessingModule.Program.Main(new[] { "process", "C:\\", "sdfsdfsf" });
            //SpeechSprocessingModule.Program.Main(new[] { "process", "C:\\sdfsdfsdf.dat" });
            //SpeechSprocessingModule.Program.Main(new[] { "process", "C:\\sdfsdfsdf.dat", "sdfsdfsd" });
            //SpeechSprocessingModule.Program.Main(new[] { "prOcesS", "C:\\" });
            //SpeechSprocessingModule.Program.Main(new[] { "Process", "C:\\" });
            //SpeechSprocessingModule.Program.Main(new[] { "process" });
            //SpeechSprocessingModule.Program.Main(new[] { "process", "C:\\" });
        }

        [TestMethod]
        public void WavReading()
        {
            //WavFileTransformer
        }

        [TestMethod]
        public void TestFileName()
        {
            var args = "C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav";
            var result = Path.Combine(args + ".files\\", "raw_wav.sbd");
            File.Create(result);
        }

       // [TestMethod]
        public void TestReadWavFile()
        {
            const string fileName = "C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav";
            var wavReader =
                new WavSimpleReader(fileName);
            var settings = FileSettings.LoadSettings(fileName);
            settings.DictorName = Path.GetFileNameWithoutExtension(fileName);

            var writer = new RawDataFiles(Path.Combine(settings.FileMetaDataFolder, "raw_wav.sbd"));
            int sampleFrequency;
            writer.WriteShortArray(wavReader.ReadFileData(out sampleFrequency), "WAVE");
            settings.SampleFrequency = sampleFrequency;
            settings.Save();
        }

       // [TestMethod]
        public void TestSpeechSearch()
        {
            const string fileName = "C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav";
            var wavReader =
                new WavSimpleReader(fileName);
            var settings = FileSettings.LoadSettings(fileName);
            settings.DictorName = Path.GetFileNameWithoutExtension(fileName);

            var writer = new RawDataFiles(Path.Combine(settings.FileMetaDataFolder, "raw_wav.sbd"));
            int sampleFrequency;
            writer.WriteShortArray(wavReader.ReadFileData(out sampleFrequency), "WAVE");
            settings.SampleFrequency = sampleFrequency;
            settings.Save();

            var speechFileDataReader = new WavSimpleReader(fileName);
            var speechFileData = speechFileDataReader.ReadFileData(out sampleFrequency);

            Assert.AreEqual(settings.SampleFrequency, sampleFrequency);//test settings reading

            var filter1 = new Hpf(70.0f, settings.SampleFrequency);
            var sound = speechFileData.Select(x => x[0]).ToArray();
            var resSound = filter1.Filter(sound);
            var zeroesNumber = resSound.Count(x => Math.Abs(x) < float.Epsilon);
            Assert.AreEqual(zeroesNumber, 0);
            Console.WriteLine("Sound successfully filtered by HPF with cut freq. 70 Hz");

            var searcher = new SpeechSearch(15, 0.04f, 0.9f, sampleFrequency);
            var energy = searcher.CalculateEnergyFunction(resSound);
            using (var writerEnergy = new StreamWriter("1.txt"))
            {
                foreach (var t in energy)
                {
                    writerEnergy.WriteLine(t);
                }
            }
            //zeroesNumber = energy.Count(x => Math.Abs(x) < float.Epsilon);
            //Assert.AreEqual(zeroesNumber, 0);
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

       // [TestMethod]
        public void TestLpcCalulations()
        {
            const string fileName = "C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav";
            var wavReader =
                new WavSimpleReader(fileName);
            var settings = FileSettings.LoadSettings(fileName);
            settings.DictorName = Path.GetFileNameWithoutExtension(fileName);

            var writer = new RawDataFiles(Path.Combine(settings.FileMetaDataFolder, "raw_wav.sbd"));
            int sampleFrequency;
            writer.WriteShortArray(wavReader.ReadFileData(out sampleFrequency), "WAVE");
            settings.SampleFrequency = sampleFrequency;
            settings.Save();
            //---------------------------------------------------------
            var speechFileDataReader = new WavSimpleReader(fileName);
            var speechFileData = speechFileDataReader.ReadFileData(out sampleFrequency);

            Assert.AreEqual(settings.SampleFrequency, sampleFrequency);//test settings reading

            var filter1 = new Hpf(70.0f, settings.SampleFrequency);
            var sound = speechFileData.Select(x => x[0]).ToArray();
            var resSound = filter1.Filter(sound);
            Console.WriteLine("Sound successfully filtered by HPF with cut freq. 70 Hz");

            var searcher = new SpeechSearch(20, 0.04f, 0.9f, sampleFrequency);
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

            //-------------------------------------------------

            var lpc = new LinearPredictCoefficient
            {
                SamleFrequency = settings.SampleFrequency,
                UsedNumberOfCoeficients = 10,
                UsedAcfWindowSizeTime = 0.04,
                Overlapping = 0.9,
                UsedWindowType = WindowFunctions.WindowType.Blackman
            };

            double[][] res;
            lpc.GetLpcImage(ref sound, out res, settings.SpeechStartPosition, settings.SpeechEndPosition);
            Image img = new Bitmap(res.Length, 256);
            DrawLpcMatrix(ref res, ref img);
            img.Save("C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav.files\\test.png");
        }

      //  [TestMethod]
        public void TestArcCalculations()
        {
            const string fileName = "C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav";
            var wavReader =
                new WavSimpleReader(fileName);
            var settings = FileSettings.LoadSettings(fileName);
            settings.DictorName = Path.GetFileNameWithoutExtension(fileName);

            var writer = new RawDataFiles(Path.Combine(settings.FileMetaDataFolder, "raw_wav.sbd"));
            int sampleFrequency;
            writer.WriteShortArray(wavReader.ReadFileData(out sampleFrequency), "WAVE");
            settings.SampleFrequency = sampleFrequency;
            settings.Save();
            //---------------------------------------------------------
            var speechFileDataReader = new WavSimpleReader(fileName);
            var speechFileData = speechFileDataReader.ReadFileData(out sampleFrequency);

            Assert.AreEqual(settings.SampleFrequency, sampleFrequency);//test settings reading

            var filter1 = new Hpf(70.0f, settings.SampleFrequency);
            var sound = speechFileData.Select(x => x[0]).ToArray();
            var resSound = filter1.Filter(sound);
            Console.WriteLine("Sound successfully filtered by HPF with cut freq. 70 Hz");

            var searcher = new SpeechSearch(20, 0.04f, 0.9f, sampleFrequency);
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

            //-------------------------------------------------

            var lpc = new LinearPredictCoefficient
            {
                SamleFrequency = settings.SampleFrequency,
                UsedNumberOfCoeficients = 10,
                UsedAcfWindowSizeTime = 0.04,
                Overlapping = 0.9,
                UsedWindowType = WindowFunctions.WindowType.Blackman
            };

            double[][] res;
            lpc.GetArcImage(ref sound, out res, settings.SpeechStartPosition, settings.SpeechEndPosition, 128);
            Image img = new Bitmap(res.Length, 256);
            DrawLpcMatrix(ref res, ref img);
            img.Save("C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav.files\\test.png");
        }

       // [TestMethod]
        public void TestVtcCalculations()
        {
            const string fileName = "C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav";
            var wavReader =
                new WavSimpleReader(fileName);
            var settings = FileSettings.LoadSettings(fileName);
            settings.DictorName = Path.GetFileNameWithoutExtension(fileName);

            var writer = new RawDataFiles(Path.Combine(settings.FileMetaDataFolder, "raw_wav.sbd"));
            int sampleFrequency;
            writer.WriteShortArray(wavReader.ReadFileData(out sampleFrequency), "WAVE");
            settings.SampleFrequency = sampleFrequency;
            settings.Save();
            //---------------------------------------------------------
            var speechFileDataReader = new WavSimpleReader(fileName);
            var speechFileData = speechFileDataReader.ReadFileData(out sampleFrequency);

            Assert.AreEqual(settings.SampleFrequency, sampleFrequency);//test settings reading

            var filter1 = new Hpf(70.0f, settings.SampleFrequency);
            var sound = speechFileData.Select(x => x[0]).ToArray();
            var resSound = filter1.Filter(sound);
            Console.WriteLine("Sound successfully filtered by HPF with cut freq. 70 Hz");

            var searcher = new SpeechSearch(20, 0.04f, 0.9f, sampleFrequency);
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

            //-------------------------------------------------

            var lpc = new LinearPredictCoefficient
            {
                SamleFrequency = settings.SampleFrequency,
                UsedNumberOfCoeficients = 10,
                UsedAcfWindowSizeTime = 0.04,
                Overlapping = 0.9,
                UsedWindowType = WindowFunctions.WindowType.Blackman
            };

            double[][] res;
            lpc.GetArcVocalTractImage(ref sound, settings.SampleFrequency, 128, out res, settings.SpeechStartPosition, settings.SpeechEndPosition);
            Image img = new Bitmap(res.Length, 256);
            DrawLpcMatrix(ref res, ref img);
            img.Save("C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav.files\\test.png");
        }

        [TestMethod]
        public void TestMfccCalculations()
        {
            const string fileName = "C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav";
            var wavReader =
                new WavSimpleReader(fileName);
            var settings = FileSettings.LoadSettings(fileName);
            settings.DictorName = Path.GetFileNameWithoutExtension(fileName);

            var writer = new RawDataFiles(Path.Combine(settings.FileMetaDataFolder, "raw_wav.sbd"));
            int sampleFrequency;
            writer.WriteShortArray(wavReader.ReadFileData(out sampleFrequency), "WAVE");
            settings.SampleFrequency = sampleFrequency;
            settings.Save();
            //---------------------------------------------------------
            var speechFileDataReader = new WavSimpleReader(fileName);
            var speechFileData = speechFileDataReader.ReadFileData(out sampleFrequency);

            Assert.AreEqual(settings.SampleFrequency, sampleFrequency);//test settings reading

            var filter1 = new Hpf(70.0f, settings.SampleFrequency);
            var sound = speechFileData.Select(x => x[0]).ToArray();
            var resSound = filter1.Filter(sound);
            Console.WriteLine("Sound successfully filtered by HPF with cut freq. 70 Hz");

            var searcher = new SpeechSearch(20, 0.04f, 0.9f, sampleFrequency);
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

            //-------------------------------------------------

            var mfcc = new Cepstrum(13, 0.04, sampleFrequency, 0.9f);

            double[][] res;
            mfcc.GetCepstrogram(ref sound, WindowFunctions.WindowType.Blackman, settings.SpeechStartPosition, settings.SpeechEndPosition, out res);
            Image img = new Bitmap(res.Length, 256);
            DrawLpcMatrix(ref res, ref img);
            img.Save("C:\\Users\\box12_000\\YandexDisk\\Documents\\Проекты записей голоса\\Экспорт\\А\\ГРР1.wav.files\\test.png");
        }


        private static void DrawLpcMatrix(ref double[][] lpc, ref Image graphic)
        {
            using (Graphics.FromImage(graphic))
            {
                var max = lpc.Max(x => x.Max());
                var min = lpc.Min(x => x.Min());
                for (int i = 0; i < graphic.Width; i++)
                {
                    for (int j = 0; j < graphic.Height; j++)
                    {
                        int iTmp = (int)Math.Round((i / ((double)graphic.Width - 1)) * (lpc.Length - 1));
                        if (iTmp >= lpc.Length)
                        {
                            iTmp = lpc.Length - 1;
                        }
                        int jTmp = (int)Math.Round((j / ((double)graphic.Height - 1)) * (lpc[iTmp].Length - 1));
                        if (jTmp >= lpc[iTmp].Length)
                        {
                            jTmp = lpc[iTmp].Length - 1;
                        }
                        int currentVal = (int)Math.Round(((lpc[iTmp][jTmp] - min) / (Math.Abs(max) - min)) * 100.0);
                        var color = SetSpectrogrammPixelColor(currentVal);
                        ((Bitmap)graphic).SetPixel(i, j, color);
                    }
                }
            }
        }

        private static Color SetSpectrogrammPixelColor(int value)
        {
            int red = 128 + (value - 40) * 4;
            if (red < 0)
                red = 0;
            else if (red > 255)
                red = 255;
            int green = (255 - (100 - value) * 5);
            if (green < 0)
                green = 0;
            if (green > 255)
                green = 255;
            int blue = (value * 8);
            if (blue > 255)
                blue = 255;
            if (blue < 0)
                blue = 0;
            return Color.FromArgb(0xff, red, green, blue);
        }
    }
}
