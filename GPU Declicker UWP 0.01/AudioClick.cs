using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    public class AudioClick : IComparable<AudioClick>
    {
        private int lenght;
        private float threshold_level_detected;
        private bool aproved;
        private AudioData audioDataBinded;
        private AudioProcessing audioProcessingBinded;
        private readonly ChannelType channelBinded;
        private int position;

        public int Position { get => position; set => position = value; }
        public int Lenght { get => lenght; set => lenght = value; }
        public float Threshold_level_detected {
            get => threshold_level_detected;
            set => threshold_level_detected = value; }
        public bool Aproved { get => aproved; set => aproved = value; }
        public AudioData AudioDataBinded {
            get => audioDataBinded;
            set => audioDataBinded = value; }
        public AudioProcessing AudioProcessingBinded {
            get => audioProcessingBinded;
            set => audioProcessingBinded = value; }

        public AudioClick(
            int position, 
            int lenght, 
            float threshold_level_detected, 
            AudioData audioData,
            AudioProcessing audioProcessing,
            ChannelType channel)
        {
            Position = position;
            Lenght = lenght;
            Threshold_level_detected = threshold_level_detected;
            // new click is always aproved initially
            Aproved = true;
            AudioDataBinded = audioData;
            AudioProcessingBinded = audioProcessing;
            channelBinded = channel;
        }

        public int CompareTo(AudioClick other)
        {
            // return the same result as for positions comparison
            return this.Position.CompareTo(other.Position);
        }

        public override bool Equals (object obj)
        {
            AudioClick audioClick = (AudioClick)obj;
            return this.Position == audioClick.Position;
        }

        public override int GetHashCode()
        {
            return this.Position.GetHashCode() ^ this.Lenght.GetHashCode();
        }

        public static bool operator == (AudioClick left, AudioClick right)
        {
            return left.Position == right.Position;
        }

        public static bool operator != (AudioClick left, AudioClick right)
        {
            return left.Position != right.Position;
        }

        public static bool operator < (AudioClick left, AudioClick right)
        {
            return left.Position < right.Position;
        }

        public static bool operator <= (AudioClick left, AudioClick right)
        {
            return left.Position <= right.Position;
        }

        public static bool operator >= (AudioClick left, AudioClick right)
        {
            return left.Position >= right.Position;
        }

        public static bool operator > (AudioClick left, AudioClick right)
        {
            return left.Position > right.Position;
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
            return AudioDataBinded.GetInputSample(position);
        }

        internal float GetOutputSample(int position)
        {
            return AudioDataBinded.GetOutputSample(position);
        }

        internal void ExpandLeft()
        {
            Position--;
            Lenght++;
            Threshold_level_detected = AudioProcessingBinded.Repair(AudioDataBinded, Position, Lenght);
        }

        internal void ShrinkLeft()
        {
            AudioDataBinded.SetOutputSample(Position, AudioDataBinded.GetInputSample(Position));
            Position++;
            Lenght--;
            Threshold_level_detected = AudioProcessingBinded.Repair(AudioDataBinded, Position, Lenght);
        }

        internal void ShrinkRight()
        {
            AudioDataBinded.SetOutputSample(Position + Lenght, AudioDataBinded.GetInputSample(Position + Lenght));
            Lenght--;
            Threshold_level_detected = AudioProcessingBinded.Repair(AudioDataBinded, Position, Lenght);
        }

        internal void ExpandRight()
        {
            Lenght++;
            Threshold_level_detected = AudioProcessingBinded.Repair(AudioDataBinded, Position, Lenght);
        }
    }
}
