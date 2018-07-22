using GPUDeclickerUWP.Model.Processing;

namespace GPUDeclickerUWP.Model.Data
{
    /// <summary>
    ///     Represents stereo audio samples and includes information
    ///     about damaged samples
    /// </summary>
    public class AudioDataStereo : AudioData
    {
        private readonly AudioChannel _leftChannel;
        private readonly AudioChannel _rightChannel;
        private ChannelType _currentChannelType;

        public AudioDataStereo(float[] leftChannelSamples, float[] rightChannelSamples)
        {
            IsStereo = true;
            _currentChannelType = ChannelType.Left;
            _leftChannel = new AudioChannel(leftChannelSamples);
            _rightChannel = new AudioChannel(rightChannelSamples);
            CurrentAudioChannel = _leftChannel;

            AudioProcessingSettings = new AudioProcessingSettings();
        }

        public override ChannelType GetCurrentChannelType()
        {
            return _currentChannelType;
        }

        public override void SetCurrentChannelType(ChannelType channelType)
        {
            _currentChannelType = channelType;
            CurrentAudioChannel = channelType == ChannelType.Left ? 
                _leftChannel : _rightChannel;
        }

        public override void ClearAllClicks()
        {
            _leftChannel.ClearAllClicks();
            _rightChannel.ClearAllClicks();
        }

        public override void SortClicks()
        {
            _leftChannel.SortClicks();
            _rightChannel.SortClicks();
        }
    }
}