using System;

namespace GPU_Declicker_UWP_0._01
{
    public static class ClickDetector
    {
        public static bool IsSampleSuspicious(
            AudioData audioData,
            int position)
        {
            var error = Math.Abs(audioData.GetPredictionErr(position));

            var errorAverage = audioData.GetErrorAverage(position - 15);

            var thresholdLevelDetected = error / errorAverage;

            return thresholdLevelDetected >
                   audioData.AudioProcessingSettings.ThresholdForDetection;
        }
    }
}