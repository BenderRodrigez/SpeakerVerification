using System;
using System.Drawing;
using System.Windows.Forms;
using HelpersLibrary.DspAlgorithms;
using NAudio.Wave;
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

        /*--------------Параметры-анализа-----------------------*/
        private static double _intervalAnaliza = 0.09; //Интервал анализа, при расчёте КЛП
        private static int _lpcNumber = 10; //Количество КЛП в одном векторе
        private static int _cepNumber = 13;
        private static int _furieSizePow = 7; //
        private static int _lpcMatrixSize = 1024; //Общее количество векторов КЛП для одного файла
        private static int _codeBookSize = 64; //Размер кодовой книги
        private const WindowFunctions.WindowType Window = WindowFunctions.WindowType.Blackman;//тип применяемой оконной функции
        /*------------------------------------------------------*/

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

            for(var type = HelpersLibrary.DspAlgorithms.WindowFunctions.WindowType.Rectangular; type <= HelpersLibrary.DspAlgorithms.WindowFunctions.WindowType.Blackman; type++)
            {
                windowTypeComboBox.Items.Add(type.ToString());
            }
            windowTypeComboBox.SelectedItem = Window.ToString();
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
                switch (featureSelectComboBox.SelectedItem as string)
                {
                    case "LPC":
                        var trainData = GetLpcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainData);
                        var vq = new HelpersLibrary.LearningAlgorithms.VectorQuantization(trainData, (int)lpcVectorLenghtUpDown.Value, 64);
                        PlotCodeBook(vq.CodeBook);
                        PlotTrainFeatureMatrix(vq.TrainingSet);
                        break;
                    case "ARC":
                        break;
                    case "MFCC":
                        break;
                    case "VTC":
                        break;
                    default:
                        return;
                }
            }
        }

        private double[][] GetLpcImage(WaveFormat speechFileFormat, float[] speechFile, int speechStart, int speechStop)
        {
            double[][] featureMatrix;
            var lpc = new HelpersLibrary.DspAlgorithms.LinearPredictCoefficient
            {
                SamleFrequency = speechFileFormat.SampleRate,
                UsedAcfWindowSizeTime = (float) analysisIntervalUpDown.Value,
                UsedNumberOfCoeficients = (int) lpcVectorLenghtUpDown.Value
            };
            HelpersLibrary.DspAlgorithms.WindowFunctions.WindowType windowType;
            Enum.TryParse(
                windowTypeComboBox.SelectedItem as string, out windowType);
            lpc.UsedWindowType = windowType;
            lpc.Overlapping = ((float)overlappingUpDown.Value) / 100;
            lpc.GetLpcImage(ref speechFile, out featureMatrix, speechStart, speechStop);
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
    }
}
