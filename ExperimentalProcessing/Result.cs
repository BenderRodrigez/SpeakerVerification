using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ExperimentalProcessing
{
    class Result
    {
        private string[] _mansList = {"AIO", "IVA", "SAV", "VBG"};
        private string[] _womansList = {"BNA", "DAG", "GNA", "IGF", "VMV"};
        private float[] _signalData;
        private double[] _ethalonPitchData;
        private double[] _pitchData;
        private double[] _distortionData;
        private int _gender;
        public string FileName { get; set; }
        public string DictorName { get; set; }
        public string Phrase { get; set; }

        public float[] SignalData
        {
            get { return _signalData; }
            set
            {
                _signalData = value;
                InsertSignal(value);
            }
        }

        private string Signal { get; set; }

        public double[] EthalonPitchData
        {
            get { return _ethalonPitchData; }
            set
            {
                _ethalonPitchData = value;
                InsertEthalonPitch(value);
            }
        }

        private string EthalonPitch { get; set; }

        public double[] PitchData
        {
            get { return _pitchData; }
            set
            {
                _pitchData = value;
                InsertPitch(value);
            }
        }

        private string Pitch { get; set; }

        public double[] DistortionData
        {
            get { return _distortionData; }
            set
            {
                _distortionData = value;
                InsertDistortion(value);
            }
        }

        private string Distortion { get; set; }

        public double SmallErrorsRate { get; set; }
        public double BigErrorsRate { get; set; }
        public double VoicedSpeechDetectorErrorRate { get; set; }

        public int SampleRate { get; set; }
        public bool IsPhoneChanel { get; set; }
        public bool IsNoised { get; set; }
        public double SignalToNoiseRaito { get; set; }

        private void InsertSignal(float[] signal)
        {
            var buffer = new byte[signal.Length*sizeof(float)];
            using (var writer = new BinaryWriter(new MemoryStream(buffer)))
            {
                foreach (var f in signal)
                {
                    writer.Write(f);
                }
            }

            Signal = Convert.ToBase64String(buffer);
        }

        private void InsertEthalonPitch(double[] ethalonPitch)
        {
            var buffer = new byte[ethalonPitch.Length * sizeof(double)];
            using (var writer = new BinaryWriter(new MemoryStream(buffer)))
            {
                foreach (var f in ethalonPitch)
                {
                    writer.Write(f);
                }
            }

            EthalonPitch = Convert.ToBase64String(buffer);
        }

        private void InsertPitch(double[] pitch)
        {
            var buffer = new byte[pitch.Length * sizeof(double)];
            using (var writer = new BinaryWriter(new MemoryStream(buffer)))
            {
                foreach (var f in pitch)
                {
                    writer.Write(f);
                }
            }

            Pitch = Convert.ToBase64String(buffer);
        }

        private void InsertDistortion(double[] distortion)
        {
            var buffer = new byte[distortion.Length * sizeof(double)];
            using (var writer = new BinaryWriter(new MemoryStream(buffer)))
            {
                foreach (var f in distortion)
                {
                    writer.Write(f);
                }
            }

            Distortion = Convert.ToBase64String(buffer);
        }

        public static void MakeReport()
        {
            var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
            var database = client.GetDatabase("experiments");
            var db = database.GetCollection<BsonDocument>("PitchDetection");

            var totalExperiments = db.Find(new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument())).ToList();

            var totalExperimentsWithoutManiulations =
                totalExperiments.Where(
                    document => !(document["IsNoised"].AsBoolean || document["IsPhoneChanel"].AsBoolean)).ToList();

            var experimentsWithNoise = totalExperiments.Where(document => document["IsNoised"].AsBoolean).ToList();

            var experimentsWithPhoneChanel =
                totalExperiments.Where(document => document["IsPhoneChanel"].AsBoolean).ToList();

            var totalGenderStatistic =
                GetReportStringFromGroups(
                    totalExperimentsWithoutManiulations.GroupBy(x => x["Gender"].AsInt32 == 1 ? "М" : "Ж")
                        .OrderBy(x => x.Key));

            var noisedChanelGenderStatistic =
                experimentsWithNoise.GroupBy(x => SnrValueRounding(x["SignalToNoiseRaito"].AsDouble))
                    .OrderBy(x => x.Key)
                    .Select(x =>
                        new Tuple<int, List<string>>(x.Key,
                            GetReportStringFromGroups(
                                x.GroupBy(doc => doc["Gender"].AsInt32 == 1 ? "М" : "Ж").OrderBy(doc => doc.Key))))
                    .ToList();

            var phoneChanelGenderStatistic =
                GetReportStringFromGroups(
                    experimentsWithPhoneChanel.GroupBy(x => x["Gender"].AsInt32 == 1 ? "М" : "Ж").OrderBy(x => x.Key));

            var totalPhrasesStatistic =
                GetReportStringFromGroups(
                    totalExperimentsWithoutManiulations.GroupBy(x => x["Phrase"].AsString.ToUpper()).OrderBy(x => x.Key));

            var noisedChanelPhrasesStatistic =
                experimentsWithNoise.GroupBy(x => SnrValueRounding(x["SignalToNoiseRaito"].AsDouble))
                    .OrderBy(x => x.Key)
                    .Select(x =>
                        new Tuple<int, List<string>>(x.Key,
                            GetReportStringFromGroups(
                                x.GroupBy(doc => doc["Phrase"].AsString.ToUpper()).OrderBy(doc => doc.Key)))).ToList();

            var phoneChanelPhrasesStatistic =
                GetReportStringFromGroups(
                    experimentsWithPhoneChanel.GroupBy(x => x["Phrase"].AsString.ToUpper()).OrderBy(x => x.Key));

            var totalNoisedChanelStatistic =
                    experimentsWithNoise.GroupBy(x => SnrValueRounding(x["SignalToNoiseRaito"].AsDouble))
                        .OrderBy(x => x.Key).ToList();
                
            using (var writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"report.csv")))
            {
                writer.WriteLine("Статистика по полу (в н.у.)");
                writer.WriteLine("Ключ;% ошибок Т/НТ;% больших ошибок;% малых ошибок");
                totalGenderStatistic.ForEach(x=> writer.WriteLine(x));
                writer.WriteLine();
                noisedChanelGenderStatistic.ForEach(x =>
                {
                    writer.WriteLine("Статистика пополу (в зашумлённом канале {0} дБ)", x.Item1);
                    writer.WriteLine("Ключ;% ошибок Т/НТ;% больших ошибок;% малых ошибок");
                    x.Item2.ForEach(doc => writer.WriteLine(doc));
                });
                writer.WriteLine();
                writer.WriteLine("Статистика по полу (в телефонном канале)");
                writer.WriteLine("Ключ;% ошибок Т/НТ;% больших ошибок;% малых ошибок");
                phoneChanelGenderStatistic.ForEach(x => writer.WriteLine(x));
                writer.WriteLine();
                writer.WriteLine("Статистика по фразе (в н.у.)");
                writer.WriteLine("Ключ;% ошибок Т/НТ;% больших ошибок;% малых ошибок");
                totalPhrasesStatistic.ForEach(x => writer.WriteLine(x));
                writer.WriteLine();
                noisedChanelPhrasesStatistic.ForEach(x =>
                {
                    writer.WriteLine("Статистика по фразе (в зашумлённом канале {0} дБ)", x.Item1);
                    writer.WriteLine("Ключ;% ошибок Т/НТ;% больших ошибок;% малых ошибок");
                    x.Item2.ForEach(doc => writer.WriteLine(doc));
                });
                writer.WriteLine();
                writer.WriteLine("Статистика по фразе (в телефонном канале)");
                writer.WriteLine("Ключ;% ошибок Т/НТ;% больших ошибок;% малых ошибок");
                phoneChanelPhrasesStatistic.ForEach(x => writer.WriteLine(x));
                writer.WriteLine();
                writer.WriteLine("Статистика по всем фразам (в н.у.)");
                writer.WriteLine("Ключ;% ошибок Т/НТ;% больших ошибок;% малых ошибок");
                writer.WriteLine("{0};{1};{2};{3}", "",
                    totalExperimentsWithoutManiulations.Average(doc => doc["VoicedSpeechDetectorErrorRate"].AsDouble),
                    totalExperimentsWithoutManiulations.Average(doc => doc["BigErrorsRate"].AsDouble),
                    totalExperimentsWithoutManiulations.Average(doc => doc["SmallErrorsRate"].AsDouble));
                writer.WriteLine();
                writer.WriteLine("Статистика по всем фразам (в зашумлённом канале)");
                writer.WriteLine("Ключ;% ошибок Т/НТ;% больших ошибок;% малых ошибок");
                totalNoisedChanelStatistic.ForEach(x => writer.WriteLine("{0};{1};{2};{3}", x.Key,
                    x.Average(doc => doc["VoicedSpeechDetectorErrorRate"].AsDouble),
                    x.Average(doc => doc["BigErrorsRate"].AsDouble),
                    x.Average(doc => doc["SmallErrorsRate"].AsDouble)));
                writer.WriteLine();
                writer.WriteLine("Статистика по всем фразам (в телефонном канале)");
                writer.WriteLine("Ключ;% ошибок Т/НТ;% больших ошибок;% малых ошибок");
                writer.WriteLine("{0};{1};{2};{3}", "",
                    experimentsWithPhoneChanel.Average(doc => doc["VoicedSpeechDetectorErrorRate"].AsDouble),
                    experimentsWithPhoneChanel.Average(doc => doc["BigErrorsRate"].AsDouble),
                    experimentsWithPhoneChanel.Average(doc => doc["SmallErrorsRate"].AsDouble));
            }
        }

        private static int SnrValueRounding(double value)
        {
            if (value < -2.5)
            {
                return -5;
            }
            if(value < 5)
            {
                return 0;
            }
            if(value < 15)
            {
                return 10;
            }
            if(value < 25)
            {
                return 20;
            }

            return 30;
        }

        private static List<string> GetReportStringFromGroups(IEnumerable<IGrouping<string, BsonDocument>> document)
        {
            return document.Select(
                x =>
                    string.Format("{0};{1};{2};{3}", x.Key,
                        x.Average(doc => doc["VoicedSpeechDetectorErrorRate"].AsDouble),
                        x.Average(doc => doc["BigErrorsRate"].AsDouble),
                        x.Average(doc => doc["SmallErrorsRate"].AsDouble))).ToList();
        }

        public static Result RestoreDocument(BsonDocument document)
        {
            var result = new Result
            {
                FileName = document["FileName"].AsString,
                DictorName = document["DictorName"].AsString,
                Phrase = document["Phrase"].AsString,
                Signal = document["Signal"].AsString,
                EthalonPitch = document["EthalonPitch"].AsString,
                Pitch = document["Pitch"].AsString,
                Distortion = document["Distortion"].AsString,
                SmallErrorsRate = document["SmallErrorsRate"].AsDouble,
                BigErrorsRate = document["BigErrorsRate"].AsDouble,
                VoicedSpeechDetectorErrorRate = document["VoicedSpeechDetectorErrorRate"].AsDouble,
                SampleRate = document["SampleRate"].AsInt32,
                IsPhoneChanel = document["IsPhoneChanel"].AsBoolean,
                IsNoised = document["IsNoised"].AsBoolean,
                _gender = document["Gender"].AsInt32
            };
            result.SignalData = RestoreSignal(result.Signal);
            result.DistortionData = RestoreCalculations(result.Distortion);
            result.EthalonPitchData = RestoreCalculations(result.EthalonPitch);
            result.PitchData = RestoreCalculations(result.Pitch);


            result.SignalToNoiseRaito = result.IsNoised ? document["SignalToNoiseRaito"].AsDouble : 0.0;

            return result;
        }

        private static float[] RestoreSignal(string encodedValue)
        {
            var signal = new List<float>();
            var buffer = Convert.FromBase64String(encodedValue);
            using (var reader = new BinaryReader(new MemoryStream(buffer)))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    signal.Add(reader.ReadSingle());
                }
            }
            return signal.ToArray();
        }

        private static double[] RestoreCalculations(string encodedValue)
        {
            var signal = new List<double>();
            var buffer = Convert.FromBase64String(encodedValue);
            using (var reader = new BinaryReader(new MemoryStream(buffer)))
            {
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    signal.Add(reader.ReadDouble());
                }
            }
            return signal.ToArray();
        }

        public async void SaveToDb()
        {
            try
            {
                _gender = (_mansList.Any(x => string.Equals(x, DictorName, StringComparison.CurrentCultureIgnoreCase))) ? 1 : 2;
                var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
                var database = client.GetDatabase("experiments");

                var newDocument = new BsonDocument
                {
                    {"FileName", FileName},
                    {"DictorName", DictorName},
                    {"Gender", _gender},
                    {"Phrase", Phrase},
                    {"Signal", Signal},
                    {"EthalonPitch", EthalonPitch},
                    {"Pitch", Pitch},
                    {"Distortion", Distortion},
                    {"SmallErrorsRate", SmallErrorsRate},
                    {"BigErrorsRate", BigErrorsRate},
                    {"VoicedSpeechDetectorErrorRate", VoicedSpeechDetectorErrorRate},
                    {"SampleRate", SampleRate},
                    {"IsPhoneChanel", IsPhoneChanel},
                    {"IsNoised", IsNoised},
                    {"SignalToNoiseRaito", IsNoised?SignalToNoiseRaito:0.0}
                };

                await database.GetCollection<BsonDocument>("PitchDetection").InsertOneAsync(newDocument);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
