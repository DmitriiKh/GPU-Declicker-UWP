using System;
using System.Threading.Tasks;
using GPUDeclickerUWP.Model.Data;

namespace GPUDeclickerUWP.Model.Processing
{
    public static class AudioProcessing
    {
        /// <summary>
        ///     Calculates prediction errors for a channel using CPU (Parallel.For)
        /// </summary>
        private static void CalculateBurgPredictionErrCpu(
            AudioData audioData,
            IProgress<double> progress)
        {
            var historyLengthSamples =
                audioData.AudioProcessingSettings.HistoryLengthSamples;
            var forwardPredictions = new float[audioData.LengthSamples()];
            var backwardPredictions = new float[audioData.LengthSamples()];
            var inputaudio = new double[audioData.LengthSamples()];

            for (var index = 0; index < audioData.LengthSamples(); index++)
                inputaudio[index] = audioData.GetInputSample(index);

            // we will use steps to report progress
            var step = audioData.LengthSamples() / 100;
            for (var index = historyLengthSamples + 1;
                index <= audioData.LengthSamples();
                index += step)
            {
                progress.Report((double) 100 * index / audioData.LengthSamples());

                var endPosition = index + step;
                if (endPosition > audioData.LengthSamples()) endPosition = audioData.LengthSamples();
                
                Parallel.For(index, endPosition,
                    indexParallelFor =>
                    {
                        var fba = new FastBurgAlgorithm64(inputaudio);
                        fba.Train(indexParallelFor, 
                            audioData.AudioProcessingSettings.CoefficientsNumber, 
                            audioData.AudioProcessingSettings.HistoryLengthSamples);
                        forwardPredictions[indexParallelFor] = (float)fba.GetForwardPrediction();
                        backwardPredictions[indexParallelFor - 
                                            audioData.AudioProcessingSettings.HistoryLengthSamples -
                                            1] = 
                            (float)fba.GetBackwardPrediction();
                    }
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

        /// <summary>
        /// Scans audio data to find damaged samples
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="progress"></param>
        /// <param name="status"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Scans channel to find damaged samples
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="progress"></param>
        /// <param name="status"></param>
        /// <returns></returns>
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
                // if prediction errors were previously calculated
                audioData.RestoreCurrentChannelPredErrors();
            }
            else
            {
                CalculateBurgPredictionErrCpu(audioData, progress);
                audioData.SetCurrentChannelIsPreprocessed();
                audioData.BackupCurrentChannelPredErrors();
            }

            for (var index = 0; index < historyLengthSamples + 16; index++)
                audioData.SetErrorAverage(index, 0.001F);

            HelperCalculator.CalculateErrorAverageCpu(
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

        /// <summary>
        /// Sets stutus report according to current channel and mesage
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="status"></param>
        /// <param name="message"></param>
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
                    segmentBeginning += 2 * historyLengthSamples;
                // for last segment shift end to the left
                if (cpuCoreIndex == cpuCoreNumber - 1)
                    segmentEnd -= 2 * historyLengthSamples +
                                  audioData.AudioProcessingSettings.MaxLengthOfCorrection;
                {
                    // need new variable to pass as a parameter
                    // because cpuCoreIndex will be changed
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

            // wait for all tasks to be finished
            for (var cpuCore = 0; cpuCore < cpuCoreNumber; cpuCore++)
                await tasks[cpuCore];

            // when all threads finished clear progress bar
            progress.Report(0);

            // as long as we used several cores to process segments
            // clicks are not in order
            audioData.SortClicks();
        }

        /// <summary>
        /// Scans segment of audio channel to find damaged samples
        /// and repai them
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="segmentStart"></param>
        /// <param name="segmentEnd"></param>
        /// <param name="progress"></param>
        /// <param name="cpuCore"></param>
        private static void ScanSegment(
            AudioData audioData,
            int segmentStart,
            int segmentEnd,
            IProgress<double> progress,
            int cpuCore)
        {
            var lastProcessedSample = 0;

            // cycle to check every sample
            for (var index = segmentStart; index < segmentEnd; index++)
            {
                // only core #0 reports progress
                if (cpuCore == 0)
                    ThrottledReportProgress(progress, index, segmentEnd);

                if (index <= lastProcessedSample || !ClickDetector.IsSampleSuspicious(
                        audioData,
                        index)) continue;

                var maxLength = HelperCalculator.GetMaxLength(audioData, index);
                var result = ClickLengthFinder.FindLengthOfClick(
                    audioData,
                    index,
                    maxLength,
                    lastProcessedSample);

                if (!result.Success) continue;

                ClickRepairer.Repair(audioData, result.Position, result.Length);
                audioData.AddClickToList(new AudioClick(
                    result.Position,
                    result.Length,
                    HelperCalculator.CalculateDetectionLevel(audioData, result.Position),
                    audioData,
                    audioData.GetCurrentChannelType()));

                lastProcessedSample = result.Position + result.Length + 1;
            }
        }

        /// <summary>
        /// Throttled progress report
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        private static void ThrottledReportProgress(IProgress<double> progress, int index, int length)
        {
            if (index % 1000 == 0)
                progress?.Report((double)100 * index / length);
        }
    }
}