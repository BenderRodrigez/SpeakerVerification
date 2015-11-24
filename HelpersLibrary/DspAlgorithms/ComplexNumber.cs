using System;

namespace HelpersLibrary.DspAlgorithms
{
    /// <summary>
    /// Type to represent complex number
    /// </summary>
    public struct ComplexNumber
    {
        public static ComplexNumber Zero { get { return new ComplexNumber(0); } }
        public static ComplexNumber ImaginaryOne { get { return new ComplexNumber(0, -1); } }
        public double Sqr { get { return Math.Pow(RealPart, 2) + Math.Pow(ImaginaryPart, 2); } }

        /// <summary>
        /// Real Part
        /// </summary>
        public float RealPart;
        /// <summary>
        /// Imaginary Part
        /// </summary>
        public float ImaginaryPart;

        public ComplexNumber(double realPart, double imagimaryPart = 0.0)
        {
            RealPart = (float) realPart;
            ImaginaryPart = (float) imagimaryPart;
        }
    }
}
