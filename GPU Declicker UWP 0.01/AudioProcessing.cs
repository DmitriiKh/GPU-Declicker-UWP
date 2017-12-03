using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace GPU_Declicker_UWP_0._01
{
    class AudioProcessing
    {
        private static object lockObject = new object();

        /// <summary>
        /// Calculates one prediction error value for one sample using CPU
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="i">indicates number of element in arrays</param>
        /// <param name="history">number of samples which are used for finding 
        /// prediction errors by Burg's method</param>
        /// <param name="coeffs">number of coefficients in Burg's method </param>
        private static void CalculateBurgPredictionThread(
            float[] inputaudio, float[] forwardPredictions,
            float[] backwardPredictions, int i, int history, int coeffs)
        {
            double[] b = new double[history];
            double[] f = new double[history];
            double[] a = new double[(coeffs + 1)];
            double ACCUM;
            double D = 0.0, mu = 0.0;
            
            for (int I = 0; I < history; I++)
            {
                b[I] = inputaudio[I + i - history];
                f[I] = b[I];
            }

            int N = history - 1;

            for (int I = 1; I <= coeffs; I++)
                a[I] = 0.0;
            a[0] = 1.0;

            D = 0.0;
            for (int I = 0; I < history; I++)
                D += 2.0 * f[I] * f[I];
            D -= f[0] * f[0] + b[N] * b[N];

            for (int k = 0; k < coeffs; k++)
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
            for (int I = 1; I <= coeffs; I++)
                ACCUM += inputaudio[i - I] * (-1) * a[I];

            forwardPredictions[i] = (float)ACCUM;

            ACCUM = 0.0;
            for (int I = 1; I <= coeffs; I++)
                ACCUM += inputaudio[i - history + I] * (-1) * a[I];

            backwardPredictions[i - history] = (float)ACCUM;

            return; 
        }

        /// <summary>
        /// Calculates prediction errors for a channel using CPU (Parallel.For)
        /// </summary>
        public static void CalculateBurgPredictionErrCPU(
            AudioDataClass audioData, 
            int history, 
            int coeffs,
            IProgress<double> progress)
        {
            int size = audioData.Length;
            float[] forwardPredictions = new float[size];
            float[] backwardPredictions = new float[size];
            float[] inputaudio = new float[size];
            for (int i = 0; i < size; i++)
                inputaudio[i] = audioData.GetInputSample(i);

            // we will use steps to report progress
            int step = audioData.Length / 100;
            for (int index = history; index <= audioData.Length; index += step)
            {
                progress.Report(100 * index / audioData.Length);
                int end_position = index + step;
                if (end_position > audioData.Length)
                {
                    end_position = audioData.Length;
                }
                Parallel.For(index, end_position,
                    i =>
                        CalculateBurgPredictionThread(
                            inputaudio,
                            forwardPredictions,
                            backwardPredictions,
                            i,
                            history,
                            coeffs
                            )
                    );
            }
            progress.Report(0);
            // for first samples forward prediction was not calculated
            for (int i = 0; i < history; i++)
            {
                audioData.SetPredictionErr(i,
                        audioData.GetInputSample(i) - backwardPredictions[i]);
            }
            // finds prediction error based on forward and backward predictions
            for (int i = history; i < size - history; i++)
            {
                audioData.SetPredictionErr(i,
                        audioData.GetInputSample(i) - 
                        (forwardPredictions[i] + backwardPredictions[i]) / 2);
            }
            // for last samples backward prediction was not calculated
            for (int i = history; i < size - history; i++)
            {
                audioData.SetPredictionErr(i,
                        audioData.GetInputSample(i) - forwardPredictions[i]);
            }
        }

        /// <summary>
        /// Processes audio
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="history"></param>
        /// <param name="coeffs"></param>
        public static async Task ProcessAudioAsync(
            AudioDataClass audioData, 
            int history, 
            int coeffs, 
            float threshold,
            IProgress<double> progress,
            IProgress<string> status)
        {
            audioData.CurrentChannel = Channel.Left;
            await Task.Run(() => ProcessChannelAsync(
                audioData, 
                history, 
                coeffs, 
                threshold, 
                progress,
                status
                ));
            
            if (audioData.IsStereo)
            {
                status.Report("Right channel: preprocessing");
                audioData.CurrentChannel = Channel.Right;
                await Task.Run(() => ProcessChannelAsync(
                    audioData, 
                    history, 
                    coeffs, 
                    threshold, 
                    progress,
                    status
                    ));
            }
        }

        private static async Task ProcessChannelAsync(
            AudioDataClass audioData, 
            int history, 
            int coeffs, 
            float threshold,
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

            CalculateBurgPredictionErrCPU(audioData, history, coeffs, progress);

            status.Report("");

            // copies input samples to output before scanning 
            for (int i = 0; i < audioData.Length; i++)
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
            await Task.Run(() => ScanAudioAsync(audioData, history, coeffs, threshold, progress));

            status.Report("");
        }


        /// <summary>
        /// Divides audio for segments and call ScanSegment for each og them
        /// </summary>
        /// <param name="audioData"></param>
        private static async Task ScanAudioAsync(
            AudioDataClass audioData, 
            int history, 
            int coeffs_for_detection, 
            float threshold,
            IProgress<double> progress
            )
        {
            int cpu_core_number = Environment.ProcessorCount;

            int segment_lenght = (int)(
                (audioData.Length - 8 - history) / cpu_core_number
                );
            // make segments overlap
            segment_lenght += 1;

            Task[] tasks = new Task[cpu_core_number];
            for (int cpu_core = 0; cpu_core < cpu_core_number; cpu_core++)
            {
                int segment_start = cpu_core * segment_lenght + history;
                int segment_end = segment_start + segment_lenght;
                int index = cpu_core;
                tasks[cpu_core] = Task.Factory.StartNew(() =>
                    ScanSegment(
                        audioData,
                        history,
                        coeffs_for_detection,
                        threshold,
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

        private static void ScanSegment(
            AudioDataClass audioData, 
            int history, 
            int coeffs_for_detection, 
            float threshold, 
            int segment_start, 
            int segment_end,
            IProgress<double> progress,
            int cpu_core
            )
        {
            float threshold_level_detected = 0;
            int max_length = 0;
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
                if ((audioData.GetMark(i) == 0) && 
                    Is_sample_suspicuous(
                        audioData, 
                        i, 
                        threshold, 
                        out threshold_level_detected)
                        )
                {
                    max_length = Get_max_length(audioData, i, threshold);
                    length = Find_sequence(
                        audioData, 
                        i, 
                        1, 
                        max_length, 
                        history, 
                        coeffs_for_detection, 
                        threshold
                        );
                }

                if (length > 0)
                {
                    audioData.AddClick(i - 2, length + 4, threshold_level_detected);

                    //audioData.Recalculate(i - 2, length + 4);

                    // if a sequence has been replaced
                    //for (int k = -2; k < length; k++) audioData.SetMark(i + k, 255);    // mark scan_data
                    // and calculate new prediction errors
                    for (int k = -2; (k < history) && (k < audioData.Length - i); k++)
                        audioData.SetPredictionErr(i + k, Calc_burg_pred(audioData, i + k, history,
                        coeffs_for_detection) - audioData.GetOutputSample(i + k));
                }
            }
        }

        private static int Find_sequence(
            AudioDataClass audioData, int i, int length, int max_length,
            int history, int coeffs_for_detection,
            float threshold)
        {
            int coeffs_for_restoration = coeffs_for_detection * 2;
            int max_length_restoration = 150;

            int l = length;                                                     // length in this iteration
            float old_sample = 0, old_sample1 = 0, old_sample2 = 0;               // variables to save sample values before changing in this iteration
            float old_pred = 0, old_pred1 = 0, old_pred2 = 0, old_pred3 = 0, old_pred4 = 0, old_pred5 = 0;    // the same for prediction errors
            if (length > max_length) return 0;                                  // the sequence can not be replaced (too long)
                                                                                /*
                                                                                // temp code
                                                                                extern int z;
                                                                                for (int j = 0; j < 100; j++)
                                                                                {
                                                                                data.scan_data[z + j] = data.prediction_errors_data[i - length + j];
                                                                                dataGetOutputSample(z + j] = dataGetOutputSample(i - length + j];
                                                                                }
                                                                                z = z + 100;
                                                                                for (int j = 0; j < 5; j++)
                                                                                {
                                                                                data.scan_data[z + j] = -10000;
                                                                                dataGetOutputSample(z + j] = -10000;
                                                                                }
                                                                                z = z + 5;
                                                                                // end of temp code
                                                                                */
            if (length == 1)
            {                                                   // replace two previous samples before starting new sequence
                old_sample2 = audioData.GetOutputSample(i - 2);
                audioData.SetOutputSample(i - 2, Calc_burg_pred(audioData, i - 2, 512, 4));
                old_sample1 = audioData.GetOutputSample(i - 1);
                audioData.SetOutputSample(i - 1, Calc_burg_pred(audioData, i - 1, history, coeffs_for_restoration));
            }
            // try to replace one sample to see conseqences
            old_sample = audioData.GetOutputSample(i);
            audioData.SetOutputSample(i, Calc_burg_pred(audioData, i, history, coeffs_for_restoration));
            // save old prediction errors and calculate new ones
            old_pred = audioData.GetPredictionErr(i);
            audioData.SetPredictionErr(i, Calc_burg_pred(audioData, i, history, coeffs_for_detection)
                - audioData.GetOutputSample(i));
            old_pred1 = audioData.GetPredictionErr(i + 1);
            audioData.SetPredictionErr(i + 1, Calc_burg_pred(audioData, i + 1, history, coeffs_for_detection)
                - audioData.GetOutputSample(i + 1));
            old_pred2 = audioData.GetPredictionErr(i + 2);
            audioData.SetPredictionErr(i + 2, Calc_burg_pred(audioData, i + 2, history, coeffs_for_detection)
                - audioData.GetOutputSample(i + 2));
            old_pred3 = audioData.GetPredictionErr(i + 3);
            audioData.SetPredictionErr(i + 3, Calc_burg_pred(audioData, i + 3, history, coeffs_for_detection)
                - audioData.GetOutputSample(i + 3));
            old_pred4 = audioData.GetPredictionErr(i + 4);
            audioData.SetPredictionErr(i + 4, Calc_burg_pred(audioData, i + 4, history, coeffs_for_detection)
                - audioData.GetOutputSample(i + 4));
            old_pred5 = audioData.GetPredictionErr(i + 5);
            audioData.SetPredictionErr(i + 5, Calc_burg_pred(audioData, i + 5, history, coeffs_for_detection)
                - audioData.GetOutputSample(i + 5));
            float threshold_level_detected;
            // if any of next two samples are suspicious go deeper (start next iteration)
            if (Is_sample_suspicuous(audioData, i + 1, threshold, out threshold_level_detected) ||
                Is_sample_suspicuous(audioData, i + 2, threshold, out threshold_level_detected))
                    l = Find_sequence(audioData, i + 1, length + 1, max_length, history, coeffs_for_detection, threshold);

            // debug code
            //if (i > 11826840 && i < 11826950) std::cout << i << " / " << l << " / " << length << " / " << max_length << std::endl;

            if ((l > max_length_restoration - 2) || (l > max_length) || (l == 0))
            {
                audioData.SetPredictionErr(i + 5, old_pred5);                 // restore previous state
                audioData.SetPredictionErr(i + 4, old_pred4);
                audioData.SetPredictionErr(i + 3, old_pred3);
                audioData.SetPredictionErr(i + 2, old_pred2);
                audioData.SetPredictionErr(i + 1, old_pred1);
                audioData.SetPredictionErr(i, old_pred);
                audioData.SetOutputSample(i, old_sample);
                if (length == 1)
                {
                    audioData.SetOutputSample(i - 1, old_sample1);
                    audioData.SetOutputSample(i - 2, old_sample2);
                }
                return 0;                                                       // attempt to find a sequence failed
            }
            return l;															// attempt is successful 
        }

        /// <summary>
        /// Returns prediction for a sample at i position
        /// </summary>
        /// <param name="audioData"></param>
        /// <param name="i"></param>
        /// <param name="history"></param>
        /// <param name="number_of_coeffs_for_restoration"></param>
        /// <returns></returns>
        public static float Calc_burg_pred(AudioDataClass audioData, int i, int history, int number_of_coeffs_for_restoration)
        {
            // use output audio as an input because it already contains
            // fixed samples before sample i
            float[] outputAudioShort = new float[history + 1];
            for (int k = 0; k < history + 1; k++)
            {
                outputAudioShort[k] = audioData.GetOutputSample(i - history + k);
            }

            // array for results
            float[] forwardPredictionsShort = new float[history + 1];

            // we need this array for calling CalculateBurgPredictionThread
            float[] backwardPredictionsShort = new float[history + 1];

            CalculateBurgPredictionThread(outputAudioShort, forwardPredictionsShort, backwardPredictionsShort, history, history, number_of_coeffs_for_restoration);

            // return prediction for i sample
            return forwardPredictionsShort[history];
        }

        private static int Get_max_length(AudioDataClass audioData, int i, float threshold)
        {
            int lenght = 0;
            float a = (Math.Abs(audioData.GetPredictionErr(i)) +
                Math.Abs(audioData.GetPredictionErr(i + 1))) / 2; 
            float a_av = Calc_a_average(audioData, i - 5);
            float rate = a / (threshold * a_av);
            while (a > (threshold * a_av))
            {
                lenght = lenght + 3;
                a = (Math.Abs(audioData.GetPredictionErr(i + lenght)) +
                    Math.Abs(audioData.GetPredictionErr(i + 1 + lenght)) +
                    Math.Abs(audioData.GetPredictionErr(i + 2 + lenght))) / 3;
            }
            int max_length = (int) (lenght * rate); // the result is multiplication lenght and rate (doubled)
            
            return max_length;
        }

        private static bool Is_sample_suspicuous(
            AudioDataClass audioData, 
            int i, 
            float threshold, 
            out float threshold_level_detected)
        {
            // average of three current errors (current and two next) makes 
            // increase in errors values more distinctive
            float a = (Math.Abs(audioData.GetPredictionErr(i)) + 
                Math.Abs(audioData.GetPredictionErr(i + 1)) + 
                Math.Abs(audioData.GetPredictionErr(i + 2))) / 3;

            // calculate average error value on the LEFT of the current sample
            float a_av = Calc_a_average(audioData, i - 5);

            threshold_level_detected = a / a_av;

            if (threshold_level_detected > threshold)                             // if threshold exceded
                if (Is_sample_suspicuous_right(audioData, i, a, a_av))             // and if the sample suspicious toward samples on the RIGHT 
                    return true;                                                // return true
                else
                    return false;                                               // else return false
            else
                return false;
        }

        private static bool Is_sample_suspicuous_right(AudioDataClass audioData, int i, float a, float a_av)
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
                    temp = (Math.Abs(audioData.GetPredictionErr(i + 100 + m + l + 1)) +
                        Math.Abs(audioData.GetPredictionErr(i + 100 + m + l + 2)) +
                        Math.Abs(audioData.GetPredictionErr(i + 100 + m + l + 3))) / 3;
                if (aa < temp)
                    aa = temp;
                if (aa > 0.7 * a) a_av_r_count_more++;   //counts number of maximums higher tnan 70% of a
                if (aa < 0.2 * a) a_av_r_count_less++;   //counts number of maximums less tnan 10% of a

                if (aa > 7 * a) aa = 0;                 // excluding potentialy broken samples

                a_av_r = a_av_r + aa;
            }
            a_av_r = a_av_r / (Base / 16) + 0.00001F;   // counts average of peaks of the next 512 samles (aa)
                                                 // if a peak is not so big but the next 512 B.p.e. samples don't contain significant peaks, it's suspicious
            if ((a > (1 * a_av_r)) /*&& (a_av_r_count_more == 0)*/) //5 3.5
                return true;
            //else
            // Else if a peak is small but the next 512 input samples are mostly predictable even if they contain big peaks, it's suspicious
            //if ((a > 2 * a_av_r) && (a_av_r_count_more > 0) && (a_av_r_count_less > 17 )) //2.5 0 5
            //return true;

            return false;
        }

        private static float Calc_a_average(AudioDataClass audioData, int i)
        {
            int Base = 512;
            float a_av = 0;
            
            for (int m = 0; m < Base - 16; m += 16)
            {                           
                // check each period of 16 errors to find maximums
                float aa = 0, temp = 0;
                for (int l = 0; l < 16; l++)
                {
                    temp = (Math.Abs(audioData.GetPredictionErr(i - Base + m + l)) + 
                        Math.Abs(audioData.GetPredictionErr(i - Base + m + l + 1)) + 
                        Math.Abs(audioData.GetPredictionErr(i - Base + m + l + 2))) / 3;
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
    }
        
}