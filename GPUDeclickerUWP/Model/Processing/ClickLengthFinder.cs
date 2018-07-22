using System;
using GPUDeclickerUWP.Model.Data;

namespace GPUDeclickerUWP.Model.Processing
{
    public static class ClickLengthFinder
    {
        public static FixResult FindLengthOfClick(
            AudioData audioData,
            int initPosition,
            int maxLength,
            int positionOfLastProcessedSample)
        {
            var bestResult = new FixResult
            {
                Success = false
            };

            var position = initPosition;
            var minPosition = GetMinPosition(
                position,
                positionOfLastProcessedSample,
                10);

            while (position > minPosition)
            {
                var backup = BackupPredictionErrAverage(
                    audioData,
                    position,
                    audioData.AudioProcessingSettings.HistoryLengthSamples + maxLength);

                var result = TryToFix(audioData,
                    position,
                    maxLength,
                    initPosition - position);

                if (result.BetterThan(bestResult))
                    bestResult = result;

                audioData.CurrentChannelRestoreInitState(position, maxLength + 16);
                RestorePredictionErrAverage(
                    audioData,
                    position,
                    audioData.AudioProcessingSettings.HistoryLengthSamples + maxLength,
                    backup);

                position--;
            }

            return bestResult;
        }

        private static int GetMinPosition(
            int position,
            int positionOfLastProcessedSample,
            int maxDepth)
        {
            // if last processed sample is closer 
            //return positionOfLastProcessedSample
            if (position - positionOfLastProcessedSample < maxDepth)
                return positionOfLastProcessedSample;
            // go for full maxDepth
            return position - maxDepth;
        }

        private static void RestorePredictionErrAverage(
            AudioData audioData,
            int position,
            int length,
            float[] backup)
        {
            for (var index = 0; index < length * 2; index++)
                audioData.SetErrorAverage(
                    position - length + index,
                    backup[index]);
        }

        private static float[] BackupPredictionErrAverage(
            AudioData audioData,
            int position,
            int length)
        {
            var backup = new float[length * 2];
            for (var index = 0; index < length * 2; index++)
                backup[index] = audioData.GetErrorAverage(
                    position - length + index);

            return backup;
        }

        private static FixResult TryToFix(
            AudioData audioData,
            int index,
            int maxLength,
            int minLength)
        {
            var result = new FixResult
            {
                Success = false,
                Position = index,
                Length = minLength,
                ErrSum = float.MaxValue
            };

            ClickRepairer.Repair(audioData, index, minLength);

            while (result.Length < maxLength)
            {
                ClickRepairer.Repair(audioData, index + result.Length, 1);
                result.Length++;

                if (!SeveralSamplesInARowAreSuspicious(
                    audioData, index + result.Length, 3))
                {
                    result.ErrSum = CalcErrSum(
                        audioData, index + result.Length, 4);

                    // if click fixed
                    if (result.ErrSum < 0.01F) //0.005F //0.03F
                    {
                        result.Success = true;
                        break;
                    }
                }
            }

            return result;
        }

        private static float CalcErrSum(AudioData audioData, int position, int length)
        {
            float errSum = 0;

            for (var index = position; index < position + length; index++)
                errSum += Math.Abs(audioData.GetPredictionErr(index));

            return errSum;
        }

        private static bool SeveralSamplesInARowAreSuspicious(
            AudioData audioData,
            int position,
            int length)
        {
            for (var index = position; index < position + length; index++)
                if (ClickDetector.IsSampleSuspicious(
                    audioData,
                    index))
                    return true;

            return false;
        }
    }
}