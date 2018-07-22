using GPUDeclickerUWP.Model.Processing;

namespace GPUDeclickerUWP.Model.Data
{
    /// <summary>
    ///     Represents mono audio samples and includes information
    ///     about damaged samples
    /// </summary>
    public class AudioDataMono : AudioData
    {
        private readonly AudioChannel _monoChannel;

        public AudioDataMono(float[] leftChannelSamples)
        {
            IsStereo = false;
            _monoChannel = new AudioChannel(leftChannelSamples);
            CurrentAudioChannel = _monoChannel;

            AudioProcessingSettings = new AudioProcessingSettings();
        }

        public override ChannelType GetCurrentChannelType()
        {
            return ChannelType.Left;
        }

        public override void SetCurrentChannelType(ChannelType channelType)
        {
            // doing nothing because it's mono
        }

        public override void ClearAllClicks()
        {
            _monoChannel.ClearAllClicks();
        }

        public override void SortClicks()
        {
            _monoChannel.SortClicks();
        }
    }
}