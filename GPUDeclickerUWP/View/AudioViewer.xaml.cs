using CarefulAudioRepair.Data;
using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace GPUDeclickerUWP.View
{
    public sealed partial class AudioViewer
    {
        // this timer creates a delay between WaveForm size changing and its redrawing
        // to throtle redrawing requests and make UI more reponsive
        private readonly DispatcherTimer _redrawingTimer;

        // audio samples to view
        public IAudio Audio
        {
            private get { return (IAudio) GetValue(AudioProperty); }

            set => SetValue(AudioProperty, value);
        }

        public static readonly DependencyProperty AudioProperty =
            DependencyProperty.Register(
                "Audio",
                typeof(IAudio),
                typeof(AudioViewer),
                new PropertyMetadata(null, AudioPropertyCallBack));

        private static void AudioPropertyCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var audioViewer = d as AudioViewer;

            var audio = audioViewer.Audio;
            _leftCnannelSamples = audio.GetInputRange(ChannelType.Left, 0, audio.LengthSamples - 1);

            if (audio.IsStereo)
            {
                _rightCnannelSamples = audio.GetInputRange(ChannelType.Right, 0, audio.LengthSamples - 1);
            }

            audioViewer?.Fill();
        }

        // magnification ratio
        // when set to 1, waveForm is most detailed
        // when set to R, waveForm drops each R-1 from R audioData samples
        private double _audioToWaveFormRatio = 1d;

        // offset from beginning of audioData to beginning waveForm
        private int _offsetPosition;
        private static double[] _leftCnannelSamples;
        private static double[] _rightCnannelSamples;

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

        // True when user slides waveForms
        private bool IsMovingByMouse { get; set; }

        private void _redrawingTimer_Tick(object sender, object e)
        {
            _redrawingTimer.Stop();
            DrawWaveForm();
        }

        /// <summary>
        /// sets Ratio and OffsetPosition
        /// </summary>
        private void Fill() 
        {
            _offsetPosition = 0;

            // Sets Ratio to show whole audio track
            _audioToWaveFormRatio =
                Audio.LengthSamples / WaveFormWidth;

            DrawWaveForm();
        }

        /// <summary>
        ///     move OffsetPositionX to the right for one waveForm length
        /// </summary>
        private void GoNextBigStep()
        {
            if (Audio == null)
                return;

            var deltaX = (int) WaveFormWidth;
            GoNextX(deltaX);
        }

        // move OffsetPositionX to the right for one tenth of waveForm length
        private void GoNextSmalStep()
        {
            if (Audio == null)
                return;

            var deltaX = (int) WaveFormWidth / 10;
            GoNextX(deltaX);
        }

        // move OffsetPositionX to the right for shiftX samples
        private void GoNextX(int deltaX)
        {
            if (Audio == null)
                return;

            // Calculate number of samples to shift
            var shift = (int) (deltaX * _audioToWaveFormRatio);
            // Calculate number of samples that waveForms show
            var samplesOnScrean = (int) (
                WaveFormWidth
                * _audioToWaveFormRatio
            );
            // if there is enough room on the right than shift offsetPosition
            if (_offsetPosition + shift + samplesOnScrean < Audio.LengthSamples)
            {
                _offsetPosition += shift;
            }
            else
            {
                // set OffsetPosition to show the end of audioData
                _offsetPosition = Audio.LengthSamples - samplesOnScrean;
                if (_offsetPosition < 0)
                    _offsetPosition = 0;
            }

            DrawWaveForm();
        }

        // move OffsetPositionX to the right for one waveForm length 
        private void GoPrevBigStep()
        {
            if (Audio == null)
                return;

            var deltaX = (int) WaveFormWidth;
            GoPrevX(deltaX);
        }

        // move OffsetPositionX to the right for one tenth of waveForm length 
        private void GoPrevSmalStep()
        {
            if (Audio == null)
                return;

            var deltaX = (int) WaveFormWidth / 10;
            GoPrevX(deltaX);
        }

        // move OffsetPositionX to the right for shiftX samples 
        private void GoPrevX(int x)
        {
            if (Audio == null)
                return;

            // Calculate number of samples to shift
            var shift = (int) (x * _audioToWaveFormRatio);
            // if there is enough room on the left than shift offsetPositionX
            if (_offsetPosition > shift) _offsetPosition -= shift;
            else
                // set OffsetPositionX to show the beginning of audioData
                _offsetPosition = 0;

            DrawWaveForm();
        }

        /// <summary>
        ///     sets points for PolyLine showing wave form
        /// </summary>
        private void DrawWaveForm()
        {
            if (Audio == null)
                return;

            LeftChannelWaveFormPoints.Clear();
            RightChannelWaveFormPoints.Clear();

            // for every x-axis position of waveForm
            for (var x = 0; x < WaveFormWidth; x++)
            {
                AddPointToWaveform(_leftCnannelSamples, LeftChannelWaveFormPoints, x);

                if (Audio.IsStereo)
                {
                    AddPointToWaveform(_rightCnannelSamples, RightChannelWaveFormPoints, x);
                }
            }
        }

        /// <summary>
        ///     Adds a point representing one or many samples to wave form
        /// </summary>
        private void AddPointToWaveform(double[] samples, PointCollection waveFormPoints, int x)
        {
            var offsetY = (int) WaveFormHeight / 2;
            var start = _offsetPosition + (int) (x * _audioToWaveFormRatio);
            var length = (int) _audioToWaveFormRatio;

            if (start < 0 || start + length >= Audio.LengthSamples)
                return;

            // looks for max and min among many samples represented by a point on wave form
            FindMinMax(samples, start, length, out var min, out var max);

            // connect previous point to a new point
            var y = (int) (-0.5 * WaveFormHeight * max) + offsetY;
            waveFormPoints.Add(new Point(x, y));

            // if min and max are close enough
            if (!(Math.Abs(min - max) > 0.01))
                return;

            // form vertical line connecting max and min
            y = (int) (-0.5 * WaveFormHeight * min) + offsetY;
            waveFormPoints.Add(new Point(x, y));
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
            double[] samples,
            int begining,
            int length,
            out double minValue,
            out double maxValue)
        {
            minValue = samples[begining];
            maxValue = minValue;

            for (var index = 0;
                index < length && begining + index < samples.Length;
                index++)
            {
                var sample = samples[begining + index];

                if (sample < minValue)
                    minValue = sample;

                if (sample > maxValue)
                    maxValue = sample;
            }
        }

        /// <summary>
        ///     Increases detalization on waveForm
        /// </summary>
        private void MagnifyMore()
        {
            if (Audio == null)
                return;

            _audioToWaveFormRatio /= 2;

            if (_audioToWaveFormRatio < 1)
                _audioToWaveFormRatio = 1d;

            DrawWaveForm();
        }

        /// <summary>
        ///     Decreases detalization on waveForm
        /// </summary>
        private void MagnifyLess()
        {
            if (Audio == null)
                return;

            _audioToWaveFormRatio *= 2;

            var maxRatio = Audio.LengthSamples / WaveFormWidth;

            if (_audioToWaveFormRatio > maxRatio)
                _audioToWaveFormRatio = maxRatio;

            AdjustOffsetIfNeeded();

            DrawWaveForm();
        }

        private void AdjustOffsetIfNeeded()
        {
            if (_offsetPosition < 0)
                _offsetPosition = 0;

            var waveFormWidthSamples = (int)(WaveFormWidth * _audioToWaveFormRatio);

            var samplesAfterOffset = Audio.LengthSamples - _offsetPosition;

            if (waveFormWidthSamples > samplesAfterOffset)
                _offsetPosition = Audio.LengthSamples - waveFormWidthSamples;
        }

        /// <summary>
        ///     Returns offset in samples for pointer position
        /// </summary>
        /// <param name="pointerPosition"> pointer position on waveForm</param>
        private int PointerOffsetPosition(double pointerPositionX) =>
            (int)(pointerPositionX * _audioToWaveFormRatio) + _offsetPosition;

        /// <summary>
        ///     Adjusts _offsetPosition to make pointer stay on the same sample.
        /// </summary>
        /// <param name="offsetAtPointer"> offset for pointer position</param>
        /// <param name="pointerPositionX"> X of pointer position on waveForm</param>
        private void SetOffsetForPointer(int offsetAtPointer, double pointerPositionX)
        {
            var samplesFromWaveFormBeginning = (int)(pointerPositionX * _audioToWaveFormRatio);

            _offsetPosition = offsetAtPointer - samplesFromWaveFormBeginning;

            if (_offsetPosition < 0)
                _offsetPosition = 0;

            DrawWaveForm();
        }

        /// <summary>
        ///     MouseWheel events handler for wave forms
        ///     Magnification adjustment
        /// </summary>
        private void WaveFormsGroupPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            // Prevent most handlers along the event route from handling the same event again.
            e.Handled = true;

            var pointer = e.GetCurrentPoint(WaveFormsGroup);

            // calculates offset for samples at pointer
            var offsetAtPointer = PointerOffsetPosition(pointer.Position.X);

            if (pointer.Properties.MouseWheelDelta > 0)
                MagnifyMore();
            else
                MagnifyLess();

            // set pointer at the same position
            SetOffsetForPointer(offsetAtPointer, pointer.Position.X);
        }

        /// <summary>
        /// Sets the horizontal position of the pointer and IsMovingByMouse
        /// state when the pointer pressed on AudioViewer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveFormsGroupPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            PointerLastPosition = pointer.Position;
            IsMovingByMouse = true;
        }

        /// <summary>
        /// Sets IsMovingByMouse to false when pointer released
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveFormsGroupPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            IsMovingByMouse = false;
        }

        /// <summary>
        /// Shifts wave form when pointer moved with left button pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveFormsGroupPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!IsMovingByMouse)
                return;

            var pointer = e.GetCurrentPoint(this);
            var shiftX = (int) (PointerLastPosition.X - pointer.Position.X);
            if (shiftX > 0)
                GoNextX(Math.Abs(shiftX));
            else
                GoPrevX(Math.Abs(shiftX));

            PointerLastPosition = pointer.Position;
        }

        /// <summary>
        /// Releases pointer when it moves out of wave form
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveFormsGroupPointerExited(object sender, PointerRoutedEventArgs e)
        {
            WaveFormsGroupPointerReleased(sender, e);
            IsMovingByMouse = false;
        }

        /// <summary>
        /// Imitates pressing left button at enter position
        /// when pointer moves into wave form zone with
        /// left button pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveFormsGroupPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            var pointer = e.GetCurrentPoint(this);
            var pointerProperties = pointer.Properties;
            if (pointerProperties.IsLeftButtonPressed)
                WaveFormsGroupPointerPressed(sender, e);
        }

        /// <summary>
        /// Changes wave form size variables and _audioDataToWaveFormRatio.
        /// Also starts _redrawingTimer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveFormSizeChanged(object sender, SizeChangedEventArgs e)
        {
            WaveFormHeight = WaveFormLeftChannel.ActualHeight;
            WaveFormWidth = WaveFormsGroup.ActualWidth;

            _offsetPosition = 0;
            // Sets Ratio to show whole audio track
            if (Audio != null)
                _audioToWaveFormRatio =
                    Audio.LengthSamples / WaveFormWidth;

            // DrawWaveForm function not called directly because it slows down 
            // It will start after a delay
            _redrawingTimer.Start();
        }

        private void GoLeftBigStepClick(object sender, RoutedEventArgs e)
        {
            GoPrevBigStep();
        }

        private void GoLeftSmallStepClick(object sender, RoutedEventArgs e)
        {
            GoPrevSmalStep();
        }

        private void GoRightBigStepClick(object sender, RoutedEventArgs e)
        {
            GoNextBigStep();
        }

        private void GoRightSmallStepClick(object sender, RoutedEventArgs e)
        {
            GoNextSmalStep();
        }

        private void MagnifyLessClick(object sender, RoutedEventArgs e)
        {
            MagnifyLess();
        }

        private void MagnifyMoreClick(object sender, RoutedEventArgs e)
        {
            MagnifyMore();
        }
    }
}