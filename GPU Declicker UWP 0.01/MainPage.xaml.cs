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
    public sealed partial class MainPage
    {
        private StorageFile _audioInputFile;

        private readonly AudioInputOutput _audioInputOutput;
        private StorageFile _audioOutputFile;

        // variables for subclasses to report progress and status
        private readonly Progress<double> _taskProgress;
        private readonly Progress<string> _taskStatus;

        public MainPage()
        {
            InitializeComponent();

            _audioInputOutput = new AudioInputOutput();

            // initialize variables for subclasses to report progress and status 
            _taskProgress = new Progress<double>(
                p => { ProgressBar.Value = p; }
            );
            _taskStatus = new Progress<string>(
                s => { Status.Text = s; }
            );
        }

        private async void OpenAudioFile_ClickAsync(object sender, RoutedEventArgs e)
        {
            // remove all clicks from display
            ClickWindowsGrid.Children.Clear();

            var initResult =
                await InitAudioInputOutputAsync();
            if (initResult == null)
                return;

            _audioInputFile = await PickInputFileAsync();

            // if file picked
            if (_audioInputFile != null)
            {
                var loadAudioResult =
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
                await ShowErrorMessageAsync(loadAudioResult.Status.ToString());
            else
                AudioViewer.Fill(_audioInputOutput.GetAudioData());
        }

        private async Task<CreateAudioFileInputNodeResult> LoadAudioAsync()
        {
            CreateAudioFileInputNodeResult loadAudioResult = null;

            try
            {
                loadAudioResult =
                    await _audioInputOutput.LoadAudioFromFile(
                        _audioInputFile,
                        _taskStatus);
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
            var initResult =
                await _audioInputOutput.Init(_taskProgress);

            if (initResult.Status != AudioGraphCreationStatus.Success)
            {
                await ShowErrorMessageAsync(
                    "AudioGraph creation error: "
                    + initResult.Status
                );
                return null;
            }

            return initResult;
        }

        private async Task<StorageFile> PickInputFileAsync()
        {
            var filePicker = new FileOpenPicker
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
        {
            AudioViewer.GoPrevBigStep();
        }

        private void GoLeftSmallStep_Click(object sender, RoutedEventArgs e)
        {
            AudioViewer.GoPrevSmalStep();
        }

        private void GoRightBigStep_Click(object sender, RoutedEventArgs e)
        {
            AudioViewer.GoNextBigStep();
        }

        private void GoRightSmallStep_Click(object sender, RoutedEventArgs e)
        {
            AudioViewer.GoNextSmalStep();
        }

        private void MagnifyLess_Click(object sender, RoutedEventArgs e)
        {
            AudioViewer.MagnifyLess();
        }

        private void MagnifyMore_Click(object sender, RoutedEventArgs e)
        {
            AudioViewer.MagnifyMore();
        }


        private async void Scan_ClickAsync(object sender, RoutedEventArgs e)
        {
            var audioData = _audioInputOutput.GetAudioData();

            // disable all buttons
            OpenButton.IsEnabled = false;
            ScanButton.IsEnabled = false;
            SaveButton.IsEnabled = false;
            ThresholdSlider.IsEnabled = false;
            MaxLengthSlider.IsEnabled = false;

            audioData.AudioProcessingSettings.ThresholdForDetection =
                (float) ThresholdSlider.Value;
            audioData.AudioProcessingSettings.MaxLengthCorrection =
                (int) MaxLengthSlider.Value;
            await Task.Run(() => AudioProcessing.ProcessAudioAsync(
                audioData, _taskProgress, _taskStatus));

            // enable Scan, Save and Open buttons
            ScanButton.IsEnabled = true;
            OpenButton.IsEnabled = true;
            SaveButton.IsEnabled = true;
            ThresholdSlider.IsEnabled = true;
            MaxLengthSlider.IsEnabled = true;

            DisplayClicks();
        }

        private void DisplayClicks()
        {
            var audioData = _audioInputOutput.GetAudioData();
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
            var textBlock = new TextBlock {Text = str};

            // using margin to position textBlock
            var margin = textBlock.Margin;
            margin.Left = 0; // on the left side
            margin.Top = cwOffsetY; // at the bottom
            textBlock.Margin = margin;

            ClickWindowsGrid.Children.Add(textBlock);

            cwOffsetY += textBlock.FontSize * 2;
        }

        private void DisplayClicks_ForChannel(
            AudioData audioData,
            ref double cwOffsetY)
        {
            const double distanceBetweenClickWindows = 10;
            var cwOffsetX = distanceBetweenClickWindows;

            // for every click in channel
            for (var clicksIndex = 0;
                clicksIndex < audioData.CurrentChannelGetNumberOfClicks();
                clicksIndex++)
            {
                // make new ClickWindow for a click
                var click = audioData.GetClick(clicksIndex);
                var clickWindow = new ClickWindow(click);

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
            DisplayClicks();
        }

        private async void SaveAudioFile_ClickAsync(object sender, RoutedEventArgs e)
        {
            var initResult =
                await InitAudioInputOutputAsync();
            if (initResult == null)
                return;

            _audioOutputFile = await PickOutputFileAsync();

            if (_audioOutputFile != null)
            {
                var saveAudioResult =
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
                await ShowErrorMessageAsync(
                    saveAudioResult.Status.ToString());
        }

        private async Task<CreateAudioFileOutputNodeResult> SaveAudioAsync()
        {
            CreateAudioFileOutputNodeResult saveAudioResult = null;

            try
            {
                saveAudioResult =
                    await _audioInputOutput.SaveAudioToFile(
                        _audioOutputFile,
                        _taskStatus);
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
            var filePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                SuggestedFileName = _audioInputFile.Name
            };

            filePicker.FileTypeChoices.Add("Audio file", new List<string> {".mp3", ".wav", ".wma", ".m4a"});

            return await filePicker.PickSaveFileAsync();
        }

        private async void AboutDialog_ClickAsync(object sender, RoutedEventArgs e)
        {
            var aboutDialog = new AboutDialog();
            await aboutDialog.ShowAsync();
        }
    }
}