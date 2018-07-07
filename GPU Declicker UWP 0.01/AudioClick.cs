using System;

namespace GPU_Declicker_UWP_0._01

{
    public class ClickEventArgs : EventArgs
    {
        public bool Shrinked { get; set; }
        public float ThresholdLevelDetected { get; set; }
    }

    /// <summary>
    /// Contains information on sequences of damaged samples 
    /// </summary>
    public sealed class AudioClick : IComparable<AudioClick>
    {
        private const bool Shrinked = true;
        private const bool NotShrinked = false;

        private readonly AudioData _audioDataOwningThisClick;

        /// <summary>
        /// Creates new object containing information on sequence of damaged
        /// samples such as position, length etc 
        /// </summary>
        /// <param name="position"> Position of begining of a sequence of
        /// damaged samples in the input audio data </param>
        /// <param name="length"> Length of sequence of damaged samles </param>
        /// <param name="thresholdLevelDetected"> Prediction error to average
        /// error ratio </param>
        /// <param name="audioData"> Object of type of AudioData containing
        /// audio containig this sequence of damaged samples</param>
        /// <param name="fromChannel"> The channel (left, right) containing
        /// this sequence of damaged samples</param>
        public AudioClick(
            int position,
            int length,
            float thresholdLevelDetected,
            AudioData audioData,
            ChannelType fromChannel)
        {
            Position = position;
            Length = length;
            ThresholdLevelDetected = thresholdLevelDetected;
            _audioDataOwningThisClick = audioData;
            ClickChanged += audioData.OnClickChanged;
            FromChannel = fromChannel;
            Aproved = true;
        }

        public int Position { get; private set; }
        public int Length { get; private set; }
        public float ThresholdLevelDetected { get; private set; }
        public bool Aproved { get; private set; }
        private ChannelType FromChannel { get; }

        public int CompareTo(AudioClick other)
        {
            if (other is null)
                return 1;
            // return the same result as for positions comparison
            return Position.CompareTo(other.Position);
        }

        public event EventHandler<ClickEventArgs> ClickChanged;

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            var audioClick = (AudioClick) obj;
            return Position == audioClick.Position;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^
                   Length.GetHashCode() ^
                   FromChannel.GetHashCode();
        }

        public static bool operator ==(AudioClick left, AudioClick right)
        {
            if (left is null || right is null)
                return false;

            return left.Position == right.Position &&
                   left.Length == right.Length &&
                   left.FromChannel == right.FromChannel;
        }

        public static bool operator !=(AudioClick left, AudioClick right)
        {
            if (left is null || right is null)
                return true;

            return left.Position != right.Position ||
                   left.Length != right.Length ||
                   left.FromChannel != right.FromChannel;
        }

        public static bool operator <(AudioClick left, AudioClick right)
        {
            if (right is null)
                return false;

            return left.Position < right.Position;
        }

        public static bool operator <=(AudioClick left, AudioClick right)
        {
            if (right is null)
                return false;

            return left.Position <= right.Position;
        }

        public static bool operator >=(AudioClick left, AudioClick right)
        {
            if (right is null)
                return true;

            return left.Position >= right.Position;
        }

        public static bool operator >(AudioClick left, AudioClick right)
        {
            if (right is null)
                return true;

            return left.Position > right.Position;
        }

        public void ChangeAproved()
        {
            Aproved = !Aproved;
        }

        public float GetInputSample(int position)
        {
            return _audioDataOwningThisClick.GetInputSample(position);
        }

        public float GetOutputSample(int position)
        {
            return _audioDataOwningThisClick.GetOutputSample(position);
        }

        public void ExpandLeft()
        {
            Position--;
            Length++;

            OnClickChanged(NotShrinked);
        }

        public void ShrinkLeft()
        {
            Position++;
            Length--;

            OnClickChanged(Shrinked);
        }

        public void ShrinkRight()
        {
            Length--;

            OnClickChanged(Shrinked);
        }

        public void ExpandRight()
        {
            Length++;

            OnClickChanged(NotShrinked);
        }

        private void OnClickChanged(bool shrinked)
        {
            var e = new ClickEventArgs {Shrinked = shrinked};

            ClickChanged?.Invoke(this, e);

            ThresholdLevelDetected = e.ThresholdLevelDetected;
        }
    }
}