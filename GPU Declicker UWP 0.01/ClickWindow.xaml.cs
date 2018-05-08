using System;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=2minMovement42minMovement6

namespace GPU_Declicker_UWP_0._01
{
    /// <summary>
    /// User interface element to show one particular click.
    /// Shows initial (audio input) state and repaired (audio output) samples.
    /// Also shows some technical data as position of the click etc.
    /// Provides user ability to change position and length of corrected arrea
    /// or turn the correction off
    /// </summary>
    public sealed partial class ClickWindow : UserControl
    {
        private AudioClick audioClickBinded;
        private bool IsPointerPressedInTheMidle;
        private Point pointerLastPosition;
        private Point pointPointerPressedInTheMidle;
        private bool IsPointerPressedInTheRightArea;
        private Point pointPointerPressedInTheRightArea;
        private bool IsPointerPressedInTheLeftArea;
        private Point pointPointerPressedInTheLeftArea;

        public ClickWindow(AudioClick audioClick)
        {
            this.InitializeComponent();
            audioClickBinded = audioClick;
            Threshold_level_detected.Text = audioClick.ThresholdLevelDetected.ToString("0.0");
            Position.Text = audioClick.Position.ToString("0");
            SetBorderColour();
            SetPolylines();
        }

        private void SetBorderColour()
        {
            if (audioClickBinded.Aproved)
                Border.Stroke = new SolidColorBrush(Colors.Aqua);
            else
                Border.Stroke = new SolidColorBrush(Colors.Yellow);
        }

        /// <summary>
        /// Forms polylines that show input and output audio samples
        /// </summary>
        private void SetPolylines()
        {
            // clear polylines
            Input.Points.Clear();
            Output.Points.Clear();
            // calculate position in audio track to show click 
            //in the center of this CW
            int cwStartPos = (int)(
                audioClickBinded.Position + 
                audioClickBinded.Length / 2 - 
                this.MainGrid.Width / 2);
            // set Input polylyne
            for (int i = 0; i < this.MainGrid.Width; i++)
            {
                float s = audioClickBinded.GetInputSample(cwStartPos + i);
                double y = 100 * (-s + 1) / 2;
                Input.Points.Add(new Point((double)i, y));
            }
            // set Output polyline two samples wider than click
            for (int i = audioClickBinded.Position - cwStartPos - 1; 
                i <= audioClickBinded.Position - cwStartPos + audioClickBinded.Length + 1; 
                i++)
            {
                float s = audioClickBinded.GetOutputSample(cwStartPos + i);
                double y = 100 * (-s + 1) / 2;
                Output.Points.Add(new Point((double)i, y));
            }
        }

        /// <summary>
        /// Areas of ClickWindow used to detect user actions
        /// </summary>
        enum Area
        {
            LeftExpand, LeftShrink, Midle, RightShrink, RightExpand
        }

        /// <summary>
        /// Event handler processing mouse left button or touch screen 
        /// user actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PointerPressed(
            object sender, 
            PointerRoutedEventArgs e)
        {
            Grid grid = sender as Grid;
            if (grid == null)
                return;
            Area areaPressed = PointerOnWhichArea(grid.ActualWidth, e);
            if (areaPressed == Area.LeftExpand)
            {
                // remember pointer position
                // action will be taken when poiner released
                IsPointerPressedInTheLeftArea = true;
                pointerLastPosition = e.GetCurrentPoint(this).Position;
                pointPointerPressedInTheLeftArea = pointerLastPosition;
            }
            if (areaPressed == Area.LeftShrink)
            {
                // shrink marked damaged sample sequence on left
                audioClickBinded.ShrinkLeft();
                Threshold_level_detected.Text = audioClickBinded.ThresholdLevelDetected.ToString("0.0");
                SetPolylines();
            }
            if (areaPressed == Area.Midle)
            {
                // remember pointer position
                // action will be taken when poiner released
                IsPointerPressedInTheMidle = true;
                pointerLastPosition = e.GetCurrentPoint(this).Position;
                pointPointerPressedInTheMidle = pointerLastPosition;
            }
            if (areaPressed == Area.RightShrink)
            {
                // shrink marked damaged sample sequence on right
                audioClickBinded.ShrinkRight();
                Threshold_level_detected.Text = audioClickBinded.ThresholdLevelDetected.ToString("0.0");
                SetPolylines();
            }
            if (areaPressed == Area.RightExpand)
            {
                // remember pointer position
                // action will be taken when poiner released
                IsPointerPressedInTheRightArea = true;
                pointerLastPosition = e.GetCurrentPoint(this).Position;
                pointPointerPressedInTheRightArea = pointerLastPosition;
            }
        }

