using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeakerVerification
{
    internal class ExperimentParameters
    {
        public enum VectorType
        {
            ARC,
            LPC,
            ANY
        };//ANY - means what program will use any type of vectors in cmputations

        public int ImageLenght;
        public int CodeBookSize;
        public int VectorLenght;
        public VectorType TypeOfCharacteristic;
        public double WindowSize;
        public double DistortionEnergy;
        public string CodeBookName;
        public string TestFileName;
        public int LpcVectorLenght;
        public int CodeBookIndex;
        public int TestFileIndex;

        public bool IsSameDictor { get; private set; }

        public void Parse(string line)
        {
            var lineElements = line.Split('|');

            foreach (
                var property in
                    lineElements.Select(lineElement => lineElement.Split(':'))
                                .Where(property => property.Length >= 2))
            {
                if (property[0].IndexOf("---]") == -1)
                {
                    switch (property[0])
                    {
                        case "CodeBook":
                            CodeBookName = property[1];
                            break;
                        case "TestFile":
                            TestFileName = property[1];
                            break;
                        case "WindowSize":
                            WindowSize = double.Parse(property[1]);
                            break;
                        case "CodebookSize":
                            CodeBookSize = int.Parse(property[1]);
                            break;
                        case "ImageLenght":
                            ImageLenght = int.Parse(property[1]);
                            break;
                        case "VectorType":
                            VectorType type;
                            if (Enum.TryParse(property[1], out type))
                            {
                                TypeOfCharacteristic = type;
                            }
                            break;
                        case "VectorSize":
                            VectorLenght = int.Parse(property[1]);
                            break;
                    }
                }
                else
                {
                    DistortionEnergy = double.Parse(property[1]);
                }
            }
        }
        
        public override string ToString()
        {
            return String.Concat("[---|CodeBook:",CodeBookName,"|TestFile:",TestFileName,"|WindowSize:",WindowSize,"|CodebookSize:",CodeBookSize,"|ImageLenght:",ImageLenght,"|VectorType:",TypeOfCharacteristic,"|VectorSize:",VectorLenght,"|---]:",DistortionEnergy);
        }
    }
}
