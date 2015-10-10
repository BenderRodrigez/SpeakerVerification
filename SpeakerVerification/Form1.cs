using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
        private readonly PlotView _codeBookPlotView;
        private readonly PlotView _featuresTrainDataPlotView;
        private readonly PlotView _featureTestDataPlotView;
        private const WindowFunctions.WindowType Window = WindowFunctions.WindowType.Blackman;//тип применяемой оконной функции
        private VectorQuantization _vqCodeBook;
        private KohonenNetwork _network;

        public Form1()
        {
            InitializeComponent();
            _codeBookPlotView = new PlotView();
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
            _codeBookPlotView.Model = model;

            _codeBookPlotView.Dock = DockStyle.Fill;
            codeBookGroupBox.Controls.Add(_codeBookPlotView);
            

            _featuresTrainDataPlotView = new PlotView();
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
            _featuresTrainDataPlotView.Model = model2;
            _featuresTrainDataPlotView.Dock = DockStyle.Fill;
            trainFeaturesGroupBox.Controls.Add(_featuresTrainDataPlotView);

            _featureTestDataPlotView = new PlotView();
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
            _featureTestDataPlotView.Model = model3;
            _featureTestDataPlotView.Dock = DockStyle.Fill;
            featureTestGroupBox.Controls.Add(_featureTestDataPlotView);

            for(var type = WindowFunctions.WindowType.Rectangular; type <= WindowFunctions.WindowType.Blackman; type++)
            {
                windowTypeComboBox.Items.Add(type.ToString());
            }
            windowTypeComboBox.SelectedItem = Window.ToString();
        }

        private bool[,] ProvideKohonenCom(double[][] trainingSet)
        {
            var outputLayerSize = (int) Math.Round(Math.Sqrt((int) vqSizeNumericUpDown.Value));
            var isWinner = new bool[outputLayerSize, outputLayerSize];
            var learningRadius = outputLayerSize / 2;
            var neigborhoodFunction = new GaussianFunction(learningRadius);
            const LatticeTopology topology = LatticeTopology.Hexagonal;
            var max = trainingSet.Max(x => x.Max());
            var min = trainingSet.Min(x => x.Min());
            var inputLayer = new KohonenLayer(trainingSet[0].Length);
            var outputLayer = new KohonenLayer(new Size(outputLayerSize, outputLayerSize), neigborhoodFunction, topology);
            new KohonenConnector(inputLayer, outputLayer) {Initializer = new RandomFunction(min, max)};
            outputLayer.SetLearningRate(0.2, 0.05d);
            outputLayer.IsRowCircular = false;
            outputLayer.IsColumnCircular = false;
            _network = new KohonenNetwork(inputLayer, outputLayer);

            var progress = 1;
            _network.BeginEpochEvent += (senderNetwork, args) => Array.Clear(isWinner, 0, isWinner.Length);

            _network.EndSampleEvent += delegate
            {
                isWinner[_network.Winner.Coordinate.X, _network.Winner.Coordinate.Y] =
                    true;
            };

            _network.EndEpochEvent += delegate
            {
                progressBar1.Value = ((progress++) * 100) / 500;
                PlotWinnersNeurons(isWinner);
                Application.DoEvents();
            };
            var trSet = new TrainingSet(trainingSet[0].Length);
            foreach (var x in trainingSet)
            {
                trSet.Add(new TrainingSample(x));
            }
            _network.Learn(trSet, 500);
            return isWinner;
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
                        if (!useNeuronNetworkCeckBox.Checked)
                        {
                            _vqCodeBook = new VectorQuantization(trainDataLpc, (int) lpcVectorLenghtUpDown.Value, cbSize);
                            PlotCodeBook(_vqCodeBook.CodeBook);
                        }
                        else
                        {
                            var winners = ProvideKohonenCom(trainDataLpc);
                            PlotWinnersNeurons(winners);
                        }
                        break;
                    case "ARC":
                        var trainDataArc = GetArcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataArc);
                        if (!useNeuronNetworkCeckBox.Checked)
                        {
                            _vqCodeBook = new VectorQuantization(trainDataArc, (int)arcVectorLenghtUpDown.Value, cbSize);
                            PlotCodeBook(_vqCodeBook.CodeBook);
                        }
                        else
                        {
                            var winners = ProvideKohonenCom(trainDataArc);
                            PlotWinnersNeurons(winners);
                        }
                        break;
                    case "MFCC":
                        var trainDataMfcc = GetMfccImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataMfcc);
                        if (!useNeuronNetworkCeckBox.Checked)
                        {
                            _vqCodeBook = new VectorQuantization(trainDataMfcc, (int)mfccVectorLenghtUpDown.Value, cbSize);
                            PlotCodeBook(_vqCodeBook.CodeBook);
                        }
                        else
                        {
                            var winners = ProvideKohonenCom(trainDataMfcc);
                            PlotWinnersNeurons(winners);
                        }
                        break;
                    case "VTC":
                        var trainDataVtc = GetVtcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataVtc);
                        if (!useNeuronNetworkCeckBox.Checked)
                        {
                            _vqCodeBook = new VectorQuantization(trainDataVtc, (int) vtcVectorLenghtUpDown.Value - 1,
                                cbSize);
                            PlotCodeBook(_vqCodeBook.CodeBook);
                        }
                        else
                        {
                            var winners = ProvideKohonenCom(trainDataVtc);
                            PlotWinnersNeurons(winners);
                        }
                        break;
                    case "ACF":
                        var corellation = new Corellation();
                        double[][] trainDataAcf;
                        WindowFunctions.WindowType windowType;
                        Enum.TryParse(
                            windowTypeComboBox.SelectedItem as string, out windowType);
                        corellation.AutCorrelationImage(ref speechFile,
                            (int) Math.Round(analysisIntervalUpDown.Value*speechFileFormat.SampleRate),
                            (float) overlappingUpDown.Value/100, out trainDataAcf, windowType,
                            speechFileFormat.SampleRate, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataAcf);
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
            _featureTestDataPlotView.Model.Series.Clear();
            _featureTestDataPlotView.Model.Series.Add(heatMap);
            _featureTestDataPlotView.Model.InvalidatePlot(true);
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
            _featuresTrainDataPlotView.Model.Series.Clear();
            _featuresTrainDataPlotView.Model.Series.Add(heatMap);
            _featuresTrainDataPlotView.Model.InvalidatePlot(true);
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
            _codeBookPlotView.Model.Series.Clear();
            _codeBookPlotView.Model.Series.Add(heatMap);
            _codeBookPlotView.Model.InvalidatePlot(true);
        }

        private void PlotWinnersNeurons(bool[,] winners)
        {
            var outputLayerSize = (int)Math.Round(Math.Sqrt((int)vqSizeNumericUpDown.Value));
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
            _codeBookPlotView.Model.Series.Clear();
            _codeBookPlotView.Model.Series.Add(heatMap);
            _codeBookPlotView.Model.InvalidatePlot(true);
        }

        private void SaveDistortionEnergyToFile(string fileName, double[][] testData)
        {
            using (var writer = new StreamWriter(fileName))
            {
                if (!useNeuronNetworkCeckBox.Checked)
                {
                    for (int i = 0; i < testData.Length; i++)
                    {
                        var distortion = VectorQuantization.QuantizationError(_vqCodeBook.Quantazation(testData[i]),
                            testData[i]);
                        writer.WriteLine(distortion);
                    }
                    writer.WriteLine("---------------");
                    writer.WriteLine(_vqCodeBook.DistortionMeasureEnergy(ref testData));
                }
                else
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

        private void TestPraatLpc()
        {
            var _praatSamplesFolderPath = "C:\\Users\\Bender\\Desktop\\Pitch";
            var _praatTrainingSet = "ГРР1.mat.txt";
            var samples = Directory.GetFiles(_praatSamplesFolderPath, "*.txt");
            var images = new Dictionary<string, double[][]>();
            VectorQuantization vq = null;
            KohonenNetwork network = null;
            KohonenConnector connector;
            var inputLayer = new KohonenLayer(1);
            var outputLayer = new KohonenLayer(new Size(8, 8), new GaussianFunction(4), LatticeTopology.Hexagonal);
            outputLayer.SetLearningRate(0.2, 0.05d);
            outputLayer.IsRowCircular = false;
            outputLayer.IsColumnCircular = false;
            foreach (var sample in samples)
            {
                var featureMatrix = new List<double[]>();
                using (var reader = new StreamReader(sample))
                {
                    var lines = reader.ReadToEnd().Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var values = lines[i].Split(' ').Where(x=> x != "0").Select(x=> x).ToArray();
                        for (int j = 0; j < values.Length && i == 0; j++)
                        {
                            featureMatrix.Add(new double[1]);
                        }
                        for (int j = 0; j < values.Length; j++)
                        {
                            featureMatrix[j][i] = Convert.ToDouble(values[j].Replace('.', ','));
                        }
                    }
                }

                images[sample] = featureMatrix.ToArray();

                if (sample.IndexOf(_praatTrainingSet) > -1)
                {
                    vq = new VectorQuantization(images[sample], 1, 64);
                    var max = images[sample].Max(x => x.Max());
                    var min = images[sample].Min(x => x.Min());
                    new KohonenConnector(inputLayer, outputLayer) { Initializer = new RandomFunction(min, max) };
                    network = new KohonenNetwork(inputLayer, outputLayer);
                    var trainSet = new TrainingSet(1);
                    foreach (var d in images[sample])
                    {
                        trainSet.Add(new TrainingSample(d));
                    }
                    network.Learn(trainSet, 500);
                }
            }
            foreach (var sample in samples)
            {
                using (var writer = new StreamWriter(sample + ".vq_dst.txt"))
                {
                    foreach (var d in images[sample])
                    {
                        writer.WriteLine(VectorQuantization.QuantizationError(vq.Quantazation(d), d));
                    }
                }

                using (var writer = new StreamWriter(sample + ".neuron_dst.txt"))
                {
                    foreach (var d in images[sample])
                    {
                        network.Run(d);
                        var place = new double[1];
                        for (int j = 0; j < network.Winner.SourceSynapses.Count; j++)
                        {
                            place[j] = network.Winner.SourceSynapses[j].Weight;
                        }
                        var distortion = VectorQuantization.QuantizationError(place, d);
                        writer.WriteLine(distortion);
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TestPraatLpc();
        }
    }
}
