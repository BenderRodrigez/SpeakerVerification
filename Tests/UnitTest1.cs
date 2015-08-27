using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using HelpersLibrary;
using HelpersLibrary.DspAlgorithms;
using HelpersLibrary.DspAlgorithms.Filters;
using HelpersLibrary.LearningAlgorithms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    [TestClass]
    public class UnitTest1
    {
        private static readonly double[][] VqTrain =
        {
            new[] {0.10880178044658201, 0.0333602908202742},
            new[] {0.06702718966578247, 0.10566825965082047},
            new[] {0.040797167013676194, 0.010524923314763845},
            new[] {0.0010668576132721735, 0.008202345145688753},
            new[] {0.0431740734454282, 0.06376289330656065},
            new[] {0.008326673815518687, 0.03134410909780651},
            new[] {0.048817912973235206, 0.09415362584657916},
            new[] {0.06999198498860949, 0.10658297073861488},
            new[] {0.9437080009030697, 0.19815340756465094},
            new[] {0.8811443309551344, 0.13648518481145563},
            new[] {0.9163804279135579, 0.05412462148103739},
            new[] {0.9994164154798004, 0.06750398153058002},
            new[] {0.9309201718148394, 0.06596494125614852},
            new[] {0.8830821182536766, 0.08387138784434048},
            new[] {0.9741940851864731, 0.11345456460926198},
            new[] {0.8275075578865703, 0.11327074913435119},
            new[] {0.12989124395220045, 0.8885122609621908},
            new[] {0.12893439438489684, 0.8102783339798203},
            new[] {0.17134798509793486, 0.823783305101222},
            new[] {0.1308850022986372, 0.9460218132450433},
            new[] {0.1520898436230279, 0.8850370604435477},
            new[] {0.1981782614130566, 0.8347389560094591},
            new[] {0.15958036940055675, 0.8062660186181377},
            new[] {0.10531982615365927, 0.9833170196546651},
            new[] {0.6819990558305502, 0.5916523569817371},
            new[] {0.6861824216989972, 0.6927330268237232},
            new[] {0.5654271200507945, 0.27123348917588513},
            new[] {0.707944153321513, 0.338347207652261},
            new[] {0.7055156149414885, 0.5736033403654146},
            new[] {0.41855827420186925, 0.6370532165486076},
            new[] {0.5027440924574637, 0.39885381537007847},
            new[] {0.6167582000428806, 0.34922195771282344}
        };

        [TestMethod]
        public void TestVqMethod()
        {
            var vq = new VectorQuantization(VqTrain, 2, 4);
            using (var writer = new StreamWriter(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "vq_codebook.txt")))
            {
                foreach (var d in vq.CodeBook)
                {
                    writer.WriteLine(d.Select(x=> x.ToString(CultureInfo.InvariantCulture)).Aggregate((accumulate, d1) => accumulate + " "+d1.ToString()));
                }
            }
        }

        private static void DrawLpcMatrix(ref double[][] lpc, ref Image graphic)
        {
            using (Graphics.FromImage(graphic))
            {
                var max = lpc.Max(x => x.Max());
                var min = lpc.Min(x => x.Min());
                for (int i = 0; i < graphic.Width; i++)
                {
                    for (int j = 0; j < graphic.Height; j++)
                    {
                        int iTmp = (int)Math.Round((i / ((double)graphic.Width - 1)) * (lpc.Length - 1));
                        if (iTmp >= lpc.Length)
                        {
                            iTmp = lpc.Length - 1;
                        }
                        int jTmp = (int)Math.Round((j / ((double)graphic.Height - 1)) * (lpc[iTmp].Length - 1));
                        if (jTmp >= lpc[iTmp].Length)
                        {
                            jTmp = lpc[iTmp].Length - 1;
                        }
                        int currentVal = (int)Math.Round(((lpc[iTmp][jTmp] - min) / (Math.Abs(max) - min)) * 100.0);
                        var color = SetSpectrogrammPixelColor(currentVal);
                        ((Bitmap)graphic).SetPixel(i, j, color);
                    }
                }
            }
        }

        private static Color SetSpectrogrammPixelColor(int value)
        {
            int red = 128 + (value - 40) * 4;
            if (red < 0)
                red = 0;
            else if (red > 255)
                red = 255;
            int green = (255 - (100 - value) * 5);
            if (green < 0)
                green = 0;
            if (green > 255)
                green = 255;
            int blue = (value * 8);
            if (blue > 255)
                blue = 255;
            if (blue < 0)
                blue = 0;
            return Color.FromArgb(0xff, red, green, blue);
        }
    }
}
