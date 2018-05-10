using System;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace GPU_Declicker_UWP_0._01
{
    public sealed partial class AudioViewer : UserControl
    {
        public AudioViewer()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// audio samples to view
        /// </summary>
        private AudioData audioData = null;

        /// <summary>
        /// offset from beginning of audioData to beginning waveForm
        /// </summary>
        private int OffsetPosition;

        /// <summary>
        /// magnification ratio
        /// when set to 1, waveForm is most detailed
        /// when set to R, waveForm drops each R-1 from R audioData samples
        /// </summary>
        private double audioDataToWaveFormRatio = 1;

        /// <summary>
        /// Last mouse pointer position touching waveForms
        /// Used to calculate new OffsetPosition when user slides waveForms
        /// </summary>
        public Point PointerLastPosition { get; internal set; }

        /// <summary>
        /// True when when user slides waveForms
        /// </summary>
        public bool IsMovingByMouse { get; internal set; }

        /// <summary>
        /// Fills this with AudioData, sets Ratio and OffsetPosition
        /// </summary>
        /// <param name="audioDataInput"></param>
        public void Fill(AudioData audioDataInput)
        {
            OffsetPosition = 0;
            // Sets Ratio to show whole audio track
            audioDataToWaveFormRatio =
                audioDataInput.LengthSamples() / waveFormLeftChannel.ActualWidth;
            audioData = audioDataInput;

            DrawWaveForm();
        }

        /// <summary>
        /// move OffsetPositionX to the right for one waveForm length
        /// </summary>
        public void GoNextBigStep()
        {
            if (audioData == null) return;

            int deltaX = (int)waveFormLeftChannel.ActualWidth;
            GoNextX(deltaX);
        }

        // move OffsetPositionX to the right for one tenth of waveForm length
        public void GoNextSmalStep()
        {
            if (audioData == null) return;

            int deltaX = (int)waveFormLeftChannel.ActualWidth / 10;
            GoNextX(deltaX);
        }

        // move OffsetPositionX to the right for shiftX samples
        public void GoNextX(int deltaX)
        {
            if (audioData == null) return;
            // Calculate number of samples to shift
            int shift = (int)(deltaX * audioDataToWaveFormRatio);
            // Calculate number of samples that waveForms show
            int samplesOnScrean = (int)(
                waveFormLeftChannel.ActualWidth
                * audioDataToWaveFormRatio
                );
            // if there is enough room on the right than shift offsetPosition
            if (OffsetPosition + shift + samplesOnScrean < audioData.LengthSamples())
                OffsetPosition += shift;
            else
            {
                // set OffsetPosition to show the end of audioData
                OffsetPosition = audioData.LengthSamples() - samplesOnScrean;
                if (OffsetPosition < 0)
                    OffsetPosition = 0;
            }

            DrawWaveForm();
        }

        // move OffsetPositionX to the right for one waveForm length 
        public void GoPrevBigStep()
        {
            if (audioData == null)
                return;

            int deltaX = (int)waveFormLeftChannel.ActualWidth;
            GoPrevX(deltaX);
        }

        // move OffsetPositionX to the right for one tenth of waveForm length 
        public void GoPrevSmalStep()
        {
            if (audioData == null)
                return;

            int deltaX = (int)waveFormLeftChannel.ActualWidth / 10;
            GoPrevX(deltaX);
        }

        // move OffsetPositionX to the right for shiftX samples 
        public void GoPrevX(int X)
        {
            if (audioData == null) return;
            // Calculate number of samples to shift
            int shift = (int)(X * audioDataToWaveFormRatio);
            // if there is enough room on the left than shift offsetPositionX
            if (OffsetPosition > shift) OffsetPosition -= shift;
            else
                // set OffsetPositionX to show the begining of audioData
                OffsetPosition = 0;

            DrawWaveForm();
        }

        /// <summary>
        /// sets points for PolyLine showing wave form
        /// </summary>
        private void DrawWaveForm()
        {
            if (audioData == null) return;

            waveFormLeftChannel.Points.Clear();
            waveFormRightCnannel.Points.Clear();

            // for every x-axis position of waveForm
            for (int x = 0; x < waveFormsGroup.ActualWidth; x++)
            {
                audioData.SetCurrentChannelType(ChannelType.Left);
                AddPointToWaveform(waveFormLeftChannel, x);

                if (audioData.IsStereo)
                {
                    audioData.SetCurrentChannelType(ChannelType.Right);
                    AddPointToWaveform(waveFormRightCnannel, x);
                }
            }
        }

        /// <summary>
        /// Adds a point representing one or many samples to wave form
        /// </summary>
        private void AddPointToWaveform(Polyline waveForm, int x)
        {
            int offsetY = (int)waveFormLeftChannel.ActualHeight / 2;
            int start = OffsetPosition + (int)(x * audioDataToWaveFormRatio);
            if (start >= audioData.LengthSamples())
            {
                return;
            }
            int length = (int)audioDataToWaveFormRatio;

            // looks for max and min among many samples represented by a point on wave form
            FindMinMax(start, length, out float min, out float max);

            // connect previous point to a new point
            int y = (int)(-0.5 * waveFormLeftChannel.ActualHeight * max) + offsetY;
            waveForm.Points.Add(new Point(x, y));
            if (min != max)
            {
                // form vertical line connecting max and min
                y = (int)(-0.5 * waveForm.ActualHeight * min) + offsetY;
                waveForm.Points.Add(new Point(x, y));
            }
        }

        /// <summary>
        /// Looks for max and min values among many samples represented 
        /// by a point on wave form
        /// </summary>
        /// <param name="begining">first sample position</param>
        /// <param name="length">number of samples</param>
        /// <param name="minValue">min value</param>
        /// <param name="maxValue">max value</param>
        private void FindMinMax(
            int begining, 
            int length, 
            out float minValue, 
            out float maxValue)
        {
            minValue = audioData.GetInputSample(begining);
            maxValue = audioData.GetInputSample(begining);
            for (int index = 0; 
                index < length && begining + index < audioData.LengthSamples(); 
                index++)
            {
                if (audioData.GetInputSample(begining + index) < minValue)
                    minValue = audioData.GetInputSample(begining + index);
                if (audioData.GetInputSample(begining + index) > maxValue)
                    maxValue = audioData.GetInputSample(begining + index);
            }
        }

        /// <summary>
        /// Increases detalization on waveForm
        /// </summary>
        public void MagnifyMore()
        {
            if (audioData == null)
                return;

            if (audioDataToWaveFormRatio >= 2)
                audioDataToWaveFormRatio /= 2;
            else
                audioDataToWaveFormRatio = 1;

            DrawWaveForm();
        }

        /// <summary>
        /// Decreases detalization on waveForm
        /// </summary>
        public void MagnifyLess()
        {
            if (audioData == null)
                return;

            if (waveFormLeftChannel.ActualWidth * audioDataToWaveFormRatio * 2
                < audioData.LengthSamples())
                audioDataToWaveFormRatio *= 2;
            else
                audioDataToWaveFormRatio =
                audioData.LengthSamples() / waveFormLeftChannel.ActualWidth;

            DrawWaveForm();
        }

        internal void AudioViewerSizeChanged()
        {
            OffsetPosition = 0;
            // Sets Ratio to show whole audio track
            if (audioData != null)
                audioDataToWaveFormRatio =
                    audioData.LengthSamples() / waveFormsGroup.ActualWidth;

            DrawWaveForm();
        }

        /// <summary>
        /// Returns offset in samples for pointer position
        /// </summary>
        /// <param name="pointerPosition"> pointer position on waveForm</param>
        public int PointerOffsetPosition(double pointerPosition)
            => (int)(pointerPosition * audioDataToWaveFormRatio)
                + OffsetPosition;

        /// <summary>
        /// Adjusts OffsetPosition to make pointer point to Offset sample
        /// </summary>
        /// <param name="Offset"> offset for pointer position</param>
        /// <param name="pointerPosition"> X of pointer position on waveForm</param>
        public void SetOffsetForPointer(int Offset, double pointerPosition)
        {
            OffsetPosition = Offset - (int)(pointerPosition * audioDataToWaveFormRatio);
            if (OffsetPosition < 0)
            {
                OffsetPosition = 0;
            }
            DrawWaveForm();
        }

        /// <summary>
        /// MouseWheel events handler for wave forms
        /// Magnification adjustment
        /// </summary>
        private void WaveFormsGroup_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint pointer = e.GetCurrentPoint(this); 
            Point pointerPosition = pointer.Position;
            // calculates offset for samples at pointer
            int offsetAtPointer = this.PointerOffsetPosition(pointerPosition.X);
            PointerPointProperties pointerProperties = pointer.Properties;
            int delta = pointerProperties.MouseWheelDelta;
            if (delta > 0)
                this.MagnifyMore();
            else
                this.MagnifyLess();
            // set pointer at the same position
            this.SetOffsetForPointer(offsetAtPointer, pointerPosition.X);
        }

        private void WaveFormsGroup_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint pointer = e.GetCurrentPoint(this); 
            this.PointerLastPosition = pointer.Position;
            this.IsMovingByMouse = true;
        }

        private void WaveFormsGroup_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            this.IsMovingByMouse = false;
        }

        private void WaveFormsGroup_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (this.IsMovingByMouse)
            {
                PointerPoint pointer = e.GetCurrentPoint(this); 
                int shiftX = (int)(this.PointerLastPosition.X - pointer.Position.X);
                if (shiftX > 0)
                    this.GoNextX(Math.Abs(shiftX));
                else
                    this.GoPrevX(Math.Abs(shiftX));

                this.PointerLastPosition = pointer.Position;
            }
        }

        private void WaveFormsGroup_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            WaveFormsGroup_PointerReleased(sender, e);
            this.IsMovingByMouse = false;
        }

        private void WaveFormsGroup_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint pointer = e.GetCurrentPoint(this); 
            PointerPointProperties pointerProperties = pointer.Properties;
            if (pointerProperties.IsLeftButtonPressed)
                WaveFormsGroup_PointerPressed(sender, e);
        }
    }
}
