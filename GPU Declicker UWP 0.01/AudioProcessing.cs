﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace GPU_Declicker_UWP_0._01
{
    public class AudioProcessing
    {
        int history_length_samples;
        int coef_number;
        float threshold_for_detection;
    
        private object lockObject = new object();

        public AudioProcessing (int history, int coef, float threshold)
        {
            history_length_samples = history;
            coef_number = coef;
            threshold_for_detection = threshold;
        }

        /// <summary>
        /// Calculates one prediction error value for one sample using CPU
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="i">indicates number of element in arrays</param>
        /// <param name="history">number of samples which are used for finding 
        /// prediction errors by Burg's method</param>
        /// <param name="coeffs">number of coefficients in Burg's method </param>
        private void CalculateBurgPredictionThread(
            float[] inputaudio, float[] forwardPredictions,
            float[] backwardPredictions, int i, 
            int coef_number_local)
        {
            double[] b = new double[history_length_samples];
            double[] f = new double[history_length_samples];
            double[] a = new double[(coef_number + 1)];
            double ACCUM;
            double D = 0.0, mu = 0.0;
            
            for (int I = 0; I < history_length_samples; I++)
            {
                b[I] = inputaudio[I + i - history_length_samples];
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
                ACCUM += inputaudio[i - I] * (-1) * a[I];

            forwardPredictions[i] = (float)ACCUM;

            ACCUM = 0.0;
            for (int I = 1; I <= coef_number; I++)
                ACCUM += inputaudio[i - history_length_samples + I] * (-1) * a[I];

            backwardPredictions[i - history_length_samples] = (float)ACCUM;

            return; 
        }

        /// <summary>
        /// Calculates prediction errors for a channel using CPU (Parallel.For)
        /// </summary>
        public void CalculateBurgPredictionErrCPU(
            AudioDataClass audioData, 
            IProgress<double> progress)
        {
            int size = audioData.Length_samples;
            float[] forwardPredictions = new float[size];
            float[] backwardPredictions = new float[size];
            float[] inputaudio = new float[size];
            for (int i = 0; i < size; i++)
                inputaudio[i] = audioData.GetInputSample(i);

            // we will use steps to report progress
            int step = audioData.Length_samples / 100;
            for (int index = history_length_samples; index <= audioData.Length_samples; index += step)
            {
                progress.Report(100 * index / audioData.Length_samples);
                int end_position = index + step;
                if (end_position > audioData.Length_samples)
                {
                    end_position = audioData.Length_samples;
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

        internal float Calc_detectoin_level(AudioDataClass audioData, int position)
        {
            float threshold_level_detected = 0;
            float a = //Math.Abs(audioData.Get_a_average(i));
                (Math.Abs(Calc_burg_pred_fromInput(audioData, position))); /* +
                Math.Abs(Calc_burg_pred_fromInput(audioData, position + 1, 256, 4)) +
                Math.Abs(Calc_burg_pred_fromInput(audioData, position + 2, 256, 4))) / 3;*/

            // calculate average error value on the LEFT of the current sample
            float a_av = audioData.Get_a_average(position - 15);
            //Calc_a_average_for_One_Sample(audioData, i - 5);

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
            AudioDataClass audioData, 
            IProgress<double> progress,
            IProgress<string> status)
        {
            audioData.CurrentChannel = Channel.Left;
            await Task.Run(() => ProcessChannelAsync(
                audioData,
                progress,
                status
                ));
            
            if (audioData.IsStereo)
            {
                status.Report("Right channel: preprocessing");
                audioData.CurrentChannel = Channel.Right;
                await Task.Run(() => ProcessChannelAsync(
                    audioData, 
                    progress,
                    status
                    ));
            }
        }

        private async Task ProcessChannelAsync(
            AudioDataClass audioData, 
            IProgress<double> progress,
            IProgress<string> status)
        {
            if (audioData.IsStereo && audioData.CurrentChannel == Channel.Left)
            {
                status.Report("Left channel: preprocessing");
            }
            if (audioData.IsStereo && audioData.CurrentChannel == Channel.Right)
            {
                status.Report("Right channel: preprocessing");
            }
            if (!audioData.IsStereo)
            {
                status.Report("Mono: preprocessing");
            }

            CalculateBurgPredictionErrCPU(audioData, progress);

            for (int i = 0; i < history_length_samples + 16; i++)
                audioData.Set_a_average(i, 0.001F);

            Calculate_a_average_CPU(
                audioData, 
                history_length_samples, 
                audioData.Length_samples, 
                history_length_samples);

            status.Report("");

            // copies input samples to output before scanning 
            for (int i = 0; i < audioData.Length_samples; i++)
            {
                audioData.SetOutputSample(i, audioData.GetInputSample(i));
            }

            if (audioData.IsStereo && audioData.CurrentChannel == Channel.Left)
            {
                status.Report("Left channel: scanning");
            }
            if (audioData.IsStereo && audioData.CurrentChannel == Channel.Right)
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

        public void Calculate_a_average_CPU(AudioDataClass audioData, int start_position, int end_position, int _base)
        {
            float a_av = 
                Calc_a_average_for_One_Sample(audioData, start_position);

            audioData.Set_a_average(start_position, a_av);
            
            for (int i = start_position; i < end_position; i += 16)
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
             
                

                //a_av = a_av / (Base / 16) + 0.0000001F; // to find average
                if (a_av < 0.001) a_av = 0.001F;      // minimum result to return is 0.001

                for (int l = i; l < i + 16; l++)
                    audioData.Set_a_average(l,
                        a_av);
                        //Calc_a_average_for_One_Sample(audioData, l));
            }
        }


        /// <summary>
        /// Divides audio for segments and call ScanSegment for each og them
        /// </summary>
        /// <param name="audioData"></param>
        private async Task ScanAudioAsync(
            AudioDataClass audioData,
            IProgress<double> progress
            )
        {
            int cpu_core_number = Environment.ProcessorCount;

            int segment_lenght = (int)(
                (audioData.Length_samples - 8 - history_length_samples) / cpu_core_number
                );
            // make segments overlap
            segment_lenght += 1;

            Task[] tasks = new Task[cpu_core_number];
            for (int cpu_core = 0; cpu_core < cpu_core_number; cpu_core++)
            {
                int segment_start = cpu_core * segment_lenght + history_length_samples;
                int segment_end = segment_start + segment_lenght;
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
            AudioDataClass audioData,
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
                if (cpu_core == 0)
                {
                    // to not report too many times
                    if ( i % 1000 == 0)
                        if (progress != null)
                            progress?.Report(100 * i / segment_end);
                }

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
                    Repair(audioData, i_ref, length); // (i - 2, length + 4, audioData.CurrentChannel);
                    audioData.AddClick(i_ref, length, max_length /*threshold_level_detected*/, this); //i - 2, length + 4, threshold_level_detected);

                    last_processed_sample = i_ref + length + 1;
                }

                if (length == 0)
                {
                    //audioData.AddClick(i, 1, max_length, this);

                    RestoreInitState(audioData, i_ref, max_length);

                    //last_processed_sample = i_ref + max_length;
                }
            }
        }

        private int Find_sequence(
            AudioDataClass audioData, 
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
                // don't go before last processed sample
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

                RestoreInitState(audioData, i + i_correction - 1, max_length - i_correction + 5);
            }

            if (success_current)
            {
                i = i_current;
                return length_current;
            }
            else
                return 0;
        }

        private void TryToFix(AudioDataClass audioData, int i, int max_length, int i_correction, out int length, out bool success, out float errSum)
        {
            length = - i_correction;
            success = false;
            float end_diff = 10000;
            Repair(audioData, i + i_correction, length + 4);
            //length += 4;
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

                    if (end_diff < 0.02F) //0.005F)
                    {
                        success = true;
                        break;
                    }
                }
            }

            errSum = end_diff; //0;
            //for (int index = i + i_correction + length + 1; index <= i + i_correction + length + 3; index++)
              //  errSum += Math.Abs(audioData.GetPredictionErr(index));
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
            AudioDataClass audioData, 
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
            AudioDataClass audioData,
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

        private int Get_max_length(AudioDataClass audioData, int i)
        {
            int lenght = 0;
            float a = (Math.Abs(audioData.GetPredictionErr(i))); // +
                //Math.Abs(audioData.GetPredictionErr(i + 1))) / 2;
            float a_av = audioData.Get_a_average(i - 15); //Calc_a_average_for_One_Sample(audioData, i - 5);
            float rate = a / (threshold_for_detection * a_av);
            while (a > /* * 2 > (threshold_for_detection * */ a_av)
            {
                lenght = lenght + 3;
                a = (Math.Abs(audioData.GetPredictionErr(i + lenght)) +
                    Math.Abs(audioData.GetPredictionErr(i + 1 + lenght)) +
                    Math.Abs(audioData.GetPredictionErr(i + 2 + lenght))) / 3;
            }
            int max_length = (int) (lenght * rate * 2); // the result is multiplication lenght and rate (doubled)

            if (max_length > 10)
                max_length = 10;

            return max_length;
        }

        private bool Is_sample_suspicuous(
            AudioDataClass audioData, 
            int i,  
            out float threshold_level_detected)
        {
            // average of three current errors (current and two next) makes 
            // increase in errors values more distinctive
            float a = //Math.Abs(Calc_burg_pred_fromInput(audioData, i, 16, 2)); //Math.Abs(audioData.Get_a_average(i));
                (Math.Abs(audioData.GetPredictionErr(i))); //  + 
                //Math.Abs(audioData.GetPredictionErr(i + 1)) + 
                //Math.Abs(audioData.GetPredictionErr(i + 2))) / 3;*/

            // calculate average error value on the LEFT of the current sample
            float a_av = audioData.Get_a_average(i - 15);
            //Calc_a_average_for_One_Sample(audioData, i - 5);

            threshold_level_detected = a / a_av;

            if (threshold_level_detected > threshold_for_detection)                             // if threshold exceded
                if (true) //Is_sample_suspicuous_right(audioData, i, a, a_av))             // and if the sample suspicious toward samples on the RIGHT 
                    return true;                                                // return true
                else
                    return false;                                               // else return false
            else
                return false;
        }

        private bool Is_sample_suspicuous_right(AudioDataClass audioData, int i, float a, float a_av)
        {
            float a_av_r = 1;                                                     // average error value on the RIGHT of the current sample (1 to escape 0/x)
            int a_av_r_count_more = 0;                                          // number of maximums significantly higher tnan "a"
            int a_av_r_count_less = 0;                                          // number of maximums significantly lower tnan "a"
            int Base = 512;                                                     // number of samples to analyze on the right side

            // If a peak is very big, it's suspicious without additional conditions 
            if (a > 100 * a_av) //100
                return true;
            // Check if it's not a change in melody:
            // calculate "a_av_r", "a_av_r_count_more" and "a_av_r_count_less"
            for (int m = 15; m < Base - 16; m += 16)
            {   // divide the 512-16 B.p.e. samples on 15 grops
                float aa = 0, temp = 0;
                for (int l = 0; l < 16; l++) // find a maximum inside each of the groups (aa)
                    temp = (Math.Abs(audioData.GetPredictionErr(i + 100 + m + l + 1))); /* +
                        Math.Abs(audioData.GetPredictionErr(i + 100 + m + l + 2)) +
                        Math.Abs(audioData.GetPredictionErr(i + 100 + m + l + 3))) / 3;*/
                if (aa < temp)
                    aa = temp;
                if (aa > 0.7 * a) a_av_r_count_more++;   //counts number of maximums higher tnan 70% of a
                if (aa < 0.2 * a) a_av_r_count_less++;   //counts number of maximums less tnan 10% of a

                if (aa > 7 * a) aa = 0;                 // excluding potentialy broken samples

                a_av_r = a_av_r + aa;
            }
            a_av_r = a_av_r / (Base / 16) + 0.00001F;   // counts average of peaks of the next 512 samles (aa)
                                                 // if a peak is not so big but the next 512 B.p.e. samples don't contain significant peaks, it's suspicious
            if ((a > (1 * a_av_r))) // && (a_av_r_count_more == 0)) //5 3.5
                return true;
            //else
            // Else if a peak is small but the next 512 input samples are mostly predictable even if they contain big peaks, it's suspicious
            //if ((a > 2 * a_av_r) && (a_av_r_count_more > 0) && (a_av_r_count_less > 17 )) //2.5 0 5
            //return true;

            return false;
        }

        internal float Calc_a_average_for_One_Sample(AudioDataClass audioData, int i)
        {
            int Base = history_length_samples;
            float a_av = 0;
            
            for (int m = 0; m < Base - 16; m += 16)
            {                           
                // check each period of 16 errors to find maximums
                float aa = 0, temp = 0;
                for (int l = 0; l < 16; l++)
                {
                    temp = Math.Abs(audioData.GetPredictionErr(i - Base + m + l));
                        //(Math.Abs(audioData.GetPredictionErr(i - Base + m + l)) + 
                        //Math.Abs(audioData.GetPredictionErr(i - Base + m + l + 1)) + 
                        //Math.Abs(audioData.GetPredictionErr(i - Base + m + l + 2))) / 3;
                    if (aa < temp) aa = temp;
                }
                a_av = a_av + aa;  // sum up maximums
            }
            
            // Parallel For works slower than
            /*float[] a_av_parallel = new float[Base / 16];
            Parallel.For(0, Base / 16, index =>
            {
                int m = index * 16;
                float aa = 0, temp = 0;
                for (int l = 0; l < 16; l++)
                {
                    temp = (Math.Abs(audioData.GetPredictionErr(i - Base + m + l)) +
                        Math.Abs(audioData.GetPredictionErr(i - Base + m + l + 1)) +
                        Math.Abs(audioData.GetPredictionErr(i - Base + m + l + 2))) / 3;
                    if (aa < temp) aa = temp;
                }
                a_av_parallel[index] = aa;
            });
            foreach (float a in a_av_parallel)
                a_av += a;   */
                
            a_av = a_av / (Base / 16) + 0.0000001F; // to find average
            if (a_av < 0.001) a_av = 0.001F;      // minimum result to return is 0.001
            return a_av;
        }

        /// <summary>
        /// Replace output sample at position with prediction and 
        /// sets prediction error sample to zero
        /// </summary>
        /// <param name="position"></param>
        /// <param name="lenght"></param>
        internal float Repair(AudioDataClass audioData, int position, int lenght)
        {
            for (int i = position; i < position + lenght; i++)
            {
                audioData.SetPredictionErr(i, 0);
                audioData.SetOutputSample(
                    i,
                    Calc_burg_pred(audioData, i)
                    );
            }

            for //(int i = position - 16; i < position + lenght /*+ 512*/; i++)
                (int i = position + lenght; i < position + lenght + 16; i++)
            {
                audioData.SetPredictionErr(
                    i,
                    Calc_burg_pred(audioData, i) -
                    //audioData.GetOutputSample(i)  
                    audioData.GetInputSample(i)
                    );
            }

            Calculate_a_average_CPU(
                audioData, 
                position - history_length_samples, 
                position + lenght + history_length_samples, 
                history_length_samples);

            return Calc_detectoin_level(audioData, position);
        }

        public void RestoreInitState(AudioDataClass audioData, int position, int lenght)
        {
            for (int i = position; i <= position + lenght; i++)
            {
                audioData.SetOutputSample(
                    i,
                    audioData.GetInputSample(i));
            }

            for (int i = position - 16; i < position + lenght /*+ 512*/; i++)
            {
                audioData.SetPredictionErr(
                    i,
                    Calc_burg_pred(audioData, i) -
                    audioData.GetInputSample(i)
                    );
            }

            Calculate_a_average_CPU(
                audioData, 
                position - history_length_samples, 
                position + lenght + history_length_samples, 
                history_length_samples);
        }
    }
        
}