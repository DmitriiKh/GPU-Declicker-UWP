using CarefulAudioRepair.Data;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Media.Transcoding;
using Windows.Storage;

namespace GPUDeclickerUWP.Model.InputOutput
{
    /// <summary>
    ///     Declare COM interface
    /// </summary>
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    /// <summary>
    ///     This class uses AudioGraph API to transfer audio from file to
    ///     float array or back
    /// </summary>
    public class AudioInputOutput
    {
        private float[] _leftChannel = null;
        private float[] _rightChannel = null;

        private int _audioCurrentPosition;
        private AudioGraph _audioGraph;
        private AudioFileOutputNode _fileOutputNode;
        private AudioFrameOutputNode _frameOutputNode;
        private IProgress<double> _ioProgress;
        private IProgress<string> _ioStatus;
        private int _audioToSaveChannelCount;
        private int _audioToReadSampleRate;
        private int _audioToReadChannelCount;
        private TaskCompletionSource<(bool, IAudio)> _readFileSuccess;
        private TaskCompletionSource<bool> _writeFileSuccess;

        private IAudio GetAudio()
        {
            var settings = new AudioProcessingSettings()
            {
                SampleRate = _audioToReadSampleRate
            };

            if (_rightChannel is null)
                return new Mono(
                    _leftChannel.Select(s => (double) s).ToArray(), 
                    settings);
            else
                return new Stereo(
                    _leftChannel.Select(s => (double)s).ToArray(),
                    _rightChannel.Select(s => (double)s).ToArray(),
                    settings);
        }

        /// <summary>
        ///     Creates an instance of AudioGraph and sets io_progress
        /// </summary>
        public async Task<CreateAudioGraphResult> Init(
            Progress<double> progress,
            IProgress<string> status)
        {
            // set io_progress var to show progress of input-output
            _ioProgress = progress;
            _ioStatus = status;

            // initialize settings for AudioGraph
            var settings =
                new AudioGraphSettings(
                    AudioRenderCategory.Media
                );

            // if audioGraph was previously created
            if (_audioGraph != null)
            {
                _audioGraph.Dispose();
                _audioGraph = null;
            }

            var result =
                await AudioGraph.CreateAsync(settings);

            if (result.Status == AudioGraphCreationStatus.Success)
                _audioGraph = result.Graph;

            return result;
        }

        /// <summary>
        ///     Creates instances of FileInputNode, FrameOutputNode, AudioData
        ///     starts AudioGraph, waits till loading of samples is finished
        /// </summary>
        /// <param name="file"> Input audio file</param>
        public async Task<(bool, IAudio)> LoadAudioFromFile(StorageFile file)
        {
            _ioStatus.Report("Reading audio file");

            // Initialize FileInputNode
            var inputNodeCreationResult =
                await _audioGraph.CreateFileInputNodeAsync(file);

            if (inputNodeCreationResult.Status != AudioFileNodeCreationStatus.Success)
                return (false, null);

            var fileInputNode = inputNodeCreationResult.FileInputNode;


            // Read audio file encoding properties to pass them 
            //to FrameOutputNode creator

            var audioEncodingProperties =
                fileInputNode.EncodingProperties;

            _audioToReadSampleRate = (int)audioEncodingProperties.SampleRate;
            _audioToReadChannelCount = (int)audioEncodingProperties.ChannelCount;

            // Initialize FrameOutputNode and connect it to fileInputNode
            _frameOutputNode = _audioGraph.CreateFrameOutputNode(
                audioEncodingProperties
            );

            fileInputNode.AddOutgoingConnection(_frameOutputNode);

            // Add a handler for achieving the end of a file
            fileInputNode.FileCompleted += FileInput_FileCompleted;
            // Add a handler which will transfer every audio frame into audioData 
            _audioGraph.QuantumStarted += FileInput_QuantumStarted;

            // Initialize audio arrays
            var numOfSamples = (int) Math.Ceiling(
                fileInputNode.Duration.TotalSeconds
                * fileInputNode.EncodingProperties.SampleRate
            );

            _leftChannel = new float[numOfSamples];

            if (audioEncodingProperties.ChannelCount == 2)
                _rightChannel = new float[numOfSamples];

            _audioCurrentPosition = 0;

            _readFileSuccess = new TaskCompletionSource<(bool, IAudio)>();

            // Start process which will read audio file frame by frame
            // and will generated events QuantumStarted when a frame is in memory
            _audioGraph.Start();

            return await _readFileSuccess.Task;
        }

