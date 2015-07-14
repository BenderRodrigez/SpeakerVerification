using System.Globalization;
using System.IO;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace HelpersLibrary.DataVisualisation
{
    public class MatrixDataPlot
    {
        public void PlotToPng(double[][] data, string fileName)
        {
            var plotModel = new PlotModel { Culture = CultureInfo.CurrentCulture };
            var linearColorAxis1 = new LinearColorAxis
            {
                HighColor = OxyColors.White,
                LowColor = OxyColors.Black,
                Position = AxisPosition.Right,
                Palette = OxyPalettes.Hot(200),
            };
            plotModel.Axes.Add(linearColorAxis1);
            var linearAxis1 = new LinearAxis {Position = AxisPosition.Bottom};
            plotModel.Axes.Add(linearAxis1);
            var linearAxis2 = new LinearAxis();
            plotModel.Axes.Add(linearAxis2);

            var series = new HeatMapSeries
            {
                Data = new double[data.Length, data[0].Length],
                X0 = 0,
                X1 = data.Length,
                Y0 = 0,
                Y1 = data[0].Length,
                Interpolate = false
            };
            for(int i = 0; i < data.Length; i++)
                for (int j = 0; j < data[i].Length; j++)
                {
                    series.Data[i, j] = data[i][j];
                }

            plotModel.Series.Add(series);

            using (var stream = File.Create(fileName))
            {
                var pngExporter = new PngExporter { Height = 512, Width = 1024 };
                pngExporter.Export(plotModel, stream);
            }
        }
    }
}
