using GPUDeclickerUWP.Model.Processing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class FastBurgAlgorithm64Tests
    {
            [TestMethod]
            public void GetForwardPrediction_SinInput_ReturnsCorrectPrediction()
            {
                const int coefNumber = 4;
                const int historyLength = 512;
                const int numberOfSamplesToCheck = 10;

                var inputAudio =
                    new double[historyLength + numberOfSamplesToCheck];

                for (var i = 0; i < inputAudio.Length; i++)
                {
                    inputAudio[i] = System.Math.Sin(
                        2 * System.Math.PI * i / (historyLength / 5.2));
                }

                var fba = new FastBurgAlgorithm64(inputAudio);

                for (var index = historyLength;
                    index < historyLength + numberOfSamplesToCheck;
                    index++)
                {
                    fba.Train(index, coefNumber, historyLength);
                    var forwardPrediction = fba.GetForwardPrediction();

                    Assert.AreEqual(
                        inputAudio[index],
                        forwardPrediction,
                        0.0000001);
                }
            }
        }
}
