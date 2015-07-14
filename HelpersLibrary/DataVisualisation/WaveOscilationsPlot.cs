using System.Globalization;
using System.IO;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;

namespace HelpersLibrary.DataVisualisation
{
    public class WaveOscilationsPlot
    {
        public void PlotToPng(short[] soundWave, string fileName)
        {
            var plotModel = new PlotModel {Culture = CultureInfo.CurrentCulture};
            var linearAxis1 = new LinearAxis
            {
                MajorGridlineStyle = LineStyle.Solid,
                MaximumPadding = 0,
                MinimumPadding = 0,
                MinorGridlineStyle = LineStyle.Dot,
                Position = AxisPosition.Bottom
            };
            plotModel.Axes.Add(linearAxis1);
            var linearAxis2 = new LinearAxis
            {
                MajorGridlineStyle = LineStyle.Solid,
                MaximumPadding = 0,
                MinimumPadding = 0,
                MinorGridlineStyle = LineStyle.Dot
            };
            plotModel.Axes.Add(linearAxis2);
            var series = new LineSeries {Color = OxyColors.Black};

            for(int i = 0; i < soundWave.Length; i++)
                series.Points.Add(new DataPoint(i, soundWave[i]));

            plotModel.Series.Add(series);

            using (var stream = File.Create(fileName))
            {
                var pngExporter = new PngExporter {Height = 512, Width = 1024};
                pngExporter.Export(plotModel, stream);
            }
        }
    }
}
