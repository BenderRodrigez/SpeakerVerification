using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        public PlotView PlotView1;
        public PlotView PlotView2;

        private string _fileName1, _fileName2;
        private float[] _wave1, _wave2;
        private WaveFormat _formatWave1, _formatWave2;
        private Image _graphic1, _graphic2;
        private VectorQuantization _vq1, _vq2;
        private double[][] _image1, _image2;
        private Cepstrum _cep1, _cep2;

        private int _activeVoiceStart1, _activeVoiceStart2, _activeVoiceStop1, _activeVoiceStop2;

        /*--------------Параметры-анализа-----------------------*/
        private static double _intervalAnaliza = 0.09; //Интервал анализа, при расчёте КЛП
        private static int _lpcNumber = 10; //Количество КЛП в одном векторе
        private static int _cepNumber = 13;
        private static int _furieSizePow = 7; //TODO:привязать к интервалу анализа
        private static int _lpcMatrixSize = 1024; //Общее количество векторов КЛП для одного файла
        private static int _codeBookSize = 64; //Размер кодовой книги

        private const WindowFunctions.WindowType Window = WindowFunctions.WindowType.Blackman;
            //тип применяемой оконной функции

        /*------------------------------------------------------*/

        public Form1()
        {
            InitializeComponent();
            //PlotView1 = new PlotView();
            //var model = new PlotModel
            //{
            //    PlotType = PlotType.XY,
            //    Background = OxyColors.White,
            //    TextColor = OxyColors.Black
            //};
            //PlotView1.Model = model;

            //PlotView1.Width = 512;
            //PlotView1.Height = 64;

            //PlotView1.Top = 10;
            //PlotView1.Left = 10;
            //groupBox1.Controls.Add(PlotView1);
            //var linearAxis1 = new LinearAxis
            //{
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MaximumPadding = 0,
            //    MinimumPadding = 0,
            //    MinorGridlineStyle = LineStyle.Dot,
            //    Position = AxisPosition.Bottom
            //};
            //PlotView1.Model.Axes.Add(linearAxis1);
            //var linearAxis2 = new LinearAxis
            //{
            //    MajorGridlineStyle = LineStyle.Solid,
            //    MaximumPadding = 0,
            //    MinimumPadding = 0,
            //    MinorGridlineStyle = LineStyle.Dot
            //};
            //PlotView1.Model.Axes.Add(linearAxis2);
            //var series = new LineSeries { Color = OxyColors.Black };

            //var rand = new Random();
            //for (int i = 0; i < 3000; i++)
            //    series.Points.Add(new DataPoint(i, rand.NextDouble()));

            //PlotView1.Model.Series.Add(series);

            //PlotView2 = new PlotView();
            //var model2 = new PlotModel
            //{
            //    PlotType = PlotType.XY,
            //    Background = OxyColors.White,
            //    TextColor = OxyColors.Black
            //};
            //PlotView2.Model = model2;

            //PlotView2.Width = 512;
            //PlotView2.Height = 64;

            //PlotView2.Top = 74;
            //PlotView2.Left = 10;
            //groupBox1.Controls.Add(PlotView2);

        }
    }
}
