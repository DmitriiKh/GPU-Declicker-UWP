using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media.Audio;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Input;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace GPU_Declicker_UWP_0._01
{
    public sealed partial class MainPage : Page
    {
        private StorageFile mediaInputFile;
        private StorageFile mediaOutputFile;

        AudioInputOutput audioInputOutput;

        // variables for subclasses to report progress and status
        Progress<double> taskProgress;
        Progress<string> taskStatus;
        

        public MainPage()
        {
            this.InitializeComponent();
            
            audioInputOutput = new AudioInputOutput();

            // variables for subclasses to report progress and status
            taskProgress = new Progress<double>(
                (p) => { ProgressBar.Value = p; }
                );
            taskStatus = new Progress<string>(
                (s) => { Status.Text = s; }
                );
        }
    
        private async void OpenAudioFile(object sender, RoutedEventArgs e)
        {
            CreateAudioGraphResult result1 = 
                await audioInputOutput.Init(taskProgress);
            
            if (result1.Status != AudioGraphCreationStatus.Success)
            {
                ShowErrorMessage(
                    "AudioGraph creation error: " 
                    + result1.Status.ToString()
                    );
            }

            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            filePicker.FileTypeFilter.Add(".mp3");
            filePicker.FileTypeFilter.Add(".wav");
            filePicker.FileTypeFilter.Add(".wma");
            filePicker.FileTypeFilter.Add(".m4a");
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            mediaInputFile = await filePicker.PickSingleFileAsync();

            if (mediaInputFile != null)
            {
                CreateAudioFileInputNodeResult result2 = 
                    await audioInputOutput.LoadAudioFromFile(mediaInputFile, taskProgress, taskStatus);

                if (result2.Status != AudioFileNodeCreationStatus.Success)
                    ShowErrorMessage(result2.Status.ToString());
                else
                    audioViewer.Fill(audioInputOutput.GetAudioData());
            }
            else
            {
                return;
            }
            
            // enable Scan button but not Save
            ScanButton.IsEnabled = true;
            SaveButton.IsEnabled = false;
        }

        private async void ShowErrorMessage(string message)
        {
            var dialog = new MessageDialog(message);
            await dialog.ShowAsync();
        }

        private void GoLeftBigStep_Click(object sender, RoutedEventArgs e)
            => audioViewer.GoPrevBigStep();

        private void GoLeftSmallStep_Click(object sender, RoutedEventArgs e) 
            => audioViewer.GoPrevSmalStep();

        private void GoRightBigStep_Click(object sender, RoutedEventArgs e) 
            => audioViewer.GoNextBigStep();

        private void GoRightSmallStep_Click(object sender, RoutedEventArgs e) 
            => audioViewer.GoNextSmalStep();

        private void MagnLess(object sender, RoutedEventArgs e) 
            => audioViewer.MagnifyLess();

        private void MagnMore(object sender, RoutedEventArgs e) 
            => audioViewer.MagnifyMore();
        
        
        private async void Scan_ClickAsync(object sender, RoutedEventArgs e)
        {
            AudioDataClass audioData = audioInputOutput.GetAudioData();
            
            // disable all buttons
            OpenButton.IsEnabled = false;
            ScanButton.IsEnabled = false;
            SaveButton.IsEnabled = false;

            await Task.Run(() => AudioProcessing.ProcessAudioAsync(audioData, 512, 4, 3, taskProgress, taskStatus));

            // enable Save and Open buttons
            OpenButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
            
            DisplayClicks();
        }

        private void DisplayClicks()
        {
            AudioDataClass audioData = audioInputOutput.GetAudioData();
            if (audioData == null)
                return;
            // clear Grid
            ClickWindowsGrid.Children.Clear();
            // set initial offsets
            double cwOffsetX = 0;
            double cwOffsetY = 0;
            
            if (audioData.IsStereo)
            {
                // add text notation on top of the Grid 
                // before inserting clicks
                TextBlock textBlock_LeftChannel = new TextBlock();
                textBlock_LeftChannel.Text = "Left Channel Clicks";
                ClickWindowsGrid.Children.Add(textBlock_LeftChannel);
                cwOffsetY += textBlock_LeftChannel.FontSize * 2;

                // insert left channel clicks
                audioData.CurrentChannel = Channel.Left;
                DisplayClicks_ForChannel(audioData, ref cwOffsetX, ref cwOffsetY);

                // add text notation to the Grid
                cwOffsetX = 0;
                TextBlock textBlock_RightChannel = new TextBlock();
                textBlock_RightChannel.Text = "Right Channel Clicks";
                Thickness margin = textBlock_RightChannel.Margin;
                margin.Left = 0;
                margin.Top = cwOffsetY;
                textBlock_RightChannel.Margin = margin;
                ClickWindowsGrid.Children.Add(textBlock_RightChannel);
                cwOffsetY += textBlock_RightChannel.FontSize * 2;

                // insert right channel clicks
                audioData.CurrentChannel = Channel.Right;
                DisplayClicks_ForChannel(audioData, ref cwOffsetX, ref cwOffsetY);
            }
            else
            {
                // insert clicks
                DisplayClicks_ForChannel(audioData, ref cwOffsetX, ref cwOffsetY);
            }
        }

        private void DisplayClicks_ForChannel(AudioDataClass audioData, ref double cwOffsetX, ref double cwOffsetY)
        {
            // for every click in channel
            for (int k = 0; k < audioData.GetNumberOfClicks(); k++)
            {
                // make new ClickWindow for a click
                AudioClick c = audioData.GetClick(k);
                ClickWindow cw = new ClickWindow(c);

                // set ClickWindow margin
                Thickness margin = cw.Margin;
                margin.Left = cwOffsetX;
                margin.Top = cwOffsetY;
                cw.Margin = margin;

                // insert the ClickWindow to the Grid
                ClickWindowsGrid.Children.Add(cw);
                
                // set offsets for new ClickWindow
                if (cwOffsetX + 210 + 200 < ClickWindowsGrid.ActualWidth)
                {
                    cwOffsetX += 210;
                }
                else
                {
                    cwOffsetX = 0;
                    cwOffsetY += 110;
                }
            }
            // set cwOffsetY for new row
            cwOffsetY += 110;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            audioViewer.AudioViewerSizeChanged();
            DisplayClicks();
        }

        private async void SaveAudioFile(object sender, RoutedEventArgs e)
        {
            CreateAudioGraphResult result1 =
                await audioInputOutput.Init(taskProgress);

            if (result1.Status != AudioGraphCreationStatus.Success)
            {
                ShowErrorMessage(
                    "AudioGraph creation error: "
                    + result1.Status.ToString()
                    );
            }

            FileSavePicker filePicker = new FileSavePicker();
            filePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            filePicker.FileTypeChoices.Add("Audio file", new List<string>() { ".mp3", ".wav", ".wma", ".m4a" });
            filePicker.SuggestedFileName = mediaInputFile.Name;

            mediaOutputFile = await filePicker.PickSaveFileAsync();

            if (mediaOutputFile != null)
            {
                CreateAudioFileOutputNodeResult result2 =
                    await audioInputOutput.SaveAudioToFile(mediaOutputFile, taskProgress, taskStatus);

                if (result2.Status != AudioFileNodeCreationStatus.Success)
                    ShowErrorMessage(result2.Status.ToString());
            }
        }

        private async void AboutDialogClick(object sender, RoutedEventArgs e)
        {
            AboutDialog aboutDialog = new AboutDialog();
            await aboutDialog.ShowAsync();
        }
    }
}
