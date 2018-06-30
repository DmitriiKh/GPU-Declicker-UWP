using System;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    public static class AudioProcessing
    {
        /// <summary>
        ///     Calculates prediction errors for a channel using CPU (Parallel.For)
        /// </summary>
        private static void CalculateBurgPredictionErrCPU(
            AudioData audioData,
            IProgress<double> progress)
        {
            var historyLengthSamples =
                audioData.AudioProcessingSettings.HistoryLengthSamples;
            var forwardPredictions = new float[audioData.LengthSamples()];
            var backwardPredictions = new float[audioData.LengthSamples()];
            var inputaudio = new float[audioData.LengthSamples()];

            for (var index = 0; index < audioData.LengthSamples(); index++)
                inputaudio[index] = audioData.GetInputSample(index);

            // we will use steps to report progress
            var step = audioData.LengthSamples() / 100;
            for (var index = historyLengthSamples;
                index <= audioData.LengthSamples();
                index += step)
            {
                progress.Report((double) 100 * index / audioData.LengthSamples());

                var end_position = index + step;
                if (end_position > audioData.LengthSamples()) end_position = audioData.LengthSamples();

                Parallel.For(index, end_position,
                    indexParallelFor =>
                        BurgPredictionCalculator.Calculate(
                            inputaudio,
                            forwardPredictions,
                            backwardPredictions,
                            indexParallelFor,
                            audioData.AudioProcessingSettings.CoefficientsNumber,
                            audioData.AudioProcessingSettings.HistoryLengthSamples)
                );
            }

            progress.Report(0);

            // for first samples forward predictions were not calculated
            // we use backward predictions only 
            for (var index = 0; index < historyLengthSamples; index++)
                audioData.SetPredictionErr(index,
                    audioData.GetInputSample(index) -
                    backwardPredictions[index]);
            // finds prediction error based on forward and backward predictions
            for (var index = historyLengthSamples;
                index < audioData.LengthSamples() - historyLengthSamples;
                index++)
                audioData.SetPredictionErr(index,
                    audioData.GetInputSample(index) -
                    (forwardPredictions[index] +
                     backwardPredictions[index]) / 2);
            // for last samples backward predictions were not calculated
            // we use forward predictions only
            for (var index = historyLengthSamples;
                index < audioData.LengthSamples() - historyLengthSamples;
                index++)
                audioData.SetPredictionErr(index,
                    audioData.GetInputSample(index) -
                    forwardPredictions[index]);
        }

        public static async Task ProcessAudioAsync(
            AudioData audioData,
            IProgress<double> progress,
            IProgress<string> status)
        {
            // clear clicks collected from previous scanning
            audioData.ClearAllClicks();

            audioData.SetCurrentChannelType(ChannelType.Left);
            await Task.Run(() => ProcessChannelAsync(
                audioData,
                progress,
                status)
            );

            if (audioData.IsStereo)
            {
                audioData.SetCurrentChannelType(ChannelType.Right);
                await Task.Run(() => ProcessChannelAsync(
                    audioData,
                    progress,
                    status)
                );
            }
        }

        private static async Task ProcessChannelAsync(
            AudioData audioData,
            IProgress<double> progress,
            IProgress<string> status)
        {
            var historyLengthSamples =
                audioData.AudioProcessingSettings.HistoryLengthSamples;

            SetStatus(audioData, status, "preprocessing");

            if (audioData.CurrentChannelIsPreprocessed())
            {
                audioData.RestoreCurrentChannelPredErrors();
            }
            else
            {
                CalculateBurgPredictionErrCPU(audioData, progress);
                audioData.SetCurrentChannelIsPreprocessed();
                audioData.BackupCurrentChannelPredErrors();
            }

            for (var index = 0; index < historyLengthSamples + 16; index++)
                audioData.SetErrorAverage(index, 0.001F);

            HelperCalculator.CalculateErrorAverageCPU(
                audioData,
                historyLengthSamples,
                audioData.LengthSamples(),
                historyLengthSamples);

            status.Report("");

            // copies input samples to output before scanning 
            for (var index = 0; index < audioData.LengthSamples(); index++)
                audioData.SetOutputSample(index, audioData.GetInputSample(index));

            SetStatus(audioData, status, "scanning");

            await Task.Run(() => ScanAudioAsync(audioData, progress));

            status.Report("");
        }

        private static void SetStatus(
            AudioData audioData,
            IProgress<string> status,
            string message)
        {
            if (audioData.IsStereo &&
                audioData.GetCurrentChannelType() == ChannelType.Left)
                status.Report("Left channel: " + message);

            if (audioData.IsStereo &&
                audioData.GetCurrentChannelType() == ChannelType.Right)
                status.Report("Right channel: " + message);

            if (!audioData.IsStereo) status.Report("Mono: " + message);
        }

        /// <summary>
        ///     Divides audio for segments and call ScanSegment for each og them
        /// </summary>
        private static async Task ScanAudioAsync(
            AudioData audioData,
            IProgress<double> progress
        )
        {
            var historyLengthSamples =
                audioData.AudioProcessingSettings.HistoryLengthSamples;

            var cpuCoreNumber = Environment.ProcessorCount;

            var segmentLenght = audioData.LengthSamples() / cpuCoreNumber;
            // make segments overlap
            segmentLenght += 1;

            var tasks = new Task[cpuCoreNumber];
            for (var cpuCoreIndex = 0; cpuCoreIndex < cpuCoreNumber; cpuCoreIndex++)
            {
                var segmentBeginning = cpuCoreIndex * segmentLenght;
                var segmentEnd = segmentBeginning + segmentLenght;
                // for first segment shift beginning to the right
                if (cpuCoreIndex == 0)
                    segmentBeginning += 2 * historyLengthSamples + 16;
                // for last segment shift end to the left
                if (cpuCoreIndex == cpuCoreNumber - 1)
                    segmentEnd -= 2 * historyLengthSamples +
                                  audioData.AudioProcessingSettings.MaxLengthCorrection;
                {
                    var index = cpuCoreIndex;
                    tasks[cpuCoreIndex] = Task.Factory.StartNew(() =>
                        ScanSegment(
                            audioData,
                            segmentBeginning,
                            segmentEnd,
                            progress,
                            index
                        ));
                }
                // need time to start task
                await Task.Delay(50);
            }

            for (var cpu_core = 0; cpu_core < cpuCoreNumber; cpu_core++) await tasks[cpu_core];

            // when all threads finished clear progress bar
            progress.Report(0);

            // as long as we used several cores to process segments
            // clicks are not in order
            audioData.SortClicks();
        }

        private static void ScanSegment(
            AudioData audioData,
            int segment_start,
            int segment_end,
            IProgress<double> progress,
            int cpu_core
        )
        {
            var result = new FixResult
            {
                Success = false
            };

            var max_length = 0;
            var last_processed_sample = 0;

            // cycle to check every sample
            for (var index = segment_start; index < segment_end; index++)
            {
                // only core #0 reports progress
                if (cpu_core == 0 &&
                    // to not report too many times
                    index % 1000 == 0)
                    progress?.Report((double) 100 * index / segment_end);

                result.Success = false;

                if (index > last_processed_sample &&
                    ClickDetector.IsSampleSuspicious(
                        audioData,
                        index))
                {
                    max_length = HelperCalculator.GetMaxLength(audioData, index);
                    result = ClickLengthFinder.FindLengthOfClick(
                        audioData,
                        index,
                        max_length,
                        last_processed_sample);
                }

                if (result.Success)
                {
                    ClickRepairer.Repair(audioData, result.Position, result.Length);
                    audioData.AddClickToList(
                        result.Position,
                        result.Length,
                        HelperCalculator.CalculateDetectionLevel(audioData, result.Position));

                    last_processed_sample = result.Position + result.Length + 1;
                }
            }
        }
    }
}