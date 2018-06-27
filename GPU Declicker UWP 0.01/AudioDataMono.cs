namespace GPU_Declicker_UWP_0._01
{

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

            AudioProcessingSettings = new AudioProcessingSettings();
        }

        public override ChannelType GetCurrentChannelType() =>
            // always answers Left for mono
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