        /// <summary>
        ///     Starts when reading of samples from input audio file finished
        /// </summary>
        private void FileInput_FileCompleted(AudioFileInputNode sender, object args)
        {
            _audioGraph.Stop();
            _audioGraph.Dispose();
            _audioGraph = null;

            _ioProgress?.Report(0);
            _ioStatus.Report("");

            _readFileSuccess.TrySetResult((true, this.GetAudio()));
        }

        /// <summary>
        ///     Starts every time when audio frame is read from a file
        /// </summary>
        private void FileInput_QuantumStarted(AudioGraph sender, object args)
        {
            // to not report too many times
            if (sender.CompletedQuantumCount % 100 == 0)
            {
                var dProgress =
                    100 *
                    (int) sender.CompletedQuantumCount
                    * sender.SamplesPerQuantum /
                    _leftChannel.Length;
                _ioProgress?.Report(dProgress);
            }

            var frame = _frameOutputNode.GetFrame();
            ProcessInputFrame(frame);
        }

        /// <summary>
        ///     Transfers samples from a frame to AudioData
        /// </summary>
        private unsafe void ProcessInputFrame(AudioFrame frame)
        {
            using (var buffer =
                frame.LockBuffer(AudioBufferAccessMode.Read))
            using (var reference =
                buffer.CreateReference())
            {
                // Get data from current buffer
                (reference as IMemoryBufferByteAccess).GetBuffer(
                    out var dataInBytes,
                    out var capacityInBytes
                );

                var dataInFloat = (float*) dataInBytes;
                var capacityInFloat = capacityInBytes / sizeof(float);
                // Number of channels defines step between samples in buffer
                var channelCount = (uint)_audioToReadChannelCount;
                // Transfer audio samples from buffer into audioData
                for (uint index = 0; index < capacityInFloat; index += channelCount)
                    if (_audioCurrentPosition < _leftChannel.Length)
                    {
                            _leftChannel[_audioCurrentPosition] = dataInFloat[index];

                        // if it's stereo
                        if (channelCount == 2)
                        {
                            _rightChannel[_audioCurrentPosition] = dataInFloat[index + 1];
                        }

                        _audioCurrentPosition++;
                    }
            }
        }

        public async Task<bool> SaveAudioToFile(StorageFile file, IAudio audio)
        {
            _ioStatus.Report("Saving audio to file");

            var mediaEncodingProfile =
                CreateMediaEncodingProfile(file);

            _audioToSaveChannelCount = audio.IsStereo ? 2 : 1;

            _leftChannel = Enumerable.Range(0, audio.LengthSamples)
                .Select(i => (float) audio.GetOutputSample(ChannelType.Left, i))
                .ToArray();

            _rightChannel = Enumerable.Range(0, audio.LengthSamples)
                .Select(i => (float) audio.GetOutputSample(ChannelType.Left, i))
                .ToArray();

            mediaEncodingProfile.Audio.SampleRate = (uint) audio.Settings.SampleRate;

            mediaEncodingProfile.Audio.ChannelCount = (uint)_audioToSaveChannelCount;

            // Initialize FileOutputNode
            var result =
                await _audioGraph.CreateFileOutputNodeAsync(file, mediaEncodingProfile);

            if (result.Status != AudioFileNodeCreationStatus.Success)
                return false;

            _fileOutputNode = result.FileOutputNode;

            var frameInputNodeProperties = _audioGraph.EncodingProperties;

            frameInputNodeProperties.SampleRate = (uint)audio.Settings.SampleRate;

            frameInputNodeProperties.ChannelCount = (uint)_audioToSaveChannelCount;

            // Initialize FrameInputNode and connect it to fileOutputNode
            var frameInputNode = _audioGraph.CreateFrameInputNode(
                // EncodingProprties are different than for input file
                frameInputNodeProperties
            );

            frameInputNode.AddOutgoingConnection(_fileOutputNode);

            // Add a handler which will transfer every audioData sample to audio frame
            frameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;

            _audioCurrentPosition = 0;

            _writeFileSuccess = new TaskCompletionSource<bool>();

            // Start process which will write audio file frame by frame
            // and will generated events QuantumStarted 
            _audioGraph.Start();            

            return await _writeFileSuccess.Task;
        }


