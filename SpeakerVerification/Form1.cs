using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;

// ReSharper disable RedundantArgumentDefaultValue

namespace SpeakerVerification
{
    public partial class Form1 : Form
    {
        private string _fileName1, _fileName2;
        private float[] _wave1, _wave2;
        private WaveFormat _formatWave1, _formatWave2;
        private Image _graphic1, _graphic2;
        private VectorQuantization _vq1;//, _vq2;
        private double[][] _lpc1, _lpc2;
        private readonly TaskFactory _factory = new TaskFactory();

        /*--------------Параметры-анализа-----------------------*/
        private const double IntervalAnaliza = 0.06; //Интервал анализа, при расчёте КЛП
        private const byte LpcNumber = 16; //Количество КЛП в одном векторе
        private const int LpcMatrixSize = 1024; //Общее количество векторов КЛП для одного файла
        private const int CodeBookSize = 64; //Размер кодовой книги
        private const Corellation.WindowType Window = Corellation.WindowType.Hamming; //тип применяемой оконной функции
        /*------------------------------------------------------*/

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _fileName1 = openFileDialog1.FileName;

                ReadFile(out _wave1, _fileName1, out _formatWave1);
                lpc1 = new LinearPredictCoefficient
                    {
                        UsedWindowType = Window,
                        UsedNumberOfCoeficients = LpcNumber,
                        UsedAcfWindowSize = (int) Math.Round(IntervalAnaliza*_formatWave1.SampleRate),
                        UsedAcfWindowSizeTime = IntervalAnaliza,
                        SamleFrequency = _formatWave1.SampleRate,
                        ImageLenght = LpcMatrixSize
                    };

                lpc1.GetLpcImage(ref _wave1, out _lpc1);
                _vq1 = new VectorQuantization(_lpc1, LpcNumber, CodeBookSize);
                
