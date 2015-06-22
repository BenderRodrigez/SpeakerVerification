using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using System.Diagnostics;

// ReSharper disable RedundantArgumentDefaultValue

namespace SpeakerVerification
{
    public partial class Form1 : Form
    {
        private string _fileName1, _fileName2;
        private float[] _wave1, _wave2;
        private WaveFormat _formatWave1, _formatWave2;
        private Image _graphic1, _graphic2;
        private VectorQuantization _vq1, _vq2;
        private double[][] _image1, _image2;
        private readonly TaskFactory _factory = new TaskFactory();
        private Cepstrum _cep1, _cep2;
        private string _vadProgram = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\YandexDisk\\Documents\\Projects\\VAD\\VAD\\bin\\Debug\\VAD.exe";
        private int _activeVoiceStart1, _activeVoiceStart2, _activeVoiceStop1, _activeVoiceStop2;

        /*--------------Параметры-анализа-----------------------*/
        private static double _intervalAnaliza = 0.09; //Интервал анализа, при расчёте КЛП
        private static int _lpcNumber = 10; //Количество КЛП в одном векторе
        private static int _cepNumber = 13;
        private static int _furieSizePow = 7;//TODO:привязать к интервалу анализа
        private static int _lpcMatrixSize = 1024; //Общее количество векторов КЛП для одного файла
        private static int _codeBookSize = 64; //Размер кодовой книги
        private const WindowFunctions.WindowType Window = WindowFunctions.WindowType.Blackman; //тип применяемой оконной функции
        /*------------------------------------------------------*/

        public Form1()
        {
            InitializeComponent();

            codeBookCombo.SelectedItem = _codeBookSize.ToString();
            windowSizeCombo.SelectedItem = _intervalAnaliza.ToString();
            imageLenghtCombo.SelectedItem = _lpcMatrixSize.ToString();
            vectorLenght.Value = _lpcNumber;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _fileName1 = openFileDialog1.FileName;
                if(_vadProgram != null && !File.Exists(_fileName1+".txt"))
                {
                    ProcessVad(_fileName1);
                }
                ReadFile(out _wave1, _fileName1, out _formatWave1, out _activeVoiceStart1, out _activeVoiceStop1);
                lpc1 = new LinearPredictCoefficient
                    {
                        UsedWindowType = Window,
                        UsedNumberOfCoeficients = _lpcNumber,
                        UsedAcfWindowSize = (int)Math.Round(_intervalAnaliza * _formatWave1.SampleRate),
                        UsedAcfWindowSizeTime = _intervalAnaliza,
                        SamleFrequency = _formatWave1.SampleRate,
                        ImageLenght = _lpcMatrixSize
                    };

                lpc1.GetLpcImage(ref _wave1, out _image1);
                double[][] arc;
                lpc1.GetArcImage(ref _image1, out arc, 128);
                _image1 = arc;
                _image1 = lpc1.GetArcVocalTractImage(ref _wave1, _intervalAnaliza, _formatWave1.SampleRate, _lpcMatrixSize, 128, ref _image1, Window);
                //_cep1 = new Cepstrum(_cepNumber, _intervalAnaliza, _formatWave1.SampleRate);
                //_image1 = _cep1.GetCepstrogram(ref _wave1, Window, _lpcMatrixSize);

                MakeDelta(ref _image1);
                _vq1 = new VectorQuantization(_image1, 127, _codeBookSize);
                _graphic1 = new Bitmap(pictureBox1.ClientSize.Width, pictureBox1.ClientSize.Height);
                pictureBox1.Image = _graphic1;
                DrawLpcMatrix(ref _image1, ref _graphic1);
                DrawAxes(ref _graphic1, ref _image1);
            }
        }

