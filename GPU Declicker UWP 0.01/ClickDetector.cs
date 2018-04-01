using System;

namespace GPU_Declicker_UWP_0._01
{
    public static class ClickDetector
    {
        static public bool IsSampleSuspicious(
            AudioData audioData,
            int position)
        {
            float error = (Math.Abs(audioData.GetPredictionErr(position)));
            
            float errorAverage = audioData.GetErrorAverage(position - 15);

            float thresholdLevelDetected = error / errorAverage;

            if (thresholdLevelDetected > 
                audioData.AudioProcessingSettings.ThresholdForDetection)
                return true;
            else
                return false;
        }
    }
}
