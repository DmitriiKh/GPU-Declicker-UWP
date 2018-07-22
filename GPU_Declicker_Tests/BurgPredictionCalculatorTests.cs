using System;
using GPUDeclickerUWP;
using GPUDeclickerUWP.Model.Processing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class BurgPredictionCalculatorTests
    {
        [TestMethod]
        public void Calculate_SinusoidInput_CalculatesPrediction()
        {
            const int coefNumber = 4;
            const int historyLength = 512;
            const int numberOfSamplesToCheck = 10;

            var inputAudio =
                new float[historyLength + numberOfSamplesToCheck];
            var forwardPredictions =
                new float[historyLength + numberOfSamplesToCheck];
            var backwardPredictions =
                new float[historyLength + numberOfSamplesToCheck];

            for (var i = 0; i < inputAudio.Length; i++)
                inputAudio[i] = (float) Math.Sin(
                    2 * Math.PI * i / (historyLength / 5.2));

            for (var index = historyLength;
                index < historyLength + numberOfSamplesToCheck;
                index++)
            {
                BurgPredictionCalculator.Calculate(
                    inputAudio,
                    forwardPredictions,
                    backwardPredictions,
                    index,
                    coefNumber,
                    historyLength);

                Assert.AreEqual(
                    forwardPredictions[index],
                    inputAudio[index],
                    0.000001);
            }
        }
    }
}