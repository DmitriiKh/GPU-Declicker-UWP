﻿using System.Collections.Generic;
using System.Linq;

namespace GPU_Declicker_UWP_0._01
{
    public class AudioChannel
    {
        private readonly List<AudioClick> _clicksList = new List<AudioClick>();
        private readonly float[] _input;
        private readonly float[] _output;
        private readonly float[] _predictionErr;
        private readonly float[] _predictionErrAverage;
        private readonly float[] _predictionErrBackup;

        public AudioChannel(float[] inputSamples)
        {
            ChannelIsPreprocessed = false;
            var length = inputSamples.Length;
            _input = inputSamples;
            _output = new float[length];
            _predictionErr = new float[length];
            _predictionErrBackup = new float[length];
            _predictionErrAverage = new float[length];
        }

        public bool ChannelIsPreprocessed { get; set; }

        public int LengthSamples()
        {
            return _input.Length;
        }

        public float GetInputSample(int position)
        {
            return _input[position];
        }

        public void SetInputSample(int position, float value)
        {
            _input[position] = value;
        }

        public float GetOutputSample(int position)
        {
            return _output[position];
        }

        public void SetOutputSample(int position, float value)
        {
            _output[position] = value;
        }

        public float GetPredictionErr(int position)
        {
            return _predictionErr[position];
        }

        public void SetPredictionErr(int position, float value)
        {
            _predictionErr[position] = value;
        }

        public float GetPredictionErrBackup(int position)
        {
            return _predictionErrBackup[position];
        }

        public void SetPredictionErrBackup(int position, float value)
        {
            _predictionErrBackup[position] = value;
        }

        public float GetPredictionErrAverage(int position)
        {
            return _predictionErrAverage[position];
        }

        public void SetPredictionErrAverage(int position, float value)
        {
            _predictionErrAverage[position] = value;
        }

        public void AddClickToList(
            int position,
            int lenght,
            float thresholdLevelDetected,
            AudioData audioData,
            ChannelType channel)
        {
            _clicksList.Add(new AudioClick(
                position,
                lenght,
                thresholdLevelDetected,
                audioData,
                channel));
        }

        public int GetNumberOfClicks()
        {
            return _clicksList.Count;
        }

        internal void RestoreInitState(int position, int lenght)
        {
            for (var index = position; index < position + lenght; index++)
            {
                _output[index] = _input[index];
                _predictionErr[index] = _predictionErrBackup[index];
            }
        }

        public void ChangeClickAproved(int index)
        {
            _clicksList[index].ChangeAproved();
        }

        public AudioClick GetClick(int index)
        {
            return _clicksList[index];
        }

        public AudioClick GetLastClick()
        {
            if (_clicksList.Count > 0) return _clicksList.Last();

            return null;
        }

        internal void ClearAllClicks()
        {
            _clicksList.Clear();
        }

        internal void SortClicks()
        {
            _clicksList.Sort();
        }
    }
}