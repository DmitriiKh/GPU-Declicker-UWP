using GPUDeclickerUWP.ViewModel;
using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace GPUDeclickerUWP.View
{
    public sealed partial class AudioViewer
    {
        // this timer creates a delay between WaveForm size changing and its redrawing
        // to throtle redrawing requests and make UI more reponsive
        private readonly DispatcherTimer _redrawingTimer;

        public AudioViewerViewModel ViewModel
        {
            get { return (AudioViewerViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(
                "ViewModel",
                typeof(AudioViewerViewModel),
                typeof(AudioViewer),
                new PropertyMetadata(null));

        public AudioViewer()
        {
            InitializeComponent();
            _redrawingTimer = new DispatcherTimer();
            _redrawingTimer.Tick += _redrawingTimer_Tick;
            _redrawingTimer.Interval = TimeSpan.FromSeconds(0.1); 
        }

        // Last mouse pointer position touching waveForms
        // Used to calculate new OffsetPosition when user slides waveForms
        private Point PointerLastPosition { get; set; }

        // True when user slides waveForms
        private bool IsMovingByMouse { get; set; }

        private void _redrawingTimer_Tick(object sender, object e)
        {
            _redrawingTimer.Stop();
            ViewModel.UpdatePointsCollections();
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
            var offsetAtPointer = ViewModel.PointerOffsetPosition(pointer.Position.X);

            if (pointer.Properties.MouseWheelDelta > 0)
                ViewModel.MagnifyMore();
            else
                ViewModel.MagnifyLess();

            // set pointer at the same position
            ViewModel.SetOffsetForPointer(offsetAtPointer, pointer.Position.X);
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
                ViewModel.GoNextX(Math.Abs(shiftX));
            else
                ViewModel.GoPrevX(Math.Abs(shiftX));

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
            {
                WaveFormsGroupPointerPressed(sender, e);
            }
        }

        /// <summary>
        /// Changes wave form size variables and _audioDataToWaveFormRatio.
        /// Also starts _redrawingTimer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WaveFormSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var width = (int)Math.Ceiling(WaveFormsGroup.ActualWidth);
            var height = (int)Math.Ceiling(WaveFormLeftChannel.ActualHeight);

            ViewModel.UpdateWaveFormSize(width, height);

            // DrawWaveForm function not called directly because it slows down 
            // It will start after a delay
            _redrawingTimer.Start();
        }

        private void GoLeftBigStepClick(object sender, RoutedEventArgs e)
        {
            ViewModel.GoPrevBigStep();
        }

        private void GoLeftSmallStepClick(object sender, RoutedEventArgs e)
        {
            ViewModel.GoPrevSmalStep();
        }

        private void GoRightBigStepClick(object sender, RoutedEventArgs e)
        {
            ViewModel.GoNextBigStep();
        }

        private void GoRightSmallStepClick(object sender, RoutedEventArgs e)
        {
            ViewModel.GoNextSmalStep();
        }

        private void MagnifyLessClick(object sender, RoutedEventArgs e)
        {
            ViewModel.MagnifyLess();
        }

        private void MagnifyMoreClick(object sender, RoutedEventArgs e)
        {
            ViewModel.MagnifyMore();
        }
    }
}