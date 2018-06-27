namespace GPU_Declicker_UWP_0._01
{
    /// <summary>
    /// Represents stereo audio samples and includes information 
    /// about damaged samples
    /// </summary>
    public class AudioDataStereo : AudioData
    {
        private ChannelType currentChannelType;
        private readonly AudioChannel leftChannel;
        private readonly AudioChannel rightChannel;

        public AudioDataStereo(float[] leftChannelSamples, float[] rightChannelSamples)
        {
            IsStereo = true;
            currentChannelType = ChannelType.Left;
            leftChannel = new AudioChannel(leftChannelSamples);
            rightChannel = new AudioChannel(rightChannelSamples);
            currentAudioChannel = leftChannel;

            AudioProcessingSettings = new AudioProcessingSettings();
        }

        public override ChannelType GetCurrentChannelType() =>
            currentChannelType;

        public override void SetCurrentChannelType(ChannelType channelType)
        {
            currentChannelType = channelType;
            if (channelType == ChannelType.Left)
                currentAudioChannel = leftChannel;
            else
                currentAudioChannel = rightChannel;
        }

        public override void ClearAllClicks()
        {
            leftChannel.ClearAllClicks();
            rightChannel.ClearAllClicks();
        }

        public override void SortClicks()
        {
            leftChannel.SortClicks();
            rightChannel.SortClicks();
        }
    }
}
