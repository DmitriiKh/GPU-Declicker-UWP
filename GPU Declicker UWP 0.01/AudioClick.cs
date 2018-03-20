using System;

namespace GPU_Declicker_UWP_0._01
{
    public class AudioClick : IComparable<AudioClick>
    {
        public int Position { get; private set; }
        public int Lenght { get; private set; }
        public float Threshold_level_detected { get; private set; }
        // new click is always aproved initially by default initializing of 
        // boolean type
        public bool Aproved { get; private set; }
        public ChannelType FromChannel { get; }

        private readonly AudioData _audioDataOwningThisClick;
        private readonly AudioProcessing _audioProcessingBinded;

        public AudioClick(
            int position, 
            int lenght, 
            float threshold_level_detected, 
            AudioData audioData,
            AudioProcessing audioProcessing,
            ChannelType fromChannel)
        {
            Position = position;
            Lenght = lenght;
            Threshold_level_detected = threshold_level_detected;
            _audioDataOwningThisClick = audioData;
            _audioProcessingBinded = audioProcessing;
            FromChannel = fromChannel;
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
                this.FromChannel.GetHashCode();
        }

        public static bool operator == (AudioClick left, AudioClick right)
        {
            return left.Position == right.Position &&
                left.Lenght == right.Lenght &&
                left.FromChannel == right.FromChannel;
        }

        public static bool operator != (AudioClick left, AudioClick right)
        {
            return left.Position != right.Position ||
                left.Lenght != right.Lenght ||
                left.FromChannel != right.FromChannel;
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
            return _audioDataOwningThisClick.GetInputSample(position);
        }

        /// <summary>
        /// Get output sample from audioData
        /// </summary>
        /// <param name="position">position from begining of audioData</param>
        /// <returns></returns>
        public float GetOutputSample(int position)
        {
            return _audioDataOwningThisClick.GetOutputSample(position);
        }

        public void ExpandLeft()
        {
            Position--;
            Lenght++;
            Threshold_level_detected = _audioProcessingBinded.Repair(
                _audioDataOwningThisClick, 
                Position, 
                Lenght);
        }

        public void ShrinkLeft()
        {
            _audioDataOwningThisClick.SetOutputSample(Position, _audioDataOwningThisClick.GetInputSample(Position));
            Position++;
            Lenght--;
            Threshold_level_detected = _audioProcessingBinded.Repair(_audioDataOwningThisClick, Position, Lenght);
        }

        public void ShrinkRight()
        {
            _audioDataOwningThisClick.SetOutputSample(Position + Lenght, _audioDataOwningThisClick.GetInputSample(Position + Lenght));
            Lenght--;
            Threshold_level_detected = _audioProcessingBinded.Repair(_audioDataOwningThisClick, Position, Lenght);
        }

        public void ExpandRight()
        {
            Lenght++;
            Threshold_level_detected = _audioProcessingBinded.Repair(_audioDataOwningThisClick, Position, Lenght);
        }
    }
}
