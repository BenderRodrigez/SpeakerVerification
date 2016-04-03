using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using ScatterSeries = OxyPlot.Series.ScatterSeries;

namespace SpeakerVerificationExperiments
{
    internal class ReportElement
    {
        private static readonly string[] MansList = {"ГРР", "ГРК"};

        public string DictorName { get; set; }
        public string Phrase { get; set; }
        public string FileName { get; set; }

        public string CodeBookDictorName { get; set; }
        public string CodeBookPhrase { get; set; }
        public string CodeBookFileName { get; set; }

        public double DistortionEnergy { get; set; }
        public double[] DistortionSignal { get; set; }
        public string FeatureType { get; set; }

        public string GetBase64DistortionSignal()
        {
            var buffer = new byte[DistortionSignal.Length*sizeof (double)];
            using (var writer = new BinaryWriter(new MemoryStream(buffer)))
            {
                foreach (var f in DistortionSignal)
                {
                    writer.Write(f);
                }
            }

            return Convert.ToBase64String(buffer);
        }

        public async void SaveToDb()
        {
            try
            {
                var gender = (MansList.Any(x => string.Equals(x, DictorName, StringComparison.CurrentCultureIgnoreCase))) ? 1 : 2;
                var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
                var database = client.GetDatabase("experiments");

                var newDocument = new BsonDocument
                {
                    {"FileName", FileName},
                    {"DictorName", DictorName},
                    {"Gender", gender},
                    {"Phrase", Phrase},
                    {"CodeBookDictorName", CodeBookDictorName},
                    {"CodeBookPhrase", CodeBookPhrase},
                    {"CodeBookFileName", CodeBookFileName},
                    {"DistortionEnergy", DistortionEnergy},
                    {"DistortionSignal", GetBase64DistortionSignal()},
                    {"FeatureType", FeatureType}
                };

                await database.GetCollection<BsonDocument>("Verification").InsertOneAsync(newDocument);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void MakeReport()
        {
            var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
            var database = client.GetDatabase("experiments");
            var db = database.GetCollection<BsonDocument>("Verification");

            var totalExperiments = db.Find(new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument())).ToList();

            var pitchExperiments = totalExperiments.Where(x => x["FeatureType"].AsString == "pitch").Select(x => x).ToList();
            var pitchDeltaExperiments = totalExperiments.Where(x => x["FeatureType"].AsString == "pitchDelta").Select(x => x).ToList();
            var lpcExperiments = totalExperiments.Where(x => x["FeatureType"].AsString == "lpc").Select(x => x).ToList();
            var lpcDeltaExperiments = totalExperiments.Where(x => x["FeatureType"].AsString == "lpcDelta").Select(x => x).ToList();
            var pitchLpcExperiments = totalExperiments.Where(x => x["FeatureType"].AsString == "pitchLpc").Select(x => x).ToList();
            var pitchLpcDeltaExperiments = totalExperiments.Where(x => x["FeatureType"].AsString == "pitchLpcDelta").Select(x => x).ToList();

            using (var writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "report2.csv")))
            {
                writer.WriteLine("pitchExperiments");
                foreach (var line in GetReportFromExperiments(pitchExperiments, "pitchExperiments"))
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();

                writer.WriteLine("pitchDeltaExperiments");
                foreach (var line in GetReportFromExperiments(pitchDeltaExperiments, "pitchDeltaExperiments"))
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();

                writer.WriteLine("lpcExperiments");
                foreach (var line in GetReportFromExperiments(lpcExperiments, "lpcExperiments"))
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();

                writer.WriteLine("lpcDeltaExperiments");
                foreach (var line in GetReportFromExperiments(lpcDeltaExperiments, "lpcDeltaExperiments"))
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();

                writer.WriteLine("pitchLpcExperiments");
                foreach (var line in GetReportFromExperiments(pitchLpcExperiments, "pitchLpcExperiments"))
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();

                writer.WriteLine("pitchLpcDeltaExperiments");
                foreach (var line in GetReportFromExperiments(pitchLpcDeltaExperiments, "pitchLpcDeltaExperiments"))
                {
                    writer.WriteLine(line);
                }
            }
        }

