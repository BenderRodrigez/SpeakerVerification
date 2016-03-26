using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HelpersLibrary;
using HelpersLibrary.DspAlgorithms;
using HelpersLibrary.LearningAlgorithms;
using Microsoft.Win32;
using NeuronDotNet.Core;
using NeuronDotNet.Core.Initializers;
using NeuronDotNet.Core.SOM;
using NeuronDotNet.Core.SOM.NeighborhoodFunctions;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using SpeakerVerificationExperiments.Annotations;

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
            throw new NotImplementedException();
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
                    trainDataAcf = GetAcfImage(inputFile, sampleRate, speechMarks);//use pitch
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
                writer.WriteLine(VqCodeBook.DistortionMeasureEnergy(ref testData));
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
                    trainDataAcf = GetAcfImage(inputFile, sampleRate, speechMarks);//use pitch
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
                    lineSeries.Points.Add(new DataPoint(isCodeBook ? i : i*jump,
                        trainDataAcf[i][0] > 0.0 ? sampleRate/trainDataAcf[i][0] : 0.0));
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

        private double[][] GetAcfImage(float[] speechFile, int sampleRate, Tuple<int, int>[] speechMarks)
        {
            double[][] trainDataAcf;
            var windowSize = (int) Math.Round(sampleRate*AnalysisInterval);
            var corellation = new Corellation();
            corellation.PitchImage(ref speechFile, windowSize, 1.0f - Overlaping, out trainDataAcf, WindowFunctions.WindowType.Blackman, sampleRate, speechMarks);
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
            double[][] featureMatrix;
            var lpc = new LinearPredictCoefficient
            {
                SamleFrequency = sampleRate,
                UsedAcfWindowSizeTime = AnalysisInterval,
                UsedNumberOfCoeficients = 10,
                UsedWindowType = WindowFunctions.WindowType.Blackman,
                Overlapping = Overlaping
            };
            lpc.GetLpcImage(ref speechFile, out featureMatrix, speechMarks[0].Item1, speechMarks[speechMarks.Length - 1].Item2);
            double[][] trainDataAcf;
            var windowSize = (int)Math.Round(sampleRate * AnalysisInterval);
            var corellation = new Corellation();
            corellation.PitchImage(ref speechFile, windowSize, 1.0f - Overlaping, out trainDataAcf, WindowFunctions.WindowType.Blackman, sampleRate, speechMarks);

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
    }
}