        private void ProcessVad(string fileName)
        {
            var info = new ProcessStartInfo(_vadProgram, "0,2 '" + fileName + "'")
            {
                UseShellExecute = false,
                RedirectStandardInput = true
            };
            var vadProg = Process.Start(info);

            while (!File.Exists(fileName + ".txt")) ;

            if (vadProg != null)
            {
                vadProg.StandardInput.WriteLine("exit");
                vadProg.WaitForExit();
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

        private void MakeDelta(ref double[][] image)
        {
            for(int i = image.Length -1; i > 0; i--)
            {
                for(int j = 0; j < image[i].Length; j++)
                {
                    image[i][j] -= image[i - 1][j];
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

        private void ReadFile(out float[] soundArray, string fileName, out WaveFormat waveFormat, out int startPoint, out int stopPont)
        {
            using (var paramReader = new StreamReader(fileName + ".txt"))
            {
                if (!int.TryParse(paramReader.ReadLine(), out startPoint))
                {
                    startPoint = 0;
                }
                if (!int.TryParse(paramReader.ReadLine(), out stopPont))
                {
                    stopPont = int.MaxValue;
                }
            }
            using (var reader = new WaveFileReader(fileName))
            {
                waveFormat = reader.WaveFormat;
                var arrayLenght = stopPont - startPoint+1;
                soundArray = new float[arrayLenght];
                int j = 0;
                for (int i = 0; i < reader.SampleCount; i++)
                    if (i >= startPoint && i <= stopPont)
                    {
                        soundArray[j] = reader.ReadNextSampleFrame()[0];
                        j++;
                    }
                    else
                    {
                        reader.ReadNextSampleFrame();
                    }
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                _fileName2 = openFileDialog1.FileName;
                if (_vadProgram != null && !File.Exists(_fileName2 + ".txt"))
                {
                    ProcessVad(_fileName2);
                }
                ReadFile(out _wave2, _fileName2, out _formatWave2, out _activeVoiceStart2, out _activeVoiceStop2);

                lpc2 = new LinearPredictCoefficient
                {
                    UsedWindowType = Window,
                    UsedNumberOfCoeficients = _lpcNumber,
                    UsedAcfWindowSize = (int)Math.Round(_intervalAnaliza * _formatWave2.SampleRate),
                    UsedAcfWindowSizeTime = _intervalAnaliza,
                    SamleFrequency = _formatWave2.SampleRate,
                    ImageLenght = _lpcMatrixSize
                };

                lpc2.GetLpcImage(ref _wave2, out _image2);
                double[][] arc;
                lpc2.GetArcImage(ref _image2, out arc, 128);
                _image2 = arc;
                _image2 = lpc2.GetArcVocalTractImage(ref _wave2, _intervalAnaliza, _formatWave2.SampleRate, _lpcMatrixSize, 128, ref _image2, Window);
                //_cep2 = new Cepstrum(_cepNumber, _intervalAnaliza, _formatWave2.SampleRate);
                //_image2 = _cep2.GetCepstrogram(ref _wave2, Window, _lpcMatrixSize);

                MakeDelta(ref _image2);
                _vq2 = new VectorQuantization(_image2, 127, _codeBookSize);
                _graphic2 = new Bitmap(pictureBox2.ClientSize.Width, pictureBox2.ClientSize.Height);
                pictureBox2.Image = _graphic2;
                DrawLpcMatrix(ref _image2, ref _graphic2);
                DrawAxes(ref _graphic2, ref _image2);
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
                DrawLpc(ref _image1, ref _graphic1);
                DrawAxes(ref _graphic1, ref _image1);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (var writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)+"//FirstToSecond.txt"))
            {
                //_vq1.SetAverageAndDispersion(ref _image2);
                for (int i = 0; i < _image2.Length; i++)
                {
                    var listOfDistortion = _vq1.QuantizationErrorNormal(_image2[i], _vq1.Quantazation(_image2[i]));//
                                          // _vq1.QuantizationError(_image1[i], _vq1.Quantazation(_image1[i]));
                    writer.WriteLine(listOfDistortion.ToString(CultureInfo.CurrentCulture));
                }

            }

            using (var writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "//codeBookDistance.txt"))
            {
                var listOfDistortion = _vq1.CodeBookDistances(_vq1.CodeBook, _vq2.CodeBook);
                for (int i = 0; i < listOfDistortion.Length; i++)
                {
                    writer.WriteLine(listOfDistortion[i].ToString(CultureInfo.CurrentCulture));
                }
            }

            using (var writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "//codeBookAverage.txt"))
            {
                var listOfDistortion = _vq1.AverageCodeBookDistance(_vq1.CodeBook, _vq2.CodeBook);
                writer.WriteLine(listOfDistortion.ToString(CultureInfo.CurrentCulture));
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

            lpc1.GetAproximatedSpectrogramm(ref _image1, out spectrogram, 256);
            lpc1.GetFormants(ref _image1, out formantsTrack, out formantsLenght, out formantsApms, out formantsEnergy, 256, _formatWave1.SampleRate);
            lpc1.GetArcAndPcImages(ref _image1, out arc, out pc, 256);

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

        private static readonly List<ExperimentParameters> Parameterses = new List<ExperimentParameters>(356400);

        private void MakeParameters(object dir)
        {
            var directory = (string)dir;
            var files = Directory.GetFiles(directory, "*.wav");

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
                                    Parameterses.Add(new ExperimentParameters
                                        {
                                            CodeBookName = Path.GetFileName(files[i]),
                                            CodeBookSize = codeBookSize,
                                            ImageLenght = imageLenght,
                                            LpcVectorLenght = vectorLenghtLpc,
                                            TestFileName = Path.GetFileName(files[j]),
                                            VectorLenght = vectorLenghtLpc,
                                            TypeOfCharacteristic = ExperimentParameters.VectorType.LPC,
                                            WindowSize = windowSize,
                                            CodeBookIndex = i,
                                            TestFileIndex = j
                                        });
                                    for (var vectorLenghtArc = 32; vectorLenghtArc < 257; vectorLenghtArc *= 2)
                                    {
                                        Parameterses.Add(new ExperimentParameters
                                        {
                                            CodeBookName = Path.GetFileName(files[i]),
                                            CodeBookSize = codeBookSize,
                                            ImageLenght = imageLenght,
                                            LpcVectorLenght = vectorLenghtLpc,
                                            TestFileName = Path.GetFileName(files[j]),
                                            VectorLenght = vectorLenghtArc,
                                            TypeOfCharacteristic = ExperimentParameters.VectorType.ARC,
                                            WindowSize = windowSize,
                                            CodeBookIndex = i,
                                            TestFileIndex = j
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
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
                int t, tt;
                ReadFile(out filesData[i], files[i], out fileFormats[i], out t, out tt);
            }

            MakeParameters(dir);

            Parallel.ForEach(Parameterses, parameters => { GetValue(fileFormats, filesData, ref result, parameters); progressBar1.Invoke(new Action(Inc)); });
                                    


            using (var writer = new StreamWriter(directory + "\\report.txt"))
            {
                for (int i = 0; i < result.Count; i++)
                {
                    writer.WriteLine(result[i]);
                }
            } //report
        }

        private void GetValue(WaveFormat[] fileFormats, float[][] filesData, ref List<string> result, ExperimentParameters parameter)
        {
            double[][] codeBookLpc;
            var trainLpc = new LinearPredictCoefficient
                {
                    UsedWindowType = WindowFunctions.WindowType.Blackman,
                    UsedNumberOfCoeficients = (byte) parameter.LpcVectorLenght,
                    UsedAcfWindowSize = (int)Math.Round(parameter.WindowSize * fileFormats[parameter.CodeBookIndex].SampleRate),
                    UsedAcfWindowSizeTime = parameter.WindowSize,
                    SamleFrequency = fileFormats[parameter.CodeBookIndex].SampleRate,
                    ImageLenght = _lpcMatrixSize
                };

            trainLpc.GetLpcImage(ref filesData[parameter.CodeBookIndex], out codeBookLpc);

            var vq = new VectorQuantization(codeBookLpc, parameter.LpcVectorLenght, parameter.CodeBookSize);

            double[][] testLpc;
            var testingLpc = new LinearPredictCoefficient
                {
                    UsedWindowType = WindowFunctions.WindowType.Blackman,
                    UsedNumberOfCoeficients = (byte) parameter.LpcVectorLenght,
                    UsedAcfWindowSize = (int) Math.Round(parameter.WindowSize*fileFormats[parameter.TestFileIndex].SampleRate),
                    UsedAcfWindowSizeTime = parameter.TestFileIndex,
                    SamleFrequency = fileFormats[parameter.TestFileIndex].SampleRate,
                    ImageLenght = _lpcMatrixSize
                };

            trainLpc.GetLpcImage(ref filesData[parameter.TestFileIndex], out testLpc);
            if (parameter.TypeOfCharacteristic == ExperimentParameters.VectorType.LPC)
            {
                var energy = vq.DistortionMeasureEnergy(ref testLpc);
                //parameter.DistortionEnergy = energy;

                result.Add(
                    string.Concat("[---|CodeBook:", parameter.CodeBookName, "|TestFile:",
                                  parameter.CodeBookName, "|WindowSize:", parameter.WindowSize,
                                  "|CodebookSize:",
                                  parameter.CodeBookSize, "|ImageLenght:", parameter.ImageLenght,
                                  "|VectorType:LPC|VectorSize:",
                                  parameter.VectorLenght, "|---]:", energy));
            }
            else
            {


                double[][] codeBookArc;
                trainLpc.GetArcImage(ref codeBookLpc, out codeBookArc,
                                     parameter.VectorLenght);
                var vqArc = new VectorQuantization(codeBookArc, parameter.VectorLenght, parameter.CodeBookSize);

                double[][] testArc;
                testingLpc.GetArcImage(ref testLpc, out testArc, parameter.VectorLenght);
                var energyArc = vqArc.DistortionMeasureEnergy(ref testArc);

                result.Add(
                    string.Concat("[---|CodeBook:", parameter.CodeBookName, "|TestFile:",
                                  parameter.TestFileName, "|WindowSize:", parameter.WindowSize, "|CodebookSize:",
                                  parameter.CodeBookSize, "|ImageLenght:", parameter.ImageLenght, "|VectorType:ARC|VectorSize:",
                                  parameter.VectorLenght, "|LPCVectorLenght:",parameter.LpcVectorLenght,"---]:", energyArc));
            }
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {
            if (windowSizeCombo.SelectedItem != null)
            {
                double d;
                if (double.TryParse((string) windowSizeCombo.SelectedItem, out d))
                    _intervalAnaliza = d;
            }

            if (codeBookCombo.SelectedItem != null)
            {
                int d;
                if (int.TryParse((string)codeBookCombo.SelectedItem, out d))
                    _codeBookSize = d;
            }

            if (imageLenghtCombo.SelectedItem != null)
            {
                int d;
                if (int.TryParse((string)imageLenghtCombo.SelectedItem, out d))
                    _lpcMatrixSize = d;
            }

            _lpcNumber = (byte) vectorLenght.Value;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if(openFileDialog2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _vadProgram = openFileDialog2.FileName;
            }
        }
    }
}
