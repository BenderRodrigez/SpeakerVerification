using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using NAudio;
using System.IO;

namespace SpeakerVerification
{
    public partial class Form1 : Form
    {
        private string FileName1, FileName2;
        private float[] Wave1, Wave2;
        private NAudio.Wave.WaveFormat FormatWave1, FormatWave2;
        private Image graphic1, graphic2;
        private VectorQuantization vq1, vq2;
        private double[][] LPC1, LPC2;

        /*--------------Параметры-анализа-----------------------*/
        private double intervalAnaliza = 0.03;//Интервал анализа, при расчёте КЛП
        private byte LPCNumber = 20;//Количество КЛП в одном векторе
        private int LPCMatrixSize = 1024;//Общее количество векторов КЛП для одного файла
        private int CodeBookSize = 64;//Размер кодовой книги
        /*------------------------------------------------------*/

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileName1 = openFileDialog1.FileName;

                ReadFile(out Wave1, FileName1, out FormatWave1);

                LinearPredictCoefficient.CalcLPCFunction(ref Wave1, FormatWave1.SampleRate, intervalAnaliza, out LPC1, LPCNumber, LPCMatrixSize);
                vq1 = new VectorQuantization(LPC1, LPCNumber, CodeBookSize);
                var tmpSet = LPC1.Where(x => x.Max() == double.PositiveInfinity).Select(x => x);
                graphic1 = new Bitmap(pictureBox1.ClientSize.Width, pictureBox1.ClientSize.Height);
                pictureBox1.Image = graphic1;
                DrawLPCMatrix(ref LPC1, ref graphic1);
                DrawAxes(ref graphic1, ref LPC1);
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

        private void DrawLPC(ref double[][] LPC, ref Image graphic)
        {
            double max = LPC.Max(x => x.Max());
            double min = LPC.Min(x => x.Min());
            for (int i = 0, graphicIndex = 0; i < LPC.Length && graphicIndex < graphic.Width; i += LPC.Length / graphic.Width, graphicIndex++)
            {//идём по времени
                for (int j = 0; j < LPC[i].Length; j++)
                {//идём по коэфициенту
                    for (int k = 0; k < (double)graphic.Height / LPC[i].Length; k++)
                        (graphic as Bitmap).SetPixel(graphicIndex, (j * graphic.Height / LPC[i].Length) + k, GetLPCColor(LPC[i][j], max, min));
                }
            }
        }

        private void DrawLPCMatrix(ref double[][] LPC, ref Image graphic)
        {
            using (var gr = Graphics.FromImage(graphic))
            {
                double max = LPC.Max(x => x.Max());
                double min = LPC.Min(x => x.Min());
                for (int i = 0; i < graphic.Width; i++)
                {
                    for (int j = 0; j < graphic.Height; j++)
                    {
                        int iTmp = (int)Math.Round((i / ((double)graphic.Width - 1)) * (LPCMatrixSize - 1));
                        int jTmp = (int)Math.Round((j / ((double)graphic.Height - 1)) * (LPCNumber - 1));
                        int currentVal = (int)Math.Round(((LPC[iTmp][jTmp] + Math.Abs(min)) / (Math.Abs(max) + Math.Abs(min))) * 100);
                        var color = SetSpectrogrammPixelColor(currentVal);
                        (graphic as Bitmap).SetPixel(i, j, color);
                    }
                }
                gr.DrawString("Max = " + max.ToString(), DefaultFont, Brushes.White, 20, 30);
                gr.DrawString("Min = " + min.ToString(), DefaultFont, Brushes.White, 20, 40);
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

        private void DrawAxes(ref Image graphic, ref double[][] LPC)
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
                        int Position = (int)Math.Round((double)graphic.Height - i);
                        gr.DrawString(Position.ToString(), DefaultFont, Brushes.White, 2, i);
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
                    gr.DrawString((Math.Round(i * (LPC.Length / (float)graphic.Width))).ToString(), DefaultFont, Brushes.White, i, graphic.Height - DefaultFont.Height);
                }
                //end axes
            }
        }

