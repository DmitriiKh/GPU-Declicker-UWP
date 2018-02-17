using System.Collections.Generic;
using System.Linq;

namespace GPU_Declicker_UWP_0._01
{
    public enum ChannelType { Left, Right };

    public class AudioChannel
    {
        private readonly float[] input;
        private readonly float[] output;
        private readonly float[] predictionErrAverage;
        private readonly float[] predictionErr;
        private readonly float[] predictionErrBackup;
        private readonly List<AudioClick> ClicksList = new List<AudioClick>();
        public bool ChannelIsPreprocessed { get; set; }

        public AudioChannel(float[] inputSamples)
        {
            ChannelIsPreprocessed = false;
            int length = inputSamples.Length;
            input = inputSamples;
            output = new float[length];
            predictionErr = new float[length];
            predictionErrBackup = new float[length];
            predictionErrAverage = new float[length];
        }

        public int LengthSamples() => 
            input.Length; 

        public float GetInputSample(int position) => input[position];
        public void SetInputSample(int position, float value) =>
            input[position] = value;

        public float GetOutputSample(int position) => output[position];
        public void SetOutputSample(int position, float value) =>
            output[position] = value;

        public float GetPredictionErr(int position) => predictionErr[position];
        public void SetPredictionErr(int position, float value) =>
            predictionErr[position] = value;

        public float GetPredictionErrBackup(int position) =>
            predictionErrBackup[position];
        public void SetPredictionErrBackup(int position, float value) =>
            predictionErrBackup[position] = value;

        public float GetPredictionErrAverage(int position) => predictionErrAverage[position];
        public void SetPredictionErrAverage(int position, float value) =>
            predictionErrAverage[position] = value;

        public void AddClickToList(
            int position,
            int lenght,
            float threshold_level_detected,
            AudioData audioData,
            AudioProcessing audioProcessing,
            ChannelType channel)
        {
            ClicksList.Add(new AudioClick(
                position,
                lenght,
                threshold_level_detected,
                audioData,
                audioProcessing,
                channel));
        }

        public int GetNumberOfClicks() =>
            ClicksList.Count;

        internal void RestoreInitState(int position, int lenght)
        {
            for (int index = position; index < position + lenght; index++)
            {
                output[index] = input[index];
                predictionErr[index] = predictionErrBackup[index];
            }
        }

        public void ChangeClickAproved(int index) =>
            ClicksList[index].ChangeAproved();

        public AudioClick GetClick(int index) =>
            ClicksList[index];

        public AudioClick GetLastClick()
        {
            if (ClicksList.Count > 0)
            {
                return ClicksList.Last();
            }
            else
            {
                return null;
            }
        }

        internal void ClearAllClicks() =>
            ClicksList.Clear();

        internal void SortClicks() =>
            ClicksList.Sort();
    }

    public abstract class AudioData
    {

        internal bool IsStereo;
        internal AudioChannel currentAudioChannel;

        public abstract ChannelType GetCurrentChannelType();
        public abstract void SetCurrentChannelType(ChannelType channelType);
        public abstract void ClearAllClicks();
        public abstract void SortClicks();

        public int LengthSamples() =>
            currentAudioChannel.LengthSamples();

        /// <summary>
        /// Restores initial prediction errors from buckup and 
        /// output samples from input samples 
        /// </summary>
        /// <param name="position">start position</param>
        /// <param name="lenght">length</param>
        public void CurrentChannelRestoreInitState(int position, int lenght) =>
            currentAudioChannel.RestoreInitState(position, lenght);

        public float GetPredictionErrBackup(int position) =>
            currentAudioChannel.GetPredictionErrBackup(position);

        public void SetCurrentChannelIsPreprocessed() =>
            currentAudioChannel.ChannelIsPreprocessed = true;

        public bool CurrentChannelIsPreprocessed() =>
            currentAudioChannel.ChannelIsPreprocessed;

        /// <summary>
        /// Backup prediction errors data for current channel
        /// </summary>
        public void BackupCurrentChannelPredErrors()
        {
            for (int index = 0;
                index < currentAudioChannel.LengthSamples();
                index++)
            {
                currentAudioChannel.SetPredictionErrBackup(
                    index,
                    currentAudioChannel.GetPredictionErr(index));
            }
        }