        private Area PointerOnWhichArea(double width, PointerRoutedEventArgs e)
        {
            Point point = e.GetCurrentPoint(this).Position;
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
            Point point = e.GetCurrentPoint(this).Position;

            if (IsPointerPressedInTheLeftArea ||
                IsPointerPressedInTheMidle ||
                IsPointerPressedInTheRightArea)
            {
                GestProcessing(point);
            }

            Grid grid = sender as Grid;
            if (grid == null)
                return;
            Area areaNavigated = PointerOnWhichArea(grid.ActualWidth, e);
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
            int minMovement = 3;
            bool changed = false;

            if (IsPointerPressedInTheMidle)
            {
                if (point.X - pointerLastPosition.X > minMovement)
                {
                    // expand marked damaged sample sequence to right
                    audioClickBinded.ExpandRight();
                    Threshold_level_detected.Text = audioClickBinded.ThresholdLevelDetected.ToString("0.0");
                    changed = true;
                }
                if (point.X - pointerLastPosition.X < -minMovement)
                {
                    // expand marked damaged sample sequence to right
                    audioClickBinded.ExpandLeft();
                    Threshold_level_detected.Text = audioClickBinded.ThresholdLevelDetected.ToString("0.0");
                    changed = true;
                }
            }

            if (IsPointerPressedInTheRightArea && point.X - pointerLastPosition.X < -minMovement)
            {
                // shrink marked damaged sample sequence on right
                audioClickBinded.ShrinkRight();
                Threshold_level_detected.Text = audioClickBinded.ThresholdLevelDetected.ToString("0.0");
                changed = true;
            }

            if (IsPointerPressedInTheLeftArea && point.X - pointerLastPosition.X > minMovement)
            {
                // shrink marked damaged sample sequence on right
                audioClickBinded.ShrinkLeft();
                Threshold_level_detected.Text = audioClickBinded.ThresholdLevelDetected.ToString("0.0");
                changed = true;
            }

            if (changed)
            {
                SetPolylines();
                pointerLastPosition = point;
            }
        }

        internal double GetMainGridWidth() => MainGrid.Width;

        internal double GetMainGridHeight() => MainGrid.Height;

        internal void SetMargin(double cwOffsetX, double cwOffsetY)
        {
            Thickness margin = Margin;
            margin.Left = cwOffsetX;
            margin.Top = cwOffsetY;
            Margin = margin;
        }

        private void Grid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ActionNotification.Text = "";
            // if we are modifying click lenght then fix changes
            if (IsPointerPressedInTheLeftArea ||
                IsPointerPressedInTheMidle ||
                IsPointerPressedInTheRightArea)
            {
                Grid_PointerReleased(sender, e);
            }
        }

        /// <summary>
        /// Event handler processing mouse left button or touch screen 
        /// user actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Point point = e.GetCurrentPoint(this).Position;

            if (IsPointerPressedInTheMidle &&
                Math.Abs(point.X - pointPointerPressedInTheMidle.X) < 1 &&
                Math.Abs(point.Y - pointPointerPressedInTheMidle.Y) < 1)
            {
                // change Aproved property of click
                audioClickBinded.ChangeAproved();
                SetBorderColour();
            }

            if (IsPointerPressedInTheRightArea &&
                Math.Abs(point.X - pointPointerPressedInTheRightArea.X) < 1 &&
                Math.Abs(point.Y - pointPointerPressedInTheRightArea.Y) < 1)
            {
                // expand marked damaged sample sequence to right
                audioClickBinded.ExpandRight();
                Threshold_level_detected.Text = audioClickBinded.ThresholdLevelDetected.ToString("0.0");
                SetPolylines();
            }

            if (IsPointerPressedInTheLeftArea &&
                Math.Abs(point.X - pointPointerPressedInTheLeftArea.X) < 1 &&
                Math.Abs(point.Y - pointPointerPressedInTheLeftArea.Y) < 1)
            {
                // expand marked damaged sample sequence to right
                audioClickBinded.ExpandLeft();
                Threshold_level_detected.Text = audioClickBinded.ThresholdLevelDetected.ToString("0.0");
                SetPolylines();
            }

            IsPointerPressedInTheMidle = false;
            IsPointerPressedInTheRightArea = false;
            IsPointerPressedInTheLeftArea = false;
        }
    }
}
