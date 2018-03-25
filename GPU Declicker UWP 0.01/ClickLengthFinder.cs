using System;

namespace GPU_Declicker_UWP_0._01
{
    static class ClickLengthFinder
    {
        public static AttemptToFixResult FindSequenceOfDamagedSamples(
            AudioData audioData,
            int initPosition,
            int maxLength,
            int positionOfLastProcessedSample)
        {
            AttemptToFixResult bestResult = new AttemptToFixResult
            {
                Success = false
            };

            int position = initPosition;
            int minPosition = GetMinPosition(
                position, 
                positionOfLastProcessedSample, 
                10);
            
            while (position > minPosition)
            {
                float[] backup = BackupPredictionErrAverage(
                    audioData,
                    position, 
                    audioData.AudioProcessingSettings.HistoryLengthSamples + maxLength);

                AttemptToFixResult result = TryToFix(audioData,
                        position,
                        maxLength,
                        initPosition - position);

                if (result.BetterThan(bestResult))
                    bestResult = result;

                audioData.CurrentChannelRestoreInitState(position, maxLength);
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
            else
                return position - maxDepth;
        }

        private static void RestorePredictionErrAverage(
            AudioData audioData, 
            int position, 
            int length, 
            float[] backup)
        {
            for (int index = 0; index < length * 2; index++)
                audioData.SetErrorAverage(
                    position - length + index,
                    backup[index]);
        }

        private static float[] BackupPredictionErrAverage(
            AudioData audioData, 
            int position, 
            int length)
        {
            float[] backup = new float[length * 2];
            for (int index = 0; index < length * 2; index++)
                backup[index] = audioData.GetErrorAverage(
                    position - length + index);

            return backup;
        }

        private static AttemptToFixResult TryToFix(
            AudioData audioData, 
            int index, 
            int maxLength,
            int minLength)
        {
            AttemptToFixResult result = new AttemptToFixResult
            {
                Success = false,
                Position = index,
                Length = minLength,
                ErrSum = float.MaxValue
            };
            
            ClickRepairer.Repair(audioData, index, result.Length + 4);

            while (result.Length < maxLength)
            {
                ClickRepairer.Repair(audioData, index + result.Length + 4, 1);
                result.Length++;

                if (!SeveralSamplesInARowAreSuspicious(audioData, index, 3))
                {
                    result.ErrSum = CalcErrSum(audioData, index + result.Length + 1, 4);
                    
                    // if click fixed
                    if (result.ErrSum < 0.03F) //0.005F
                    {
                        result.Success = true;
                        // correction for better fix
                        result.Length++;
                        break;
                    }

                    result.Length++;
                }
            }

            return result;
        }

        private static float CalcErrSum(AudioData audioData, int position, int length)
        {
            float errSum = 0;

            for (int index = position; index < position + length; index++)
            {
                errSum += Math.Abs(
                               audioData.GetOutputSample(index) -
                               audioData.GetInputSample(index));
            }

            return errSum;
        }

        private static bool SeveralSamplesInARowAreSuspicious(
            AudioData audioData, 
            int position, 
            int length)
        {
            for (int index = position; index < position + length; index++)
            {
                if (ClickDetector.IsSampleSuspicious(
                        audioData,
                        index + length))
                    return true;
            }

            return false;
        }
    }
}
