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
        public int Length { get; set; }
        private byte[] LeftChannelMarks = null;
        private byte[] RightChannelMarks = null;
        private float[] LeftChannelPredictionErr = null;
        private float[] RightChannelPredictionErr = null;

        private List<AudioClick> Clicks_LeftChannel = new List<AudioClick>();
        private List<AudioClick> Clicks_RightChannel = new List<AudioClick>();

        public AudioDataClass(float[] leftChannel, float[] rightChannel)
        {
            IsStereo = true;
            CurrentChannel = Channel.Left;
            Length = leftChannel.Length;
            LeftChannelAudioInput = leftChannel;
            RightChannelAudioInput = rightChannel;
            LeftChannelAudioOutput = new float[Length];
            RightChannelAudioOutput = new float[Length];
            LeftChannelMarks = new byte[Length];
            RightChannelMarks = new byte[Length];
            LeftChannelPredictionErr = new float[Length];
            RightChannelPredictionErr = new float[Length];
        }

        internal void Recalculate(int pos, int len)
        {
            float prediction;

            for (int i = pos; i < pos + len; i++)
            {
                prediction = AudioProcessing.Calc_burg_pred(this, i, 512, 16);

                SetPredictionErr( i, 0);
                SetOutputSample( i, prediction);
            }
        }

        public AudioDataClass(float[] monoChannel)
        {
            IsStereo = false;
            Length = monoChannel.Length;
            LeftChannelAudioInput = monoChannel;
            LeftChannelAudioOutput = new float[Length];
            LeftChannelMarks = new byte[Length];
            LeftChannelPredictionErr = new float[Length];
        }

        public void AddClick(int pos, int len, float threshold_level_detected)
        {
            if (IsStereo)
                if (CurrentChannel == Channel.Left)
                    Clicks_LeftChannel.Add(new AudioClick(pos, len, threshold_level_detected, this));
                else
                    Clicks_RightChannel.Add(new AudioClick(pos, len, threshold_level_detected, this));
            else
                Clicks_LeftChannel.Add(new AudioClick(pos, len, threshold_level_detected, this));
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
            if (position >= Length || position < 0 )
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
            if (position >= Length || position < 0)
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
            if (position >= Length || position < 0)
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
            if (position >= Length || position < 0)
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

        public byte GetMark(int position)
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
        }

        public float GetPredictionErr(int position)
        {
            if (position >= Length || position < 0)
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
            if (position >= Length || position < 0)
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

        internal void SortClicks()
        {
            Clicks_LeftChannel.Sort();
            
            if (IsStereo)
            {
                Clicks_RightChannel.Sort();
            }
        }
    }
}