        private Color GetLPCColor(double value, double max, double min)
        {
            if (value > 0)
            {
                //если в верхней половине, то кресненьким
                return Color.FromArgb((int)Math.Round((value + Math.Abs(min)) * 255.0 / Math.Abs(max + Math.Abs(min))), 0, 0);
            }
            else
            {
                //иначе синеньким
                return Color.FromArgb(0, 0, (int)Math.Round((value + Math.Abs(min)) * 255.0 / Math.Abs(max + Math.Abs(min))));
            }
        }

        private void ReadFile(out float[] soundArray, string fileName, out NAudio.Wave.WaveFormat waveFormat)
        {
            using (NAudio.Wave.WaveFileReader reader = new NAudio.Wave.WaveFileReader(fileName))
            {
                waveFormat = reader.WaveFormat;
                soundArray = new float[reader.SampleCount];
                for (int i = 0; i < soundArray.Length; i++)
                    soundArray[i] = reader.ReadNextSampleFrame()[0];
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileName2 = openFileDialog1.FileName;

                ReadFile(out Wave2, FileName2, out FormatWave2);

                LinearPredictCoefficient.CalcLPCFunction(ref Wave2, FormatWave2.SampleRate, intervalAnaliza, out LPC2, LPCNumber, LPCMatrixSize);
                vq2 = new VectorQuantization(LPC2, LPCNumber, CodeBookSize);
                graphic2 = new Bitmap(pictureBox2.ClientSize.Width, pictureBox2.ClientSize.Height);
                pictureBox2.Image = graphic2;
                DrawLPCMatrix(ref LPC2, ref graphic2);
                DrawAxes(ref graphic2, ref LPC2);
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
                double[] coellation;
                //Comparer.
            }
            else
            {
                //TODO: Show LPC1 values
                DrawLPC(ref LPC1, ref graphic1);
                DrawAxes(ref graphic1, ref LPC1);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (StreamWriter writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)+"//FirstToSecond.txt"))
            {
                for (int i = 0; i < LPC2.Length; i++)
                {
                    writer.WriteLine(vq1.QuantizationError(LPC2[i], vq1.Quantazation(LPC2[i])).ToString());
                }

            }
            //using(StreamWriter writer = new StreamWriter("energy.txt"))
            //{//не рабочий метод
            //    double[] energy = new double[10], selfEnergy = new double[10];
            //    for (int i = 0; i < 10; i++)
            //    {
            //        energy[i] = vq1.DistortionMeasureEnergy(ref LPC1, ref LPC2, i, 10);
            //        selfEnergy[i] = vq1.DistortionMeasureEnergy(ref LPC1, ref LPC1, i, 10);
            //        writer.WriteLine(energy[i].ToString());
            //    }
            //    //label1.Text = "Решение: " + Verificator.MakeDecision(energy, selfEnergy, 10).ToString();
            //}
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folder = folderBrowserDialog1.SelectedPath;
                string[] files = Directory.GetFiles(folder, "*.wav");
                var shortNamesFiles = files.Select(x => x.Split('\\').Last()).ToArray();
                progressBar1.Maximum = (int)(360 * Math.Pow(files.Length, 2));

