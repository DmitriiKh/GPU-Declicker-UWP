using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
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
            ChannelType channel)
        {
            ClicksList.Add(new AudioClick(
                position,
                lenght,
                threshold_level_detected,
                audioData,
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
}
