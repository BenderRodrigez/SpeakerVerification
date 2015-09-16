using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using HelpersLibrary.DspAlgorithms;
using HelpersLibrary.LearningAlgorithms;
using NAudio.Wave;
using NeuronDotNet.Core;
using NeuronDotNet.Core.Initializers;
using NeuronDotNet.Core.SOM;
using NeuronDotNet.Core.SOM.NeighborhoodFunctions;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

// ReSharper disable RedundantArgumentDefaultValue

namespace SpeakerVerification
{
    public partial class Form1 : Form
    {
        public PlotView CodeBookPlotView;
        public PlotView FeaturesTrainDataPlotView;
        public PlotView FeatureTestDataPlotView;
        private const WindowFunctions.WindowType Window = WindowFunctions.WindowType.Blackman;//тип применяемой оконной функции
        private VectorQuantization _vqCodeBook;

        public Form1()
        {
            InitializeComponent();
            CodeBookPlotView = new PlotView();
            var model = new PlotModel
            {
                PlotType = PlotType.XY,
                Background = OxyColors.White,
                TextColor = OxyColors.Black
            };
            var linearColorAxis = new LinearColorAxis
            {
                HighColor = OxyColors.White,
                LowColor = OxyColors.Black,
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Hot(200),
            };
            model.Axes.Add(linearColorAxis);
            CodeBookPlotView.Model = model;

            CodeBookPlotView.Dock = DockStyle.Fill;
            codeBookGroupBox.Controls.Add(CodeBookPlotView);
            

            FeaturesTrainDataPlotView = new PlotView();
            var model2 = new PlotModel
            {
                PlotType = PlotType.XY,
                Background = OxyColors.White,
                TextColor = OxyColors.Black
            };
            var linearColorAxis1 = new LinearColorAxis
            {
                HighColor = OxyColors.White,
                LowColor = OxyColors.Black,
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Hot(200),
            };
            model2.Axes.Add(linearColorAxis1);
            FeaturesTrainDataPlotView.Model = model2;
            FeaturesTrainDataPlotView.Dock = DockStyle.Fill;
            trainFeaturesGroupBox.Controls.Add(FeaturesTrainDataPlotView);

            FeatureTestDataPlotView = new PlotView();
            var model3 = new PlotModel
            {
                PlotType = PlotType.XY,
                Background = OxyColors.White,
                TextColor = OxyColors.Black
            };
            var linearColorAxis2 = new LinearColorAxis
            {
                HighColor = OxyColors.White,
                LowColor = OxyColors.Black,
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Hot(200),
            };
            model3.Axes.Add(linearColorAxis2);
            FeatureTestDataPlotView.Model = model3;
            FeatureTestDataPlotView.Dock = DockStyle.Fill;
            featureTestGroupBox.Controls.Add(FeatureTestDataPlotView);

            for(var type = WindowFunctions.WindowType.Rectangular; type <= WindowFunctions.WindowType.Blackman; type++)
            {
                windowTypeComboBox.Items.Add(type.ToString());
            }
            windowTypeComboBox.SelectedItem = Window.ToString();
        }

        private void ProvideKohonenCom(TrainingSet trainingSet, bool[,] isWinner)
        {
            var learningRadius = Math.Max(64, 10)/2;
            var neigborhoodFunction = new GaussianFunction(learningRadius);
            const LatticeTopology topology = LatticeTopology.Hexagonal;

            var inputLayer = new KohonenLayer(10);
            var outputLayer = new KohonenLayer(new Size(64, 10), neigborhoodFunction, topology);
            var connector = new KohonenConnector(inputLayer, outputLayer) {Initializer = new RandomFunction(0, 100)};
            outputLayer.SetLearningRate(0.2, 0.05d);
            outputLayer.IsRowCircular = false;
            outputLayer.IsColumnCircular = false;
            var network = new KohonenNetwork(inputLayer, outputLayer);

            var progress = 1;
            network.BeginEpochEvent += (senderNetwork, args) => Array.Clear(isWinner, 0, 64*10);

            network.EndSampleEvent += delegate
            {
                isWinner[network.Winner.Coordinate.X, network.Winner.Coordinate.Y] =
                    true;
            };

            network.EndEpochEvent += delegate
            {
                progressBar1.Value = ((progress++) * 100) / 500;
                Application.DoEvents();
            };

            network.Learn(trainingSet, 500);
        }
        
