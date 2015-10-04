using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelpersLibrary.DspAlgorithms;
using HelpersLibrary.LearningAlgorithms;
using NAudio.Wave;
using Path = System.IO.Path;

namespace ExperimentalProcessing
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ProcessExperiment()
        {
            using (var db = new ExperimentalDataStorageDataContext())
            {
                var experiments = db.EXPERIMENT.Where(x => !x.Energy.HasValue).Select(x => x).Take(100);

                while (experiments.Any())
                {
                    foreach (var experiment in experiments)
                    {
                        WaveFormat speechFileFormat;
                        var speechFile = ReadSpeechFile(experiment.RECORDS_TRAIN.Path, out speechFileFormat);

                        int speechStartPosition;
                        int speechStopPosition;
                        var speechSearcher = new SpeechSearch(20, 0.04f, 0.99f, speechFileFormat.SampleRate);
                        speechSearcher.GetMarks(speechFile, out speechStartPosition, out speechStopPosition);

                        double[][] featureMatrix;
                        var lpc = new LinearPredictCoefficient
                        {
                            SamleFrequency = speechFileFormat.SampleRate,
                            UsedAcfWindowSizeTime = 0.04,
                            UsedNumberOfCoeficients = 10,
                            UsedWindowType = WindowFunctions.WindowType.Blackman,
                            Overlapping = 0.99
                        };
                        lpc.GetLpcImage(ref speechFile, out featureMatrix, speechStartPosition, speechStopPosition);
                        var vq = new VectorQuantization(featureMatrix, 10, 64);

                        WaveFormat speechFileFormatTest;
                        var speechFileTest = ReadSpeechFile(experiment.RECORDS_TEST.Path, out speechFileFormatTest);

                        var speechSearcherTest = new SpeechSearch(20, 0.04f, 0.99f, speechFileFormatTest.SampleRate);
                        int speechStartPositionTest;
                        int speechStopPositionTest;
                        speechSearcherTest.GetMarks(speechFileTest, out speechStartPositionTest, out speechStopPositionTest);

                        double[][] featureMatrixTest;
                        var lpcTest = new LinearPredictCoefficient
                        {
                            SamleFrequency = speechFileFormatTest.SampleRate,
                            UsedAcfWindowSizeTime = 0.04,
                            UsedNumberOfCoeficients = 10,
                            UsedWindowType = WindowFunctions.WindowType.Blackman,
                            Overlapping = 0.99
                        };
                        lpcTest.GetLpcImage(ref speechFileTest, out featureMatrixTest, speechStartPositionTest, speechStopPositionTest);

                        var energy = vq.DistortionMeasureEnergy(ref featureMatrixTest);
                        experiment.Energy = (float)energy;
                    }
                    db.SubmitChanges();
                    experiments = db.EXPERIMENT.Where(x => !x.Energy.HasValue).Select(x => x).Take(100);
                }
            }
        }

        private static float[] ReadSpeechFile(string filePath, out WaveFormat speechFileFormat)
        {
            float[] speechFile;
            using (var reader = new WaveFileReader(filePath))
            {
                speechFile = new float[reader.SampleCount];
                for (int i = 0; i < reader.SampleCount; i++)
                {
                    speechFile[i] = reader.ReadNextSampleFrame()[0];
                }
                speechFileFormat = reader.WaveFormat;
            }
            return speechFile;
        }

        private void GenerateExperiments()
        {
            using (var db = new ExperimentalDataStorageDataContext())
            {
                var records = db.RECORDS.Select(x => x);
                foreach (var record in records)//train record
                {
                    foreach (var record1 in records)//test record
                    {
                        if(record.PhraseId != record1.PhraseId) continue;

                        var experiment = new EXPERIMENT
                        {
                            TrainRecordId = record.Id,
                            TestRecordId = record1.Id,
                            TestDictorId = record1.DictorId,
                            TrainDictorId = record.DictorId,
                            Settings = ""
                        };
                        db.EXPERIMENT.InsertOnSubmit(experiment);
                    }
                }
                db.SubmitChanges();
            }
        }

        private void InitDataBase(string samplesFolderPath)
        {
            using (var db = new ExperimentalDataStorageDataContext())
            {
                var phrasesNames = Directory.GetDirectories(samplesFolderPath);
                if (phrasesNames.Length <= 0) return;

                foreach (var phrasesName in phrasesNames)
                {
                    var dictors = Directory.GetDirectories(phrasesName);
                    if (dictors.Length <= 0) continue;

                    foreach (var dictor in dictors)
                    {
                        var records = Directory.GetFiles(dictor, "*.wav");
                        foreach (var record in records)
                        {
                            var phraseDb = new PHRASES {Title = Path.GetDirectoryName(phrasesName)};
                            db.PHRASES.InsertOnSubmit(phraseDb);

                            var dictorDb =
                                db.DICTORS.FirstOrDefault(x => string.Equals(x.Name, dictor, StringComparison.CurrentCultureIgnoreCase));
                            if (dictorDb == null)
                            {
                                dictorDb = new DICTORS {Name = Path.GetDirectoryName(dictor)};
                                db.DICTORS.InsertOnSubmit(dictorDb);
                            }

                            db.SubmitChanges();
                            phraseDb = db.PHRASES.FirstOrDefault(x => string.Equals(x.Title, phrasesName, StringComparison.CurrentCultureIgnoreCase));
                            dictorDb = db.DICTORS.FirstOrDefault(x => string.Equals(x.Name, dictor, StringComparison.CurrentCultureIgnoreCase));

                            var recordDb = new RECORDS
                            {
                                DictorId = dictorDb.Id,
                                PhraseId = phraseDb.Id,
                                Path = record
                            };
                            db.RECORDS.InsertOnSubmit(recordDb);
                            db.SubmitChanges();
                        }
                    }
                }
            }
        }
    }
}
