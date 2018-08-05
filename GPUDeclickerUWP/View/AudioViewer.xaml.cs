using GPUDeclickerUWP.Model.Data;
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
        // to make resizing faster
        private readonly DispatcherTimer _redrawingTimer;

        // audio samples to view
        public AudioData AudioData
        {
            private get { return (AudioData) GetValue(AudioDataProperty); }

            set => SetValue(AudioDataProperty, value);
        }

        public static readonly DependencyProperty AudioDataProperty =
            DependencyProperty.Register(
                "AudioData", typeof(AudioData),
                typeof(AudioViewer),
                new PropertyMetadata(null, AudioDataPropertyCallBack));

        private static void AudioDataPropertyCallBack(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var audioViewer = d as AudioViewer;
            audioViewer?.Fill();
        }

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
        /// sets Ratio and OffsetPosition
        /// </summary>
        public void Fill() 
        {
            _offsetPosition = 0;

            // Sets Ratio to show whole audio track
            _audioDataToWaveFormRatio =
                AudioData.LengthSamples() / WaveFormWidth;

            DrawWaveForm();
        }

        /// <summary>
        ///     move OffsetPositionX to the right for one waveForm length
        /// </summary>
        private void GoNextBigStep()
        {
            if (AudioData == null)
                return;

            var deltaX = (int) WaveFormWidth;
            GoNextX(deltaX);
        }

        // move OffsetPositionX to the right for one tenth of waveForm length
        private void GoNextSmalStep()
        {
            if (AudioData == null)
                return;

            var deltaX = (int) WaveFormWidth / 10;
            GoNextX(deltaX);
        }

        // move OffsetPositionX to the right for shiftX samples
        private void GoNextX(int deltaX)
        {
            if (AudioData == null)
                return;

            // Calculate number of samples to shift
            var shift = (int) (deltaX * _audioDataToWaveFormRatio);
            // Calculate number of samples that waveForms show
            var samplesOnScrean = (int) (
                WaveFormWidth
                * _audioDataToWaveFormRatio
            );
            // if there is enough room on the right than shift offsetPosition
            if (_offsetPosition + shift + samplesOnScrean < AudioData.LengthSamples())
            {
                _offsetPosition += shift;
            }
            else
            {
                // set OffsetPosition to show the end of audioData
                _offsetPosition = AudioData.LengthSamples() - samplesOnScrean;
                if (_offsetPosition < 0)
                    _offsetPosition = 0;
            }

            DrawWaveForm();
        }

        // move OffsetPositionX to the right for one waveForm length 
        private void GoPrevBigStep()
        {
            if (AudioData == null)
                return;

            var deltaX = (int) WaveFormWidth;
            GoPrevX(deltaX);
        }

        // move OffsetPositionX to the right for one tenth of waveForm length 
        private void GoPrevSmalStep()
        {
            if (AudioData == null)
                return;

            var deltaX = (int) WaveFormWidth / 10;
            GoPrevX(deltaX);
        }

        // move OffsetPositionX to the right for shiftX samples 
        private void GoPrevX(int x)
        {
            if (AudioData == null)
                return;

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
            if (AudioData == null)
                return;

            LeftChannelWaveFormPoints.Clear();
            RightChannelWaveFormPoints.Clear();

            var audioData = AudioData;
            // for every x-axis position of waveForm
            for (var x = 0; x < WaveFormWidth; x++)
            {
                audioData.SetCurrentChannelType(ChannelType.Left);
                AddPointToWaveform(LeftChannelWaveFormPoints, x);

                if (!audioData.IsStereo)
                    continue;

                audioData.SetCurrentChannelType(ChannelType.Right);
                AddPointToWaveform(RightChannelWaveFormPoints, x);
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

            if (start < 0 || start + length >= AudioData.LengthSamples())
                return;

            // looks for max and min among many samples represented by a point on wave form
            FindMinMax(start, length, out var min, out var max);

            // connect previous point to a new point
            var y = (int) (-0.5 * WaveFormHeight * max) + offsetY;
            waveFormPoints.Add(new Point(x, y));
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
            int begining,
            int length,
            out float minValue,
            out float maxValue)
        {
            var audioData = AudioData;

            minValue = audioData.GetInputSample(begining);
            maxValue = audioData.GetInputSample(begining);

            for (var index = 0;
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
        ///     Increases detalization on waveForm
        /// </summary>
        private void MagnifyMore()
        {
            if (AudioData == null)
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
        private void MagnifyLess()
        {
            if (AudioData == null)
                return;

            if (WaveFormWidth * _audioDataToWaveFormRatio * 2
                < AudioData.LengthSamples())
                _audioDataToWaveFormRatio *= 2;
            else
                _audioDataToWaveFormRatio =
                    AudioData.LengthSamples() / WaveFormWidth;

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
        private void WaveFormsGroupPointerWheelChanged(object sender, PointerRoutedEventArgs e)
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
        /// left batton pressed
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
            if (AudioData != null)
                _audioDataToWaveFormRatio =
                    AudioData.LengthSamples() / WaveFormWidth;

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