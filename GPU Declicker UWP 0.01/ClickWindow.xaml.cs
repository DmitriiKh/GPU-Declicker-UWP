using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=2minMovement42minMovement6

namespace GPU_Declicker_UWP_0._01
{
    public sealed partial class ClickWindow : UserControl
    {
        AudioClick audioClickBinded;
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
            Threshold_level_detected.Text = audioClick.Threshold_level_detected.ToString("00.0");
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

        private void SetPolylines()
        {
            // clear polylines
            Input.Points.Clear();
            Output.Points.Clear();
            // calculate position in audio track to show click in this CW
            int cwStartPos = (int)(
                audioClickBinded.Pos + 
                audioClickBinded.Len / 2 - 
                MainGrid.Width / 2);
            // set Input polylyne
            for (int i = 0; i < MainGrid.Width; i++)
            {
                float s = audioClickBinded.GetInputSample(cwStartPos + i);
                double y = 100 * (-s + 1) / 2;
                Input.Points.Add(new Point((double)i, y));
            }
            // set Output polyline
            for (int i = audioClickBinded.Pos - cwStartPos - 5; 
                i < audioClickBinded.Pos - cwStartPos + audioClickBinded.Len + 5; 
                i++)
            {
                float s = audioClickBinded.GetOutputSample(cwStartPos + i);
                double y = 100 * (-s + 1) / 2;
                Output.Points.Add(new Point((double)i, y));
            }
        }

        enum Area
        {
            LeftExpand, LeftShrink, Midle, RightShrink, RightExpand
        }

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
                // expand marked damaged sample sequence to left
                audioClickBinded.ExpandLeft();
                SetPolylines();
            }
            if (areaPressed == Area.LeftShrink)
            {
                // shrink marked damaged sample sequence on left
                audioClickBinded.ShrinkLeft();
                SetPolylines();
            }
            if (areaPressed == Area.Midle)
            {
                // remember pointer position
                IsPointerPressedInTheMidle = true;
                pointerLastPosition = e.GetCurrentPoint(this).Position;
                pointPointerPressedInTheMidle = pointerLastPosition;
            }
            if (areaPressed == Area.RightShrink)
            {
                // shrink marked damaged sample sequence on right
                audioClickBinded.ShrinkRight();
                SetPolylines();
            }
            if (areaPressed == Area.RightExpand)
            {
                // remember pointer position
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
                    changed = true;
                }
                if (point.X - pointerLastPosition.X < -minMovement)
                {
                    // expand marked damaged sample sequence to right
                    audioClickBinded.ExpandLeft();
                    changed = true;
                }
            }

            if (IsPointerPressedInTheRightArea)
            {
                if (point.X - pointerLastPosition.X < -minMovement)
                {
                    // shrink marked damaged sample sequence on right
                    audioClickBinded.ShrinkRight();
                    changed = true;
                }
            }

            if (IsPointerPressedInTheLeftArea)
            {
                if (point.X - pointerLastPosition.X > minMovement)
                {
                    // shrink marked damaged sample sequence on right
                    audioClickBinded.ShrinkLeft();
                    changed = true;
                }
            }

            if (changed)
            {
                SetPolylines();
                pointerLastPosition = point;
            }
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
                SetPolylines();
            }

            if (IsPointerPressedInTheLeftArea &&
                Math.Abs(point.X - pointPointerPressedInTheLeftArea.X) < 1 &&
                Math.Abs(point.Y - pointPointerPressedInTheLeftArea.Y) < 1)
            {
                // expand marked damaged sample sequence to right
                audioClickBinded.ExpandLeft();
                SetPolylines();
            }

            IsPointerPressedInTheMidle = false;
            IsPointerPressedInTheRightArea = false;
            IsPointerPressedInTheLeftArea = false;
        }
    }
}