                _graphic1 = new Bitmap(pictureBox1.ClientSize.Width, pictureBox1.ClientSize.Height);
                pictureBox1.Image = _graphic1;
                DrawLpcMatrix(ref _lpc1, ref _graphic1);
                DrawAxes(ref _graphic1, ref _lpc1);
                //using (StreamWriter writer = new StreamWriter("file1.txt"))
                //{
                //    for (int i = 0; i < vq1.CodeBook.Length; i++)
                //    {
                //        for (int j = 0; j < vq1.CodeBook[i].Length; j++)
                //            writer.Write(vq1.CodeBook[i][j].ToString() + " ");
                //        writer.WriteLine();
                //    }
                //}
            }
        }

        private void DrawLpc(ref double[][] lpc, ref Image graphic)
        {
            var max = lpc.Max(x => x.Max());
            var min = lpc.Min(x => x.Min());
            for (int i = 0, graphicIndex = 0; i < lpc.Length && graphicIndex < graphic.Width; i += lpc.Length / graphic.Width, graphicIndex++)
            {//идём по времени
                for (int j = 0; j < lpc[i].Length; j++)
                {//идём по коэфициенту
                    for (int k = 0; k < (double)graphic.Height / lpc[i].Length; k++)
                        ((Bitmap) graphic).SetPixel(graphicIndex, (j * graphic.Height / lpc[i].Length) + k, GetLPCColor(lpc[i][j], max, min));
                }
            }
        }

        private static void DrawLpcMatrix(ref double[][] lpc, ref Image graphic)
        {
            using (var gr = Graphics.FromImage(graphic))
            {
                double max = lpc.Max(x => x.Max());
                double min = lpc.Min(x => x.Min());
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
                        ((Bitmap) graphic).SetPixel(i, j, color);
                    }
                }
                gr.DrawString("Max = " + max, DefaultFont, Brushes.White, 20, 30);
                gr.DrawString("Min = " + min, DefaultFont, Brushes.White, 20, 40);
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

        private static void DrawAxes(ref Image graphic, ref double[][] lpc)
        {
            using (var gr = Graphics.FromImage(graphic))
            {
                //Start axes
                for (int i = 0; i < graphic.Height; i += graphic.Height / 16)
                {
                    if (i + graphic.Height / 16 <= graphic.Height)
                    {
                        gr.DrawLine(Pens.White, 0, i, 5, i);
                        gr.DrawLine(Pens.White, graphic.Width - 5, i, graphic.Width, i);
                        //int position = (int)Math.Round((double)graphic.Height - i);
                        //gr.DrawString(Position.ToString(), DefaultFont, Brushes.White, 2, i);
                    }
                    else
                    {
                        gr.DrawLine(Pens.White, 0, graphic.Height - 1, 5, graphic.Height - 1);
                        gr.DrawLine(Pens.White, graphic.Width - 5, graphic.Height - 1, graphic.Width, graphic.Height - 1);
                    }
                }
                for (int i = 0; i < graphic.Width; i += graphic.Width / 16)
                {
                    gr.DrawLine(Pens.White, i, graphic.Height, i, graphic.Height - 5);
                    gr.DrawLine(Pens.White, i, 0, i, 5);
                    gr.DrawString((Math.Round(i * (lpc.Length / (float)graphic.Width))).ToString(CultureInfo.CurrentCulture), DefaultFont, Brushes.White, i, graphic.Height - DefaultFont.Height);
                }
                //end axes
            }
        }

        private Color GetLPCColor(double value, double max, double min)
        {
            return value > 0 ? Color.FromArgb((int)Math.Round((value + Math.Abs(min)) * 255.0 / Math.Abs(max + Math.Abs(min))), 0, 0) : Color.FromArgb(0, 0, (int)Math.Round((value + Math.Abs(min)) * 255.0 / Math.Abs(max + Math.Abs(min))));
        }

        private void ReadFile(out float[] soundArray, string fileName, out WaveFormat waveFormat)
        {
            using (var reader = new WaveFileReader(fileName))
            {
                waveFormat = reader.WaveFormat;
                soundArray = new float[reader.SampleCount];
                for (int i = 0; i < soundArray.Length; i++)
                    soundArray[i] = reader.ReadNextSampleFrame()[0];
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _fileName2 = openFileDialog1.FileName;

                ReadFile(out _wave2, _fileName2, out _formatWave2);

                lpc2 = new LinearPredictCoefficient
                {
                    UsedWindowType = Window,
                    UsedNumberOfCoeficients = LpcNumber,
                    UsedAcfWindowSize = (int)Math.Round(IntervalAnaliza * _formatWave1.SampleRate),
                    UsedAcfWindowSizeTime = IntervalAnaliza,
                    SamleFrequency = _formatWave1.SampleRate,
                    ImageLenght = LpcMatrixSize
                };

                lpc2.GetLpcImage(ref _wave2, out _lpc2);
                //vq2 = new VectorQuantization(LPC2, LPCNumber, CodeBookSize);
                _graphic2 = new Bitmap(pictureBox2.ClientSize.Width, pictureBox2.ClientSize.Height);
                pictureBox2.Image = _graphic2;
                DrawLpcMatrix(ref _lpc2, ref _graphic2);
                DrawAxes(ref _graphic2, ref _lpc2);
                //using (StreamWriter writer = new StreamWriter("file2.txt"))
                //{
                //    for (int i = 0; i < vq2.CodeBook.Length; i++)
                //    {
                //        for (int j = 0; j < vq2.CodeBook[i].Length; j++)
                //            writer.Write(vq2.CodeBook[i][j].ToString() + " ");
                //        writer.WriteLine();
                //    }
                //}
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                //TODO: Calc autocorrelation
                //double[] coellation;
                //Comparer.
            }
            else
            {
                //TODO: Show LPC1 values
                DrawLpc(ref _lpc1, ref _graphic1);
                DrawAxes(ref _graphic1, ref _lpc1);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)+"//FirstToSecond.txt"))
            {
                for (int i = 0; i < _lpc2.Length; i++)
                {
                    writer.WriteLine(_vq1.QuantizationError(_lpc2[i], _vq1.Quantazation(_lpc2[i])).ToString(CultureInfo.CurrentCulture));
                }

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            double[][] spectrogram;
            double[][] formantsTrack;
            double[][] formantsLenght;
            double[][] formantsApms;
            double[][] formantsEnergy;
            double[][] arc;
            double[][] pc;

            lpc1.GetAproximatedSpectrogramm(ref _lpc1, out spectrogram, 256);
            lpc1.GetFormants(ref _lpc1, out formantsTrack, out formantsLenght, out formantsApms, out formantsEnergy, 256, _formatWave1.SampleRate);
            lpc1.GetArcAndPcImages(ref _lpc1, out arc, out pc, 256);

            var culture = CultureInfo.CreateSpecificCulture("en-US");

            using (var writer = new StreamWriter("spectrogram.txt"))
            {
                for (int i = 0; i < spectrogram.Length; i++)
                {
                    for (int j = 0; j < spectrogram[i].Length; j++)
                    {
                        writer.Write(spectrogram[i][j] + " ");
                    }
                    writer.WriteLine();
                }
            }

            using (var writer = new StreamWriter("ARC.txt"))
            {

                for (int i = 0; i < arc.Length; i++)
                {
                    for (int j = 0; j < arc[i].Length; j++)
                        writer.Write(arc[i][j].ToString(culture) + " ");
                    writer.WriteLine();
                }
            }

            using (var writer = new StreamWriter("formants.txt"))
            {
                
                for (int i = 0; i < formantsTrack.Length; i++)
                {
                    for (int j = 0; j < formantsTrack[i].Length; j++)
                        writer.Write(formantsTrack[i][j] + " ");
                    writer.WriteLine();
                }
            }

            using (var writer = new StreamWriter("formantsEnergy.txt"))
            {

                for (int i = 0; i < formantsEnergy.Length; i++)
                {
                    for (int j = 0; j < formantsEnergy[i].Length; j++)
                        writer.Write(formantsEnergy[i][j] + " ");
                    writer.WriteLine();
                }
            }

            using (var writer = new StreamWriter("formantsLenght.txt"))
            {
                for (int i = 0; i < formantsLenght.Length; i++)
                {
                    for (int j = 0; j < formantsLenght[i].Length; j++)
                        writer.Write(formantsLenght[i][j] + " ");
                    writer.WriteLine();
                }
            }

            using (var writer = new StreamWriter("formantsAmplitude.txt"))
            {
                for (int i = 0; i < formantsApms.Length; i++)
                {
                    for (int j = 0; j < formantsApms[i].Length; j++)
                        writer.Write(formantsApms[i][j] + " ");
                    writer.WriteLine();
                }
            }

            _graphic2 = new Bitmap(pictureBox2.ClientSize.Width, pictureBox2.ClientSize.Height);
            pictureBox2.Image = _graphic2;
            DrawLpcMatrix(ref spectrogram, ref _graphic2);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if(folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
            {
                var directories = Directory.GetDirectories(folderBrowserDialog1.SelectedPath);
                progressBar1.Maximum = 4900500;
                foreach (var directory in directories)
                {
                    _factory.StartNew(Experiment, directory);
                }
            }
        }

        private void Inc()
        {
            try
            {
                progressBar1.Increment(1);
            }
            catch (Exception)
            {
                if (progressBar1 != null) progressBar1.Maximum *= 2;
            }
        }

        private void SetTimeSpanValue(TimeSpan time)
        {
            label1.Text = time.TotalSeconds.ToString(CultureInfo.InvariantCulture);
        }

        private void Experiment(object dir)
        {
            var directory = (string)dir;
            //var content = directory.Split('\\').Last();// lalalalla\\A
            var result = new List<string>(445500);

            var files = Directory.GetFiles(directory, "*.wav");

            var filesData = new float[files.Length][];
            var fileFormats = new WaveFormat[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                ReadFile(out filesData[i], files[i], out fileFormats[i]);
            }

            for (var windowSize = 0.01; windowSize < 0.10; windowSize += 0.02)
            {
                for (var codeBookSize = 32; codeBookSize < 257; codeBookSize *= 2)
                {
                    for (var imageLenght = 128; imageLenght < 2049; imageLenght *= 2)
                    {
                        for (var i = 0; i < files.Length; i++)
                        {
                            for (var j = 0; j < files.Length; j++)
                            {
                                for (var vectorLenghtLpc = 4; vectorLenghtLpc < 25; vectorLenghtLpc += 2)
                                {
                                    double[][] codeBookLpc;
                                    var dtStart = DateTime.Now;
                                    var trainLpc = new LinearPredictCoefficient
                                        {
                                            UsedWindowType = Corellation.WindowType.Hamming,
                                            UsedNumberOfCoeficients = (byte) vectorLenghtLpc,
                                            UsedAcfWindowSize = (int) Math.Round(windowSize*fileFormats[i].SampleRate),
                                            UsedAcfWindowSizeTime = windowSize,
                                            SamleFrequency = fileFormats[i].SampleRate,
                                            ImageLenght = LpcMatrixSize
                                        };

                                    trainLpc.GetLpcImage(ref filesData[i], out codeBookLpc);

                                    var vq = new VectorQuantization(codeBookLpc, vectorLenghtLpc, codeBookSize);

                                    double[][] testLpc;
                                    var testingLpc = new LinearPredictCoefficient
                                        {
                                            UsedWindowType = Corellation.WindowType.Hamming,
                                            UsedNumberOfCoeficients = (byte) vectorLenghtLpc,
                                            UsedAcfWindowSize = (int) Math.Round(windowSize*fileFormats[j].SampleRate),
                                            UsedAcfWindowSizeTime = windowSize,
                                            SamleFrequency = fileFormats[j].SampleRate,
                                            ImageLenght = LpcMatrixSize
                                        };

                                    trainLpc.GetLpcImage(ref filesData[j], out testLpc);

                                    var energy = vq.DistortionMeasureEnergy(ref testLpc);

                                    result.Add(
                                        string.Concat("[---|CodeBook:", Path.GetFileName(files[i]), "|TestFile:",
                                            Path.GetFileName(files[j]), "|WindowSize:", windowSize, "|CodebookSize:",
                                            codeBookSize, "|ImageLenght:", imageLenght, "|VectorType:LPC|VectorSize:",
                                            vectorLenghtLpc, "|---]:", energy));

                                    for (var vectorLenghtArc = 32; vectorLenghtArc < 257; vectorLenghtArc *= 2)
                                    {
                                        double[][] codeBookArc;
                                        trainLpc.GetArcImage(ref codeBookLpc, out codeBookArc,
                                            vectorLenghtArc);
                                        var vqArc = new VectorQuantization(codeBookArc, vectorLenghtArc, codeBookSize);

                                        double[][] testArc;
                                        testingLpc.GetArcImage(ref testLpc, out testArc, vectorLenghtArc);
                                        var energyArc = vqArc.DistortionMeasureEnergy(ref testArc);

                                        result.Add(
                                        string.Concat("[---|CodeBook:", Path.GetFileName(files[i]), "|TestFile:",
                                            Path.GetFileName(files[j]), "|WindowSize:", windowSize, "|CodebookSize:",
                                            codeBookSize, "|ImageLenght:", imageLenght, "|VectorType:ARC|VectorSize:",
                                            vectorLenghtLpc, "|---]:", energyArc));
                                        progressBar1.Invoke(new Action(Inc));
                                    } //ARC vectors
                                    var dtStop = DateTime.Now;
                                    label1.Invoke(new Action<TimeSpan>(SetTimeSpanValue), dtStop - dtStart);
                                } //LPC vectors
                            } //testfiles
                        } //codebooks
                    } //imageLenght
                } //codebook size
            } //window size


            using (var writer = new StreamWriter(directory + "\\report.txt"))
            {
                for (int i = 0; i < result.Count; i++)
                {
                    writer.WriteLine(result[i]);
                }
            } //report
        }
    }
}
