using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace GPU_Declicker_UWP_0._01
{
    public sealed partial class AudioViewer
    {
        // this timer creates a delay between WaveForm size changing and its redrawing
        // to make resizing faster
        private readonly DispatcherTimer _redrawingTimer;

        // audio samples to view
        private AudioData _audioData;

        // magnification ratio
        // when set to 1, waveForm is most detailed
        // when set to R, waveForm drops each R-1 from R audioData samples
        private double _audioDataToWaveFormRatio = 1;

        // offset from beginning of audioData to beginning waveForm
        private int _offsetPosition;

        public AudioViewer()
        {
            InitializeComponent();
            LeftChannelWaveFormPoints = new PointCollection();
            RightChannelWaveFormPoints = new PointCollection();
            _redrawingTimer = new DispatcherTimer();
            _redrawingTimer.Tick += _redrawingTimer_Tick;
            _redrawingTimer.Interval = TimeSpan.FromSeconds(0.1);
        }

        // These two collections of points are Binded to Polylines 
        // in XAML that represent WaveForms on screen
        public PointCollection LeftChannelWaveFormPoints { get; }
        public PointCollection RightChannelWaveFormPoints { get; }

        // These are updated every time when WaveForm's size changed
        // by event handler WaveFormLeftChannel_SizeChanged
        public double WaveFormWidth { get; set; }
        public double WaveFormHeight { get; set; }

        // Last mouse pointer position touching waveForms
        // Used to calculate new OffsetPosition when user slides waveForms
        private Point PointerLastPosition { get; set; }

        // True when when user slides waveForms
        private bool IsMovingByMouse { get; set; }

        private void _redrawingTimer_Tick(object sender, object e)
        {
            _redrawingTimer.Stop();
            DrawWaveForm();
        }

        /// <summary>
        ///     Fills this with AudioData, sets Ratio and OffsetPosition
        /// </summary>
        public void Fill(AudioData audioDataInput)
        {
            _offsetPosition = 0;

            // Sets Ratio to show whole audio track
            _audioDataToWaveFormRatio =
                audioDataInput.LengthSamples() / WaveFormWidth;
            _audioData = audioDataInput;

            DrawWaveForm();
        }

        /// <summary>
        ///     move OffsetPositionX to the right for one waveForm length
        /// </summary>
        public void GoNextBigStep()
        {
            if (_audioData == null) return;

            var deltaX = (int) WaveFormWidth;
            GoNextX(deltaX);
        }

        // move OffsetPositionX to the right for one tenth of waveForm length
        public void GoNextSmalStep()
        {
            if (_audioData == null) return;

            var deltaX = (int) WaveFormWidth / 10;
            GoNextX(deltaX);
        }

        // move OffsetPositionX to the right for shiftX samples
        private void GoNextX(int deltaX)
        {
            if (_audioData == null) return;
            // Calculate number of samples to shift
            var shift = (int) (deltaX * _audioDataToWaveFormRatio);
            // Calculate number of samples that waveForms show
            var samplesOnScrean = (int) (
                WaveFormWidth
                * _audioDataToWaveFormRatio
            );
            // if there is enough room on the right than shift offsetPosition
            if (_offsetPosition + shift + samplesOnScrean < _audioData.LengthSamples())
            {
                _offsetPosition += shift;
            }
            else
            {
                // set OffsetPosition to show the end of audioData
                _offsetPosition = _audioData.LengthSamples() - samplesOnScrean;
                if (_offsetPosition < 0)
                    _offsetPosition = 0;
            }

            DrawWaveForm();
        }

        // move OffsetPositionX to the right for one waveForm length 
        public void GoPrevBigStep()
        {
            if (_audioData == null)
                return;

            var deltaX = (int) WaveFormWidth;
            GoPrevX(deltaX);
        }

        // move OffsetPositionX to the right for one tenth of waveForm length 
        public void GoPrevSmalStep()
        {
            if (_audioData == null)
                return;

            var deltaX = (int) WaveFormWidth / 10;
            GoPrevX(deltaX);
        }

        // move OffsetPositionX to the right for shiftX samples 
        private void GoPrevX(int x)
        {
            if (_audioData == null) return;
            // Calculate number of samples to shift
            var shift = (int) (x * _audioDataToWaveFormRatio);
            // if there is enough room on the left than shift offsetPositionX
            if (_offsetPosition > shift) _offsetPosition -= shift;
            else
                // set OffsetPositionX to show the begining of audioData
                _offsetPosition = 0;

            DrawWaveForm();
        }

        /// <summary>
        ///     sets points for PolyLine showing wave form
        /// </summary>
        private void DrawWaveForm()
        {
            if (_audioData == null) return;

            LeftChannelWaveFormPoints.Clear();
            RightChannelWaveFormPoints.Clear();

            // for every x-axis position of waveForm
            for (var x = 0; x < WaveFormWidth; x++)
            {
                _audioData.SetCurrentChannelType(ChannelType.Left);
                AddPointToWaveform(LeftChannelWaveFormPoints, x);

                if (_audioData.IsStereo)
                {
                    _audioData.SetCurrentChannelType(ChannelType.Right);
                    AddPointToWaveform(RightChannelWaveFormPoints, x);
                }
            }
        }

        /// <summary>
        ///     Adds a point representing one or many samples to wave form
        /// </summary>
        private void AddPointToWaveform(PointCollection waveFormPoints, int x)
        {
            var offsetY = (int) WaveFormHeight / 2;
            var start = _offsetPosition + (int) (x * _audioDataToWaveFormRatio);
            var length = (int) _audioDataToWaveFormRatio;

            if (start < 0 || start + length >= _audioData.LengthSamples()) return;

            // looks for max and min among many samples represented by a point on wave form
            FindMinMax(start, length, out var min, out var max);

            // connect previous point to a new point
            var y = (int) (-0.5 * WaveFormHeight * max) + offsetY;
            waveFormPoints.Add(new Point(x, y));
            if (Math.Abs(min - max) > 0.01)
            {
                // form vertical line connecting max and min
                y = (int) (-0.5 * WaveFormHeight * min) + offsetY;
                waveFormPoints.Add(new Point(x, y));
            }
        }

        /// <summary>
        ///     Looks for max and min values among many samples represented
        ///     by a point on wave form
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
            minValue = _audioData.GetInputSample(begining);
            maxValue = _audioData.GetInputSample(begining);

            for (var index = 0;
                index < length && begining + index < _audioData.LengthSamples();
                index++)
            {
                if (_audioData.GetInputSample(begining + index) < minValue)
                    minValue = _audioData.GetInputSample(begining + index);
                if (_audioData.GetInputSample(begining + index) > maxValue)
                    maxValue = _audioData.GetInputSample(begining + index);
            }
        }

        /// <summary>
        ///     Increases detalization on waveForm
        /// </summary>
        public void MagnifyMore()
        {
            if (_audioData == null)
                return;

            if (_audioDataToWaveFormRatio >= 2)
                _audioDataToWaveFormRatio /= 2;
            else
                _audioDataToWaveFormRatio = 1;

            DrawWaveForm();
        }

        /// <summary>
        ///     Decreases detalization on waveForm
        /// </summary>
        public void MagnifyLess()
        {
            if (_audioData == null)
                return;

            if (WaveFormWidth * _audioDataToWaveFormRatio * 2
                < _audioData.LengthSamples())
                _audioDataToWaveFormRatio *= 2;
            else
                _audioDataToWaveFormRatio =
                    _audioData.LengthSamples() / WaveFormWidth;

            DrawWaveForm();
        }

        /// <summary>
        ///     Returns offset in samples for pointer position
        /// </summary>
        /// <param name="pointerPosition"> pointer position on waveForm</param>
        private int PointerOffsetPosition(double pointerPosition)
        {
            return (int) (pointerPosition * _audioDataToWaveFormRatio)
                   + _offsetPosition;
        }

        /// <summary>
        ///     Adjusts OffsetPosition to make pointer point to Offset sample
        /// </summary>
        /// <param name="offset"> offset for pointer position</param>
        /// <param name="pointerPosition"> X of pointer position on waveForm</param>
        private void SetOffsetForPointer(int offset, double pointerPosition)
        {
            _offsetPosition = offset - (int) (pointerPosition * _audioDataToWaveFormRatio);
            if (_offsetPosition < 0) _offsetPosition = 0;
            DrawWaveForm();
        }

        /// <summary>
        ///     MouseWheel events handler for wave forms
        ///     Magnification adjustment
        /// </summary>
        private void WaveFormsGroup_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            var pointerPosition = pointer.Position;
            // calculates offset for samples at pointer
            var offsetAtPointer = PointerOffsetPosition(pointerPosition.X);
            var pointerProperties = pointer.Properties;
            var delta = pointerProperties.MouseWheelDelta;
            if (delta > 0)
                MagnifyMore();
            else
                MagnifyLess();
            // set pointer at the same position
            SetOffsetForPointer(offsetAtPointer, pointerPosition.X);
        }

        private void WaveFormsGroup_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            PointerLastPosition = pointer.Position;
            IsMovingByMouse = true;
        }

        private void WaveFormsGroup_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            IsMovingByMouse = false;
        }

        private void WaveFormsGroup_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (IsMovingByMouse)
            {
                var pointer = e.GetCurrentPoint(this);
                var shiftX = (int) (PointerLastPosition.X - pointer.Position.X);
                if (shiftX > 0)
                    GoNextX(Math.Abs(shiftX));
                else
                    GoPrevX(Math.Abs(shiftX));

                PointerLastPosition = pointer.Position;
            }
        }

        private void WaveFormsGroup_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            WaveFormsGroup_PointerReleased(sender, e);
            IsMovingByMouse = false;
        }

        private void WaveFormsGroup_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            var pointerProperties = pointer.Properties;
            if (pointerProperties.IsLeftButtonPressed)
                WaveFormsGroup_PointerPressed(sender, e);
        }

        private void WaveForm_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            WaveFormHeight = WaveFormLeftChannel.ActualHeight;
            WaveFormWidth = WaveFormsGroup.ActualWidth;

            _offsetPosition = 0;
            // Sets Ratio to show whole audio track
            if (_audioData != null)
                _audioDataToWaveFormRatio =
                    _audioData.LengthSamples() / WaveFormWidth;

            // DrawWaveForm function not called because it slows down significantly
            // It will start after a delay
            _redrawingTimer.Start();
        }
    }
}