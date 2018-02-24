using System;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    public class AudioProcessing
    {
        readonly int history_length_samples;
        readonly int coef_number;
        float threshold_for_detection;
        int max_lenghth_correction;

        public float Threshold_for_detection {
            get => threshold_for_detection;
            set => threshold_for_detection = value; }
        public int Max_lenghth_correction {
            get => max_lenghth_correction;
            set => max_lenghth_correction = value; }

        public AudioProcessing (int history, int coef, float threshold, int max_length)
        {
            history_length_samples = history;
            coef_number = coef;
            Threshold_for_detection = threshold;
            Max_lenghth_correction = max_length;
        }

        /// <summary>
        /// Calculates one prediction error value for one sample using CPU
        /// For details please see 
        /// "A tutorial on Burg's method, algorithm and recursion.pdf"
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="position">indicates position of the sample in array</param>
        /// <param name="history">number of samples which are used for finding 
        /// prediction errors by Burg's method</param>
        /// <param name="coeffs">number of coefficients in Burg's method </param>
        public void CalculateBurgPredictionThread(
            float[] inputaudio, float[] forwardPredictions,
            float[] backwardPredictions, int position, 
            int coef_number_local)
        {
            double[] b = new double[history_length_samples];
            double[] f = new double[history_length_samples];
            double[] a = new double[(coef_number + 1)];
            double ACCUM;
            double D = 0.0, mu = 0.0;
            
            for (int I = 0; I < history_length_samples; I++)
            {
                b[I] = inputaudio[I + position - history_length_samples];
                f[I] = b[I];
            }

            int N = history_length_samples - 1;

            for (int I = 1; I <= coef_number; I++)
                a[I] = 0.0;
            a[0] = 1.0;

            D = 0.0;
            for (int I = 0; I < history_length_samples; I++)
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
                ACCUM += inputaudio[position - history_length_samples + I] * (-1) * a[I];

            backwardPredictions[position - history_length_samples] = (float)ACCUM;

            return; 
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
            for (int i = 0; i < size; i++)
                inputaudio[i] = audioData.GetInputSample(i);

            // we will use steps to report progress
            int step = audioData.LengthSamples() / 100;
            for (int index = history_length_samples; index <= audioData.LengthSamples(); index += step)
            {
                progress.Report((double)100 * index / audioData.LengthSamples());
                int end_position = index + step;
                if (end_position > audioData.LengthSamples())
                {
                    end_position = audioData.LengthSamples();
                }
                Parallel.For(index, end_position,
                    i =>
                        CalculateBurgPredictionThread(
                            inputaudio,
                            forwardPredictions,
                            backwardPredictions,
                            i,
                            coef_number)
                    );
            }
            progress.Report(0);
            // for first samples forward prediction was not calculated
            for (int i = 0; i < history_length_samples; i++)
            {
                audioData.SetPredictionErr(i,
                        audioData.GetInputSample(i) - backwardPredictions[i]);
            }
            // finds prediction error based on forward and backward predictions
            for (int i = history_length_samples; i < size - history_length_samples; i++)
            {
                audioData.SetPredictionErr(i,
                        audioData.GetInputSample(i) - 
                        (forwardPredictions[i] + backwardPredictions[i]) / 2);
            }
            // for last samples backward prediction was not calculated
            for (int i = history_length_samples; i < size - history_length_samples; i++)
            {
                audioData.SetPredictionErr(i,
                        audioData.GetInputSample(i) - forwardPredictions[i]);
            }
        }

        internal float Calc_detectoin_level(AudioData audioData, int position)
        {
            float threshold_level_detected = 0;
            float a = (Math.Abs(Calc_burg_pred_fromInput(audioData, position))); 

            // calculate average error value on the LEFT of the current sample
            float a_av = audioData.Get_a_average(position - 15);

            threshold_level_detected = a / a_av;
            return threshold_level_detected;
        }

        /// <summary>
        /// Processes audio
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="history"></param>
        /// <param name="coeffs"></param>
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
            if (audioData.IsStereo && audioData.GetCurrentChannelType() == ChannelType.Left)
            {
                status.Report("Left channel: preprocessing");
            }
            if (audioData.IsStereo && audioData.GetCurrentChannelType() == ChannelType.Right)
            {
                status.Report("Right channel: preprocessing");
            }
            if (!audioData.IsStereo)
            {
                status.Report("Mono: preprocessing");
            }

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

            for (int i = 0; i < history_length_samples + 16; i++)
                audioData.Set_a_average(i, 0.001F);

            Calculate_a_average_CPU(
                audioData, 
                history_length_samples, 
                audioData.LengthSamples(), 
                history_length_samples);

            status.Report("");

            // copies input samples to output before scanning 
            for (int i = 0; i < audioData.LengthSamples(); i++)
            {
                audioData.SetOutputSample(i, audioData.GetInputSample(i));
            }

            if (audioData.IsStereo && audioData.GetCurrentChannelType() == ChannelType.Left)
            {
                status.Report("Left channel: scanning");
            }
            if (audioData.IsStereo && audioData.GetCurrentChannelType() == ChannelType.Right)
            {
                status.Report("Right channel: scanning");
            }
            if (!audioData.IsStereo)
            {
                status.Report("Mono: scanning");
            }

            //              
            await Task.Run(() => ScanAudioAsync(audioData, progress));

            status.Report("");
        }

        public void Calculate_a_average_CPU(AudioData audioData, int start_position, int end_position, int _base)
        {
            float a_av = 
                Calc_a_average_for_One_Sample(audioData, start_position);

            audioData.Set_a_average(start_position, a_av);
            
            for (int i = start_position; i < end_position - 15; i += 16)
            {
                // check each period of 16 errors to find maximums
                float aa_first_16 = 0, aa_last_16 = 0, temp_first = 0, temp_last = 0;
                for (int l = i; l < i + 16; l++)
                {
                    temp_first = Math.Abs(audioData.GetPredictionErr(l - _base));
                    if (aa_first_16 < temp_first) aa_first_16 = temp_first;
                    temp_last = Math.Abs(audioData.GetPredictionErr(l));
                    if (aa_last_16 < temp_last) aa_last_16 = temp_last;
                }
                a_av = a_av + (aa_last_16 - aa_first_16) / (_base / 16);  // sum up maximums
                          
                if (a_av < 0.0001)
                    a_av = 0.0001F;      // minimum result to return is 0.001

                for (int l = i; l < i + 16; l++)
                    audioData.Set_a_average(l,
                        a_av);
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
                    segment_start += 2 * history_length_samples + 16;
                if (cpu_core == cpu_core_number - 1)
                    segment_end -= 2 * history_length_samples + max_lenghth_correction;
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
            for (int i = segment_start; i < segment_end; i++)
            {
                // only core #0 reports progress
                if (cpu_core == 0 &&
                    // to not report too many times
                    i % 1000 == 0 && 
                    progress != null)
                        progress?.Report((double)100 * i / segment_end);

                int length = -1;
                int i_ref = i;
                if ( i > last_processed_sample && 
                    Is_sample_suspicuous(
                        audioData, 
                        i,  
                        out threshold_level_detected)
                        )
                {
                    max_length = Get_max_length(audioData, i);
                    length = Find_sequence(
                        audioData, 
                        ref i_ref,
                        max_length,
                        last_processed_sample);
                }

                if (length > 0)
                {
                    Repair(audioData, i_ref, length); 
                    audioData.AddClickToList(i_ref, length, threshold_level_detected, this); 

                    last_processed_sample = i_ref + length + 1;
                }

                if (length == 0)
                {
                    RestoreInitState(audioData, i_ref, max_length + 20);
                }
            }
        }

        private int Find_sequence(
            AudioData audioData, 
            ref int i, 
            int max_length,
            int last_processed_sample)
        {
            bool success_current = false;
            int i_current = i;
            int length_current = 200 * max_length;
            float errSum_current = 10000;

            for(int i_correction = 0; i_correction >= -10; i_correction--)
            {
                // skip before last processed sample
                if (i + i_correction < last_processed_sample)
                    break;

                TryToFix(audioData, i, max_length, i_correction, out int length, out bool success, out float errSum);

                if (success)
                {
                    success_current = true;
                    if (length < length_current || errSum < errSum_current)
                    {
                        i_current = i + i_correction;
                        length_current = length;
                    }
                }

                RestoreInitState(audioData, i + i_correction - 1, max_length - i_correction + 20);
            }

            if (success_current)
            {
                i = i_current;
                //return length one sample longer
                return length_current + 1;
            }
            else
                return 0;
        }

        private void TryToFix(AudioData audioData, int i, int max_length, int i_correction, out int length, out bool success, out float errSum)
        {
            length = - i_correction;
            success = false;
            float end_diff = 10000;
            Repair(audioData, i + i_correction, length + 4);
            while (length < max_length)
            {
                Repair(audioData, i + i_correction + length + 4, 1);
                length++;

                if (!Is_sample_suspicuous(audioData, i + i_correction + length + 4 + 1, out float threshold_level_detected1) &&
                    !Is_sample_suspicuous(audioData, i + i_correction + length + 4 + 2, out float threshold_level_detected2) &&
                    !Is_sample_suspicuous(audioData, i + i_correction + length + 4 + 3, out float threshold_level_detected3))
                {
                    end_diff = Math.Abs(audioData.GetOutputSample(i + i_correction + length + 1) - audioData.GetInputSample(i + i_correction + length + 1)) +
                    Math.Abs(audioData.GetOutputSample(i + i_correction + length + 2) - audioData.GetInputSample(i + i_correction + length + 2)) +
                    Math.Abs(audioData.GetOutputSample(i + i_correction + length + 3) - audioData.GetInputSample(i + i_correction + length + 3)) +
                    Math.Abs(audioData.GetOutputSample(i + i_correction + length + 4) - audioData.GetInputSample(i + i_correction + length + 4));

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
        /// <param name="audioData"></param>
        /// <param name="i"></param>
        /// <param name="history"></param>
        /// <param name="number_of_coeffs_for_restoration"></param>
        /// <returns></returns>
        public float Calc_burg_pred(
            AudioData audioData, 
            int i)
        {
            // use output audio as an input because it already contains
            // fixed samples before sample i
            float[] audioShort = new float[history_length_samples + 1];
            for (int k = 0; k < history_length_samples + 1; k++)
            {
                    audioShort[k] = audioData.GetOutputSample(i - history_length_samples + k);
            }

            // array for results
            float[] forwardPredictionsShort = new float[history_length_samples + 1];

            // we need this array for calling CalculateBurgPredictionThread
            float[] backwardPredictionsShort = new float[history_length_samples + 1];
            
            CalculateBurgPredictionThread(
                audioShort, 
                forwardPredictionsShort, 
                backwardPredictionsShort, 
                history_length_samples,
                coef_number * 2);

            // return prediction for i sample
            return forwardPredictionsShort[history_length_samples];
        }

        public float Calc_burg_pred_fromInput(
            AudioData audioData,
            int i)
        {
            // use output audio as an input because it already contains
            // fixed samples before sample i
            float[] audioShort = new float[history_length_samples + 1];
            for (int k = 0; k < history_length_samples + 1; k++)
            {
                audioShort[k] = audioData.GetInputSample(i - history_length_samples + k);
            }

            // array for results
            float[] forwardPredictionsShort = new float[history_length_samples + 1];

            // we need this array for calling CalculateBurgPredictionThread
            float[] backwardPredictionsShort = new float[history_length_samples + 1];

            CalculateBurgPredictionThread(
                audioShort, 
                forwardPredictionsShort, 
                backwardPredictionsShort, 
                history_length_samples, 
                coef_number * 2);

            // return prediction for i sample
            return forwardPredictionsShort[history_length_samples];
        }

        private int Get_max_length(AudioData audioData, int i)
        {
            int lenght = 0;
            float a = (Math.Abs(audioData.GetPredictionErr(i))); 
            float a_av = audioData.Get_a_average(i - 15); 
            float rate = a / (Threshold_for_detection * a_av);
            while (a > a_av)
            {
                lenght = lenght + 3;
                a = (Math.Abs(audioData.GetPredictionErr(i + lenght)) +
                    Math.Abs(audioData.GetPredictionErr(i + 1 + lenght)) +
                    Math.Abs(audioData.GetPredictionErr(i + 2 + lenght))) / 3;
            }
            int max_length = (int) (lenght * rate * 2); // the result is multiplication lenght and rate (doubled)

            // follow user's limit
            if (max_length > Max_lenghth_correction)
                max_length = Max_lenghth_correction;

            return max_length;
        }

        private bool Is_sample_suspicuous(
            AudioData audioData, 
            int i,  
            out float threshold_level_detected)
        {
            // average of three current errors (current and two next) makes 
            // increase in errors values more distinctive
            float a = (Math.Abs(audioData.GetPredictionErr(i)));  

            // calculate average error value on the LEFT of the current sample
            float a_av = audioData.Get_a_average(i - 15);

            threshold_level_detected = a / a_av;

            if (threshold_level_detected > Threshold_for_detection)                      
                return true;                                         
            else
                return false;
        }

        internal float Calc_a_average_for_One_Sample(AudioData audioData, int position)
        {
            int Base = history_length_samples;
            float a_av = 0;
            
            for (int m = 0; m < Base - 16; m += 16)
            {                           
                // check each period of 16 errors to find maximums
                float aa = 0, temp = 0;
                for (int l = 0; l < 16; l++)
                {
                    temp = Math.Abs(audioData.GetPredictionErr(position - Base + m + l));
                    if (aa < temp) aa = temp;
                }
                a_av = a_av + aa;  // sum up maximums
            }
                
            a_av = a_av / (Base / 16) + 0.0000001F; // to find average
            if (a_av < 0.0001)
                a_av = 0.0001F;      // minimum result to return is 0.001
            return a_av;
        }

        /// <summary>
        /// Replace output sample at position with prediction and 
        /// sets prediction error sample to zero
        /// </summary>
        /// <param name="position"></param>
        /// <param name="lenght"></param>
        internal float Repair(AudioData audioData, int position, int lenght)
        {
            for (int i = position; i <= position + lenght; i++)
            {
                audioData.SetPredictionErr(i, 0.001F);
                audioData.SetOutputSample(
                    i,
                    Calc_burg_pred(audioData, i)
                    );
            }
            
            Calculate_a_average_CPU(
                audioData, 
                position - history_length_samples, 
                position + lenght + history_length_samples, 
                history_length_samples);

            return Calc_detectoin_level(audioData, position);
        }

        public void RestoreInitState(AudioData audioData, int position, int lenght)
        {
            audioData.CurrentChannelRestoreInitState(position, lenght);

            Calculate_a_average_CPU(
                audioData, 
                position - history_length_samples, 
                position + lenght + history_length_samples, 
                history_length_samples);
        }
    }
}