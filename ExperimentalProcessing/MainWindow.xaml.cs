using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ExperimentalProcessing.Annotations;
using HelpersLibrary.DspAlgorithms;
using Microsoft.Win32;
using NAudio.Wave;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace ExperimentalProcessing
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public sealed partial class MainWindow : INotifyPropertyChanged
    {
        public PlotModel PitchPlotModel { get; private set; }
        public PlotModel AcfPlotModel { get; private set; }
        public PlotModel AcfsPlotModel { get; private set; }
        public PlotModel AcfPreview { get; private set; }
        public PlotModel AcfsPreview { get; private set; }
        public Cursor WindowCursor { get; private set; }
        public string MaxSize { get; private set; }

        private int _samplePos = 0;
        public int SamplePosition
        {
            get { return _samplePos; }
            set
            {
                if (_acf != null && _acfs != null && value > -1 && value < _acf.Length)
                {
                    _samplePos = value;
                    PlotAcfSample(_samplePos);
                    PlotAcfsSample(_samplePos);
                    OnPropertyChanged();
                }
            }
        }

        double[][] _acf;
        double[][] _acfs;

        public MainWindow()
        {
            InitializeComponent();

            PitchPlotModel = new PlotModel { Title = "Трек ОТ" };
            PitchPlotModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1));
            OnPropertyChanged("PitchPlotModel");

            AcfPlotModel = new PlotModel { Title = "АКФ" };
            AcfPlotModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1));
            OnPropertyChanged("AcfPlotModel");

            AcfsPlotModel = new PlotModel { Title = "АКФС" };
            AcfsPlotModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1));
            OnPropertyChanged("AcfsPlotModel");

            AcfPreview = new PlotModel {Title = "Кореллограмма АКФ"};
            var linearColorAxis = new LinearColorAxis
            {
                HighColor = OxyColors.White,
                LowColor = OxyColors.Black,
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Hot(200),
            };
            AcfPreview.Axes.Add(linearColorAxis);
            AcfPreview.Series.Add(new FunctionSeries(Math.Cosh, 0, 10, 0.1));
            OnPropertyChanged("AcfPreview");
            AcfsPreview = new PlotModel { Title = "Кореллограмма АКФС" };
            var linearColorAxis1 = new LinearColorAxis
            {
                HighColor = OxyColors.White,
                LowColor = OxyColors.Black,
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Hot(200),
            };
            AcfsPreview.Axes.Add(linearColorAxis1);
            AcfsPreview.Series.Add(new FunctionSeries(Math.Cosh, 0, 10, 0.1));
            OnPropertyChanged("AcfsPreview");
        }

        private static float[] ReadSpeechFile(string filePath, out WaveFormat speechFileFormat)
        {
            float[] speechFile;
            using (var reader = new WaveFileReader(filePath))
            {
                speechFile = new float[reader.SampleCount];
                for (int i = 0; i < reader.SampleCount; i++)
                {
                    speechFile[i] = reader.ReadNextSampleFrame()[0];
                }
                speechFileFormat = reader.WaveFormat;
            }
            return speechFile;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OpenFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            var openFileDlg = new OpenFileDialog();
            openFileDlg.FileOk += OpenFileDlgOnFileOk;
            openFileDlg.ShowDialog(this);
        }

        private void OpenFileDlgOnFileOk(object sender, CancelEventArgs cancelEventArgs)
        {
            var fileName = ((OpenFileDialog) sender).FileName;
            var task = new Task(OpenFile, fileName);
            task.Start();
        }

        private void OpenFile(object fileName)
        {
            WindowCursor = Cursors.Wait;
            OnPropertyChanged("WindowCursor");
            WaveFormat signalFormat;
            var signal = ReadSpeechFile(fileName.ToString(), out signalFormat);
            var tonalSpeechSelector = new TonalSpeechSelector(signal, 0.8f, 0.95f, signalFormat.SampleRate,
                TonalSpeechSelector.Algorithm.Acfs);
            var speechMarks = tonalSpeechSelector.GetTonalSpeechMarks();
            var trainDataAcf = GetAcfImage(signal, signalFormat, speechMarks, out _acf, out _acfs);
            SamplePosition = 0;
            PlotPitch(trainDataAcf);
            PlotAcfPreview();
            PlotAcfsPreview();
            WindowCursor = Cursors.Arrow;
            OnPropertyChanged("WindowCursor");
        }

        private void PlotAcfsPreview()
        {
            var heatMap = new HeatMapSeries
            {
                Data = new double[_acfs.Length, _acfs[0].Length],
                X0 = 0,
                X1 = _acfs.Length,
                Y0 = 0,
                Y1 = _acfs[0].Length,
                Interpolate = false
            };
            for (int i = 0; i < _acfs.Length; i++)
                for (int j = 0; j < _acfs[i].Length; j++)
                {
                    heatMap.Data[i, j] = _acfs[i][j];
                }
            AcfsPreview.Series.Clear();
            AcfsPreview.Series.Add(heatMap);
            AcfsPreview.InvalidatePlot(true);
            OnPropertyChanged("AcfsPreview");
        }

        private void PlotAcfPreview()
        {
            var heatMap = new HeatMapSeries
            {
                Data = new double[_acf.Length, _acf[0].Length],
                X0 = 0,
                X1 = _acf.Length,
                Y0 = 0,
                Y1 = _acf[0].Length,
                Interpolate = false
            };
            for (int i = 0; i < _acf.Length; i++)
                for (int j = 0; j < _acf[i].Length; j++)
                {
                    heatMap.Data[i, j] = _acf[i][j];
                }
            AcfPreview.Series.Clear();
            AcfPreview.Series.Add(heatMap);
            AcfPreview.InvalidatePlot(true);
            OnPropertyChanged("AcfPreview");
        }

        private double[][] GetAcfImage(float[] speechFile, WaveFormat speechFileFormat, Tuple<int, int>[] speechMarks,
            out double[][] acf, out double[][] acfs)
        {
            double[][] trainDataAcf;
            var corellation = new Corellation();
            corellation.AutCorrelationImage(ref speechFile, 441, 0.05f, out trainDataAcf,
                WindowFunctions.WindowType.Blackman, speechFileFormat.SampleRate, speechMarks, true);
            acf = corellation.Acf;
            acfs = corellation.Acfs;
            MaxSize = string.Format(" из {0}", acf.Length - 1);
            OnPropertyChanged("MaxSize");
            return trainDataAcf;
        }

        private void PlotAcfSample(int pos)
        {
            var heatMap = new LineSeries();
            for (int i = 0; i < _acf[pos].Length; i++)
                heatMap.Points.Add(new DataPoint(i, _acf[pos][i]));
            AcfPlotModel.Series.Clear();
            AcfPlotModel.Series.Add(heatMap);
            AcfPlotModel.InvalidatePlot(true);
        }

        private void PlotAcfsSample(int pos)
        {
            var heatMap = new LineSeries();
            for (int i = 0; i < _acfs[pos].Length; i++)
                heatMap.Points.Add(new DataPoint(i, _acfs[pos][i]));
            AcfsPlotModel.Series.Clear();
            AcfsPlotModel.Series.Add(heatMap);
            AcfsPlotModel.InvalidatePlot(true);
        }

        private void PlotPitch(double[][] featureSet)
        {
            var heatMap = new LineSeries();
            for (int i = 0; i < featureSet.Length; i++)
                heatMap.Points.Add(new DataPoint(i, featureSet[i][0]));
            PitchPlotModel.Series.Clear();
            PitchPlotModel.Series.Add(heatMap);
            PitchPlotModel.InvalidatePlot(true);
        }

        private void SampleNumberTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Return:
                    int t;
                    if (int.TryParse(SampleNumberTextBox.Text, out t))
                        SamplePosition = t;
                    e.Handled = true;
                    break;
                case Key.Left:
                    SamplePosition--;
                    e.Handled = true;
                    break;
                case Key.Right:
                    SamplePosition++;
                    e.Handled = true;
                    break;
            }
        }

        private void GoForward_OnClick(object sender, RoutedEventArgs e)
        {
            if (SamplePosition < _acf.Length - 1) SamplePosition++;
        }

        private void GoBackward_OnClick(object sender, RoutedEventArgs e)
        {
            if (SamplePosition > 0) SamplePosition--;
        }

        private void ToEnd_OnClick(object sender, RoutedEventArgs e)
        {
            SamplePosition = _acf.Length - 1;
        }

        private void ToStart_OnClick(object sender, RoutedEventArgs e)
        {
            SamplePosition = 0;
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Right)
            {
                SamplePosition++;
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                SamplePosition--;
                e.Handled = true;
            }
        }
    }
}
