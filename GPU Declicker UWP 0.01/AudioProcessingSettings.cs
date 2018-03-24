
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    public class AudioProcessingSettings
    {
        public int HistoryLengthSamples { get; set; }
        public int CoefficientsNumber { get; set; }
        public float ThresholdForDetection { get; set; }
        public int Max_lenghth_correction { get; set; }

        public AudioProcessingSettings()
        {
            HistoryLengthSamples = 512;
            CoefficientsNumber = 4;
            ThresholdForDetection = 10;
            Max_lenghth_correction = 250;
        }
    }
}
