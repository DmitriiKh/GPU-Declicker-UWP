using CarefulAudioRepair.Data;
using System;

namespace GPUDeclickerUWP.ViewModel
{
    public class AudioViewerViewModel : ViewModelBase
    {
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

            this.DrawWaveForm();
        }

        private void InitializeState()
        {
            this.offsetPosition = 0;

            this.audioToWaveFormRatio = this.MinDetailsRatio();
        }

        private double MinDetailsRatio() =>
            this.leftCnannelSamples.Length / this.waveFormWidth;

        private void DrawWaveForm()
        {
            throw new NotImplementedException();
        }

        internal void UpdateWaveFormSize(int waveFormWidth, int waveFormHeight)
        {
            this.waveFormWidth = waveFormWidth;
            this.waveFormHeight = waveFormHeight;

            InitializeState();
        }
    }
}
