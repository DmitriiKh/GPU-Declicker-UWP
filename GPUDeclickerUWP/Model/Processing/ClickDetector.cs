using System;
using GPUDeclickerUWP.Model.Data;

namespace GPUDeclickerUWP.Model.Processing
{
    public static class ClickDetector
    {
        public static bool IsSampleSuspicious(
            AudioData audioData,
            int position)
        {
            var error = Math.Abs(audioData.GetPredictionErr(position));

            // usualy average prediction error is stable in 10-20
            // samples before click
            const int offset = 15;
            var errorAverage = audioData.GetErrorAverage(position - offset);

            var errorLevelDetected = error / errorAverage;

            return errorLevelDetected >
                   audioData.AudioProcessingSettings.ThresholdForDetection;
        }
    }
}