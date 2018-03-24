using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    class ClickRepairer
    {
        /// <summary>
        /// Replace output sample at position with prediction and 
        /// sets prediction error sample to zero
        /// </summary>
        /// <param name="position"></param>
        /// <param name="lenght"></param>
        static public float Repair(AudioData audioData, int position, int lenght)
        {
            for (int index = position; index <= position + lenght; index++)
            {
                audioData.SetPredictionErr(index, 0.001F);
                audioData.SetOutputSample(
                    index,
                    CalcBurgPred(audioData, index)
                    );
            }

            int historyLengthSamples =
                audioData.AudioProcessingSettings.HistoryLengthSamples;

            HelperCalculator.CalculateErrorAverageCPU(
                audioData,
                position - historyLengthSamples,
                position + lenght + historyLengthSamples,
                historyLengthSamples);

            return HelperCalculator.CalculateDetectionLevel(audioData, position);
        }

        /// <summary>
        /// Returns prediction for a sample at position
        /// </summary>
        public static float CalcBurgPred(
            AudioData audioData,
            int position)
        {
            int historyLengthSamples =
                audioData.AudioProcessingSettings.HistoryLengthSamples;

            // use output audio as an input because it already contains
            // fixed samples before sample at position
            float[] audioShort = new float[historyLengthSamples + 1];
            for (int index = 0; index < historyLengthSamples + 1; index++)
            {
                audioShort[index] = audioData.GetOutputSample(
                    position - historyLengthSamples + index);
            }

            // array for results
            float[] forwardPredictionsShort =
                new float[historyLengthSamples + 1];

            // we need this array for calling CalculateBurgPredictionThread
            float[] backwardPredictionsShort =
                new float[historyLengthSamples + 1];

            BurgPredictionCalculator.Calculate(
                audioShort,
                forwardPredictionsShort,
                backwardPredictionsShort,
                historyLengthSamples,
                audioData.AudioProcessingSettings.CoefficientsNumber * 2,
                historyLengthSamples);

            // return prediction for sample at position
            return forwardPredictionsShort[historyLengthSamples];
        }
    }
}
