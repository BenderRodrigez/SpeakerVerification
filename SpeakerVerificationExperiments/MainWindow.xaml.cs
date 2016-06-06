using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using FLS;
using HelpersLibrary;
using HelpersLibrary.DspAlgorithms;
using HelpersLibrary.LearningAlgorithms;
using NeuronDotNet.Core;
using NeuronDotNet.Core.Initializers;
using NeuronDotNet.Core.SOM;
using NeuronDotNet.Core.SOM.NeighborhoodFunctions;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SpeakerVerificationExperiments.Annotations;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace SpeakerVerificationExperiments
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow: INotifyPropertyChanged
    {
        public PlotModel TrainDataModel { get; set; }
        public PlotModel TestDataModel { get; set; }
        public PlotModel CodeBookModel { get; set; }
        public Cursor WindowCursor { get; private set; }
        private VectorQuantization VqCodeBook { get; set; }
        public bool IsTestButtonEnabled { get; private set; }

        public MainWindow()
        {
            InitializeComponent();

            TrainDataModel = new PlotModel {Title = "Образец для обучения", TitleFontSize = 10.0};
            TrainDataModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1));
            var linearColorAxis = new LinearColorAxis
            {
                HighColor = OxyColors.White,
                LowColor = OxyColors.Black,
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Hot(200),
            };
            TrainDataModel.Axes.Add(linearColorAxis);
            OnPropertyChanged("TrainDataModel");

            TestDataModel = new PlotModel {Title = "Тестовый образец", TitleFontSize = 10.0};
            TestDataModel.Series.Add(new FunctionSeries(Math.Sin, 0, 10, 0.1));
            var linearColorAxisTest = new LinearColorAxis
            {
                HighColor = OxyColors.White,
                LowColor = OxyColors.Black,
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Hot(200),
            };
            TestDataModel.Axes.Add(linearColorAxisTest);
            OnPropertyChanged("TestDataModel");

            CodeBookModel = new PlotModel {Title = "Кодовая книга", TitleFontSize = 10.0};
            CodeBookModel.Series.Add(new FunctionSeries(Math.Acos, 0, 10, 0.1));
            var linearColorAxisCodeBook = new LinearColorAxis
            {
                HighColor = OxyColors.White,
                LowColor = OxyColors.Black,
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Hot(200),
            };
            CodeBookModel.Axes.Add(linearColorAxisCodeBook);
            AddDeltaToFeatures = false;
            IsTestButtonEnabled = false;
            OnPropertyChanged("CodeBookModel");
        }

        private enum SelectedFeature
        {
            Pitch,
            Lpc,
            Combine
        }

        private enum UsedQuatizationAlgorithm
        {
            Lbg,
            Kohonen
        }

        private const float Overlaping = 0.95f;
        private const float AnalysisInterval = 0.04f;
        private SelectedFeature _selectedFeature = SelectedFeature.Pitch;
        public bool AddDeltaToFeatures { get; private set; }
        private UsedQuatizationAlgorithm _uesdAlgorithm = UsedQuatizationAlgorithm.Lbg;
        private KohonenNetwork _network;

        private void SelectLpcModeRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            _selectedFeature = SelectedFeature.Lpc;
        }

        private void SelectPitchModeRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            _selectedFeature = SelectedFeature.Pitch;
        }

        private void SelectCombinedModeRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            _selectedFeature = SelectedFeature.Combine;
        }

        private void GenerateReportButton_OnClick(object sender, RoutedEventArgs e)
        {
            var selectFolder = new FolderBrowserDialog {ShowNewFolderButton = false};
            if (selectFolder.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var subFolders = Directory.GetDirectories(selectFolder.SelectedPath);
                if (subFolders.Any(x => Directory.GetFiles(x, "*.wav").Any()))
                {
                    var pitchImagesDictionary = new Dictionary<string, double[][]>();
                    var lpcImagesDictionary = new Dictionary<string, double[][]>();
                    var pitchDeltaImagesDictionary = new Dictionary<string, double[][]>();
                    var lpcDeltaImagesDictionary = new Dictionary<string, double[][]>();
                    var pitchLpcImagesDictionary = new Dictionary<string, double[][]>();
                    var pitchLpcDeltaImagesDictionary = new Dictionary<string, double[][]>();

                    foreach (var dir in subFolders)
                    {
                        foreach (var fileName in Directory.GetFiles(dir, "*.wav"))
                        {
                            int sampleRate;
                            var inputFile = FileReader.ReadFileNormalized(fileName, out sampleRate);

                            var tonalSpeechSelector = new TonalSpeechSelector();
                            tonalSpeechSelector.InitData(inputFile, AnalysisInterval, Overlaping, sampleRate);

                            var speechMarks = tonalSpeechSelector.GetTonalSpeechMarks();

                            var pitchImage = GetPitchImage(inputFile, sampleRate, speechMarks);
                            pitchImagesDictionary.Add(fileName, pitchImage);

                            var pitchWithDelta = DeltaGenerator.AddDelta(pitchImage);
                            pitchDeltaImagesDictionary.Add(fileName, pitchWithDelta);

                            var lpcImage = GetLpcImage(inputFile, sampleRate, speechMarks);
                            lpcImagesDictionary.Add(fileName, lpcImage);

                            var lpcWithDelta = DeltaGenerator.AddDelta(lpcImage);
                            lpcDeltaImagesDictionary.Add(fileName, lpcWithDelta);

                            var pitchLpcImage = GetCombineImage(inputFile, sampleRate, speechMarks);
                            pitchLpcImagesDictionary.Add(fileName, pitchLpcImage);

                            var pitchLpcWithDelta = DeltaGenerator.AddDelta(pitchImage);
                            pitchLpcDeltaImagesDictionary.Add(fileName, pitchLpcWithDelta);
                        }
                    }
                    var factory = new TaskFactory();
                    factory.StartNew(() => ProcessSamples(pitchImagesDictionary, "pitch"));
                    factory.StartNew(() => ProcessSamples(pitchDeltaImagesDictionary, "pitchDelta"));
                    factory.StartNew(() => ProcessSamples(lpcImagesDictionary, "lpc"));
                    factory.StartNew(() => ProcessSamples(lpcDeltaImagesDictionary, "lpcDelta"));
                    factory.StartNew(() => ProcessSamples(pitchLpcImagesDictionary, "pitchLpc"));
                    factory.StartNew(() => ProcessSamples(pitchLpcDeltaImagesDictionary, "pitchLpcDelta"));
                    return;
                }
                //wrong
                System.Windows.MessageBox.Show("Выберете папку, содержащую корректно оформленную БД записей речи.",
                    "Некорректная БД образцов.", MessageBoxButton.OK);
            }
        }

        private KohonenNetwork ProvideKohonenNetwork(double[][] trainingSet)
        {
            var outputLayerSize = 8;
            var learningRadius = outputLayerSize/2;
            var neigborhoodFunction = new GaussianFunction(learningRadius);
            const LatticeTopology topology = LatticeTopology.Hexagonal;
            var max = trainingSet.Max(x => x.Max());
            var min = trainingSet.Min(x => x.Min());
            var inputLayer = new KohonenLayer(trainingSet[0].Length);
            var outputLayer = new KohonenLayer(new System.Drawing.Size(outputLayerSize, outputLayerSize),
                neigborhoodFunction, topology);
            new KohonenConnector(inputLayer, outputLayer) {Initializer = new RandomFunction(min, max)};
            outputLayer.SetLearningRate(0.2, 0.05d);
            outputLayer.IsRowCircular = false;
            outputLayer.IsColumnCircular = false;
            var network = new KohonenNetwork(inputLayer, outputLayer);

            var trSet = new TrainingSet(trainingSet[0].Length);
            foreach (var x in trainingSet)
            {
                trSet.Add(new TrainingSample(x));
            }
            network.Learn(trSet, 500);
            return network;
        }

        private void ProcessSamples(Dictionary<string, double[][]> features, string featureType)
        {
            foreach (var codeBookFileName in features.Keys)
            {
                var cb = new VectorQuantization(features[codeBookFileName], features[codeBookFileName][0].Length, 128);
//                var neuron = ProvideKohonenNetwork(features[codeBookFileName]);
                foreach (var testFileName in features.Keys)
                {
//                    var energy = 0.0;
                    var distortionSignal = new double[features[testFileName].Length];
                    for (int i = 0; i < features[testFileName].Length; i++)
                    {
                        var distortion =
                            cb.QuantizationErrorNormal(cb.Quantazation(features[testFileName][i]),
                                features[testFileName][i]);
//                        neuron.Run(features[testFileName][i]);
//                        var place = new double[features[testFileName][i].Length];
//                        for (int j = 0; j < neuron.Winner.SourceSynapses.Count; j++)
//                        {
//                            place[j] = neuron.Winner.SourceSynapses[j].Weight;
//                        }
//                        var distortion = VectorQuantization.QuantizationError(place, features[testFileName][i]);
//                        distortionSignal[i] = distortion;
//                        energy += distortion;
                    }
//                    energy /= features[testFileName].Length;
                    var energy = cb.DistortionMeasureEnergy(features[testFileName]);

                    var testFileInfo = new FileInfo(testFileName);
                    var codeBookFileInfo = new FileInfo(codeBookFileName);
                    var report = new ReportElement
                    {
                        FileName = testFileName,
                        CodeBookFileName = codeBookFileName,
                        DistortionSignal = distortionSignal,
                        DistortionEnergy = energy,
                        DictorName = testFileInfo.Name.Substring(0, 3),
                        Phrase = testFileInfo.Directory.Name,
                        CodeBookDictorName = codeBookFileInfo.Name.Substring(0, 3),
                        CodeBookPhrase = codeBookFileInfo.Directory.Name,
                        FeatureType = featureType
                    };

                    report.SaveToDb();
                }
                cb = null;
//                neuron = null;
                GC.Collect();
            }
        }


        private void SelectTestSampleButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDlg = new OpenFileDialog();
            openFileDlg.FileOk += (o, args) =>
            {
                var fileName = ((OpenFileDialog)o).FileName;
                var task = new Task(OpenTestFile, fileName);
                task.Start();
            };
            openFileDlg.ShowDialog(this);
        }

        private void OpenTestFile(object fileName)
        {
            WindowCursor = Cursors.Wait;
            OnPropertyChanged("WindowCursor");
            int sampleRate;
            var inputFile = FileReader.ReadFileNormalized(fileName.ToString(), out sampleRate);

            var tonalSpeechSelector = new TonalSpeechSelector();
            tonalSpeechSelector.InitData(inputFile, AnalysisInterval, Overlaping, sampleRate);

            var speechMarks = tonalSpeechSelector.GetTonalSpeechMarks();
            double[][] trainDataAcf;

            switch (_selectedFeature)
            {
                case SelectedFeature.Pitch:
                    trainDataAcf = GetPitchImage(inputFile, sampleRate, speechMarks);//use pitch
                    break;
                case SelectedFeature.Lpc:
                    trainDataAcf = GetLpcImage(inputFile, sampleRate, speechMarks);//use LPC
                    break;
                case SelectedFeature.Combine:
                    trainDataAcf = GetCombineImage(inputFile, sampleRate, speechMarks);//use both features
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (AddDeltaToFeatures)
            {
                trainDataAcf = DeltaGenerator.AddDelta(trainDataAcf);
            }

            PlotData(trainDataAcf, (int)Math.Round(sampleRate * AnalysisInterval * (1.0 - Overlaping)), sampleRate, TestDataModel);

            switch (_uesdAlgorithm)
            {
                case UsedQuatizationAlgorithm.Lbg:
                    SaveDistortionEnergyToFile(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            "distortionMeasure.txt"), trainDataAcf);
                    break;
                case UsedQuatizationAlgorithm.Kohonen:
                    SaveDistortionEnergyToFileNeuron(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            "distortionMeasure.txt"), trainDataAcf);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            WindowCursor = Cursors.Arrow;
            OnPropertyChanged("WindowCursor");
        }

        private void SaveDistortionEnergyToFileNeuron(string fileName, double[][] testData)
        {
            using (var writer = new StreamWriter(fileName))
            {
                var energy = 0.0;
                for (int i = 0; i < testData.Length; i++)
                {
                    _network.Run(testData[i]);
                    var place = new double[testData[0].Length];
                    for (int j = 0; j < _network.Winner.SourceSynapses.Count; j++)
                    {
                        place[j] = _network.Winner.SourceSynapses[j].Weight;
                    }
                    var distortion = VectorQuantization.QuantizationError(place, testData[i]);
                    writer.WriteLine(distortion);
                    energy += Math.Pow(distortion, 2);
                }
                energy /= testData.Length;
                writer.WriteLine("---------------");
                writer.WriteLine(energy);
            }
        }

        private void SaveDistortionEnergyToFile(string fileName, double[][] testData)
        {
            using (var writer = new StreamWriter(fileName))
            {
                for (int i = 0; i < testData.Length; i++)
                {
                    var distortion = VqCodeBook.QuantizationErrorNormal(VqCodeBook.Quantazation(testData[i]),
                        testData[i]);
                    writer.WriteLine(distortion);
                }
                writer.WriteLine("---------------");
                writer.WriteLine(VqCodeBook.DistortionMeasureEnergy(testData));
            }
        }

        private void SelectTrainSampleButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDlg = new OpenFileDialog();
            openFileDlg.FileOk += (o, args) =>
            {
                var fileName = ((OpenFileDialog) o).FileName;
                var task = new Task(OpenTrainFile, fileName);
                task.Start();
            };
            openFileDlg.ShowDialog(this);
        }

        private void OpenTrainFile(object fileName)
        {
            WindowCursor = Cursors.Wait;
            OnPropertyChanged("WindowCursor");
            int sampleRate;
            var inputFile = FileReader.ReadFileNormalized(fileName.ToString(), out sampleRate);

            var tonalSpeechSelector = new TonalSpeechSelector();
            tonalSpeechSelector.InitData(inputFile, AnalysisInterval, Overlaping, sampleRate);

            var speechMarks = tonalSpeechSelector.GetTonalSpeechMarks();
            double[][] trainDataAcf;

            switch (_selectedFeature)
            {
                case SelectedFeature.Pitch:
                    trainDataAcf = GetPitchImage(inputFile, sampleRate, speechMarks);//use pitch
                    break;
                case SelectedFeature.Lpc:
                    trainDataAcf = GetLpcImage(inputFile, sampleRate, speechMarks);//use LPC
                    break;
                case SelectedFeature.Combine:
                    trainDataAcf = GetCombineImage(inputFile, sampleRate, speechMarks);//use both features
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (AddDeltaToFeatures)
            {
                trainDataAcf = DeltaGenerator.AddDelta(trainDataAcf);
            }

            PlotData(trainDataAcf, (int) Math.Round(sampleRate*AnalysisInterval*(1.0 - Overlaping)), sampleRate, TrainDataModel);

            switch (_uesdAlgorithm)
            {
                case UsedQuatizationAlgorithm.Lbg:
                    VqCodeBook = new VectorQuantization(trainDataAcf, trainDataAcf[0].Length, 64);
                    PlotData(VqCodeBook.CodeBook, (int)Math.Round(sampleRate * AnalysisInterval * (1.0 - Overlaping)), sampleRate, CodeBookModel);
                    break;
                case UsedQuatizationAlgorithm.Kohonen:
                    ProvideKohonenCom(trainDataAcf);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            WindowCursor = Cursors.Arrow;
            OnPropertyChanged("WindowCursor");
            IsTestButtonEnabled = true;
            OnPropertyChanged("IsTestButtonEnabled");
        }

        private void ProvideKohonenCom(double[][] trainingSet)
        {
            var outputLayerSize = 8;
            var isWinner = new bool[outputLayerSize, outputLayerSize];
            var learningRadius = outputLayerSize / 2;
            var neigborhoodFunction = new GaussianFunction(learningRadius);
            const LatticeTopology topology = LatticeTopology.Hexagonal;
            var max = trainingSet.Max(x => x.Max());
            var min = trainingSet.Min(x => x.Min());
            var inputLayer = new KohonenLayer(trainingSet[0].Length);
            var outputLayer = new KohonenLayer(new System.Drawing.Size(outputLayerSize, outputLayerSize), neigborhoodFunction, topology);
            new KohonenConnector(inputLayer, outputLayer) { Initializer = new RandomFunction(min, max) };
            outputLayer.SetLearningRate(0.2, 0.05d);
            outputLayer.IsRowCircular = false;
            outputLayer.IsColumnCircular = false;
            _network = new KohonenNetwork(inputLayer, outputLayer);

            _network.BeginEpochEvent += (senderNetwork, args) => Array.Clear(isWinner, 0, isWinner.Length);

            _network.EndSampleEvent += delegate
            {
                isWinner[_network.Winner.Coordinate.X, _network.Winner.Coordinate.Y] =
                    true;
            };

            _network.EndEpochEvent += delegate
            {
                PlotWinnersNeurons(isWinner);
            };
            var trSet = new TrainingSet(trainingSet[0].Length);
            foreach (var x in trainingSet)
            {
                trSet.Add(new TrainingSample(x));
            }
            _network.Learn(trSet, 500);
            PlotWinnersNeurons(isWinner);
        }

        private void PlotWinnersNeurons(bool[,] winners)
        {
            var outputLayerSize = 8;
            var heatMap = new HeatMapSeries
            {
                Data = new double[outputLayerSize, outputLayerSize],
                X0 = 0,
                X1 = outputLayerSize,
                Y0 = 0,
                Y1 = outputLayerSize,
                Interpolate = false
            };
            for (int i = 0; i < outputLayerSize; i++)
                for (int j = 0; j < outputLayerSize; j++)
                {
                    heatMap.Data[i, j] = winners[i, j] ? 1.0 : 0.0;
                }

            CodeBookPlotView.Dispatcher.Invoke(() =>
            {
                CodeBookPlotView.Model.Series.Clear();
                CodeBookPlotView.Model.Series.Add(heatMap);
                CodeBookPlotView.Model.InvalidatePlot(true);
            });
        }

        private void PlotData(double[][] trainDataAcf, int jump, double sampleRate, PlotModel model, bool isCodeBook = false)
        {
            if (trainDataAcf[0].Length == 1)
            {
                //plot as LineSeries
                var lineSeries = new LineSeries();
                for (int i = 0; i < trainDataAcf.Length; i++)
                {
                    lineSeries.Points.Add(new DataPoint(isCodeBook ? i : i*jump, trainDataAcf[i][0]));
                }
                model.Series.Clear();
                model.Series.Add(lineSeries);
                model.InvalidatePlot(true);
            }
            else
            {
                //plot as HeatMap
                var heatMap = new HeatMapSeries
                {
                    Data = new double[trainDataAcf.Length, trainDataAcf[0].Length],
                    X0 = 0,
                    X1 = trainDataAcf.Length*jump,
                    Y0 = 0,
                    Y1 = trainDataAcf[0].Length,
                    Interpolate = false
                };
                for (int i = 0; i < trainDataAcf.Length; i++)
                    for (int j = 0; j < trainDataAcf[i].Length; j++)
                    {
                        heatMap.Data[i, j] = trainDataAcf[i][j];
                    }
                model.Series.Clear();
                model.Series.Add(heatMap);
                model.InvalidatePlot(true);
            }
        }

        private double[][] GetPitchImage(float[] speechFile, int sampleRate, Tuple<int, int>[] speechMarks)
        {
            double[][] trainDataAcf;
            var windowSize = (int) Math.Round(sampleRate*AnalysisInterval);
            var corellation = new Corellation();
            corellation.PitchImage(ref speechFile, windowSize, 1.0f - Overlaping, out trainDataAcf, WindowFunctions.WindowType.Blackman, sampleRate, speechMarks);
            trainDataAcf.ForEach(x=> x[0] = x[0] > 0.0? sampleRate/x[0]:0.0);
            return trainDataAcf;
        }

        private double[][] GetLpcImage(float[] speechFile, int sampleRate, Tuple<int, int>[] speechMarks)
        {
            double[][] featureMatrix;
            var lpc = new LinearPredictCoefficient
            {
                SamleFrequency = sampleRate,
                UsedAcfWindowSizeTime = AnalysisInterval,
                UsedNumberOfCoeficients = 10,
                UsedWindowType = WindowFunctions.WindowType.Blackman,
                Overlapping = Overlaping
            };
            lpc.GetLpcImage(ref speechFile, out featureMatrix, speechMarks[0].Item1, speechMarks[speechMarks.Length -1].Item2);
            return featureMatrix;
        }

        private double[][] GetCombineImage(float[] speechFile, int sampleRate, Tuple<int, int>[] speechMarks)
        {
            var featureMatrix = GetLpcImage(speechFile, sampleRate, speechMarks);
            var windowSize = Math.Round(sampleRate*0.04);
            var trainDataAcf = GetPitchImage(speechFile, sampleRate, speechMarks);

            return MixFeatures(featureMatrix, trainDataAcf, speechMarks[0].Item1,
                    (int)Math.Round((1.0 - Overlaping) * windowSize));
        }

        private double[][] MixFeatures(double[][] lpcFeature, double[][] pitchFeature, int speechStartPosition, int jumpSize)
        {
            var pitchSkiping = (int) Math.Round(speechStartPosition/(double) jumpSize);
            var newFeature = new List<double[]>();
            for (int i = 0; i - pitchSkiping < lpcFeature.Length || i < pitchFeature.Length; i++)
            {
                var feature = new List<double>();
                feature.AddRange(i - pitchSkiping < lpcFeature.Length && i - pitchSkiping > -1
                    ? lpcFeature[i - pitchSkiping]
                    : new double[lpcFeature[0].Length]);

                feature.AddRange(i < pitchFeature.Length ? pitchFeature[i] : new double[pitchFeature[0].Length]);

                newFeature.Add(feature.ToArray());
            }
            return newFeature.ToArray();
        }

        private void SelectLbgVqModeRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            _uesdAlgorithm = UsedQuatizationAlgorithm.Lbg;
        }

        private void SelectKohonenModeRadioButton_OnClick(object sender, RoutedEventArgs e)
        {
            _uesdAlgorithm = UsedQuatizationAlgorithm.Kohonen;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if(PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void GenerateReport_OnClick(object sender, RoutedEventArgs e)
        {
            ReportElement.MakeReport();
            ReportElement.MakeVerificationReport();
        }
    }
}
