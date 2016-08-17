using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using FLS.MembershipFunctions;
using HelpersLibrary;
using MongoDB.Bson;
using MongoDB.Driver;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using CategoryAxis = OxyPlot.Axes.CategoryAxis;
using LinearAxis = OxyPlot.Axes.LinearAxis;
using ScatterSeries = OxyPlot.Series.ScatterSeries;

namespace SpeakerVerificationExperiments
{
    internal class ReportElement
    {
        private static readonly string[] MansList = {"ГРР", "ГРК", "НРМ", "ЧНА", "ОКА", "КДО", "КЕА"};

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

                await database.GetCollection<BsonDocument>("Verification_Phone").InsertOneAsync(newDocument);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void MakeVerificationReport()
        {
            var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
            var database = client.GetDatabase("experiments");
            var db = database.GetCollection<BsonDocument>("Verification_Phone");

            using (var writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "report3.csv")))
            {

                writer.WriteLine("pitchExperiments");
                foreach (var line in GetVerificationReportFromExperiments(db.Find(new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument(new BsonElement("FeatureType", "pitch")))).ToList(), "pitchExperiments"))
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();

                writer.WriteLine("pitchDeltaExperiments");
                foreach (var line in GetVerificationReportFromExperiments(db.Find(new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument(new BsonElement("FeatureType", "pitchDelta")))).ToList(), "pitchDeltaExperiments"))
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();

                writer.WriteLine("lpcExperiments");
                foreach (var line in GetVerificationReportFromExperiments(db.Find(new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument(new BsonElement("FeatureType", "lpc")))).ToList(), "lpcExperiments"))
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();

                writer.WriteLine("lpcDeltaExperiments");
                foreach (var line in GetVerificationReportFromExperiments(db.Find(new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument(new BsonElement("FeatureType", "lpcDelta")))).ToList(), "lpcDeltaExperiments"))
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();

                writer.WriteLine("pitchLpcExperiments");
                foreach (var line in GetVerificationReportFromExperiments(db.Find(new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument(new BsonElement("FeatureType", "pitchLpc")))).ToList(), "pitchLpcExperiments"))
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();

                writer.WriteLine("pitchLpcDeltaExperiments");
                foreach (var line in GetVerificationReportFromExperiments(db.Find(new BsonDocumentFilterDefinition<BsonDocument>(new BsonDocument(new BsonElement("FeatureType", "pitchLpcDelta")))).ToList(), "pitchLpcDeltaExperiments"))
                {
                    writer.WriteLine(line);
                }
            }
        }

        private static string[] GetVerificationSubreport(IEnumerable<IGrouping<string, BsonDocument>> experiments, string featureType)
        {
            var report = new List<string> { "KEY;TOTAL;ACCURACY;1CLASS;2CLASS;NO_SOLUTION" };
            foreach (var group in experiments)
            {
                var solutions = group.Select(x=> Verification(x, featureType));

                var total = group.Count();
                var correctSolution =
                    solutions.Count(x => (x.Item2 && x.Item1 == SolutionState.SameDictor) || (!x.Item2 && x.Item1 == SolutionState.ForeignDictor));
                var firstClassError = solutions.Count(x => (!x.Item2 && x.Item1 == SolutionState.SameDictor));
                var secondClassError = solutions.Count(x => (x.Item2 && x.Item1 == SolutionState.ForeignDictor));
                var notClearSolution = solutions.Count(x => x.Item1 == SolutionState.NoClearSolution);
                
                report.Add(string.Format("{0};{1};={2}/{1};={3}/{1};={4}/{1};={5}/{1}", group.Key, total, correctSolution, firstClassError, secondClassError, notClearSolution));
            }
            return report.ToArray();
        }

        private static Tuple<SolutionState, bool> Verification(BsonDocument document, string featureType)
        {
            var solver = new FuzzySolver();
//            var solver = new BordersSolver();

            if (featureType.IndexOf("pitch", StringComparison.InvariantCultureIgnoreCase) < 0)
            {
                solver.OwnSolutionFunction = new BellMembershipFunction("Own solution", 2.5, 3, 4.5);
                solver.ForeignSolutionFunction = new SShapedMembershipFunction("Foreign solution", 7.0, 3.5);
            }
            return new Tuple<SolutionState, bool>(solver.GetSolution(document["DistortionEnergy"].AsDouble),
                document["CodeBookDictorName"].AsString == document["DictorName"].AsString);
        }

        private static string[] GetVerificationReportFromExperiments(List<BsonDocument> experiments, string featureType)
        {
            var fuzzySolver = new FuzzySolver();

            if (featureType.IndexOf("pitch", StringComparison.InvariantCultureIgnoreCase) < 0)
            {
                fuzzySolver.OwnSolutionFunction = new BellMembershipFunction("Own solution", 2.5, 3, 4.5);
                fuzzySolver.ForeignSolutionFunction = new SShapedMembershipFunction("Foreign solution", 7.0, 3.5);
            }

            var correctSolution = 0;
            var firstClassError = 0;
            var secondClassError = 0;
            var notClearSolution = 0;
            var total = 0;
            var avgFirstErrorEnergy = 0.0;

            var phrasesSummary = experiments.GroupBy(x => x["CodeBookPhrase"].AsString);
            var dictorSummary = experiments.GroupBy(x => x["CodeBookDictorName"].AsString.ToUpper());
            var genderSummary = experiments.GroupBy(x => MansList.Contains(x["CodeBookDictorName"].AsString) ? "1" : "2");

            var report = new List<string> { "ФРАЗЫ" };
            var enumerable = phrasesSummary as IGrouping<string, BsonDocument>[] ?? phrasesSummary.OrderBy(x => x.Key).ToArray();
            report.AddRange(GetVerificationSubreport(enumerable, featureType));
            MakeGraphs(enumerable, "phrases_" + featureType);
            report.Add("ДИКТОР");
            var summary = dictorSummary as IGrouping<string, BsonDocument>[] ?? dictorSummary.OrderBy(x => x.Key).ToArray();
            report.AddRange(GetVerificationSubreport(summary, featureType));
            MakeGraphs(summary, "dictors_" + featureType);
            report.Add("ПОЛ");
            var genderSummary1 = genderSummary as IGrouping<string, BsonDocument>[] ?? genderSummary.OrderBy(x => x.Key).ToArray();
            report.AddRange(GetVerificationSubreport(genderSummary1, featureType));
            MakeGraphs(genderSummary1, "gender_" + featureType);

            report.Add("ОБЩАЯ");
            var ownExperiments = experiments.Where(x => x["CodeBookDictorName"].AsString == x["DictorName"]);
            var foreignExperiments = experiments.Where(x => x["CodeBookDictorName"].AsString != x["DictorName"]);

            ownExperiments.ToList().ForEach(x =>
            {
                total++;
                var solution = fuzzySolver.GetSolution(x["DistortionEnergy"].AsDouble);
                if (solution == SolutionState.SameDictor)
                {
                    correctSolution++;
                }
                else
                {
                    secondClassError++;
                }
            });

            foreignExperiments.ToList().ForEach(x =>
            {
                total++;
                var solution = fuzzySolver.GetSolution(x["DistortionEnergy"].AsDouble);
                if (solution == SolutionState.SameDictor)
                {
                    firstClassError++;
                    avgFirstErrorEnergy += x["DistortionEnergy"].AsDouble;
                }
                else if (solution == SolutionState.ForeignDictor)
                {
                    correctSolution++;
                }
                else
                {
                    notClearSolution++;
                }
            });
            report.Add("");
            report.Add("VERIFICATION");
            report.Add("TOTAL;CORRECT;1CLASS;2CLASS;NOTCLEAR;AVG_1ERROR");
            report.Add(string.Format("{0};={1}/{0};={2}/{0};={3}/{0};={4}/{0};={5}/{2}", total, correctSolution, firstClassError, secondClassError,
                notClearSolution, avgFirstErrorEnergy));

            return report.ToArray();
        }

        public static void MakeReport()
        {
            var client = new MongoClient(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);

            using (var writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "report2.csv")))
            {
                writer.WriteLine("pitchExperiments");
                var docs = client.GetDatabase("experiments").GetCollection<BsonDocument>("Verification_Phone").Find(
                    new BsonDocumentFilterDefinition<BsonDocument>(
                        new BsonDocument(new BsonElement("FeatureType", "pitch")))).ToList();
                var repo =
                    GetReportFromExperiments(docs, "pitchExperiments");
                foreach (var line in repo)
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();
                docs = null;
                repo = null;
                GC.Collect();
                writer.WriteLine("pitchDeltaExperiments");
                docs = client.GetDatabase("experiments").GetCollection<BsonDocument>("Verification_Phone").Find(
                    new BsonDocumentFilterDefinition<BsonDocument>(
                        new BsonDocument(new BsonElement("FeatureType", "pitchDelta")))).ToList();
                repo =
                    GetReportFromExperiments(docs, "pitchDeltaExperiments");
                foreach (var line in repo)
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();
                docs = null;
                repo = null;
                GC.Collect();
                docs = client.GetDatabase("experiments").GetCollection<BsonDocument>("Verification_Phone").Find(
                    new BsonDocumentFilterDefinition<BsonDocument>(
                        new BsonDocument(new BsonElement("FeatureType", "lpc")))).ToList();
                writer.WriteLine("lpcExperiments");
                repo =
                    GetReportFromExperiments(docs, "lpcExperiments");
                foreach (var line in repo)
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();
                docs = null;
                repo = null;
                GC.Collect();
                writer.WriteLine("lpcDeltaExperiments");
                docs = client.GetDatabase("experiments").GetCollection<BsonDocument>("Verification_Phone").Find(
                    new BsonDocumentFilterDefinition<BsonDocument>(
                        new BsonDocument(new BsonElement("FeatureType", "lpcDelta")))).ToList();
                repo =
                    GetReportFromExperiments(docs, "lpcDeltaExperiments");
                foreach (var line in repo)
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();
                repo = null;
                GC.Collect();
                writer.WriteLine("pitchLpcExperiments");
                docs = client.GetDatabase("experiments").GetCollection<BsonDocument>("Verification_Phone").Find(
                    new BsonDocumentFilterDefinition<BsonDocument>(
                        new BsonDocument(new BsonElement("FeatureType", "pitchLpc")))).ToList();
                repo =
                    GetReportFromExperiments(docs, "pitchLpcExperiments");
                foreach (var line in repo)
                {
                    writer.WriteLine(line);
                }
                writer.WriteLine();
                writer.WriteLine();
                docs = null;
                repo = null;
                GC.Collect();
                writer.WriteLine("pitchLpcDeltaExperiments");
                docs = client.GetDatabase("experiments").GetCollection<BsonDocument>("Verification_Phone").Find(
                    new BsonDocumentFilterDefinition<BsonDocument>(
                        new BsonDocument(new BsonElement("FeatureType", "pitchLpcDelta")))).ToList();
                repo =
                    GetReportFromExperiments(docs, "pitchLpcDeltaExperiments");
                foreach (var line in repo)
                {
                    writer.WriteLine(line);
                }
                repo = null;
                docs = null;
                GC.Collect();
            }
        }

        private static string[] GetReportFromExperiments(List<BsonDocument> experiments, string experimentType)
        {
            /*var banned = new[] {"БТМ", "БКИ", "НРМ", "ГМЗ", "ЧНА", "ОКА"};
            experiments =
                experiments.Where(
                    x =>
                        !(banned.Contains(x["CodeBookDictorName"].AsString) || banned.Contains(x["DictorName"].AsString)))
                    .ToList();*/

            var phrasesSummary = experiments.GroupBy(x => x["CodeBookPhrase"].AsString);
            var dictorSummary = experiments.GroupBy(x => x["CodeBookDictorName"].AsString.ToUpper());
            var genderSummary = experiments.GroupBy(x => MansList.Contains(x["CodeBookDictorName"].AsString) ? "1" : "2");

            var report = new List<string> {"ФРАЗЫ"};
            var enumerable = phrasesSummary as IGrouping<string, BsonDocument>[] ?? phrasesSummary.OrderBy(x=> x.Key).ToArray();
            report.AddRange(GetSubReportFromExperiments(enumerable));
            MakeGraphs(enumerable, "phrases_" + experimentType);
            report.Add("ДИКТОР");
            var summary = dictorSummary as IGrouping<string, BsonDocument>[] ?? dictorSummary.OrderBy(x => x.Key).ToArray();
            report.AddRange(GetSubReportFromExperiments(summary));
            MakeGraphs(summary, "dictors_" + experimentType);
            report.Add("ПОЛ");
            var genderSummary1 = genderSummary as IGrouping<string, BsonDocument>[] ?? genderSummary.OrderBy(x => x.Key).ToArray();
            report.AddRange(GetSubReportFromExperiments(genderSummary1));
            MakeGraphs(genderSummary1, "gender_" + experimentType);

            report.Add("ОБЩАЯ");
            var ownExperiments = experiments.Where(x => x["CodeBookDictorName"].AsString == x["DictorName"]);
            var foreignExperiments = experiments.Where(x => x["CodeBookDictorName"].AsString != x["DictorName"]);

            var maxOwnEnergy = ownExperiments.Max(x => x["DistortionEnergy"].AsDouble);
            var minForeignEnergy = foreignExperiments.Min(x => x["DistortionEnergy"].AsDouble);
            var averageOwnEnergy = ownExperiments.Average(x => x["DistortionEnergy"].AsDouble);
            var averageFereignEnergy = foreignExperiments.Average(x => x["DistortionEnergy"].AsDouble);

            var standartDeviationOwn = Math.Sqrt(experiments.Where(x => x["CodeBookDictorName"].AsString == x["DictorName"])
                .Sum(x => Math.Pow(x["DistortionEnergy"].AsDouble - averageOwnEnergy, 2))/ownExperiments.Count());

            var standartDeviationForeign = Math.Sqrt(experiments.Where(x => x["CodeBookDictorName"].AsString != x["DictorName"])
                .Sum(x => Math.Pow(x["DistortionEnergy"].AsDouble - averageFereignEnergy, 2)) / foreignExperiments.Count());

            var fuzzySolver = new FuzzySolver();
            var correctSolution = 0;
            var firstClassError = 0;
            var secondClassError = 0;
            var notClearSolution = 0;
            var total = 0;
            var avgFirstErrorEnergy = 0.0;

            ownExperiments.ToList().ForEach(x =>
            {
                total++;
                var solution = fuzzySolver.GetSolution(x["DistortionEnergy"].AsDouble);
                if (solution == SolutionState.SameDictor)
                {
                    correctSolution++;
                }
                else
                {
                    secondClassError++;
                }
            });

            foreignExperiments.ToList().ForEach(x =>
            {
                total++;
                var solution = fuzzySolver.GetSolution(x["DistortionEnergy"].AsDouble);
                if (solution == SolutionState.SameDictor)
                {
                    firstClassError++;
                    avgFirstErrorEnergy += x["DistortionEnergy"].AsDouble;
                }
                else if (solution == SolutionState.ForeignDictor)
                {
                    correctSolution++;
                }
                else
                {
                    notClearSolution++;
                }
            });

            report.Add("Key;AV_OWN;AV_FOR;MIN_FOR;MAX_OWN;DEV_OWN;DEV_FOR");
            report.Add(string.Format("{0};{1};{2};{3};{4};{5};{6}", string.Empty, averageOwnEnergy, averageFereignEnergy,
                minForeignEnergy, maxOwnEnergy, standartDeviationOwn, standartDeviationForeign));
            report.Add("");
            report.Add("VERIFICATION");
            report.Add("TOTAL;CORRECT;1CLASS;2CLASS;NOTCLEAR;AVG_1ERROR");
            report.Add(string.Format("{0};{1};{2};{3};{4};{5}", total, correctSolution, firstClassError, secondClassError,
                notClearSolution, avgFirstErrorEnergy));

            return report.ToArray();
        }

        private static void MakeGraphs(IEnumerable<IGrouping<string, BsonDocument>> experiments, string featureGrouping)
        {
            var plot = GetPlot(experiments);

            plot.SaveBitmap(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),"Reports",
                    "report_" + featureGrouping + ".png"), 1000, 500, OxyColors.Transparent);
        }

        private static PlotView GetPlot(IEnumerable<IGrouping<string, BsonDocument>> experiments)
        {
            var plot = new PlotView();
            var plotModel = new PlotModel();
            var axes = new CategoryAxis();
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
