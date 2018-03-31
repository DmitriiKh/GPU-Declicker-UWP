using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    public static class HelperCalculator
    {
        /// <summary>
        /// Calculates average prediction errors level using algorithm:
        /// First: find max error in each block of 16 prediction errors
        /// Second: find average of maximums over all blocks
        /// (blocks number is _base/16)
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        /// <param name="_base">number of prediction errors used for calculation</param>
        public static void CalculateErrorAverageCPU(
            AudioData audioData,
            int startPosition,
            int endPosition,
            int _base)
        {
            float errorAverage =
                CalcErrorAverageForOneSample(audioData, startPosition);
            // set one average error sample 
            audioData.SetErrorAverage(startPosition, errorAverage);

            // Fast version of CalcErrorAverageForOneSample
            // Keep sliding to next block of 16 Burg prediction errors
            for (int index = startPosition;
                index < endPosition - 15;
                index += 16)
            {
                // check each period of 16 errors to find maximums
                float maxErrorInExcludedBlock = 0,
                    maxErrorInIncludedBlock = 0;

                for (int indexCurrent = index;
                    indexCurrent < index + 16;
                    indexCurrent++)
                {
                    float tempExcludedBlock = 0,
                    tempIncludedBlock = 0;
                    // find max in the block which will be excluded
                    tempExcludedBlock = Math.Abs(
                        audioData.GetPredictionErr(indexCurrent - _base));
                    if (maxErrorInExcludedBlock < tempExcludedBlock)
                        maxErrorInExcludedBlock = tempExcludedBlock;
                    // find max in the block which will be included
                    tempIncludedBlock = Math.Abs(
                        audioData.GetPredictionErr(indexCurrent));
                    if (maxErrorInIncludedBlock < tempIncludedBlock)
                        maxErrorInIncludedBlock = tempIncludedBlock;
                }
                // correction based on previously calculated errorAverage
                errorAverage = errorAverage +
                    (maxErrorInIncludedBlock - maxErrorInExcludedBlock) /
                    (_base / 16);

                // minimum result to return is 0.0001
                if (errorAverage < 0.0001)
                    errorAverage = 0.0001F;

                // all 16 results in block are the same
                for (int l = index; l < index + 16; l++)
                    audioData.SetErrorAverage(l,
                        errorAverage);
            }
        }

        private static float CalcErrorAverageForOneSample(AudioData audioData, int position)
        {
            int Base = audioData.AudioProcessingSettings.HistoryLengthSamples;
            float errorAverage = 0;

            for (int blockIndex = 0; blockIndex < Base - 16; blockIndex += 16)
            {
                // check each period of 16 errors to find maximum
                float maxErrorInBlock = 0;
                for (int indexInBlock = 0; indexInBlock < 16; indexInBlock++)
                {
                    float temp = Math.Abs(audioData.GetPredictionErr(
                        position - Base + blockIndex + indexInBlock
                        ));
                    if (maxErrorInBlock < temp) maxErrorInBlock = temp;
                }
                // sum up maximums 
                errorAverage = errorAverage + maxErrorInBlock;
            }
            // find average
            errorAverage = errorAverage / (Base / 16) + 0.0000001F;
            // minimum result to return is 0.0001
            if (errorAverage < 0.0001)
                errorAverage = 0.0001F;

            return errorAverage;
        }

        public static float CalculateDetectionLevel(AudioData audioData, int position)
        {
            float threshold_level_detected = 0;
            // use original input samples 
            float error = (Math.Abs(CalcBurgPredFromInput(audioData, position)));
            // find average error value on the LEFT of the current sample
            float errorAverage = audioData.GetErrorAverage(position - 16);
            threshold_level_detected = error / errorAverage;

            return threshold_level_detected;
        }


        private static float CalcBurgPredFromInput(
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
                audioShort[index] = audioData.GetInputSample(position -
                    historyLengthSamples + index);
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

            // return prediction for i sample
            return forwardPredictionsShort[historyLengthSamples];
        }

        /// <summary>
        /// Predicts max length of damaged samples sequence 
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static int GetMaxLength(AudioData audioData, int position)
        {
            int lenght = 0;
            float error = (Math.Abs(audioData.GetPredictionErr(position)));
            float errorAverage = audioData.GetErrorAverage(position - 15);
            float rate = error /
                (audioData.AudioProcessingSettings.ThresholdForDetection *
                errorAverage);
            while (error > errorAverage)
            {
                lenght = lenght + 3;
                error = (Math.Abs(audioData.GetPredictionErr(position + lenght)) +
                    Math.Abs(audioData.GetPredictionErr(position + 1 + lenght)) +
                    Math.Abs(audioData.GetPredictionErr(position + 2 + lenght))) / 3;
            }
            // the result is multiplication lenght and rate (doubled)
            int max_length = (int)(lenght * rate * 2);

            // follow user's limit
            if (max_length > audioData.AudioProcessingSettings.MaxLengthCorrection)
                max_length = audioData.AudioProcessingSettings.MaxLengthCorrection;

            return max_length;
        }
    }
}
