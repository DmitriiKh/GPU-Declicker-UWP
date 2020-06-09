using CarefulAudioRepair.Data;
using GPUDeclickerUWP.Model.Data;
using GPUDeclickerUWP.Model.InputOutput;
using GPUDeclickerUWP.Model.Processing;
using GPUDeclickerUWP.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Media.Audio;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;

namespace GPUDeclickerUWP.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private StorageFile _audioInputFile;
        
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

        public int MaxLengthCorrection { get; set; } = 250;
        public double ThresholdForDetection { get; set; } = 10;

        private AudioData _audioData;
        public AudioData AudioData
        {
            get => _audioData;
            private set
            {
                _audioData = value;
                OnPropertyChanged(nameof(AudioData));
            }
        }

        public IAudio Audio 
        {
            get => _audio;
            private set
            {
                _audio = value;
                OnPropertyChanged(nameof(Audio));
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

            _audioInputOutput = new AudioInputOutput();

            // initialize variables for subclasses to report progress and status
            _progress = new Progress<double>(ProgressValueChanged);
            _status = new Progress<string>(StatusValueChanged);
           
        }

        public ICommand OpenAudioFileClicked => new DeligateCommand(OpenAudioFile);

        /// <summary>
        /// Allows user to pick audio file and transfers data to AudioData property.
        /// Also enables Scan button
        /// </summary>
        private async void OpenAudioFile()
        {
            var initResult =
                await InitAudioInputOutputAsync();
            if (initResult == null)
                // creation of AudioGraph failed
                return;

            _audioInputFile = await PickInputFileAsync();
            if (_audioInputFile == null)
                // file not picked
                return;

            var loadAudioResult = await LoadAudioAsync();

            if (loadAudioResult.Status != AudioFileNodeCreationStatus.Success)
            {
                // creation of audio file Node failed
                await ShowErrorMessageAsync(loadAudioResult.Status.ToString());
                return;
            }

            AudioData = _audioInputOutput.GetAudioData();
            Audio = _audioInputOutput.GetAudio();
            
            // remove all clicks from display
            LeftChannelClickWindowsCollection.Clear();
            RightChannelClickWindowsCollection.Clear();

            // enable Scan button but not Save button yet
            IsReadyToOpenFile = true;
            IsReadyToScan = true;
            IsReadyToSaveFile = false;
        }

        /// <summary>
        /// Initializes AudioInputOutput instance
        /// </summary>
        /// <returns></returns>
        private async Task<CreateAudioGraphResult> InitAudioInputOutputAsync()
        {
            var initResult =
                await _audioInputOutput.Init(_progress);

            if (initResult.Status == AudioGraphCreationStatus.Success)
                return initResult;

            await ShowErrorMessageAsync(
                "AudioGraph creation error: "
                + initResult.Status
            );
            return null;
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
        private async Task<CreateAudioFileInputNodeResult> LoadAudioAsync()
        {
            CreateAudioFileInputNodeResult loadAudioResult = null;

            try
            {
                loadAudioResult =
                    await _audioInputOutput.LoadAudioFromFile(
                        _audioInputFile,
                        _status);
            }
            catch (Exception ex)
            {
                await ShowExeptionAsync(
                    "An error occured while trying to read file.",
                    ex);
            }

            return loadAudioResult;
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
            IsReadyToOpenFile = false;
            IsReadyToScan = false;
            IsReadyToSaveFile = false;

            // clear ClickWindowsGrid before adding new ClickWindows
            LeftChannelClickWindowsCollection.Clear();
            RightChannelClickWindowsCollection.Clear();

            // set parameters for scanning 
            AudioData.AudioProcessingSettings.ThresholdForDetection =
                (float) ThresholdForDetection;
            AudioData.AudioProcessingSettings.MaxLengthOfCorrection =
                MaxLengthCorrection;
            Audio.Settings.ThresholdForDetection = ThresholdForDetection;
            Audio.Settings.MaxLengthOfCorrection = MaxLengthCorrection;

            // scan and repair
            await Task.Run(() => AudioProcessing.ProcessAudioAsync(
                AudioData,
                _progress, 
                _status));
            await Audio.ScanAsync(_status, _progress);


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
            if (AudioData == null || Audio is null)
                return;

            // insert left channel clicks
            AudioData.SetCurrentChannelType(Model.Data.ChannelType.Left);
            AddClicksForCurrentChannel(LeftChannelClickWindowsCollection);

            if (!AudioData.IsStereo && !Audio.IsStereo)
                return;

            // insert right channel clicks
            AudioData.SetCurrentChannelType(Model.Data.ChannelType.Right);
            AddClicksForCurrentChannel(RightChannelClickWindowsCollection);
        }

        /// <summary>
        /// Adds clicks from current channel to ObservableCollection
        /// </summary>
        /// <param name="clickWindowsCollection">ObservableCollection</param>
        private void AddClicksForCurrentChannel(
            ICollection<ClickWindow> clickWindowsCollection)
        {
            // for every click in channel
            for (var clicksIndex = 0;
                clicksIndex < AudioData.CurrentChannelGetNumberOfClicks();
                clicksIndex++)
            {
                // make new ClickWindow for a click
                var click = AudioData.GetClick(clicksIndex);
                var clickWindow = new ClickWindow(click);

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
            var initResult =
                await InitAudioInputOutputAsync();
            if (initResult == null)
                // failed to create AudioGraph
                return;

            var audioOutputFile = await PickOutputFileAsync();
            if (audioOutputFile == null)
                // output file not picked
                return;

            var saveAudioResult =
                await SaveAudioAsync(audioOutputFile);
            if (saveAudioResult.Status == AudioFileNodeCreationStatus.Success)
                return;

            // failed to create audio file node
            await ShowErrorMessageAsync(
                saveAudioResult.Status.ToString());
        }

        /// <summary>
        /// Saves AudioDada to file
        /// </summary>
        /// <param name="audioOutputFile"></param>
        /// <returns></returns>
        private async Task<CreateAudioFileOutputNodeResult> SaveAudioAsync(StorageFile audioOutputFile)
        {
            CreateAudioFileOutputNodeResult saveAudioResult = null;
            
            try
            {
                saveAudioResult =
                    await _audioInputOutput.SaveAudioToFile(
                        audioOutputFile,
                        _status,
                        _audioData);
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
