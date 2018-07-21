using System;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=2minMovement42minMovement6

namespace GPUDeclickerUWP
{
    /// <summary>
    ///     User interface element to show one particular click.
    ///     Shows initial (audio input) state and repaired (audio output) samples.
    ///     Also shows some technical data as position of the click etc.
    ///     Provides user ability to change position and length of corrected arrea
    ///     or turn the correction off
    /// </summary>
    public sealed partial class ClickWindow
    {
        private readonly AudioClick _audioClickBinded;
        private bool _isPointerPressedInTheLeftArea;
        private bool _isPointerPressedInTheMidle;
        private bool _isPointerPressedInTheRightArea;
        private Point _pointerLastPosition;
        private Point _pointPointerPressedInTheLeftArea;
        private Point _pointPointerPressedInTheMidle;
        private Point _pointPointerPressedInTheRightArea;

        public ClickWindow(AudioClick audioClick)
        {
            InitializeComponent();
            _audioClickBinded = audioClick;
            ThresholdLevelDetected.Text = 
                audioClick.ErrorLevelDetected.ToString("0.0");
            Position.Text = audioClick.Position.ToString("0");
            SetBorderColour();
            SetPolylines();
        }

        private void SetBorderColour()
        {
            Border.Stroke = _audioClickBinded.Aproved ? 
                new SolidColorBrush(Colors.Aqua) : new SolidColorBrush(Colors.Yellow);
        }

        /// <summary>
        ///     Forms polylines that show input and output audio samples
        /// </summary>
        private void SetPolylines()
        {
            if (Input.Points is null || Output.Points is null)
                return;

            // clear polylines
            Input.Points.Clear();
            Output.Points.Clear();
            // calculate position in audio track to show click 
            //in the center of this CW
            var cwStartPos = (int) (
                _audioClickBinded.Position +
                _audioClickBinded.Length / 2 -
                MainGrid.Width / 2);
            // set Input polylyne
            for (var i = 0; i < MainGrid.Width; i++)
            {
                var s = _audioClickBinded.GetInputSample(cwStartPos + i);
                double y = 100 * (-s + 1) / 2;
                Input.Points.Add(new Point(i, y));
            }

            // set Output polyline two samples wider than click
            for (var i = _audioClickBinded.Position - cwStartPos - 1;
                i <= _audioClickBinded.Position - cwStartPos + _audioClickBinded.Length + 1;
                i++)
            {
                var s = _audioClickBinded.GetOutputSample(cwStartPos + i);
                double y = 100 * (-s + 1) / 2;
                Output.Points.Add(new Point(i, y));
            }
        }

        /// <summary>
        ///     Event handler processing mouse left button or touch screen
        ///     user actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PointerPressed(
            object sender,
            PointerRoutedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid == null)
                return;
            var areaPressed = PointerOnWhichArea(grid.ActualWidth, e);
            if (areaPressed == Area.LeftExpand)
            {
                // remember pointer position
                // action will be taken when poiner released
                _isPointerPressedInTheLeftArea = true;
                _pointerLastPosition = e.GetCurrentPoint(this).Position;
                _pointPointerPressedInTheLeftArea = _pointerLastPosition;
            }

            if (areaPressed == Area.LeftShrink)
            {
                // shrink marked damaged sample sequence on left
                _audioClickBinded.ShrinkLeft();
                ThresholdLevelDetected.Text = _audioClickBinded.ErrorLevelDetected.ToString("0.0");
                SetPolylines();
            }

            if (areaPressed == Area.Midle)
            {
                // remember pointer position
                // action will be taken when poiner released
                _isPointerPressedInTheMidle = true;
                _pointerLastPosition = e.GetCurrentPoint(this).Position;
                _pointPointerPressedInTheMidle = _pointerLastPosition;
            }

            if (areaPressed == Area.RightShrink)
            {
                // shrink marked damaged sample sequence on right
                _audioClickBinded.ShrinkRight();
                ThresholdLevelDetected.Text = _audioClickBinded.ErrorLevelDetected.ToString("0.0");
                SetPolylines();
            }

