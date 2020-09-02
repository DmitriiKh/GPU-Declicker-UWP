using AudioGraphExtensions;
using CarefulAudioRepair.Data;
using GPUDeclickerUWP.Model.InputOutput;
using GPUDeclickerUWP.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;

namespace GPUDeclickerUWP.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private StorageFile _audioInputFile;

        public AudioViewerViewModel AudioViewerViewModelInstance { get; } =
            new AudioViewerViewModel();

        // variables for subclasses to report progress and status
        private readonly Progress<double> _progress;
        private void ProgressValueChanged(double d)
        {
            TaskProgress = d;
        }

        private double _taskProgress;
        public double TaskProgress
        {
            get => _taskProgress;
            private set
            {
                _taskProgress = value;
                OnPropertyChanged(nameof(TaskProgress));
            }
        }

        private readonly Progress<string> _status;

        private void StatusValueChanged(string d)
        {
            TaskStatus = d;
            IsInProcess = !string.IsNullOrEmpty(d);
        }

        private string _taskStatus;
        public string TaskStatus
        {
            get => _taskStatus;
            private set
            {
                _taskStatus = value;
                OnPropertyChanged(nameof(TaskStatus));
            }
        }

        private readonly AudioInputOutput _audioInputOutput;

        public int MaxLengthCorrection { get; set; } = 150;
        public double ThresholdForDetection { get; set; } = 10;

        public IAudio Audio 
        {
            get => _audio;
            private set
            {
                _audio = value;
                OnPropertyChanged(nameof(Audio));

                AudioViewerViewModelInstance.UpdateAudio(_audio);
            }
        }

        private ObservableCollection<ClickWindow> _leftChannelClickWindowsCollection;
        public ObservableCollection<ClickWindow> LeftChannelClickWindowsCollection
        {
            get => _leftChannelClickWindowsCollection;
            private set
            {
                _leftChannelClickWindowsCollection = value;
                OnPropertyChanged(nameof(LeftChannelClickWindowsCollection));
            }
        }

        private ObservableCollection<ClickWindow> _rightChannelClickWindowsCollection;
        public ObservableCollection<ClickWindow> RightChannelClickWindowsCollection
        {
            get => _rightChannelClickWindowsCollection;
            private set
            {
                _rightChannelClickWindowsCollection = value;
                OnPropertyChanged(nameof(RightChannelClickWindowsCollection));
            }
        }

        private bool _isInProcess;
        public bool IsInProcess
        {
            get => _isInProcess;
            private set
            {
                _isInProcess = value;
                OnPropertyChanged(nameof(IsInProcess));
                OnPropertyChanged(nameof(IsNotInProcess));
            }
        }

        public bool IsNotInProcess
        {
            get => !_isInProcess;
        }

        private bool _isReadyToOpenFile;
        public bool IsReadyToOpenFile
        {
            get => _isReadyToOpenFile;
            private set
            {
                _isReadyToOpenFile = value;
                OnPropertyChanged(nameof(IsReadyToOpenFile));
            }
        }

        private bool _isReadyToScan;
        public bool IsReadyToScan
        {
            get => _isReadyToScan;
            private set
            {
                _isReadyToScan = value;
                OnPropertyChanged(nameof(IsReadyToScan));
            }
        }

        private bool _isReadyToSaveFile;
        private IAudio _audio;

        public bool IsReadyToSaveFile
        {
            get => _isReadyToSaveFile;
            private set
            {
                _isReadyToSaveFile = value;
                OnPropertyChanged(nameof(IsReadyToSaveFile));
            }
        }

        public MainViewModel()
        {
            IsReadyToOpenFile = true;
            IsReadyToScan = false;
            IsReadyToSaveFile = false;

            LeftChannelClickWindowsCollection = 
                new ObservableCollection<ClickWindow>();
            RightChannelClickWindowsCollection =
                new ObservableCollection<ClickWindow>();

            // initialize variables for subclasses to report progress and status
            _progress = new Progress<double>(ProgressValueChanged);
            _status = new Progress<string>(StatusValueChanged);

            _audioInputOutput = new AudioInputOutput(_progress, _status);
        }

        public ICommand OpenAudioFileClicked => new DeligateCommand(OpenAudioFile);

        /// <summary>
        /// Allows user to pick audio file and transfers data to AudioData property.
        /// Also enables Scan button
        /// </summary>
        private async void OpenAudioFile()
        {
            _audioInputFile = await PickInputFileAsync();
            if (_audioInputFile == null)
                // file not picked
                return;

            var success = await LoadAudioAsync();

            if (!success)
            {
                await ShowErrorMessageAsync("Can not load audio");
                return;
            }
            
            // remove all clicks from display
            LeftChannelClickWindowsCollection.Clear();
            RightChannelClickWindowsCollection.Clear();

            // enable Scan button but not Save button yet
            IsReadyToOpenFile = true;
            IsReadyToScan = true;
            IsReadyToSaveFile = false;
        }

        /// <summary>
        /// Picks audio file
        /// </summary>
        /// <returns></returns>
        private static async Task<StorageFile> PickInputFileAsync()
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

        /// <summary>
        /// Shows message in a Dialog Window
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static async Task ShowErrorMessageAsync(string message)
        {
            var dialog = new MessageDialog(message);
            await dialog.ShowAsync();
        }

        /// <summary>
        /// Loads audio from file
        /// </summary>
        /// <returns></returns>
        private async Task<bool> LoadAudioAsync()
        {
            var success = false;

            try
            {
                (success, Audio) =
                    await _audioInputOutput.LoadAudioFromFile(_audioInputFile);
            }
            catch (Exception ex)
            {
                await ShowExeptionAsync(
                    "An error occurred while trying to read file.",
                    ex);
            }

            return success;
        }

        /// <summary>
        /// Shows message and exception information
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        private static async Task ShowExeptionAsync(string message, Exception exception)
        {
            await ShowErrorMessageAsync(message + "\n" +
                                        "Details:\n" +
                                        "Message: " + exception.Message + "\n" +
                                        "Source: " + exception.Source + "\n" +
                                        "StackTrace: " + exception.StackTrace + "\n" +
                                        "InnerExeption.Message: " + 
                                        exception.InnerException?.Message);
        }

        public ICommand ScanClicked => new DeligateCommand(ScanAsync);

        /// <summary>
        /// Scans AudioData for damaged samples (clicks, pops etc.)
        /// </summary>
        private async void ScanAsync()
        {
            // disable all buttons
            IsReadyToOpenFile = true;
            IsReadyToScan = true;
            IsReadyToSaveFile = false;

            // clear ClickWindowsGrid before adding new ClickWindows
            LeftChannelClickWindowsCollection.Clear();
            RightChannelClickWindowsCollection.Clear();

            // set parameters for scanning
            Audio.Settings.ThresholdForDetection = ThresholdForDetection;
            Audio.Settings.MaxLengthOfCorrection = MaxLengthCorrection;

            // scan and repair
            try
            {
                await Audio.ScanAsync(_status, _progress);
            } catch (Exception e)
            {
                await ShowExeptionAsync(
                    "An error occurred while trying to process file.",
                    e);
            }

            // enable Scan, Save and Open buttons
            IsReadyToOpenFile = true;
            IsReadyToScan = true;
            IsReadyToSaveFile = true;

            AddClicksToCollection();
        }

        /// <summary>
        /// Adds clicks found in ScanAsync() to ObservableCollections
        /// </summary>
        private void AddClicksToCollection()
        {
            if (Audio is null)
                return;

            // insert left channel clicks
            AddClicksForCurrentChannel(LeftChannelClickWindowsCollection, ChannelType.Left);

            if (!Audio.IsStereo)
                return;

            // insert right channel clicks
            AddClicksForCurrentChannel(RightChannelClickWindowsCollection, ChannelType.Right);
        }

        /// <summary>
        /// Adds clicks from current channel to ObservableCollection
        /// </summary>
        /// <param name="clickWindowsCollection">ObservableCollection</param>
        private void AddClicksForCurrentChannel(
            ICollection<ClickWindow> clickWindowsCollection,
            ChannelType channelType)
        {
            // for every click in channel
            foreach (var click in Audio.GetPatches(channelType))
            {
                // make new ClickWindow for a click
                var clickWindow = new ClickWindow(click, Audio, channelType);

                // set ClickWindow margin to space ClickWindow
                clickWindow.SetMargin(10, 10);

                // insert the ClickWindow to ClickWindowsGrid
                clickWindowsCollection.Add(clickWindow);
            }
        }
        
        public ICommand SaveAudioFileClicked => new DeligateCommand(SaveAudioFileAsync);

        /// <summary>
        /// Saves AudioData to audio file picked by user
        /// </summary>
        private async void SaveAudioFileAsync()
        {
            var audioOutputFile = await PickOutputFileAsync();
            if (audioOutputFile == null)
                // output file not picked
                return;

            var success = await SaveAudioAsync(audioOutputFile);

            if (!success)
            {
                await ShowErrorMessageAsync("Can not save audio");
                return;
            }

            return;
        }

        /// <summary>
        /// Saves AudioDada to file
        /// </summary>
        /// <param name="audioOutputFile"></param>
        /// <returns></returns>
        private async Task<bool> SaveAudioAsync(StorageFile audioOutputFile)
        {
            var saveAudioResult = false;
            
            try
            {
                var left = Array.ConvertAll(_audio.GetOutputArray(ChannelType.Left), s => (float)s);
                var right = _audio.IsStereo ? Array.ConvertAll(_audio.GetOutputArray(ChannelType.Left), s => (float)s) : null;

                saveAudioResult =
                    await (await AudioGraphOutputStream.ToFile(audioOutputFile, _progress, _status, (uint)_audio.Settings.SampleRate, _audio.IsStereo ? 2u : 1u))
                    .Transfer(left, right);
                    //await _audioInputOutput.SaveAudioToFile(audioOutputFile, _audio);
            }
            catch (Exception exception)
            {
                await ShowExeptionAsync(
                    "An error occurred while trying to save file.",
                    exception);
            }

            return saveAudioResult;
        }

        /// <summary>
        /// Allows user to pick output audio file
        /// </summary>
        /// <returns></returns>
        private async Task<StorageFile> PickOutputFileAsync()
        {
            var filePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                SuggestedFileName = _audioInputFile.Name
            };

            filePicker.FileTypeChoices.Add("Audio file", new List<string> { ".mp3", ".wav", ".wma", ".m4a" });

            return await filePicker.PickSaveFileAsync();
        }

        public ICommand AboutClicked => new DeligateCommand(AboutDialogAsync);

        /// <summary>
        /// Shows About Dialog
        /// </summary>
        private static async void AboutDialogAsync()
        {
            var aboutDialog = new AboutDialog();
            await aboutDialog.ShowAsync();
        }
    }
}
