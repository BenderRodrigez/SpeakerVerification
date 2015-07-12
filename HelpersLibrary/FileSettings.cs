using System;
using System.IO;
using System.Xml.Linq;

namespace HelpersLibrary
{
    public class FileSettings
    {
        public string FileName { get; set; }

        public string FileMetaDataFolder
        {
            get { return FileName + ".files\\"; }
        }

        public int SpeechStartPosition { get; set; }
        public int SpeechEndPosition { get; set; }

        public string DictorName { get; set; }

        public int SampleFrequency { get; set; }

        //public FileSettings(string fileName)
        //{
        //    FileName = fileName;
        //    if (File.Exists(SettingsFileName(fileName)))
        //    {
        //        var settings = LoadSettings(fileName);
        //        SampleFrequency = settings.SampleFrequency;
        //        DictorName = settings.DictorName;
        //        SpeechEndPosition = settings.SpeechEndPosition;
        //        SpeechStartPosition = settings.SpeechStartPosition;
        //    }
        //}

        public void Save()
        {
            var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"));
            var root = new XElement(XName.Get("settings"));
            var fileNameXml = new XElement(XName.Get("filename")) {Value = FileName};
            var dictorNameXml = new XElement(XName.Get("dictorname")) {Value = DictorName};
            var wavSettingsXml = new XElement(XName.Get("wavsettings"));
            wavSettingsXml.Add(new XElement(XName.Get("samplefrequency"))
            {
                Value = SampleFrequency.ToString()
            });

            var speechSettingsXml = new XElement(XName.Get("speechsettings"));
            speechSettingsXml.Add(new XElement(XName.Get("speechstart")) {Value = SpeechStartPosition.ToString()});
            speechSettingsXml.Add(new XElement(XName.Get("speechend")) {Value = SpeechEndPosition.ToString()});

            root.Add(fileNameXml);
            root.Add(dictorNameXml);
            root.Add(wavSettingsXml);
            root.Add(speechSettingsXml);
            doc.Add(root);
            var path = SettingsFileName(FileName);
            doc.Save(path);
        }

        private static string SettingsFileName(string fileName)
        {
            var tmpPath = fileName + ".files\\";
            return Path.Combine(tmpPath, "settings.xml");
        }

        public static FileSettings LoadSettings(string fileName)
        {
            var doc = XDocument.Load(SettingsFileName(fileName));

            var root = doc.Element(XName.Get("settings"));
            if (root != null)
            {
                var fN = root.Element(XName.Get("filename")).Value;
                var dictName = root.Element(XName.Get("dictorname")).Value;
                var wavSettings = root.Element(XName.Get("wavsettings"));
                var settings = new FileSettings {DictorName = dictName, FileName = fileName};

                if (wavSettings != null)
                {
                    var sampleFreqency = wavSettings.Element(XName.Get("samplefrequency")).Value;
                    int sampleFreq;
                    if (int.TryParse(sampleFreqency, out sampleFreq))
                        settings.SampleFrequency = sampleFreq;
                }
                var speechSettings = root.Element(XName.Get("speechsettings"));
                if (speechSettings != null)
                {
                    var speechStart = speechSettings.Element(XName.Get("speechstart")).Value;
                    var spechEnd = speechSettings.Element(XName.Get("speechend")).Value;
                    int speechStartPosition, speechStopPosition;
                    if (int.TryParse(speechStart, out speechStartPosition) &&
                        int.TryParse(spechEnd, out speechStopPosition))
                    {
                        settings.SpeechStartPosition = speechStartPosition;
                        settings.SpeechEndPosition = speechStopPosition;
                    }
                }
                return settings;
            }
            throw new FileLoadException("Ошибка при чтении файла. Проверте корректность структуры фала с настройками.");
        }
    }
}