        private void FrameInputNode_QuantumStarted(
            AudioFrameInputNode sender,
            FrameInputNodeQuantumStartedEventArgs args)
        {
            var numSamplesNeeded = args.RequiredSamples;

            if (numSamplesNeeded == 0 || _audioGraph is null)
                return;

            (var frame, var finished) = ProcessOutputFrame(numSamplesNeeded);
            sender.AddFrame(frame);

            if (finished)
            {
                _audioGraph?.Stop();
                _fileOutputNode.Stop();
                var result = _fileOutputNode.FinalizeAsync().GetResults();

                _writeFileSuccess.TrySetResult(result == TranscodeFailureReason.None);

                // clean status and progress 
                _ioStatus.Report("");
                _ioProgress.Report(0);

                return;
            }

            // to not report too many times
            if (_audioGraph.CompletedQuantumCount % 100 == 0)
            {
                var dProgress =
                    (double) 100 *
                    _audioCurrentPosition /
                    _leftChannel.Length;
                _ioProgress?.Report(dProgress);
            }
        }

        private MediaEncodingProfile CreateMediaEncodingProfile(StorageFile file)
        {
            switch (file.FileType.ToLowerInvariant())
            {
                case ".wma":
                    return MediaEncodingProfile.CreateWma(AudioEncodingQuality.High);
                case ".mp3":
                    return MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
                case ".wav":
                    return MediaEncodingProfile.CreateWav(AudioEncodingQuality.High);
                default:
                    throw new ArgumentException(
                        "Can't create MediaEncodingProfile for this file extension");
            }
        }

        private unsafe (AudioFrame frame, bool finished)
            ProcessOutputFrame(int requiredSamples)
        {
            var channelCount = (uint)_audioToSaveChannelCount;

            var bufferSize = (uint)(requiredSamples * sizeof(float) * channelCount);

            var frame = new AudioFrame(bufferSize);

            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (var reference = buffer.CreateReference())
            {
                // Get the buffer from the AudioFrame
                (reference as IMemoryBufferByteAccess).GetBuffer(
                    out var dataInBytes,
                    out var capacityInBytes);

                // Cast to float since the data we are generating is float
                var dataInFloat = (float*) dataInBytes;
                var capacityInFloat = capacityInBytes / sizeof(float);

                for (uint index = 0; index < capacityInFloat; index += channelCount)
                {
                    if (_audioCurrentPosition < _leftChannel.Length)
                    {
                        dataInFloat[index] = _leftChannel[_audioCurrentPosition];
                    }

                    // if it's stereo
                    if (channelCount == 2)
                    {
                        // if processed audio is stereo
                        if (_audioToSaveChannelCount == 2)
                        {
                            dataInFloat[index + 1] = _rightChannel[_audioCurrentPosition];
                        }
                        else
                        {
                            // mute channel
                            dataInFloat[index + 1] = 0;
                        }
                    }

                    _audioCurrentPosition++;
                    if (_audioCurrentPosition >= _leftChannel.Length)
                    {
                        // last frame may be not full
                        return (frame, true);
                    }
                }
            }

            return (frame, false);
        }
    }
}