        private static string[] GetReportFromExperiments(List<BsonDocument> experiments, string experimentType)
        {
            var phrasesSummary = experiments.GroupBy(x => x["CodeBookPhrase"].AsString);
            var dictorSummary = experiments.GroupBy(x => x["CodeBookDictorName"].AsString);
            var genderSummary = experiments.GroupBy(x => MansList.Contains(x["CodeBookDictorName"].AsString) ? "1" : "2");

            var report = new List<string> {"ФРАЗЫ"};
            var enumerable = phrasesSummary as IGrouping<string, BsonDocument>[] ?? phrasesSummary.ToArray();
            report.AddRange(GetSubReportFromExperiments(enumerable));
            MakeGraphs(enumerable, "phrases_" + experimentType);
            report.Add("ДИКТОР");
            var summary = dictorSummary as IGrouping<string, BsonDocument>[] ?? dictorSummary.ToArray();
            report.AddRange(GetSubReportFromExperiments(summary));
            MakeGraphs(summary, "dictors_" + experimentType);
            report.Add("ПОЛ");
            var genderSummary1 = genderSummary as IGrouping<string, BsonDocument>[] ?? genderSummary.ToArray();
            report.AddRange(GetSubReportFromExperiments(genderSummary1));
            MakeGraphs(genderSummary1, "gender_" + experimentType);

            report.Add("ОБЩАЯ");
            var maxOwnEnergy =
                    experiments.Where(x => x["CodeBookDictorName"].AsString == x["DictorName"])
                        .Max(x => x["DistortionEnergy"].AsDouble);
            var minForeignEnergy = experiments.Where(x => x["CodeBookDictorName"].AsString != x["DictorName"])
                .Min(x => x["DistortionEnergy"].AsDouble);
            var averageOwnEnergy =
                experiments.Where(x => x["CodeBookDictorName"].AsString == x["DictorName"])
                    .Average(x => x["DistortionEnergy"].AsDouble);
            var averageFereignEnergy = experiments.Where(x => x["CodeBookDictorName"].AsString != x["DictorName"])
                .Average(x => x["DistortionEnergy"].AsDouble);

            report.Add("Key;AV_OWN;AV_FOR;MIN_FOR;MAX_OWN");
            report.Add(string.Format("{0};{1};{2};{3};{4}", string.Empty, averageOwnEnergy, averageFereignEnergy,
                minForeignEnergy, maxOwnEnergy));
            return report.ToArray();
        }

        private static void MakeGraphs(IEnumerable<IGrouping<string, BsonDocument>> experiments, string featureGrouping)
        {
            var plot = GetPlot(experiments);

            plot.SaveBitmap(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "report_" + featureGrouping + ".png"), 1000, 500, OxyColors.Transparent);
        }

        private static PlotView GetPlot(IEnumerable<IGrouping<string, BsonDocument>> experiments)
        {
            var plot = new PlotView();
            var plotModel = new PlotModel();
            var axes = new OxyPlot.Axes.CategoryAxis();
            axes.ActualLabels.AddRange(experiments.Select(x => x.Key));
            axes.Position = AxisPosition.Bottom;
            plotModel.Axes.Add(axes);
            plotModel.PlotType = PlotType.XY;
            var selfVerif = new ScatterSeries();
            selfVerif.MarkerType = MarkerType.Circle;
            var foreigVierif = new ScatterSeries();
            foreigVierif.MarkerType = MarkerType.Triangle;
            var pos = 0;
            var maxYValue = double.NegativeInfinity;
            foreach (var experiment in experiments)
            {
                foreach (var rep in experiment.Where(exp => exp["CodeBookPhrase"].AsString == exp["Phrase"].AsString))
                {
                    if (rep["CodeBookDictorName"].AsString == rep["DictorName"])
                    {
                        selfVerif.Points.Add(new ScatterPoint(pos-0.2, rep["DistortionEnergy"].AsDouble, tag: experiment.Key));
                        if (rep["DistortionEnergy"].AsDouble > maxYValue)
                        {
                            maxYValue = rep["DistortionEnergy"].AsDouble;
                        }
                    }
                    else
                    {
                        foreigVierif.Points.Add(new ScatterPoint(pos+0.2, rep["DistortionEnergy"].AsDouble, tag: experiment.Key));
                    }
                }
                pos++;
            }
            var yAxes = new LinearAxis
            {
                AbsoluteMaximum = maxYValue > 0?maxYValue*1.3:20000,
                AbsoluteMinimum = 0.0,
                Position = AxisPosition.Left
            };
            plotModel.Axes.Add(yAxes);
            plotModel.Series.Add(foreigVierif);
            plotModel.Series.Add(selfVerif);
            plot.Model = plotModel;
            return plot;
        }

        private static string[] GetSubReportFromExperiments(IEnumerable<IGrouping<string, BsonDocument>> experiments)
        {
            var report = new List<string> {"Key;AV_OWN;AV_FOR;MIN_FOR;MAX_OWN"};
            foreach (var group in experiments)
            {
                var maxOwnEnergy =
                    group.Where(x => x["CodeBookDictorName"].AsString == x["DictorName"])
                        .Max(x => x["DistortionEnergy"].AsDouble);
                var minForeignEnergy = group.Where(x => x["CodeBookDictorName"].AsString != x["DictorName"])
                    .Min(x => x["DistortionEnergy"].AsDouble);
                var averageOwnEnergy =
                    group.Where(x => x["CodeBookDictorName"].AsString == x["DictorName"])
                        .Average(x => x["DistortionEnergy"].AsDouble);
                var averageFereignEnergy = group.Where(x => x["CodeBookDictorName"].AsString != x["DictorName"])
                    .Average(x => x["DistortionEnergy"].AsDouble);

                report.Add(string.Format("{0};{1};{2};{3};{4}", group.Key, averageOwnEnergy, averageFereignEnergy,
                    minForeignEnergy, maxOwnEnergy));
            }
            return report.ToArray();
        }
    }
}
