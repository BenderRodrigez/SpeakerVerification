using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpersLibrary.Experiment
{
    public class ExperimentalDataParser
    {
        public float[] SignalData { get; private set; }
        public double[] PitchTrajectory { get; private set; }
        public int SampleRate { get; private set; }

        private short[] _rawSignalData;

        public ExperimentalDataParser(string binaryDataFileName, string markersDataFileName)
        {
            var samples = new List<short>();
            using (var binReader = new BinaryReader(new FileStream(binaryDataFileName, FileMode.Open)))
            {
                SampleRate = binReader.ReadInt32();
                while (binReader.BaseStream.Position < binReader.BaseStream.Length)
                {
                    samples.Add(binReader.ReadInt16());
                }
            }

            _rawSignalData = samples.ToArray();
            SignalData = samples.Select(x => (float) x/short.MaxValue).ToArray();

            var markers = new List<Tuple<int,int,short>>();//1st = number, 2nd = position, 3rd = value
            using (var markersReader = new StreamReader(markersDataFileName))
            {
                while (!markersReader.EndOfStream)
                {
                    var line = markersReader.ReadLine();
                    if (line != null)
                    {
                        var currentString = line
                            .Split(new[] {' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);

                        markers.Add(new Tuple<int, int, short>(int.Parse(currentString[0]), int.Parse(currentString[1]),
                            short.Parse(currentString[2])));
                    }
                }
            }

            var prevPos = 1;
            var pitch = new List<double>();
            for (int i = 0; i < markers.Count; i++)
            {
                var pitchValue = (markers[i].Item2 - prevPos)/(double)SampleRate;
                if (pitchValue < 0.02 && pitchValue > 0.0)
                {
                    for (int j = prevPos; j < markers[i].Item2; j++)
                    {
                        pitch.Add(1.0/pitchValue);
                    }
                }
                else
                {
                    for (int j = prevPos; j < markers[i].Item2; j++)
                    {
                        pitch.Add(0.0);
                    }
                }
                prevPos = markers[i].Item2;
            }

            PitchTrajectory = pitch.ToArray();
        }
    }
}
