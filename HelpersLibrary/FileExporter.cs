using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace HelpersLibrary
{
    public class FileExporter
    {
        public int SampleRate { get; set; }
        public float[] Data { get; set; }

        public void SaveAsWav(string fileName)
        {
            using (var writer = new WaveFileWriter(fileName, WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1)))
            {
                writer.WriteSamples(Data, 0, Data.Length);
            }
        }
    }
}
