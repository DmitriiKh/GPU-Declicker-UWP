using System;
using GPU_Declicker_UWP_0._01;
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

            float[] input_audio =
                new float[historyLength + numberOfSamplesToCheck];
            float[] forwardPredictions =
                new float[historyLength + numberOfSamplesToCheck];
            float[] backwardPredictions =
                new float[historyLength + numberOfSamplesToCheck];

            for (int i = 0; i < input_audio.Length; i++)
            {
                input_audio[i] = (float)Math.Sin(
                    2 * Math.PI * i / (historyLength / 5.2));
            }

            for (int index = historyLength;
                index < historyLength + numberOfSamplesToCheck;
                index++)
            {
                BurgPredictionCalculator.Calculate(
                    input_audio,
                    forwardPredictions,
                    backwardPredictions,
                    index,
                    coefNumber,
                    historyLength);

                Assert.AreEqual(
                    forwardPredictions[index],
                    input_audio[index],
                    0.000001);
            }
        }
    }
}
