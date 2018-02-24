using System;

namespace GPU_Declicker_UWP_0._01
{
    public class AudioClick : IComparable<AudioClick>
    {
        private int lenght;
        private float threshold_level_detected;
        private bool aproved;
        private readonly AudioData audioDataOwningThisClick;
        private AudioProcessing audioProcessingBinded;
        private readonly ChannelType channelBinded;
        private int position;

        public int Position { get => position; set => position = value; }
        public int Lenght { get => lenght; set => lenght = value; }
        public float Threshold_level_detected {
            get => threshold_level_detected;
            set => threshold_level_detected = value; }
        public bool Aproved { get => aproved; set => aproved = value; }
        public AudioData AudioDataOwningThisClick {
            get => audioDataOwningThisClick; }
        public AudioProcessing AudioProcessingBinded {
            get => audioProcessingBinded;
            set => audioProcessingBinded = value; }
        public ChannelType ChannelType { get => channelBinded; }

        public AudioClick(
            int position, 
            int lenght, 
            float threshold_level_detected, 
            AudioData audioData,
            AudioProcessing audioProcessing,
            ChannelType channelType)
        {
            Position = position;
            Lenght = lenght;
            Threshold_level_detected = threshold_level_detected;
            // new click is always aproved initially
            Aproved = true;
            audioDataOwningThisClick = audioData;
            AudioProcessingBinded = audioProcessing;
            channelBinded = channelType;
        }

        public int CompareTo(AudioClick other)
        {
            // return the same result as for positions comparison
            return this.Position.CompareTo(other.Position);
        }

        public override bool Equals(object obj)
        {
            AudioClick audioClick = (AudioClick)obj;
            return this.Position == audioClick.Position;
        }

        public override int GetHashCode()
        {
            return this.Position.GetHashCode() ^ 
                this.Lenght.GetHashCode() ^
                this.ChannelType.GetHashCode();
        }

        public static bool operator == (AudioClick left, AudioClick right)
        {
            return left.Position == right.Position &&
                left.Lenght == right.Lenght &&
                left.ChannelType == right.ChannelType;
        }

        public static bool operator != (AudioClick left, AudioClick right)
        {
            return left.Position != right.Position ||
                left.Lenght != right.Lenght ||
                left.ChannelType != right.ChannelType;
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
        
        /// <summary>
        /// Get input sample from audioData
        /// </summary>
        /// <param name="position">position from begining of audioData</param>
        /// <returns></returns>
        public float GetInputSample(int position)
        {
            return AudioDataOwningThisClick.GetInputSample(position);
        }

        /// <summary>
        /// Get output sample from audioData
        /// </summary>
        /// <param name="position">position from begining of audioData</param>
        /// <returns></returns>
        public float GetOutputSample(int position)
        {
            return AudioDataOwningThisClick.GetOutputSample(position);
        }

        internal void ExpandLeft()
        {
            Position--;
            Lenght++;
            Threshold_level_detected = AudioProcessingBinded.Repair(
                AudioDataOwningThisClick, 
                Position, 
                Lenght);
        }

        public void ShrinkLeft()
        {
            AudioDataOwningThisClick.SetOutputSample(Position, AudioDataOwningThisClick.GetInputSample(Position));
            Position++;
            Lenght--;
            Threshold_level_detected = AudioProcessingBinded.Repair(AudioDataOwningThisClick, Position, Lenght);
        }

        internal void ShrinkRight()
        {
            AudioDataOwningThisClick.SetOutputSample(Position + Lenght, AudioDataOwningThisClick.GetInputSample(Position + Lenght));
            Lenght--;
            Threshold_level_detected = AudioProcessingBinded.Repair(AudioDataOwningThisClick, Position, Lenght);
        }

        internal void ExpandRight()
        {
            Lenght++;
            Threshold_level_detected = AudioProcessingBinded.Repair(AudioDataOwningThisClick, Position, Lenght);
        }
    }
}
