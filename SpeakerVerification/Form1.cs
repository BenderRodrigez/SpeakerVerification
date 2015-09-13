﻿using System;
using System.Windows.Forms;
using HelpersLibrary.DspAlgorithms;
using HelpersLibrary.LearningAlgorithms;
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

            for(var type = WindowFunctions.WindowType.Rectangular; type <= WindowFunctions.WindowType.Blackman; type++)
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
                        var trainDataLpc = GetLpcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataLpc);
                        var vqLpc = new VectorQuantization(trainDataLpc, (int)lpcVectorLenghtUpDown.Value, 64);
                        PlotCodeBook(vqLpc.CodeBook);
                        PlotTrainFeatureMatrix(vqLpc.TrainingSet);
                        break;
                    case "ARC":
                        var trainDataArc = GetArcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataArc);
                        var vqArc = new VectorQuantization(trainDataArc, (int)arcVectorLenghtUpDown.Value, 64);
                        PlotCodeBook(vqArc.CodeBook);
                        PlotTrainFeatureMatrix(vqArc.TrainingSet);
                        break;
                    case "MFCC":
                        var trainDataMfcc = GetMfccImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataMfcc);
                        var vqMfcc = new VectorQuantization(trainDataMfcc, (int)mfccVectorLenghtUpDown.Value, 64);
                        PlotCodeBook(vqMfcc.CodeBook);
                        PlotTrainFeatureMatrix(vqMfcc.TrainingSet);
                        break;
                    case "VTC":
                        var trainDataVtc = GetVtcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTrainFeatureMatrix(trainDataVtc);
                        var vqVtc = new VectorQuantization(trainDataVtc, (int)vtcVectorLenghtUpDown.Value, 64);
                        PlotCodeBook(vqVtc.CodeBook);
                        PlotTrainFeatureMatrix(vqVtc.TrainingSet);
                        break;
                    default:
                        return;
                }
            }
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
                        break;
                    case "ARC":
                        var testDataArc = GetArcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTestFeatureMatrix(testDataArc);
                        break;
                    case "MFCC":
                        var trainDataMfcc = GetMfccImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTestFeatureMatrix(trainDataMfcc);
                        break;
                    case "VTC":
                        var testDataVtc = GetVtcImage(speechFileFormat, speechFile, speechStartPosition, speechStopPosition);
                        PlotTestFeatureMatrix(testDataVtc);
                        break;
                    default:
                        return;
                }
            }
        }
    }
}