            if (areaPressed == Area.RightExpand)
            {
                // remember pointer position
                // action will be taken when poiner released
                _isPointerPressedInTheRightArea = true;
                _pointerLastPosition = e.GetCurrentPoint(this).Position;
                _pointPointerPressedInTheRightArea = _pointerLastPosition;
            }
        }

        private Area PointerOnWhichArea(double width, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(this).Position;
            if (point.X < width / 6)
                return Area.LeftExpand;
            if (point.X >= width / 6 && point.X < width / 3)
                return Area.LeftShrink;
            if (point.X >= 2 * width / 3 && point.X < 5 * width / 6)
                return Area.RightShrink;
            if (point.X >= 5 * width / 6)
                return Area.RightExpand;

            return Area.Midle;
        }

        private void Grid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(this).Position;

            if (_isPointerPressedInTheLeftArea ||
                _isPointerPressedInTheMidle ||
                _isPointerPressedInTheRightArea)
                GestProcessing(point);

            if (!(sender is Grid grid))
                return;
            var areaNavigated = PointerOnWhichArea(grid.ActualWidth, e);
            if (areaNavigated == Area.LeftExpand)
            {
                // show left arrow in LeftExpand area
                ActionNotification.Text = "\u21A4";
                ActionNotification.HorizontalAlignment = HorizontalAlignment.Left;
            }

            if (areaNavigated == Area.LeftShrink)
            {
                // show right arrow in LeftShrink area
                ActionNotification.Text = "\u21A6";
                ActionNotification.HorizontalAlignment = HorizontalAlignment.Left;
            }

            if (areaNavigated == Area.Midle)
            {
                // show on/off in Midle area
                ActionNotification.Text = "On/Off";
                ActionNotification.HorizontalAlignment = HorizontalAlignment.Center;
            }

            if (areaNavigated == Area.RightShrink)
            {
                // show left arrow in RightShrink area
                ActionNotification.Text = "\u21A4";
                ActionNotification.HorizontalAlignment = HorizontalAlignment.Right;
            }

            if (areaNavigated == Area.RightExpand)
            {
                // show right arrow in RightExpand area
                ActionNotification.Text = "\u21A6";
                ActionNotification.HorizontalAlignment = HorizontalAlignment.Right;
            }
        }

        private void GestProcessing(Point point)
        {
            var minMovement = 3;
            var changed = false;

            if (_isPointerPressedInTheMidle)
            {
                if (point.X - _pointerLastPosition.X > minMovement)
                {
                    // expand marked damaged sample sequence to right
                    _audioClickBinded.ExpandRight();
                    ThresholdLevelDetected.Text = _audioClickBinded.ErrorLevelDetected.ToString("0.0");
                    changed = true;
                }

                if (point.X - _pointerLastPosition.X < -minMovement)
                {
                    // expand marked damaged sample sequence to right
                    _audioClickBinded.ExpandLeft();
                    ThresholdLevelDetected.Text = _audioClickBinded.ErrorLevelDetected.ToString("0.0");
                    changed = true;
                }
            }

            if (_isPointerPressedInTheRightArea && point.X - _pointerLastPosition.X < -minMovement)
            {
                // shrink marked damaged sample sequence on right
                _audioClickBinded.ShrinkRight();
                ThresholdLevelDetected.Text = _audioClickBinded.ErrorLevelDetected.ToString("0.0");
                changed = true;
            }

            if (_isPointerPressedInTheLeftArea && point.X - _pointerLastPosition.X > minMovement)
            {
                // shrink marked damaged sample sequence on right
                _audioClickBinded.ShrinkLeft();
                ThresholdLevelDetected.Text = _audioClickBinded.ErrorLevelDetected.ToString("0.0");
                changed = true;
            }

            if (changed)
            {
                SetPolylines();
                _pointerLastPosition = point;
            }
        }

        internal double GetMainGridWidth()
        {
            return MainGrid.Width;
        }

        internal double GetMainGridHeight()
        {
            return MainGrid.Height;
        }

        internal void SetMargin(double cwOffsetX, double cwOffsetY)
        {
            var margin = Margin;
            margin.Left = cwOffsetX;
            margin.Top = cwOffsetY;
            Margin = margin;
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ActionNotification.Text = "";
            // if we are modifying click lenght then fix changes
            if (_isPointerPressedInTheLeftArea ||
                _isPointerPressedInTheMidle ||
                _isPointerPressedInTheRightArea)
                Grid_PointerReleased(sender, e);
        }

        /// <summary>
        ///     Event handler processing mouse left button or touch screen
        ///     user actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(this).Position;

            if (_isPointerPressedInTheMidle &&
                Math.Abs(point.X - _pointPointerPressedInTheMidle.X) < 1 &&
                Math.Abs(point.Y - _pointPointerPressedInTheMidle.Y) < 1)
            {
                // change Aproved property of click
                _audioClickBinded.ChangeAproved();
                SetBorderColour();
            }

            if (_isPointerPressedInTheRightArea &&
                Math.Abs(point.X - _pointPointerPressedInTheRightArea.X) < 1 &&
                Math.Abs(point.Y - _pointPointerPressedInTheRightArea.Y) < 1)
            {
                // expand marked damaged sample sequence to right
                _audioClickBinded.ExpandRight();
                ThresholdLevelDetected.Text = _audioClickBinded.ErrorLevelDetected.ToString("0.0");
                SetPolylines();
            }

            if (_isPointerPressedInTheLeftArea &&
                Math.Abs(point.X - _pointPointerPressedInTheLeftArea.X) < 1 &&
                Math.Abs(point.Y - _pointPointerPressedInTheLeftArea.Y) < 1)
            {
                // expand marked damaged sample sequence to right
                _audioClickBinded.ExpandLeft();
                ThresholdLevelDetected.Text = _audioClickBinded.ErrorLevelDetected.ToString("0.0");
                SetPolylines();
            }

            _isPointerPressedInTheMidle = false;
            _isPointerPressedInTheRightArea = false;
            _isPointerPressedInTheLeftArea = false;
        }

        /// <summary>
        ///     Areas of ClickWindow used to detect user actions
        /// </summary>
        private enum Area
        {
            LeftExpand,
            LeftShrink,
            Midle,
            RightShrink,
            RightExpand
        }
    }
}