        private void trainDataSelectButton_Click(object sender, EventArgs e)
        {
            if (wavFileOpenDialog.ShowDialog(this) == DialogResult.OK)
            {
                trainDataFileNameLabel.Text = wavFileOpenDialog.FileName;

                int speechStartPosition, speechStopPosition;
                WaveFormat speechFileFormat;
                float[] speechFile;
                ReadWavFile(wavFileOpenDialog.FileName, out speechFileFormat, out speechFile);
                var speechSearcher = new SpeechSearch((byte) histogrammBagsNumberUpDown.Value,
                    (float) analysisIntervalUpDown.Value, ((float) overlappingUpDown.Value)/100,
                    speechFileFormat.SampleRate);
                speechSearcher.GetMarks(speechFile, out speechStartPosition, out speechStopPosition);
                var cbSize = (IsPowerOfTwo((uint)vqSizeNumericUpDown.Value)) ? (int)vqSizeNumericUpDown.Value : 64;
                switch (featureSelectComboBox.SelectedItem as string)
                {
                    case "LPC":
                        var trainDataLpc = GetLpcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataLpc);
                        _vqCodeBook = new VectorQuantization(trainDataLpc, (int)lpcVectorLenghtUpDown.Value, cbSize);
                        PlotCodeBook(_vqCodeBook.CodeBook);
                        break;
                    case "ARC":
                        var trainDataArc = GetArcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataArc);
                        _vqCodeBook = new VectorQuantization(trainDataArc, (int)arcVectorLenghtUpDown.Value, cbSize);
                        PlotCodeBook(_vqCodeBook.CodeBook);
                        break;
                    case "MFCC":
                        var trainDataMfcc = GetMfccImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataMfcc);
                        _vqCodeBook = new VectorQuantization(trainDataMfcc, (int)mfccVectorLenghtUpDown.Value, cbSize);
                        PlotCodeBook(_vqCodeBook.CodeBook);
                        break;
                    case "VTC":
                        var trainDataVtc = GetVtcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataVtc);
                        _vqCodeBook = new VectorQuantization(trainDataVtc, (int)vtcVectorLenghtUpDown.Value, cbSize);
                        PlotCodeBook(_vqCodeBook.CodeBook);
                        break;
                    default:
                        return;
                }
            }
        }

        private bool IsPowerOfTwo(uint val)
        {
            return val != 0 && (val & (val - 1)) == 0;
        }

        private double[][] GetLpcImage(WaveFormat speechFileFormat, float[] speechFile, int speechStart, int speechStop)
        {
            double[][] featureMatrix;
            var lpc = new LinearPredictCoefficient
            {
                SamleFrequency = speechFileFormat.SampleRate,
                UsedAcfWindowSizeTime = (float) analysisIntervalUpDown.Value,
                UsedNumberOfCoeficients = (int) lpcVectorLenghtUpDown.Value
            };
            WindowFunctions.WindowType windowType;
            Enum.TryParse(
                windowTypeComboBox.SelectedItem as string, out windowType);
            lpc.UsedWindowType = windowType;
            lpc.Overlapping = ((float)overlappingUpDown.Value) / 100;
            lpc.GetLpcImage(ref speechFile, out featureMatrix, speechStart, speechStop);
            return featureMatrix;
        }

        private double[][] GetArcImage(WaveFormat speechFileFormat, float[] speechFile, int speechStart, int speechStop)
        {
            double[][] featureMatrix;
            var lpc = new LinearPredictCoefficient
            {
                SamleFrequency = speechFileFormat.SampleRate,
                UsedAcfWindowSizeTime = (float)analysisIntervalUpDown.Value,
                UsedNumberOfCoeficients = (int)lpcVectorLenghtUpDown.Value
            };
            WindowFunctions.WindowType windowType;
            Enum.TryParse(
                windowTypeComboBox.SelectedItem as string, out windowType);
            lpc.UsedWindowType = windowType;
            lpc.Overlapping = ((float)overlappingUpDown.Value) / 100;
            lpc.GetArcImage(ref speechFile, out featureMatrix, speechStart, speechStop, (int)arcVectorLenghtUpDown.Value);
            return featureMatrix;
        }

        private double[][] GetVtcImage(WaveFormat speechFileFormat, float[] speechFile, int speechStart, int speechStop)
        {
            double[][] featureMatrix;
            var lpc = new LinearPredictCoefficient
            {
                SamleFrequency = speechFileFormat.SampleRate,
                UsedAcfWindowSizeTime = (float)analysisIntervalUpDown.Value,
                UsedNumberOfCoeficients = (int)lpcVectorLenghtUpDown.Value
            };
            WindowFunctions.WindowType windowType;
            Enum.TryParse(
                windowTypeComboBox.SelectedItem as string, out windowType);
            lpc.UsedWindowType = windowType;
            lpc.Overlapping = ((float)overlappingUpDown.Value) / 100;
            lpc.GetArcVocalTractImage(ref speechFile, speechFileFormat.SampleRate, (int)vtcVectorLenghtUpDown.Value, out featureMatrix, speechStart, speechStop);
            return featureMatrix;
        }

        private double[][] GetMfccImage(WaveFormat speechFileFormat, float[] speechFile, int speechStart, int speechStop)
        {
            double[][] featureMatrix;
            var mfcc = new Cepstrum((int)mfccVectorLenghtUpDown.Value, (double)analysisIntervalUpDown.Value, speechFileFormat.SampleRate, ((float)overlappingUpDown.Value)/100.0f);
            WindowFunctions.WindowType windowType;
            Enum.TryParse(
                windowTypeComboBox.SelectedItem as string, out windowType);
            mfcc.GetCepstrogram(ref speechFile,windowType, speechStart, speechStop, out featureMatrix);
            return featureMatrix;
        }

        private static void ReadWavFile(string fileName, out WaveFormat speechFileFormat, out float[] speechFile)
        {
            using (var reader = new WaveFileReader(fileName))
            {
                speechFile = new float[reader.SampleCount];
                for (int i = 0; i < reader.SampleCount; i++)
                {
                    speechFile[i] = reader.ReadNextSampleFrame()[0];
                }
                speechFileFormat = reader.WaveFormat;
            }
        }

        private void PlotTestFeatureMatrix(double[][] featureSet)
        {
            var heatMap = new HeatMapSeries
            {
                Data = new double[featureSet.Length, featureSet[0].Length],
                X0 = 0,
                X1 = featureSet.Length,
                Y0 = 0,
                Y1 = featureSet[0].Length,
                Interpolate = false
            };
            for (int i = 0; i < featureSet.Length; i++)
                for (int j = 0; j < featureSet[i].Length; j++)
                {
                    heatMap.Data[i, j] = featureSet[i][j];
                }
            FeatureTestDataPlotView.Model.Series.Clear();
            FeatureTestDataPlotView.Model.Series.Add(heatMap);
            FeatureTestDataPlotView.Model.InvalidatePlot(true);
        }

        private void PlotTrainFeatureMatrix(double[][] featureSet)
        {
            var heatMap = new HeatMapSeries
            {
                Data = new double[featureSet.Length, featureSet[0].Length],
                X0 = 0,
                X1 = featureSet.Length,
                Y0 = 0,
                Y1 = featureSet[0].Length,
                Interpolate = false
            };
            for (int i = 0; i < featureSet.Length; i++)
                for (int j = 0; j < featureSet[i].Length; j++)
                {
                    heatMap.Data[i, j] = featureSet[i][j];
                }
            FeaturesTrainDataPlotView.Model.Series.Clear();
            FeaturesTrainDataPlotView.Model.Series.Add(heatMap);
            FeaturesTrainDataPlotView.Model.InvalidatePlot(true);
        }

        private void PlotCodeBook(double[][] codeBook)
        {
            var heatMap = new HeatMapSeries
            {
                Data = new double[codeBook.Length, codeBook[0].Length],
                X0 = 0,
                X1 = codeBook.Length,
                Y0 = 0,
                Y1 = codeBook[0].Length,
                Interpolate = false
            };
            for (int i = 0; i < codeBook.Length; i++)
                for (int j = 0; j < codeBook[i].Length; j++)
                {
                    heatMap.Data[i, j] = codeBook[i][j];
                }
            CodeBookPlotView.Model.Series.Add(heatMap);
            CodeBookPlotView.Model.InvalidatePlot(true);
        }

        private void SaveDistortionEnergyToFile(string fileName, double[][] testData)
        {
            using (var writer = new StreamWriter(fileName))
            {
                for (int i = 0; i < testData.Length; i++)
                {
                    var distortion = _vqCodeBook.QuantizationError(_vqCodeBook.Quantazation(testData[i]), testData[i]);
                    writer.WriteLine(distortion);
                }
            }
        }

        private void testDataSelectButton_Click(object sender, EventArgs e)
        {
            if (wavFileOpenDialog.ShowDialog(this) == DialogResult.OK)
            {
                testDataFileNameLabel.Text = wavFileOpenDialog.FileName;

                int speechStartPosition, speechStopPosition;
                WaveFormat speechFileFormat;
                float[] speechFile;
                ReadWavFile(wavFileOpenDialog.FileName, out speechFileFormat, out speechFile);
                var speechSearcher = new SpeechSearch((byte)histogrammBagsNumberUpDown.Value,
                    (float)analysisIntervalUpDown.Value, ((float)overlappingUpDown.Value) / 100,
                    speechFileFormat.SampleRate);
                speechSearcher.GetMarks(speechFile, out speechStartPosition, out speechStopPosition);
                switch (featureSelectComboBox.SelectedItem as string)
                {
                    case "LPC":
                        var testDataLpc = GetLpcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTestFeatureMatrix(testDataLpc);
                        SaveDistortionEnergyToFile(
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                "distortionMeasure.txt"), testDataLpc);
                        break;
                    case "ARC":
                        var testDataArc = GetArcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTestFeatureMatrix(testDataArc);
                        SaveDistortionEnergyToFile(
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                "distortionMeasure.txt"), testDataArc);
                        break;
                    case "MFCC":
                        var trainDataMfcc = GetMfccImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTestFeatureMatrix(trainDataMfcc);
                        SaveDistortionEnergyToFile(
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                "distortionMeasure.txt"), trainDataMfcc);
                        break;
                    case "VTC":
                        var testDataVtc = GetVtcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTestFeatureMatrix(testDataVtc);
                        SaveDistortionEnergyToFile(
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                                "distortionMeasure.txt"), testDataVtc);
                        break;
                    default:
                        return;
                }
            }
        }
    }
}