        /// <summary>
        /// Restore prediction errors data for current channel
        /// </summary>
        public void RestoreCurrentChannelPredErrors()
        {
            for (int index = 0;
                index < currentAudioChannel.LengthSamples();
                index++)
            {
                currentAudioChannel.SetPredictionErr(
                    index,
                    currentAudioChannel.GetPredictionErrBackup(index));
            }
        }

        public void AddClickToList(
            int position,
            int lenght,
            float threshold_level_detected,
            AudioProcessing audioProcessing) =>

                currentAudioChannel.AddClickToList(
                    position, lenght,
                    threshold_level_detected,
                    this,
                    audioProcessing,
                    GetCurrentChannelType());

        public int CurrentChannelGetNumberOfClicks() =>
            currentAudioChannel.GetNumberOfClicks();

        public void ChangeClickAproved(int index) =>
            currentAudioChannel.ChangeClickAproved(index);

        public AudioClick GetClick(int index) =>
            currentAudioChannel.GetClick(index);

        public AudioClick GetLastClick() =>
            currentAudioChannel.GetLastClick();

        public float GetInputSample(int position) =>
            currentAudioChannel.GetInputSample(position);

        public void SetInputSample(int position, float sample) =>
            currentAudioChannel.SetInputSample(position, sample);

        public float GetOutputSample(int position) =>
            currentAudioChannel.GetOutputSample(position);

        public void SetOutputSample(int position, float sample) =>
            currentAudioChannel.SetOutputSample(position, sample);

        public float GetPredictionErr(int position) =>
            currentAudioChannel.GetPredictionErr(position);

        public void SetPredictionErr(int position, float prediction) =>
            currentAudioChannel.SetPredictionErr(position, prediction);

        public float Get_a_average(int position) =>
            currentAudioChannel.GetPredictionErrAverage(position);

        public void Set_a_average(int position, float a_average) =>
            currentAudioChannel.SetPredictionErrAverage(position, a_average);
    }

    /// <summary>
    /// Represents stereo audio samples and includes information 
    /// about damaged samples
    /// </summary>
    public class AudioDataStereo : AudioData
    {
        private ChannelType currentChannelType;
        private readonly AudioChannel leftChannel;
        private readonly AudioChannel rightChannel;

        public AudioDataStereo(float[] leftChannelSamples, float[] rightChannelSamples)
        {
            IsStereo = true;
            currentChannelType = ChannelType.Left;
            leftChannel = new AudioChannel(leftChannelSamples);
            rightChannel = new AudioChannel(rightChannelSamples);
            currentAudioChannel = leftChannel;
        }

        public override ChannelType GetCurrentChannelType() =>
            currentChannelType;

        public override void SetCurrentChannelType(ChannelType channelType)
        {
            currentChannelType = channelType;
            if (channelType == ChannelType.Left)
                currentAudioChannel = leftChannel;
            else
                currentAudioChannel = rightChannel;
        }

        public override void ClearAllClicks()
        {
            leftChannel.ClearAllClicks();
            rightChannel.ClearAllClicks();
        }

        public override void SortClicks()
        {
            leftChannel.SortClicks();
            rightChannel.SortClicks();
        }
    }

    /// <summary>
    /// Represents mono audio samples and includes information 
    /// about damaged samples
    /// </summary>
    public class AudioDataMono : AudioData
    {
        private readonly AudioChannel monoChannel;

        public AudioDataMono(float[] leftChannelSamples)
        {
            IsStereo = false;
            monoChannel = new AudioChannel(leftChannelSamples);
            currentAudioChannel = monoChannel;
        }

        public override ChannelType GetCurrentChannelType() =>
            // always answer Left for mono
            ChannelType.Left;

        public override void SetCurrentChannelType(ChannelType channelType)
        {
            // doing nothing because it's mono
        }
            
        public override void ClearAllClicks() =>
            monoChannel.ClearAllClicks();

        public override void SortClicks() =>
            monoChannel.SortClicks();
    }
}