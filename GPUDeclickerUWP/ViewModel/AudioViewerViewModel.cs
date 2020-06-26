﻿using CarefulAudioRepair.Data;
using System;
using Windows.Foundation;
using Windows.UI.Xaml.Media;

namespace GPUDeclickerUWP.ViewModel
{
    public class AudioViewerViewModel
    {
        // These two collections of points are Binded to Polylines 
        // in XAML that represent WaveForms on screen
        public PointCollection LeftChannelWaveFormPoints { get; } = 
            new PointCollection();

        public PointCollection RightChannelWaveFormPoints { get; } =
            new PointCollection();

        public double[] leftCnannelSamples;

        public double[] rightCnannelSamples;

        // magnification ratio
        // when set to 1, waveForm is most detailed
        // when set to R, waveForm drops each R-1 from R audioData samples
        private double audioToWaveFormRatio = 1d;

        // offset from beginning of audioData to beginning waveForm
        private int offsetPosition;

        private int waveFormWidth;
        private int waveFormHeight;

        internal void UpdateAudio(IAudio audio)
        {
            if (audio is null)
            {
                return;
            }

            this.leftCnannelSamples = audio.GetInputRange(ChannelType.Left, 0, audio.LengthSamples - 1);

            if (audio.IsStereo)
            {
                this.rightCnannelSamples = audio.GetInputRange(ChannelType.Right, 0, audio.LengthSamples - 1);
            }

            this.InitializeState();

            this.UpdatePointsCollections();
        }

        private void InitializeState()
        {
            this.offsetPosition = 0;

            this.SetRatioToMax();
        }

        private void SetRatioToMax()
        {
            if (this.leftCnannelSamples != null && this.waveFormWidth > 0)
            {
                this.audioToWaveFormRatio =
                        this.leftCnannelSamples.Length / this.waveFormWidth;
            }
        }

        /// <summary>
        ///     sets points for PolyLine showing wave form
        /// </summary>
        internal void UpdatePointsCollections()
        {
            if (this.leftCnannelSamples is null)
            {
                return;
            }

            this.LeftChannelWaveFormPoints.Clear();
            this.RightChannelWaveFormPoints.Clear();

            // for every x-axis position of waveForm
            for (var xPosition = 0; xPosition < this.waveFormWidth; xPosition++)
            {
                this.AddPointToWaveform(
                    this.leftCnannelSamples,
                    this.LeftChannelWaveFormPoints,
                    xPosition);

                if (this.rightCnannelSamples != null)
                {
                    this.AddPointToWaveform(
                        this.rightCnannelSamples,
                        this.RightChannelWaveFormPoints,
                        xPosition);
                }
            }
        }

        /// <summary>
        ///     Adds a point representing one or many samples to wave form
        /// </summary>
        private void AddPointToWaveform(
            double[] samples,
            PointCollection waveFormPoints,
            int xPosition)
        {
            var offsetY = (int)this.waveFormHeight / 2;
            var start = this.offsetPosition + (int)(xPosition * this.audioToWaveFormRatio);
            var length = (int)this.audioToWaveFormRatio;

            if (start < 0 || start + length >= this.leftCnannelSamples.Length)
            {
                return;
            }

            // look for max and min among many samples represented by a point on wave form
            FindMinMax(samples, start, length, out var min, out var max);

            // connect previous point to a new point
            var yPosition = (int)(-0.5 * this.waveFormHeight * max) + offsetY;
            waveFormPoints.Add(new Point(xPosition, yPosition));

            // if min and max are close enough
            if (!(Math.Abs(min - max) > 0.01))
                return;

            // form vertical line connecting max and min
            yPosition = (int)(-0.5 * this.waveFormHeight * min) + offsetY;
            waveFormPoints.Add(new Point(xPosition, yPosition));
        }

        /// <summary>
        ///     Looks for max and min values among many samples represented
        ///     by a point on wave form
        /// </summary>
        /// <param name="begining">first sample position</param>
        /// <param name="length">number of samples</param>
        /// <param name="minValue">min value</param>
        /// <param name="maxValue">max value</param>
        private void FindMinMax(
            double[] samples,
            int begining,
            int length,
            out double minValue,
            out double maxValue)
        {
            minValue = samples[begining];
            maxValue = minValue;

            for (var index = 0;
                index < length && begining + index < samples.Length;
                index++)
            {
                var sample = samples[begining + index];

                if (sample < minValue)
                    minValue = sample;

                if (sample > maxValue)
                    maxValue = sample;
            }
        }

        internal void UpdateWaveFormSize(int waveFormWidth, int waveFormHeight)
        {
            this.waveFormWidth = waveFormWidth;
            this.waveFormHeight = waveFormHeight;

            InitializeState();

            this.UpdatePointsCollections();
        }
    }
}
