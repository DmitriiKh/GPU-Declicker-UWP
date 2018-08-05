using System;
using GPUDeclickerUWP.Model.Data;

namespace GPUDeclickerUWP.Model.Processing
{
    public static class HelperCalculator
    {
        /// <summary>
        ///     Calculates average prediction errors level using algorithm:
        ///     First: find max error in each block of 16 prediction errors
        ///     Second: find average of maximums over all blocks
        ///     (blocks number is _base/16)
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="startPosition"></param>
        /// <param name="endPosition"></param>
        /// <param name="_base">number of prediction errors used for calculation</param>
        public static void CalculateErrorAverageCpu(
            AudioData audioData,
            int startPosition,
            int endPosition,
            int _base)
        {
            var errorAverage =
                CalcErrorAverageForOneSample(audioData, startPosition);
            // set one average error sample 
            audioData.SetErrorAverage(startPosition, errorAverage);

            // Fast version of CalcErrorAverageForOneSample
            // Keep sliding to next block of 16 Burg prediction errors
            for (var index = startPosition;
                index < endPosition - 15;
                index += 16)
            {
                // check each period of 16 errors to find maximums
                float maxErrorInExcludedBlock = 0,
                    maxErrorInIncludedBlock = 0;

                for (var indexCurrent = index;
                    indexCurrent < index + 16;
                    indexCurrent++)
                {
                    // find max in the block which will be excluded
                    var tempExcludedBlock = Math.Abs(
                        audioData.GetPredictionErr(indexCurrent - _base));
                    if (maxErrorInExcludedBlock < tempExcludedBlock)
                        maxErrorInExcludedBlock = tempExcludedBlock;
                    // find max in the block which will be included
                    var tempIncludedBlock = Math.Abs(
                        audioData.GetPredictionErr(indexCurrent));
                    if (maxErrorInIncludedBlock < tempIncludedBlock)
                        maxErrorInIncludedBlock = tempIncludedBlock;
                }

                // correction based on previously calculated errorAverage
                errorAverage = errorAverage +
                               (maxErrorInIncludedBlock - maxErrorInExcludedBlock) /
                               ((float)_base / 16);

                // minimum result to return is 0.0001
                if (errorAverage < 0.0001)
                    errorAverage = 0.0001F;

                // all 16 results in block are the same
                for (var l = index; l < index + 16; l++)
                    audioData.SetErrorAverage(l,
                        errorAverage);
            }
        }

        private static float CalcErrorAverageForOneSample(AudioData audioData, int position)
        {
            var Base = audioData.AudioProcessingSettings.HistoryLengthSamples;
            float errorAverage = 0;

            for (var blockIndex = 0; blockIndex < Base - 16; blockIndex += 16)
            {
                // check each period of 16 errors to find maximum
                float maxErrorInBlock = 0;
                for (var indexInBlock = 0; indexInBlock < 16; indexInBlock++)
                {
                    var temp = Math.Abs(audioData.GetPredictionErr(
                        position - Base + blockIndex + indexInBlock
                    ));
                    if (maxErrorInBlock < temp) maxErrorInBlock = temp;
                }

                // sum up maximums 
                errorAverage = errorAverage + maxErrorInBlock;
            }

            // find average
            errorAverage = errorAverage / ((float)Base / 16) + 0.0000001F;
            // minimum result to return is 0.0001
            if (errorAverage < 0.0001)
                errorAverage = 0.0001F;

            return errorAverage;
        }

        public static float CalculateDetectionLevel(AudioData audioData, int position)
        {
            // use original input samples 
            var error = Math.Abs(CalcBurgPredFromInput(audioData, position));
            // find average error value on the LEFT of the current sample
            var errorAverage = audioData.GetErrorAverage(position - 16);

            return error / errorAverage;
        }


        private static float CalcBurgPredFromInput(
            AudioData audioData,
            int position)
        {
            var historyLengthSamples =
                audioData.AudioProcessingSettings.HistoryLengthSamples;
            
            var audioShort = new double[historyLengthSamples + 1];
            for (var index = 0; index < historyLengthSamples + 1; index++)
                audioShort[index] = audioData.GetInputSample(position -
                                                             historyLengthSamples + index);

            var fba = new FastBurgAlgorithm64(audioShort);
            fba.Train(historyLengthSamples,
                audioData.AudioProcessingSettings.CoefficientsNumber * 2,
                historyLengthSamples);

            return (float)fba.GetForwardPrediction();
        }

        /// <summary>
        ///     Predicts max length of damaged samples sequence
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static int GetMaxLength(AudioData audioData, int position)
        {
            var lenght = 0;
            var error = Math.Abs(audioData.GetPredictionErr(position));
            var errorAverage = audioData.GetErrorAverage(position - 15);
            var rate = error /
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
            var maxLength = (int) (lenght * rate * 2);

            // follow user's limit
            if (maxLength > audioData.AudioProcessingSettings.MaxLengthOfCorrection)
                maxLength = audioData.AudioProcessingSettings.MaxLengthOfCorrection;

            return maxLength;
        }
    }
}