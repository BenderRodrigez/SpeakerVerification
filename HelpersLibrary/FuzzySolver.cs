using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FLS.MembershipFunctions;

namespace HelpersLibrary
{
    public class FuzzySolver
    {
        public double OwnSolutionBorder { get; set; }
        public double ForeignSolutionBorder { get; set; }

        public BellMembershipFunction OwnSolutionFunction { get; set; }
        public SShapedMembershipFunction ForeignSolutionFunction { get; set; }

        public FuzzySolver()
        {
            OwnSolutionBorder = 0.5;
            ForeignSolutionBorder = 0.2;

            OwnSolutionFunction = new BellMembershipFunction("Own solution", 450, 5, 450);
            ForeignSolutionFunction = new SShapedMembershipFunction("Foreign solution", 1100, 550);
        }

        public SolutionState GetSolution(double distortionEnergy)
        {
            var ownVal = OwnSolutionFunction.Fuzzify(distortionEnergy);
            var foreignVal = ForeignSolutionFunction.Fuzzify(distortionEnergy);

            if (ownVal > OwnSolutionBorder && ownVal > foreignVal)
            {
                return SolutionState.SameDictor;
            }
            if (foreignVal > ForeignSolutionBorder && foreignVal > ownVal)
            {
                return SolutionState.ForeignDictor;
            }
            return SolutionState.NoClearSolution;
        }
    }
}
