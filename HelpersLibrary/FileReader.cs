using System;
using System.Linq;
using NAudio.Wave;

namespace HelpersLibrary
{
    public static class FileReader
    {
        public static float[] ReadFile(string fileName, out int sampleRate)
        {
            float[] speechFile;
            using (var reader = new WaveFileReader(fileName))
            {
                var sampleProvider = reader.ToSampleProvider();
                speechFile = new float[reader.SampleCount];
                sampleProvider.Read(speechFile, 0, (int)reader.SampleCount);
                sampleRate = reader.WaveFormat.SampleRate;
            }
            var max = speechFile.Max(x => Math.Abs(x));
            speechFile = speechFile.Select(x => x / max).ToArray();
            return speechFile;
        }
    }
}
