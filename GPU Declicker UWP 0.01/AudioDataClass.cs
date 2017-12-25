using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    public enum Channel { Left, Right};

    /// <summary>
    /// Represents stereo or mono audio samples and includes information 
    /// about damaged samples
    /// </summary>
    public class AudioDataClass
    {
        public Boolean IsStereo = true;
        public Channel CurrentChannel { get; set; }
        private float[] LeftChannelAudioInput = null;
        private float[] RightChannelAudioInput = null;
        private float[] LeftChannelAudioOutput = null;
        private float[] RightChannelAudioOutput = null;
        private float[] LeftChannel_a_average = null;
        private float[] RightChannel_a_average = null;
        public int Length_samples { get; set; }
        //private byte[] LeftChannelMarks = null;
        //private byte[] RightChannelMarks = null;
        private float[] LeftChannelPredictionErr = null;
        private float[] RightChannelPredictionErr = null;

        private List<AudioClick> Clicks_LeftChannel = new List<AudioClick>();
        private List<AudioClick> Clicks_RightChannel = new List<AudioClick>();

        // Constractor for stereo
        public AudioDataClass(float[] leftChannelSamples, float[] rightChannelSamples)
        {
            IsStereo = true;
            CurrentChannel = Channel.Left;
            Length_samples = leftChannelSamples.Length;
            LeftChannelAudioInput = leftChannelSamples;
            RightChannelAudioInput = rightChannelSamples;
            LeftChannelAudioOutput = new float[Length_samples];
            RightChannelAudioOutput = new float[Length_samples];
            //LeftChannelMarks = new byte[Length];
            //RightChannelMarks = new byte[Length];
            LeftChannelPredictionErr = new float[Length_samples];
            RightChannelPredictionErr = new float[Length_samples];
            LeftChannel_a_average = new float[Length_samples];
            RightChannel_a_average = new float[Length_samples];
        }

        // Constructor for mono
        public AudioDataClass(float[] monoChannel)
        {
            IsStereo = false;
            Length_samples = monoChannel.Length;
            LeftChannelAudioInput = monoChannel;
            LeftChannelAudioOutput = new float[Length_samples];
            //LeftChannelMarks = new byte[Length];
            LeftChannelPredictionErr = new float[Length_samples];
            LeftChannel_a_average = new float[Length_samples];
        }

        /// <summary>
        /// Replace output sample at position with prediction and 
        /// sets prediction error sample to zero
        /// </summary>
        /// <param name="position"></param>
        /// <param name="lenght"></param>
        internal float Repair(int position, int lenght, Channel channel)
        {
            CurrentChannel = channel;

            for (int i = position; i < position + lenght; i++)
            {
                SetPredictionErr( i, 0);
                SetOutputSample( 
                    i,
                    AudioProcessing.Calc_burg_pred(this, i, 128, 4)
                    );
            }

            for (int i = position - 16; i < position + lenght + 16; i++)
                //(int i = position + lenght; i < position + lenght + 16; i++)
            {
                SetPredictionErr(
                    i,
                    AudioProcessing.Calc_burg_pred(this, i, 16, 2) -
                    GetOutputSample(i)  //GetInputSample(i)
                    );
            }

            AudioProcessing.Calculate_a_average_CPU(this, position - 512, position + lenght + 512);

            return AudioProcessing.Calc_detectoin_level(this, position);
        }

        public void RestoreInitState(int position, int lenght, Channel channel)
        {
            CurrentChannel = channel;

            for (int i = position; i < position + lenght; i++)
            {
                SetOutputSample(i, GetInputSample(i));
            }

            for (int i = position - 16; i < position + lenght + 16; i++)
            {
                SetPredictionErr(
                    i,
                    AudioProcessing.Calc_burg_pred(this, i, 256, 4) -
                    GetInputSample(i)
                    );
            }

            AudioProcessing.Calculate_a_average_CPU(this, position - 512, position + lenght + 512);
        }

        /// <summary>
        /// Adds a click to the list and stores threshold_level_detected
        /// </summary>
        /// <param name="position"></param>
        /// <param name="lenght"></param>
        /// <param name="threshold_level_detected"></param>
        public void AddClick(int position, int lenght, float threshold_level_detected)
        {
            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    Clicks_LeftChannel.Add(new AudioClick(position, lenght, threshold_level_detected, this, Channel.Left));
                else
                    Clicks_RightChannel.Add(new AudioClick(position, lenght, threshold_level_detected, this, Channel.Right));
            else
                Clicks_LeftChannel.Add(new AudioClick(position, lenght, threshold_level_detected, this, Channel.Left));
        }

        public int GetNumberOfClicks()
        {
            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    return Clicks_LeftChannel.Count;
                else
                    return Clicks_RightChannel.Count;
            else
                return Clicks_LeftChannel.Count;
        }

        public void ChangeClickAproved(int index)
        {
            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    Clicks_LeftChannel[index].ChangeAproved();
                else
                    Clicks_RightChannel[index].ChangeAproved();
            else
                Clicks_LeftChannel[index].ChangeAproved();
        }

        public AudioClick GetClick(int index)
        {
            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    return Clicks_LeftChannel[index];
                else
                    return Clicks_RightChannel[index];
            else
                return Clicks_LeftChannel[index];
        }

        public float GetInputSample(int position)
        {
            if (position >= Length_samples || position < 0 )
                return 0;

            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    return LeftChannelAudioInput[position];
                else
                    return RightChannelAudioInput[position];
            else
                return LeftChannelAudioInput[position]; // mono
        }

        public void SetInputSample(int position, float sample)
        {
            if (position >= Length_samples || position < 0)
                return;

            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    LeftChannelAudioInput[position] = sample;
                else
                    RightChannelAudioInput[position] = sample;
            else
                LeftChannelAudioInput[position] = sample; // mono

            return;
        }

        public float GetOutputSample(int position)
        {
            if (position >= Length_samples || position < 0)
                return 0;

            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    return LeftChannelAudioOutput[position];
                else
                    return RightChannelAudioOutput[position];
            else
                return LeftChannelAudioOutput[position]; // mono
        }

        public void SetOutputSample(int position, float sample)
        {
            if (position >= Length_samples || position < 0)
                return;

            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    LeftChannelAudioOutput[position] = sample;
                else
                    RightChannelAudioOutput[position] = sample;
            else
                LeftChannelAudioOutput[position] = sample; // mono

            return;
        }

        /*public byte GetMark(int position)
        {
            if (position >= Length || position < 0)
                return 0;

            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    return LeftChannelMarks[position];
                else
                    return RightChannelMarks[position];
            else
                return LeftChannelMarks[position]; // mono
        }

        public void SetMark(int position, byte mark)
        {
            if (position >= Length || position < 0)
                return;

            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    LeftChannelMarks[position] = mark;
                else
                    RightChannelMarks[position] = mark;
            else
                LeftChannelMarks[position] = mark; // mono

            return;
        }*/

        public float GetPredictionErr(int position)
        {
            if (position >= Length_samples || position < 0)
                return 0;

            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    return LeftChannelPredictionErr[position];
                else
                    return RightChannelPredictionErr[position];
            else
                return LeftChannelPredictionErr[position]; // mono
        }

        public void SetPredictionErr(int position, float prediction)
        {
            if (position >= Length_samples || position < 0)
                return;

            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    LeftChannelPredictionErr[position] = prediction;
                else
                    RightChannelPredictionErr[position] = prediction;
            else
                LeftChannelPredictionErr[position] = prediction; // mono

            return;
        }

        public float Get_a_average(int position)
        {
            if (position >= Length_samples || position < 0)
                return 0;

            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    return LeftChannel_a_average[position];
                else
                    return RightChannel_a_average[position];
            else
                return LeftChannel_a_average[position]; // mono
        }

        public void Set_a_average(int position, float a_average)
        {
            if (position >= Length_samples || position < 0)
                return;

            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    LeftChannel_a_average[position] = a_average;
                else
                    RightChannel_a_average[position] = a_average;
            else
                LeftChannel_a_average[position] = a_average; // mono

            return;
        }

        internal void SortClicks()
        {
            Clicks_LeftChannel.Sort();
            
            if (IsStereo)
                Clicks_RightChannel.Sort();
        }
    }
}
