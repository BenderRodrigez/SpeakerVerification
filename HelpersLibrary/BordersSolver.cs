using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpersLibrary
{
    public class BordersSolver
    {
        public double SameDictorBorder { get; set; }
        public double ForeignDictorBorder { get; set; }

        public BordersSolver()
        {
            SameDictorBorder = 900.0;
            ForeignDictorBorder = 1100.0;
        }

        public SolutionState GetSolution(double energy)
        {
            if(energy < SameDictorBorder)
                return SolutionState.SameDictor;
            else if(energy < ForeignDictorBorder)
                return SolutionState.ForeignDictor;
            return SolutionState.NoClearSolution;
        }
    }
}
