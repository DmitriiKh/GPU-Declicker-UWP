using GPU_Declicker_UWP_0._01;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class AudioProcessingTest
    {
        [TestMethod]
        public void AudioProcessingCalculateBurgPredictionThread()
        {
            const int coef_number = 4;
            const int history_length = 512;
            AudioProcessing audioProcessing_for_test = 
                new AudioProcessing(history_length, coef_number, 3.5F, 250);

            float[] input_audio = new float[history_length + 1];
            float[] forwardPredictions = new float[history_length + 1];
            float[] backwardPredictions = new float[history_length + 1];

            for (int i = 0; i < input_audio.Length; i++)
            {
                input_audio[i] = (float) Math.Sin(2 * Math.PI * i / (history_length / 5.2));
            }

            audioProcessing_for_test.CalculateBurgPredictionThread(
                input_audio,
                forwardPredictions,
                backwardPredictions,
                history_length,
                coef_number);
                
            Assert.AreEqual(
                forwardPredictions[forwardPredictions.Length - 1],
                input_audio[input_audio.Length - 1],
                0.0001);
        }

        [TestMethod]
        public void AudioProcessingCalc_burg_pred()
        {
            const int coef_number = 4;
            const int history_length = 512;
            AudioProcessing audioProcessing_for_test =
                new AudioProcessing(history_length, coef_number, 3.5F, 250);

            float[] input_audio = new float[history_length + 1];

            for (int i = 0; i < input_audio.Length; i++)
            {
                input_audio[i] = (float)Math.Sin(2 * Math.PI * i / (history_length / 5.2));
            }

            AudioData audioData =
                new AudioDataMono(input_audio);

            for (int index = 0; index < audioData.LengthSamples(); index++)
            {
                audioData.SetOutputSample(
                    index,
                    audioData.GetInputSample(index));
            }

            float prediction = audioProcessing_for_test.Calc_burg_pred(
                audioData,
                history_length);

            Assert.AreEqual(
                prediction,
                input_audio[input_audio.Length - 1],
                0.0001);
        }
    }
}