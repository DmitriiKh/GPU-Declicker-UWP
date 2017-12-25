using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    public class AudioClick : IComparable<AudioClick>
    {
        public int Position, Lenght;
        public float Threshold_level_detected;
        public bool Aproved;
        public AudioDataClass audioDataBinded;
        private Channel channelBinded;

        public AudioClick(
            int position, 
            int lenght, 
            float threshold_level_detected, 
            AudioDataClass audioData,
            Channel channel)
        {
            Position = position;
            Lenght = lenght;
            Threshold_level_detected = threshold_level_detected;
            // new click is always aproved initially
            Aproved = true;
            audioDataBinded = audioData;
            channelBinded = channel;
        }

        public int CompareTo(AudioClick anotherAudioClick)
        {
            // return the same result as for positions comparison
            return this.Position.CompareTo(anotherAudioClick.Position);
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
            Position--;
            Lenght++;
            Threshold_level_detected = audioDataBinded.Repair(Position, Lenght, channelBinded);
        }

        internal void ShrinkLeft()
        {
            audioDataBinded.SetOutputSample(Position, audioDataBinded.GetInputSample(Position));
            Position++;
            Lenght--;
            Threshold_level_detected = audioDataBinded.Repair(Position, Lenght, channelBinded);
        }

        internal void ShrinkRight()
        {
            audioDataBinded.SetOutputSample(Position + Lenght, audioDataBinded.GetInputSample(Position + Lenght));
            Lenght--;
            Threshold_level_detected = audioDataBinded.Repair(Position, Lenght, channelBinded);
        }

        internal void ExpandRight()
        {
            Lenght++;
            Threshold_level_detected = audioDataBinded.Repair(Position, Lenght, channelBinded);
        }
    }
}
