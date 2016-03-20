using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using HelpersLibrary;
using HelpersLibrary.DspAlgorithms;
using HelpersLibrary.LearningAlgorithms;
using Microsoft.Win32;
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
            OnPropertyChanged(nameof(TrainDataModel));

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
            OnPropertyChanged(nameof(TestDataModel));

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
            OnPropertyChanged(nameof(CodeBookModel));
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
        private bool _addDeltaToFeatures;
        private UsedQuatizationAlgorithm _uesdAlgorithm = UsedQuatizationAlgorithm.Lbg;

        private void AddDeltaCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            _addDeltaToFeatures = AddDeltaCheckBox.IsChecked.HasValue && AddDeltaCheckBox.IsChecked.Value;
        }

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
            throw new NotImplementedException();
        }

        private void SelectTrainSampleButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDlg = new OpenFileDialog();
            openFileDlg.FileOk += (o, args) =>
            {
                var fileName = ((OpenFileDialog) o).FileName;
                var task = new Task(OpenFile, fileName);
                task.Start();
            };
            openFileDlg.ShowDialog(this);
        }

        private void OpenFile(object fileName)
        {
            WindowCursor = Cursors.Wait;
            OnPropertyChanged(nameof(WindowCursor));
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
                    trainDataAcf = GetAcfImage(inputFile, sampleRate, speechMarks);//use LPC
                    break;
                case SelectedFeature.Combine:
                    trainDataAcf = GetAcfImage(inputFile, sampleRate, speechMarks);//use both features
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_addDeltaToFeatures)
            {
                trainDataAcf = DeltaGenerator.AddDelta(trainDataAcf);
            }

            PlotPitch(trainDataAcf, (int) Math.Round(sampleRate*AnalysisInterval*(1.0 - Overlaping)), sampleRate, TrainDataModel);

            switch (_uesdAlgorithm)
            {
                case UsedQuatizationAlgorithm.Lbg:
                    var vqCodeBook = new VectorQuantization(trainDataAcf, trainDataAcf[0].Length, 64);
                    PlotPitch(vqCodeBook.CodeBook, (int)Math.Round(sampleRate * AnalysisInterval * (1.0 - Overlaping)), sampleRate, CodeBookModel);
                    break;
                case UsedQuatizationAlgorithm.Kohonen:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            WindowCursor = Cursors.Arrow;
            OnPropertyChanged(nameof(WindowCursor));
        }

        private void PlotPitch(double[][] trainDataAcf, int jump, double sampleRate, PlotModel model, bool isCodeBook = false)
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
