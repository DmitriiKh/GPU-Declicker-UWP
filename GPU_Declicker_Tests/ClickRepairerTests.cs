using System;
using GPUDeclickerUWP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class ClickRepairerTests
    {
        [TestMethod]
        public void CalcBurgPred_SinusoidalInput_ReturnsPrediction()
        {
            const int historyLength = 512;

            var inputAudio = new float[historyLength + 1];

            for (var i = 0; i < inputAudio.Length; i++)
                inputAudio[i] = (float) Math.Sin(2 * Math.PI * i / 
                                                 (historyLength / 5.2));

            AudioData audioData =
                new AudioDataMono(inputAudio);

            for (var index = 0; index < audioData.LengthSamples(); index++)
                audioData.SetOutputSample(
                    index,
                    audioData.GetInputSample(index));

            var prediction = ClickRepairer.CalcBurgPred(
                audioData,
                historyLength);

            Assert.AreEqual(
                prediction,
                inputAudio[inputAudio.Length - 1],
                0.000001);
        }
    }
}