                using (StreamWriter writer = new StreamWriter("report.txt"), wrtiter2 = new StreamWriter("summary.txt"))
                {
                    for (int LPCImageSize = 512; LPCImageSize < 4097; LPCImageSize*=2)
                    {
                        int LPCImageSizeCount = 0;
                        int LPCImageSizeCountTotal = 0;
                        for (int bookSize = 16; bookSize < 1025; bookSize *= 2)
                        {
                            int bookSizeCount = 0;
                            int bookSizeCountTotal = 0;
                            for (double ACFWindow = 0.05; ACFWindow < 0.26; ACFWindow += 0.05)
                            {
                                int ACFWindowCount = 0;
                                int ACFWindowCountTotal = 0;
                                for (byte LPCNumber = 10; LPCNumber <= 20; LPCNumber += 2)
                                {
                                    int LPCNumberCount = 0;
                                    int LPCNumberCountTotal = 0;
                                    writer.WriteLine("----------------------------------------------------------------------------------------------------------");
                                    writer.WriteLine("Размер сжатого изображения КЛП: {0}", LPCImageSize);
                                    writer.WriteLine("Размер кодовой книги: {0}", bookSize);
                                    writer.WriteLine("Количество КЛП: {0}", LPCNumber);
                                    writer.WriteLine("Ширина окна: {0}", ACFWindow);
                                    var now = DateTime.Now;
                                    int totalPassed = 0;
                                    int rightSolution = 0;
                                    for (int i = 0; i < files.Length; i++)
                                    {
                                        string user = shortNamesFiles[i].Substring(0, 3);
                                        writer.WriteLine("Используется кодовая книга для файла: {0}", shortNamesFiles[i]);
                                        for (int j = 0; j < files.Length; j++)
                                        {
                                            writer.WriteLine("Сравниваем с файлом: {0}", shortNamesFiles[j]);
                                            ReadFile(out Wave1, files[i], out FormatWave1);

                                            LinearPredictCoefficient.CalcLPCFunction(ref Wave1, FormatWave1.SampleRate, ACFWindow, out LPC1, LPCNumber, LPCImageSize);
                                            vq1 = new VectorQuantization(LPC1, LPCNumber, bookSize);

                                            ReadFile(out Wave2, files[j], out FormatWave2);

                                            LinearPredictCoefficient.CalcLPCFunction(ref Wave2, FormatWave1.SampleRate, ACFWindow, out LPC2, LPCNumber, LPCImageSize);
                                            vq2 = new VectorQuantization(LPC2, LPCNumber, bookSize);

                                            double[] enrg = new double[LPCImageSize];
                                            for (int energys = 0; energys < LPCImageSize; energys++)
                                            {
                                                enrg[energys] = vq1.QuantizationError(LPC2[energys], vq1.Quantazation(LPC2[energys]));
                                            }

                                            bool solution = Verificator.MakeDecision(enrg, vq1.AverageDistortionMeasure);
                                            if (solution)
                                            {
                                                totalPassed++;
                                                LPCImageSizeCountTotal++;
                                                LPCNumberCountTotal++;
                                                bookSizeCountTotal++;
                                                ACFWindowCountTotal++;
                                            }
                                            if (shortNamesFiles[j].IndexOf(user) > -1 && solution)
                                            {
                                                rightSolution++;
                                                LPCImageSizeCount++;
                                                LPCNumberCount++;
                                                bookSizeCount++;
                                                ACFWindowCount++;
                                            }
                                            writer.WriteLine("Образец совпадает: " + solution.ToString());
                                            progressBar1.Value++;
                                        }//files2
                                    }//files1
                                    var now2 = DateTime.Now;
                                    var timeAverage = DateTime.FromBinary((long)Math.Round(((now - now2).Ticks / (double)progressBar1.Value) * progressBar1.Maximum));
                                    label1.Text = timeAverage.ToString();
                                    wrtiter2.WriteLine("[summary] LPCSz: {0}; BkSz: {1}; LPC {2}; WndSz: {3}, | TotalPassed: {4}; RightPassed: {5}", LPCImageSize, bookSize, LPCNumber, ACFWindow, totalPassed, rightSolution);
                                    wrtiter2.WriteLine("[LPCNumber] LPCNumber total passed: {0}; LPCNumber right passed {1}; Value: {2}", LPCNumberCountTotal, LPCNumberCount, LPCNumber);
                                }//LPC
                                wrtiter2.WriteLine("[ACFWindow] ACFWindow total passed: {0}; ACFWindow right passed {1}; Value: {2}", ACFWindowCountTotal, ACFWindowCount, ACFWindow);
                            }//ACF
                            wrtiter2.WriteLine("[bookSize] bookSize total passed: {0}; bookSize right passed {1}; Value: {2}", bookSizeCountTotal, bookSizeCount, bookSize);
                        }//book size
                        wrtiter2.WriteLine("[LPCImageSize] LPCImageSize total passed: {0}; LPCImageSize right passed {1}; Value: {2}", LPCImageSizeCountTotal, LPCImageSizeCount, LPCImageSize);
                    }//useWindow
                }//using
            }//if
        }
    }
}
