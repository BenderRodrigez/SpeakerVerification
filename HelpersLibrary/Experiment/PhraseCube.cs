using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HelpersLibrary.Experiment
{
    class PhraseCube
    {
        public double[, ,] Cube { get; set; }
        public string Phrase { get; set; }
        public int Pronounsations { get; set; }

        public PhraseCube(string phrase, int dictorsCount, int pronounsationCount)
        {
            Cube = new double[dictorsCount, dictorsCount, pronounsationCount];
            Phrase = phrase;
            Pronounsations = pronounsationCount;
        }
    }
}
