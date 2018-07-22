namespace GPUDeclickerUWP.Model.Processing
{
    public class AudioProcessingSettings
    {
        public AudioProcessingSettings()
        {
            HistoryLengthSamples = 512;
            CoefficientsNumber = 4;
            ThresholdForDetection = 10;
            MaxLengthCorrection = 250;
        }

        public int HistoryLengthSamples { get; }
        public int CoefficientsNumber { get; }
        public float ThresholdForDetection { get; set; }
        public int MaxLengthCorrection { get; set; }
    }
}