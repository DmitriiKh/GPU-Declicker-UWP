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
        public int Lenght { get; private set; }
        public float ThresholdLevelDetected { get; private set; }
        public bool Aproved { get; private set; }
        public ChannelType FromChannel { get; }

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
            Lenght = lenght;
            ThresholdLevelDetected = thresholdLevelDetected;
            _audioDataOwningThisClick = audioData;
            ClickChanged += audioData.OnClickChanged;
            FromChannel = fromChannel;
            Aproved = true;
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

        public static bool operator <(AudioClick left, AudioClick right) => 
            left.Position < right.Position;

        public static bool operator <=(AudioClick left, AudioClick right) => 
            left.Position <= right.Position;

        public static bool operator >=(AudioClick left, AudioClick right) => 
            left.Position >= right.Position;

        public static bool operator >(AudioClick left, AudioClick right) => 
            left.Position > right.Position;

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
            Lenght++;

            OnClickChanged(false);
        }

        public void ShrinkLeft()
        {
            Position++;
            Lenght--;

            OnClickChanged(true);
        }

        public void ShrinkRight()
        {
            Lenght--;

            OnClickChanged(true);
        }

        public void ExpandRight()
        {
            Lenght++;

            OnClickChanged(false);
        }

        protected virtual void OnClickChanged(bool shrinked)
        {
            var e = new ClickEventArgs { Shrinked = shrinked };

            ClickChanged?.Invoke(this, e);

            ThresholdLevelDetected = e.ThresholdLevelDetected;
        }
    }
}
