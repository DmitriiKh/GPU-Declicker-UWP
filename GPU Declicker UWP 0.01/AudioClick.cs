using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    public class AudioClick : IComparable<AudioClick>
    {
        public int Pos, Len;
        public float Threshold_level_detected;
        public bool Aproved;
        public AudioDataClass audioDataBinded;

        public AudioClick(int pos, int len, float threshold_level_detected, AudioDataClass audioData)
        {
            Pos = pos;
            Len = len;
            Threshold_level_detected = threshold_level_detected;
            Aproved = true;
            audioDataBinded = audioData;
        }

        public int CompareTo(AudioClick anotherAudioClick)
        {
            return this.Pos.CompareTo(anotherAudioClick.Pos);
        }

        public void ChangeAproved()
        {
            if (!Aproved)
            {
                Aproved = true;
            }
            else
            {
                Aproved = false;
            }
        }

        internal float GetInputSample(int position)
        {
            return audioDataBinded.GetInputSample(position);
        }

        internal float GetOutputSample(int position)
        {
            return audioDataBinded.GetOutputSample(position);
        }

        internal void ExpandLeft()
        {
            Pos--;
            Len++;
            audioDataBinded.Recalculate(Pos, Len);
        }

        internal void ShrinkLeft()
        {
            audioDataBinded.SetOutputSample(Pos, audioDataBinded.GetInputSample(Pos));
            Pos++;
            Len--;
            audioDataBinded.Recalculate(Pos, Len);
        }

        internal void ShrinkRight()
        {
            audioDataBinded.SetOutputSample(Pos + Len, audioDataBinded.GetInputSample(Pos + Len));
            Len--;
            audioDataBinded.Recalculate(Pos, Len);
        }

        internal void ExpandRight()
        {
            Len++;
            audioDataBinded.Recalculate(Pos, Len);
        }
    }
}
