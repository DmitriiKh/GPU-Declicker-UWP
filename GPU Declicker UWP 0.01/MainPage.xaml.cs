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
    
        private async void OpenAudioFile_ClickAsync(object sender, RoutedEventArgs e)
        {
            // remove all clicks from display
            ClickWindowsGrid.Children.Clear();

            CreateAudioGraphResult init_result = 
                await InitAudioInputOutputAsync();
            if (init_result == null)
                return;
            
            audioInputFile = await PickInputFileAsync();

            // if file picked
            if (audioInputFile != null)
            {
                CreateAudioFileInputNodeResult loadAudioResult =
                    await LoadAudioAsync();

                await ProcessLoadAudioResultAsync(loadAudioResult);
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

        private async Task ProcessLoadAudioResultAsync(
            CreateAudioFileInputNodeResult loadAudioResult)
        {

            if (loadAudioResult == null)
                return;

            if (loadAudioResult.Status != AudioFileNodeCreationStatus.Success)
            {
                await ShowErrorMessageAsync(loadAudioResult.Status.ToString());
                return;
            }
            else
                audioViewer.Fill(audioInputOutput.GetAudioData());
        }

        private async Task<CreateAudioFileInputNodeResult> LoadAudioAsync()
        {
            CreateAudioFileInputNodeResult loadAudioResult = null;

            try
            {
                loadAudioResult =
                    await audioInputOutput.LoadAudioFromFile(
                        audioInputFile,
                        taskProgress,
                        taskStatus);
            }
            catch (Exception ex)
            {
                await ShowExeptionAsync(
                    "An error occured while trying to read file.",
                    ex);
            }

            return loadAudioResult;
        }

        private async Task ShowExeptionAsync(string str, Exception ex)
        {
            await ShowErrorMessageAsync(str + "\n" +
               "Details:\n" +
               "Message: " + ex.Message + "\n" +
               "Source: " + ex.Source + "\n" +
               "StackTrace: " + ex.StackTrace + "\n" +
               "InnerExeption.Message: " + ex.InnerException?.Message);
        }

        private async Task<CreateAudioGraphResult> InitAudioInputOutputAsync()
        {
            CreateAudioGraphResult initResult =
                await audioInputOutput.Init(taskProgress);

            if (initResult.Status != AudioGraphCreationStatus.Success)
            {
                await ShowErrorMessageAsync(
                    "AudioGraph creation error: "
                    + initResult.Status.ToString()
                    );
                return null;
            }

            return initResult;
        }

        private async Task<StorageFile> PickInputFileAsync()
        {
            FileOpenPicker filePicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary
            };
            filePicker.FileTypeFilter.Add(".mp3");
            filePicker.FileTypeFilter.Add(".wav");
            filePicker.FileTypeFilter.Add(".wma");
            filePicker.FileTypeFilter.Add(".m4a");
            filePicker.ViewMode = PickerViewMode.Thumbnail;

            return await filePicker.PickSingleFileAsync();
        }

        private async Task ShowErrorMessageAsync(string message)
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
            // initialize offset
            double cwOffsetY = 10;
            
            if (audioData.IsStereo)
            {
                AddLabelToClickWindow(
                    "Left Channel Clicks",
                    ref cwOffsetY);

                // insert left channel clicks
                audioData.SetCurrentChannelType(ChannelType.Left);
                DisplayClicks_ForChannel(audioData, ref cwOffsetY);

                AddLabelToClickWindow(
                    "Right Channel Clicks", 
                    ref cwOffsetY);

                // insert right channel clicks
                audioData.SetCurrentChannelType(ChannelType.Right);
                DisplayClicks_ForChannel(audioData, ref cwOffsetY);
            }
            // for mono
            else
            {
                // insert clicks
                DisplayClicks_ForChannel(audioData, ref cwOffsetY);
            }
        }

        private void AddLabelToClickWindow(
            string str, 
            ref double cwOffsetY)
        {
            TextBlock textBlock = new TextBlock{ Text = str };

            // using margin to position textBlock
            Thickness margin = textBlock.Margin;
            margin.Left = 0;  // on the left side
            margin.Top = cwOffsetY;  // at the bottom
            textBlock.Margin = margin;

            ClickWindowsGrid.Children.Add(textBlock);

            cwOffsetY += textBlock.FontSize * 2;
        }

        private void DisplayClicks_ForChannel(
            AudioData audioData, 
            ref double cwOffsetY)
        {
            const double distanceBetweenClickWindows = 10;
            double cwOffsetX = distanceBetweenClickWindows;

            // for every click in channel
            for (int clicks_index = 0; 
                clicks_index < audioData.CurrentChannelGetNumberOfClicks(); 
                clicks_index++)
            {
                // make new ClickWindow for a click
                AudioClick click = audioData.GetClick(clicks_index);
                ClickWindow clickWindow = new ClickWindow(click);

                // set ClickWindow margin to position ClickWindow
                clickWindow.SetMargin(cwOffsetX, cwOffsetY);

                // insert the ClickWindow to ClickWindowsGrid
                ClickWindowsGrid.Children.Add(clickWindow);

                SetOffsetsForNextClickWindow(
                    ref cwOffsetX, 
                    ref cwOffsetY,
                    clickWindow.GetMainGridWidth(),
                    clickWindow.GetMainGridHeight(),
                    distanceBetweenClickWindows);
            }

            SetOffsetYForNextChanellClickWindows(
                ref cwOffsetY,
                distanceBetweenClickWindows);
        }

        private void SetOffsetYForNextChanellClickWindows(
            ref double cwOffsetY,
            double distanceBetweenClickWindows)
        {
            // use new ClickWindow to get height
            cwOffsetY += new ClickWindow(
                new AudioClick(
                    512, 1, 3f, 
                    new AudioDataMono(new float[1024]), 
                    ChannelType.Left))
                .GetMainGridHeight() +
                distanceBetweenClickWindows;
        }

        private void SetOffsetsForNextClickWindow(
            ref double cwOffsetX, 
            ref double cwOffsetY, 
            double width, 
            double height, 
            double distanceBetween)
        {
            cwOffsetX += width + distanceBetween;

            // if no more space in the row
            if (cwOffsetX + width > ClickWindowsGrid.ActualWidth)
            {
                // make a new row
                cwOffsetX = distanceBetween;
                cwOffsetY += height + distanceBetween;
            }
        }

        private void PageSizeChangedEventHandler(object sender, SizeChangedEventArgs e)
        {
            audioViewer.AudioViewerSizeChanged();
            DisplayClicks();
        }

        private async void SaveAudioFile_ClickAsync(object sender, RoutedEventArgs e)
        {
            CreateAudioGraphResult init_result =
                 await InitAudioInputOutputAsync();
            if (init_result == null)
                return;

            audioOutputFile = await PickOutputFileAsync();

            if (audioOutputFile != null)
            {
                CreateAudioFileOutputNodeResult saveAudioResult =
                    await SaveAudioAsync();

                await ProcessSaveAudioResultAsync(saveAudioResult);
            }
        }

        private async Task ProcessSaveAudioResultAsync(CreateAudioFileOutputNodeResult saveAudioResult)
        {
            if (saveAudioResult == null)
                return;

            if (saveAudioResult.Status !=
                AudioFileNodeCreationStatus.Success)
            {
                await ShowErrorMessageAsync(
                    saveAudioResult.Status.ToString());
            }
        }

        private async Task<CreateAudioFileOutputNodeResult> SaveAudioAsync()
        {
            CreateAudioFileOutputNodeResult saveAudioResult = null;

            try
            {
                saveAudioResult =
                    await audioInputOutput.SaveAudioToFile(
                        audioOutputFile, 
                        taskProgress, 
                        taskStatus);
            }
            catch (Exception ex)
            {
                await ShowExeptionAsync(
                    "An error occured while trying to save file.",
                    ex);
            }

            return saveAudioResult;
        }

        private async Task<StorageFile> PickOutputFileAsync()
        {
            FileSavePicker filePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                SuggestedFileName = audioInputFile.Name
            };

            filePicker.FileTypeChoices.Add("Audio file", new List<string>() { ".mp3", ".wav", ".wma", ".m4a" });

            return await filePicker.PickSaveFileAsync();
        }

        private async void AboutDialog_ClickAsync(object sender, RoutedEventArgs e)
        {
            AboutDialog aboutDialog = new AboutDialog();
            await aboutDialog.ShowAsync();
        }
    }
}
