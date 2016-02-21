using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpersLibrary.DspAlgorithms
{
    public class NoiseGenerator
    {
        public float[] Noise { get; private set; }
        public double SNR { get; private set; }

        public NoiseGenerator(float[] signal, int noiseLength, double noiseAmplitude, Tuple<int,int> maxEnergyInterval)
        {
            var signalEnergyLog = 0.0;
            for (int i = maxEnergyInterval.Item1; i < maxEnergyInterval.Item2; i++)
            {
                signalEnergyLog += Math.Pow(signal[i], 2.0);
            }
            signalEnergyLog = 20.0*Math.Log10(signalEnergyLog);

            var noise = new float[noiseLength];
            var rand = new Random();
            var energy = 0.0;
            for (int i = 0; i < noiseLength; i++)
            {
                noise[i] = (float) ((rand.NextDouble()*2.0 - 1.0)*noiseAmplitude);
                energy += Math.Pow(noise[i], 2.0);
            }
            energy = 20.0*Math.Log10(energy);
            SNR = signalEnergyLog - energy;
            Noise = noise;
        }

        public float[] ApplyNoise(float[] signal)
        {
            var noisedSignal = new float[signal.Length];
            for (int i = 0; i < noisedSignal.Length; i++)
            {
                noisedSignal[i] = signal[i] + Noise[i];
            }
            return noisedSignal;
        }
    }
}
