using CarefulAudioRepair.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPUDeclickerUWP.ViewModel
{
    public class AudioViewerViewModel : ViewModelBase
    {

        public double[] _leftCnannelSamples;
        public double[] _rightCnannelSamples;

        // magnification ratio
        // when set to 1, waveForm is most detailed
        // when set to R, waveForm drops each R-1 from R audioData samples
        private double _audioToWaveFormRatio = 1d;

        // offset from beginning of audioData to beginning waveForm
        private int _offsetPosition;

        private int waveFormWidth;
        private int waveFormHeight;

        public void UpdateAudio(IAudio audio)
        {
            if (audio is null)
            {
                return;
            }

            _leftCnannelSamples = audio.GetInputRange(ChannelType.Left, 0, audio.LengthSamples - 1);

            if (audio.IsStereo)
            {
                _rightCnannelSamples = audio.GetInputRange(ChannelType.Right, 0, audio.LengthSamples - 1);
            }

            _offsetPosition = 0;

            // Sets Ratio to show whole audio track
            _audioToWaveFormRatio =
                _leftCnannelSamples.Length / waveFormWidth;

            DrawWaveForm();
        }

        private void DrawWaveForm()
        {
            throw new NotImplementedException();
        }

        internal void UpdateWaveFormSize(int waveFormWidth, int waveFormHeight)
        {
            this.waveFormWidth = waveFormWidth;
            this.waveFormHeight = waveFormHeight;
        }
    }
}
