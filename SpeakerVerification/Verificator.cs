using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeakerVerification
{
    static class Verificator
    {
        public static bool MakeDecision(double[] distortionMeasure, double averageValue)
        {
            int count = 0;
            for(int i = 0; i < distortionMeasure.Length; i++)
            {
                count += (averageValue / distortionMeasure[i] > 0.2) ? 1 : 0;
            }
            if(count > 0)
                return distortionMeasure.Length / count < 3;
            return false;
        }
    }
}
