using System;

namespace GPU_Declicker_UWP_0._01

{
    public class ClickEventArgs: EventArgs
    {
        public bool Shrinked { get; set; }
        public float ThresholdLevelDetected { get; set; }
    }

    public class AudioClick : IComparable<AudioClick>
    {
        public int Position { get; private set; }
        public int Length { get; private set; }
        public float ThresholdLevelDetected { get; private set; }
        public bool Aproved { get; private set; }
        private ChannelType FromChannel { get; }
        const bool Shrinked = true;
        const bool NotShrinked = false;

        private readonly AudioData _audioDataOwningThisClick;

        public event EventHandler<ClickEventArgs> ClickChanged;

        public AudioClick(
            int position, 
            int lenght, 
            float thresholdLevelDetected, 
            AudioData audioData,
            ChannelType fromChannel)
        {
            Position = position;
            Length = lenght;
            ThresholdLevelDetected = thresholdLevelDetected;
            _audioDataOwningThisClick = audioData;
            ClickChanged += audioData.OnClickChanged;
            FromChannel = fromChannel;
            Aproved = true;
        }

        public int CompareTo(AudioClick other)
        {
            if (other is null)
                return 1;
            // return the same result as for positions comparison
            return Position.CompareTo(other.Position);
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            AudioClick audioClick = (AudioClick)obj;
            return Position == audioClick.Position;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode() ^ 
                Length.GetHashCode() ^
                FromChannel.GetHashCode();
        }

        public static bool operator == (AudioClick left, AudioClick right)
        {
            if (right is null)
                return false;

            return left.Position == right.Position &&
                left.Length == right.Length &&
                left.FromChannel == right.FromChannel;
        }

        public static bool operator != (AudioClick left, AudioClick right)
        {
            if (right is null)
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
            if (!Aproved)
            {
                Aproved = true;
            }
            else
            {
                Aproved = false;
            }
        }

        public float GetInputSample(int position) => 
            _audioDataOwningThisClick.GetInputSample(position);

        public float GetOutputSample(int position) => 
            _audioDataOwningThisClick.GetOutputSample(position);

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

        protected virtual void OnClickChanged(bool shrinked)
        {
            var e = new ClickEventArgs { Shrinked = shrinked };

            ClickChanged?.Invoke(this, e);

            ThresholdLevelDetected = e.ThresholdLevelDetected;
        }
    }
}
