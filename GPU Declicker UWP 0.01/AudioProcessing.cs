using System;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    public class AudioProcessing
    {
        readonly int historyLengthSamples;
        readonly int coef_number;
        float threshold_for_detection;
        int max_lenghth_correction;

        public float ThresholdForDetection {
            get => threshold_for_detection;
            set => threshold_for_detection = value; }
        public int Max_lenghth_correction {
            get => max_lenghth_correction;
            set => max_lenghth_correction = value; }

        public AudioProcessing (
            int history, 
            int coef, 
            float threshold, 
            int max_length)
        {
            historyLengthSamples = history;
            coef_number = coef;
            ThresholdForDetection = threshold;
            Max_lenghth_correction = max_length;
        }

        /// <summary>
        /// Calculates one prediction error value for one sample using CPU
        /// For details please see 
        /// "A tutorial on Burg's method, algorithm and recursion.pdf"
        /// </summary>
        public void CalculateBurgPredictionThread(
            float[] inputaudio, float[] forwardPredictions,
            float[] backwardPredictions, int position, 
            int coef_number_local)
        {
            double[] b = new double[historyLengthSamples];
            double[] f = new double[historyLengthSamples];
            double[] a = new double[(coef_number + 1)];
            double ACCUM;
            double D = 0.0, mu = 0.0;
            
            for (int I = 0; I < historyLengthSamples; I++)
            {
                b[I] = inputaudio[I + position - historyLengthSamples];
                f[I] = b[I];
            }

            int N = historyLengthSamples - 1;

            for (int I = 1; I <= coef_number; I++)
                a[I] = 0.0;
            a[0] = 1.0;

            D = 0.0;
            for (int I = 0; I < historyLengthSamples; I++)
                D += 2.0 * f[I] * f[I];
            D -= f[0] * f[0] + b[N] * b[N];

            for (int k = 0; k < coef_number; k++)
            {
                mu = 0.0;
                for (int n = 0; n <= N - k - 1; n++)
                    mu += f[n + k + 1] * b[n];

                if (mu != 0)
                    mu *= -2.0 / D;

                for (int n = 0; n <= (k + 1) / 2; n++)
                {
                    double t1 = a[n] + mu * a[k + 1 - n];
                    double t2 = a[k + 1 - n] + mu * a[n];
                    a[n] = t1;
                    a[k + 1 - n] = t2;
                }

                for (int n = 0; n <= N - k - 1; n++)
                {
                    double t1 = f[n + k + 1] + mu * b[n];
                    double t2 = b[n] + mu * f[n + k + 1];
                    f[n + k + 1] = t1;
                    b[n] = t2;
                }

                D = (1.0 - mu * mu) * D - 
                    f[k + 1] * f[k + 1] 
                    - b[N - k - 1] * b[N - k - 1];
            }
            
            ACCUM = 0.0;
            for (int I = 1; I <= coef_number; I++)
                ACCUM += inputaudio[position - I] * (-1) * a[I];

            forwardPredictions[position] = (float)ACCUM;

            ACCUM = 0.0;
            for (int I = 1; I <= coef_number; I++)
                ACCUM += inputaudio[position - historyLengthSamples + I] *
                    (-1) * a[I];

            backwardPredictions[position - historyLengthSamples] = 
                (float)ACCUM;
        }

        /// <summary>
        /// Calculates prediction errors for a channel using CPU (Parallel.For)
        /// </summary>
        public void CalculateBurgPredictionErrCPU(
            AudioData audioData, 
            IProgress<double> progress)
        {
            int size = audioData.LengthSamples();
            float[] forwardPredictions = new float[size];
            float[] backwardPredictions = new float[size];
            float[] inputaudio = new float[size];
            for (int index = 0; index < size; index++)
                inputaudio[index] = audioData.GetInputSample(index);

            // we will use steps to report progress
            int step = audioData.LengthSamples() / 100;
            for (int index = historyLengthSamples; 
                index <= audioData.LengthSamples(); 
                index += step)
            {
                progress.Report((double)100 * index / audioData.LengthSamples());
                int end_position = index + step;
                if (end_position > audioData.LengthSamples())
                {
                    end_position = audioData.LengthSamples();
                }
                Parallel.For(index, end_position,
                    indexParallelFor =>
                        CalculateBurgPredictionThread(
                            inputaudio,
                            forwardPredictions,
                            backwardPredictions,
                            indexParallelFor,
                            coef_number)
                    );
            }
            progress.Report(0);
            // for first samples forward prediction was not calculated
            for (int index = 0; index < historyLengthSamples; index++)
            {
                audioData.SetPredictionErr(index,
                        audioData.GetInputSample(index) - 
                        backwardPredictions[index]);
            }
            // finds prediction error based on forward and backward predictions
            for (int index = historyLengthSamples; 
                index < size - historyLengthSamples; 
                index++)
            {
                audioData.SetPredictionErr(index,
                        audioData.GetInputSample(index) - 
                        (forwardPredictions[index] + 
                        backwardPredictions[index]) / 2);
            }
            // for last samples backward prediction was not calculated
            for (int index = historyLengthSamples; 
                index < size - historyLengthSamples; 
                index++)
            {
                audioData.SetPredictionErr(index,
                        audioData.GetInputSample(index) - 
                        forwardPredictions[index]);
            }
        }

        internal float Calc_detectoin_level(AudioData audioData, int position)
        {
            float threshold_level_detected = 0;
            float error = (Math.Abs(Calc_burg_pred_fromInput(audioData, position))); 

            // calculate average error value on the LEFT of the current sample
            float errorAverage = audioData.GetErrorAverage(position - 15);

            threshold_level_detected = error / errorAverage;
            return threshold_level_detected;
        }

        /// <summary>
        /// Processes audio
        /// </summary>
        public async Task ProcessAudioAsync(
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
                status
                ));
            
            if (audioData.IsStereo)
            {
                status.Report("Right channel: preprocessing");
                audioData.SetCurrentChannelType(ChannelType.Right);
                await Task.Run(() => ProcessChannelAsync(
                    audioData, 
                    progress,
                    status
                    ));
            }
        }

        private async Task ProcessChannelAsync(
            AudioData audioData, 
            IProgress<double> progress,
            IProgress<string> status)
        {
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

            for (int index = 0; index < historyLengthSamples + 16; index++)
                audioData.SetErrorAverage(index, 0.001F);

            CalculateErrorAverage_CPU(
                audioData,
                historyLengthSamples,
                audioData.LengthSamples(),
                historyLengthSamples);

            status.Report("");

            // copies input samples to output before scanning 
            for (int i = 0; i < audioData.LengthSamples(); i++)
            {
                audioData.SetOutputSample(i, audioData.GetInputSample(i));
            }

            SetStatus(audioData, status, "scanning");
            
            await Task.Run(() => ScanAudioAsync(audioData, progress));

            status.Report("");
        }

        private static void SetStatus(
            AudioData audioData, 
            IProgress<string> status,
            String message)
        {
            if (audioData.IsStereo &&
                            audioData.GetCurrentChannelType() == ChannelType.Left)
            {
                status.Report("Left channel: " + message);
            }
            if (audioData.IsStereo &&
                audioData.GetCurrentChannelType() == ChannelType.Right)
            {
                status.Report("Right channel: " + message);
            }
            if (!audioData.IsStereo)
            {
                status.Report("Mono: " + message);
            }
        }

        public void CalculateErrorAverage_CPU(
            AudioData audioData, 
            int start_position, 
            int end_position, 
            int _base)
        {
            float errorAverage = 
                CalcErrorAverageForOneSample(audioData, start_position);

            audioData.SetErrorAverage(start_position, errorAverage);

            // Fast version of CalcErrorAverageForOneSample
            // Keep sliding to next block of 16 Burg prediction errors
            for (int index = start_position; 
                index < end_position - 15; 
                index += 16)
            {
                // check each period of 16 errors to find maximums
                float maxErrorInExcludedBlock = 0, 
                    maxErrorInIncludedBlock = 0, 
                    tempExcludedBlock = 0, 
                    tempIncludedBlock = 0;

                for (int indexCurrent = index; indexCurrent < index + 16; indexCurrent++)
                {
                    // find max in the block which will be excluded
                    tempExcludedBlock = Math.Abs(audioData.GetPredictionErr(indexCurrent - _base));
                    if (maxErrorInExcludedBlock < tempExcludedBlock) maxErrorInExcludedBlock = tempExcludedBlock;
                    // find max in the block which will be included
                    tempIncludedBlock = Math.Abs(audioData.GetPredictionErr(indexCurrent));
                    if (maxErrorInIncludedBlock < tempIncludedBlock) maxErrorInIncludedBlock = tempIncludedBlock;
                }
                // correction based on previously calculated errorAverage
                errorAverage = errorAverage + (maxErrorInIncludedBlock - maxErrorInExcludedBlock) / (_base / 16);
                          
                if (errorAverage < 0.0001)
                    errorAverage = 0.0001F;      // minimum result to return is 0.0001

                for (int l = index; l < index + 16; l++)
                    audioData.SetErrorAverage(l,
                        errorAverage);
            }
        }


        /// <summary>
        /// Divides audio for segments and call ScanSegment for each og them
        /// </summary>
        /// <param name="audioData"></param>
        private async Task ScanAudioAsync(
            AudioData audioData,
            IProgress<double> progress
            )
        {
            int cpu_core_number = Environment.ProcessorCount;

            int segment_lenght = (int)(
                audioData.LengthSamples() / cpu_core_number
                );
            // make segments overlap
            segment_lenght += 1;

            Task[] tasks = new Task[cpu_core_number];
            for (int cpu_core = 0; cpu_core < cpu_core_number; cpu_core++)
            {
                int segment_start = cpu_core * segment_lenght;
                int segment_end = segment_start + segment_lenght;
                if (cpu_core == 0)
                    segment_start += 2 * historyLengthSamples + 16;
                if (cpu_core == cpu_core_number - 1)
                    segment_end -= 2 * historyLengthSamples + 
                        max_lenghth_correction;
                int index = cpu_core;
                tasks[cpu_core] = Task.Factory.StartNew(() =>
                    ScanSegment(
                        audioData,
                        segment_start,
                        segment_end,
                        progress,
                        index
                        ));
                // need time to start task
                await Task.Delay(50);
            }
            for (int cpu_core = 0; cpu_core < cpu_core_number; cpu_core++)
            {
                await tasks[cpu_core];
            }

            // when all threads finished clear progress bar
            progress.Report(0);

            // as long as we used several cores to process segments
            // clicks are not in order
            audioData.SortClicks();
        }

        private void ScanSegment(
            AudioData audioData,
            int segment_start, 
            int segment_end,
            IProgress<double> progress,
            int cpu_core
            )
        {
            float threshold_level_detected = 0;
            int max_length = 0;
            int last_processed_sample = 0;

            // cycle to check every sample
            for (int index = segment_start; index < segment_end; index++)
            {
                // only core #0 reports progress
                if (cpu_core == 0 &&
                    // to not report too many times
                    index % 1000 == 0 && 
                    progress != null)
                        progress?.Report((double)100 * index / segment_end);

                int length = -1;
                int indexRef = index;
                if ( index > last_processed_sample && 
                    Is_sample_suspicuous(
                        audioData, 
                        index,  
                        out threshold_level_detected)
                        )
                {
                    max_length = GetMaxLength(audioData, index);
                    length = FindSequenceOfDamagedSamples(
                        audioData, 
                        ref indexRef,
                        max_length,
                        last_processed_sample);
                }

                if (length > 0)
                {
                    Repair(audioData, indexRef, length); 
                    audioData.AddClickToList(indexRef, 
                        length, 
                        threshold_level_detected, 
                        this); 

                    last_processed_sample = indexRef + length + 1;
                }

                if (length == 0)
                {
                    RestoreInitState(audioData, indexRef, max_length + 20);
                }
            }
        }

        private int FindSequenceOfDamagedSamples(
            AudioData audioData, 
            ref int index, 
            int maxLength,
            int indexOfLastProcessedSample)
        {
            bool successCurrent = false;
            int indexCurrent = index;
            // initialize lengthCurrent with a big number
            int lengthCurrent = 200 * maxLength;
            float errSumCurrent = 10000;

            for(int correction = 0; correction >= -10; correction--)
            {
                // skip indexes before last processed sample
                if (index + correction < indexOfLastProcessedSample)
                    break;

                TryToFix(audioData, 
                    index, 
                    maxLength, 
                    correction, 
                    out int length, 
                    out bool success, 
                    out float errSum);

                if (success)
                {
                    successCurrent = true;
                    if (length < lengthCurrent || errSum < errSumCurrent)
                    {
                        indexCurrent = index + correction;
                        lengthCurrent = length;
                    }
                }

                RestoreInitState(
                    audioData, 
                    index + correction - 1, 
                    maxLength - correction + 20);
            }

            if (successCurrent)
            {
                index = indexCurrent;
                //return length one sample longer
                return lengthCurrent + 1;
            }
            else
                return 0;
        }

        private void TryToFix(
            AudioData audioData, 
            int index, 
            int max_length, 
            int correction, 
            out int length, 
            out bool success, 
            out float errSum)
        {
            length = - correction;
            success = false;
            float end_diff = 10000;
            Repair(audioData, index + correction, length + 4);
            while (length < max_length)
            {
                Repair(audioData, index + correction + length + 4, 1);
                length++;

                if (!Is_sample_suspicuous(
                        audioData, 
                        index + correction + length + 4 + 1, 
                        out float threshold_level_detected1) &&
                    !Is_sample_suspicuous(
                        audioData, 
                        index + correction + length + 4 + 2, 
                        out float threshold_level_detected2) &&
                    !Is_sample_suspicuous(
                        audioData, 
                        index + correction + length + 4 + 3, 
                        out float threshold_level_detected3))
                {
                    end_diff = 
                        Math.Abs(
                            audioData.GetOutputSample(index + correction + length + 1) -
                            audioData.GetInputSample(index + correction + length + 1)) +
                        Math.Abs(
                            audioData.GetOutputSample(index + correction + length + 2) -
                            audioData.GetInputSample(index + correction + length + 2)) +
                        Math.Abs(
                            audioData.GetOutputSample(index + correction + length + 3) -
                            audioData.GetInputSample(index + correction + length + 3)) +
                        Math.Abs(
                            audioData.GetOutputSample(index + correction + length + 4) -
                            audioData.GetInputSample(index + correction + length + 4));

                    if (end_diff < 0.03F) //0.005F
                    {
                        success = true;
                        break;
                    }
                }
            }

            errSum = end_diff; 
        }

        /// <summary>
        /// Returns prediction for a sample at i position
        /// </summary>
        public float Calc_burg_pred(
            AudioData audioData, 
            int position)
        {
            // use output audio as an input because it already contains
            // fixed samples before sample i
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
            
            CalculateBurgPredictionThread(
                audioShort, 
                forwardPredictionsShort, 
                backwardPredictionsShort, 
                historyLengthSamples,
                coef_number * 2);

            // return prediction for i sample
            return forwardPredictionsShort[historyLengthSamples];
        }

        public float Calc_burg_pred_fromInput(
            AudioData audioData,
            int position)
        {
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

            CalculateBurgPredictionThread(
                audioShort, 
                forwardPredictionsShort, 
                backwardPredictionsShort, 
                historyLengthSamples, 
                coef_number * 2);

            // return prediction for i sample
            return forwardPredictionsShort[historyLengthSamples];
        }

        private int GetMaxLength(AudioData audioData, int position)
        {
            int lenght = 0;
            float error = (Math.Abs(audioData.GetPredictionErr(position))); 
            float errorAverage = audioData.GetErrorAverage(position - 15); 
            float rate = error / (ThresholdForDetection * errorAverage);
            while (error > errorAverage)
            {
                lenght = lenght + 3;
                error = (Math.Abs(audioData.GetPredictionErr(position + lenght)) +
                    Math.Abs(audioData.GetPredictionErr(position + 1 + lenght)) +
                    Math.Abs(audioData.GetPredictionErr(position + 2 + lenght))) / 3;
            }
            // the result is multiplication lenght and rate (doubled)
            int max_length = (int) (lenght * rate * 2); 

            // follow user's limit
            if (max_length > Max_lenghth_correction)
                max_length = Max_lenghth_correction;

            return max_length;
        }

        private bool Is_sample_suspicuous(
            AudioData audioData, 
            int position,  
            out float thresholdLevelDetected)
        {
            // average of three current errors (current and two next) makes 
            // increase in errors values more distinctive
            float error = (Math.Abs(audioData.GetPredictionErr(position)));  

            // calculate average error value on the LEFT of the current sample
            float errorAverage = audioData.GetErrorAverage(position - 15);

            thresholdLevelDetected = error / errorAverage;

            if (thresholdLevelDetected > ThresholdForDetection)                      
                return true;                                         
            else
                return false;
        }

        internal float CalcErrorAverageForOneSample(AudioData audioData, int position)
        {
            int Base = historyLengthSamples;
            float errorAverage = 0;
            
            for (int blockIndex = 0; blockIndex < Base - 16; blockIndex += 16)
            {
                // check each period of 16 errors to find maximums
                float maxErrorInBlock = 0;
                for (int indexInBlock = 0; indexInBlock < 16; indexInBlock++)
                {
                    float temp = Math.Abs(audioData.GetPredictionErr(
                        position - Base + blockIndex + indexInBlock
                        ));
                    if (maxErrorInBlock < temp) maxErrorInBlock = temp;
                }
                errorAverage = errorAverage + maxErrorInBlock;  // sum up maximums
            }
                
            errorAverage = errorAverage / (Base / 16) + 0.0000001F; // to find average
            if (errorAverage < 0.0001)
                errorAverage = 0.0001F;      // minimum result to return is 0.0001
            return errorAverage;
        }

        /// <summary>
        /// Replace output sample at position with prediction and 
        /// sets prediction error sample to zero
        /// </summary>
        /// <param name="position"></param>
        /// <param name="lenght"></param>
        internal float Repair(AudioData audioData, int position, int lenght)
        {
            for (int index = position; index <= position + lenght; index++)
            {
                audioData.SetPredictionErr(index, 0.001F);
                audioData.SetOutputSample(
                    index,
                    Calc_burg_pred(audioData, index)
                    );
            }
            
            CalculateErrorAverage_CPU(
                audioData, 
                position - historyLengthSamples, 
                position + lenght + historyLengthSamples, 
                historyLengthSamples);

            return Calc_detectoin_level(audioData, position);
        }

        public void RestoreInitState(
            AudioData audioData, 
            int position, 
            int lenght)
        {
            audioData.CurrentChannelRestoreInitState(position, lenght);

            CalculateErrorAverage_CPU(
                audioData, 
                position - historyLengthSamples, 
                position + lenght + historyLengthSamples, 
                historyLengthSamples);
        }
    }
}