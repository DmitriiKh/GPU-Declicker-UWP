using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace GPU_Declicker_UWP_0._01
{
    public sealed partial class MainPage : Page
    {
        private StorageFile audioInputFile;
        private StorageFile audioOutputFile;

        AudioInputOutput audioInputOutput;

        // variables for subclasses to report progress and status
        Progress<double> taskProgress;
        Progress<string> taskStatus;

        public MainPage()
        {
            this.InitializeComponent();

            audioInputOutput = new AudioInputOutput();
            
            // initialize variables for subclasses to report progress and status 
            taskProgress = new Progress<double>(
                (p) => { ProgressBar.Value = p; }
                );
            taskStatus = new Progress<string>(
                (s) => { Status.Text = s; }
                );
        }
    
        private async void OpenAudioFile_Click(object sender, RoutedEventArgs e)
        {
            ClickWindowsGrid.Children.Clear();

            CreateAudioGraphResult init_result = 
                await audioInputOutput.Init(taskProgress);
            
            if (init_result.Status != AudioGraphCreationStatus.Success)
            {
                await ShowErrorMessage(
                    "AudioGraph creation error: " 
                    + init_result.Status.ToString()
                    );
                return;
            }

            FileOpenPicker filePicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary
            };
            filePicker.FileTypeFilter.Add(".mp3");
            filePicker.FileTypeFilter.Add(".wav");
            filePicker.FileTypeFilter.Add(".wma");
            filePicker.FileTypeFilter.Add(".m4a");
            filePicker.ViewMode = PickerViewMode.Thumbnail;

            audioInputFile = await filePicker.PickSingleFileAsync();

            // if file picked
            if (audioInputFile != null)
            {
                CreateAudioFileInputNodeResult loadAudioResult = 
                    await audioInputOutput.LoadAudioFromFile(
                        audioInputFile, 
                        taskProgress, 
                        taskStatus);

                if (loadAudioResult.Status != AudioFileNodeCreationStatus.Success)
                {
                    await ShowErrorMessage(loadAudioResult.Status.ToString());
                    return;
                }
                else
                    audioViewer.Fill(audioInputOutput.GetAudioData());
            }
            // if no file picked
            else
            {
                return;
            }
            
            // enable Scan button but not Save button yet
            ScanButton.IsEnabled = true;
            SaveButton.IsEnabled = false;
        }

        private async Task ShowErrorMessage(string message)
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

        private void MagnifyLess_Click(object sender, RoutedEventArgs e) 
            => audioViewer.MagnifyLess();

        private void MagnifyMore_Click(object sender, RoutedEventArgs e) 
            => audioViewer.MagnifyMore();
        
        
        private async void Scan_ClickAsync(object sender, RoutedEventArgs e)
        {
            AudioData audioData = audioInputOutput.GetAudioData();
            
            // disable all buttons
            OpenButton.IsEnabled = false;
            ScanButton.IsEnabled = false;
            SaveButton.IsEnabled = false;
            Threshold_Slider.IsEnabled = false;
            Max_length_Slider.IsEnabled = false;

            audioData.AudioProcessingSettings.ThresholdForDetection = 
                (float)Threshold_Slider.Value;
            audioData.AudioProcessingSettings.MaxLengthCorrection =
                (int)Max_length_Slider.Value;
            await Task.Run(() => AudioProcessing.ProcessAudioAsync(
                audioData, taskProgress, taskStatus));

            // enable Scan, Save and Open buttons
            ScanButton.IsEnabled = true;
            OpenButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
            Threshold_Slider.IsEnabled = true;
            Max_length_Slider.IsEnabled = true;
            
            DisplayClicks();
        }

        private void DisplayClicks()
        {
            AudioData audioData = audioInputOutput.GetAudioData();
            if (audioData == null)
                return;

            // clear ClickWindowsGrid before adding new ClickWindows
            ClickWindowsGrid.Children.Clear();
            // initialize offsets 
            double cwOffsetX = 0;
            double cwOffsetY = 0;
            
            if (audioData.IsStereo)
            {
                // add text notation on top of the ClickWindowsGrid 
                // before inserting clicks
                TextBlock textBlock_LeftChannel = new TextBlock
                {
                    Text = "Left Channel Clicks"
                };
                ClickWindowsGrid.Children.Add(textBlock_LeftChannel);
                cwOffsetY += textBlock_LeftChannel.FontSize * 2;

                // insert left channel clicks
                audioData.SetCurrentChannelType(ChannelType.Left);
                DisplayClicks_ForChannel(audioData, ref cwOffsetX, ref cwOffsetY);

                // add text notation to the ClickWindowsGrid
                cwOffsetX = 0;
                TextBlock textBlock_RightChannel = new TextBlock
                {
                    Text = "Right Channel Clicks"
                };
                // using margin to position textBlock_RightChannel
                Thickness margin = textBlock_RightChannel.Margin;
                margin.Left = 0;
                margin.Top = cwOffsetY;
                textBlock_RightChannel.Margin = margin;
                ClickWindowsGrid.Children.Add(textBlock_RightChannel);
                cwOffsetY += textBlock_RightChannel.FontSize * 2;

                // insert right channel clicks
                audioData.SetCurrentChannelType(ChannelType.Right);
                DisplayClicks_ForChannel(audioData, ref cwOffsetX, ref cwOffsetY);
            }
            // for mono
            else
            {
                // insert clicks
                DisplayClicks_ForChannel(audioData, ref cwOffsetX, ref cwOffsetY);
            }
        }

        private void DisplayClicks_ForChannel(
            AudioData audioData, 
            ref double cwOffsetX, 
            ref double cwOffsetY)
        {
            // for every click in channel
            for (int clicks_index = 0; 
                clicks_index < audioData.CurrentChannelGetNumberOfClicks(); 
                clicks_index++)
            {
                // make new ClickWindow for a click
                AudioClick click = audioData.GetClick(clicks_index);
                ClickWindow clickWindow = new ClickWindow(click);

                // set ClickWindow margin to position ClickWindow
                Thickness margin = clickWindow.Margin;
                margin.Left = cwOffsetX;
                margin.Top = cwOffsetY;
                clickWindow.Margin = margin;

                // insert the ClickWindow to ClickWindowsGrid
                ClickWindowsGrid.Children.Add(clickWindow);
                
                // set offsets for next ClickWindow
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

        private void PageSizeChangedEventHandler(object sender, SizeChangedEventArgs e)
        {
            audioViewer.AudioViewerSizeChanged();
            DisplayClicks();
        }

        private async void SaveAudioFile_Click(object sender, RoutedEventArgs e)
        {
            CreateAudioGraphResult IO_init_result =
                await audioInputOutput.Init(taskProgress);

            if (IO_init_result.Status != AudioGraphCreationStatus.Success)
            {
                await ShowErrorMessage(
                    "AudioGraph creation error: "
                    + IO_init_result.Status.ToString());
                return;
            }

            FileSavePicker filePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                SuggestedFileName = audioInputFile.Name
            };
            filePicker.FileTypeChoices.Add("Audio file", new List<string>() { ".mp3", ".wav", ".wma", ".m4a" });

            audioOutputFile = await filePicker.PickSaveFileAsync();

            if (audioOutputFile != null)
            {
                CreateAudioFileOutputNodeResult save_audio_result =
                    await audioInputOutput.SaveAudioToFile(audioOutputFile, taskProgress, taskStatus);

                if (save_audio_result.Status !=
                    AudioFileNodeCreationStatus.Success)
                {
                    await ShowErrorMessage(
                        save_audio_result.Status.ToString());
                }
            }
        }

        private async void AboutDialog_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog aboutDialog = new AboutDialog();
            await aboutDialog.ShowAsync();
        }
    }
}
