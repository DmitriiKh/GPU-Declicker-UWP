using System;
using GPU_Declicker_UWP_0._01;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class ClickRepairerTests
    {
        [TestMethod]
        public void CalcBurgPred_SinusoidalInput_ReturnsPrediction()
        {
            const int history_length = 512;

            var input_audio = new float[history_length + 1];

            for (var i = 0; i < input_audio.Length; i++)
                input_audio[i] = (float) Math.Sin(2 * Math.PI * i / (history_length / 5.2));

            AudioData audioData =
                new AudioDataMono(input_audio);

            for (var index = 0; index < audioData.LengthSamples(); index++)
                audioData.SetOutputSample(
                    index,
                    audioData.GetInputSample(index));

            var prediction = ClickRepairer.CalcBurgPred(
                audioData,
                history_length);

            Assert.AreEqual(
                prediction,
                input_audio[input_audio.Length - 1],
                0.000001);
        }
    }
}