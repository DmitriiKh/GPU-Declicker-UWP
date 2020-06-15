using CarefulAudioRepair.Data;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
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
        private AudioFileInputNode _fileInputNode;
        private AudioFileOutputNode _fileOutputNode;
        private bool _finished;
        private AudioFrameInputNode _frameInputNode;
        private AudioFrameOutputNode _frameOutputNode;
        private IProgress<double> _ioProgress;
        private IProgress<string> _ioStatus;
        private bool _audioToSaveIsStereo;
        private int _audioToReadSampleRate;

        public IAudio GetAudio()
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
        /// <param name="status"></param>
        public async Task<CreateAudioFileInputNodeResult>
            LoadAudioFromFile(StorageFile file)
        {
            _finished = false;
            _ioStatus.Report("Reading audio file");

            // Initialize FileInputNode
            var inputNodeCreationResult =
                await _audioGraph.CreateFileInputNodeAsync(file);

            if (inputNodeCreationResult.Status != AudioFileNodeCreationStatus.Success)
                return inputNodeCreationResult;

            _fileInputNode = inputNodeCreationResult.FileInputNode;


            // Read audio file encoding properties to pass them 
            //to FrameOutputNode creator

            var audioEncodingProperties =
                _fileInputNode.EncodingProperties;

            _audioToReadSampleRate = (int) audioEncodingProperties.SampleRate;

            // Initialize FrameOutputNode and connect it to fileInputNode
            _frameOutputNode = _audioGraph.CreateFrameOutputNode(
                audioEncodingProperties
            );
            _frameOutputNode.Stop();
            _fileInputNode.AddOutgoingConnection(_frameOutputNode);

            // Add a handler for achieving the end of a file
            _fileInputNode.FileCompleted += FileInput_FileCompleted;
            // Add a handler which will transfer every audio frame into audioData 
            _audioGraph.QuantumStarted += FileInput_QuantumStarted;

            // Initialize audioData
            var numOfSamples = (int) Math.Ceiling(
                (decimal) 0.0000001
                * _fileInputNode.Duration.Ticks
                * _fileInputNode.EncodingProperties.SampleRate
            );

            _leftChannel = new float[numOfSamples];

            if (audioEncodingProperties.ChannelCount == 2)
                _rightChannel = new float[numOfSamples];

            _audioCurrentPosition = 0;

            // Start process which will read audio file frame by frame
            // and will generated events QuantumStarted when a frame is in memory
            _audioGraph.Start();

            // didn't find a better way to wait for data
            while (!_finished)
                await Task.Delay(50);

            // clear status line
            _ioStatus.Report("");

            return inputNodeCreationResult;
        }

        /// <summary>
        ///     Starts when reading of samples from input audio file finished
        /// </summary>
        private void FileInput_FileCompleted(AudioFileInputNode sender, object args)
        {
            _audioGraph.Stop();
            _frameOutputNode?.Stop();
            _audioGraph.Dispose();
            _audioGraph = null;
            _finished = true;
            _ioProgress?.Report(0);
        }

        /// <summary>
        ///     Starts every time when audio frame is read from a file
        /// </summary>
        private void FileInput_QuantumStarted(AudioGraph sender, object args)
        {
            // to not report too many times
            if (sender.CompletedQuantumCount % 100 == 0)
            {
                var numOfSamples =
                    0.0000001
                    * _fileInputNode.Duration.Ticks
                    * _fileInputNode.EncodingProperties.SampleRate;
                var dProgress =
                    100 *
                    (int) sender.CompletedQuantumCount
                    * sender.SamplesPerQuantum /
                    numOfSamples;
                _ioProgress?.Report(dProgress);
            }

            if (_audioCurrentPosition == 0) _frameOutputNode.Start();

            var frame = _frameOutputNode.GetFrame();
            ProcessInputFrame(frame);

            if (_finished)
            {
                _frameOutputNode?.Stop();
                _audioGraph?.Stop();
            }
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
                // Discard first frame; it's full of zeros because of latency
                if (_audioGraph.CompletedQuantumCount == 1) return;

                var dataInFloat = (float*) dataInBytes;
                var capacityInFloat = capacityInBytes / sizeof(float);
                // Number of channels defines step between samples in buffer
                var channelCount = _fileInputNode.EncodingProperties.ChannelCount;
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

        public async Task<CreateAudioFileOutputNodeResult>
            SaveAudioToFile(StorageFile file, IAudio audio)
        {
            _finished = false;
            _ioStatus.Report("Saving audio to file");

            var mediaEncodingProfile =
                CreateMediaEncodingProfile(file);

            _audioToSaveIsStereo = audio.IsStereo;

            _leftChannel = Enumerable.Range(0, audio.LengthSamples)
                .Select(i => (float) audio.GetOutputSample(ChannelType.Left, i))
                .ToArray();

            _rightChannel = Enumerable.Range(0, audio.LengthSamples)
                .Select(i => (float) audio.GetOutputSample(ChannelType.Left, i))
                .ToArray();

            mediaEncodingProfile.Audio.SampleRate = (uint) audio.Settings.SampleRate;

            if (!_audioToSaveIsStereo && mediaEncodingProfile.Audio != null)
                    mediaEncodingProfile.Audio.ChannelCount = 1;

            // Initialize FileOutputNode
            var result =
                await _audioGraph.CreateFileOutputNodeAsync(file, mediaEncodingProfile);

            if (result.Status != AudioFileNodeCreationStatus.Success)
                return result;

            _fileOutputNode = result.FileOutputNode;

            _fileOutputNode.EncodingProperties.SampleRate = (uint)audio.Settings.SampleRate;

            if (!_audioToSaveIsStereo && _fileOutputNode != null)
                _fileOutputNode.EncodingProperties.ChannelCount = 1;

            _fileOutputNode.Stop();

            var frameInputNodeProperties = _audioGraph.EncodingProperties;

            frameInputNodeProperties.SampleRate = (uint)audio.Settings.SampleRate;

            if (!_audioToSaveIsStereo && _fileOutputNode != null)
                frameInputNodeProperties.ChannelCount = 1;

            // Initialize FrameInputNode and connect it to fileOutputNode
            _frameInputNode = _audioGraph.CreateFrameInputNode(
                // EncodingProprties are different than for input file
                frameInputNodeProperties
            );

            _frameInputNode.AddOutgoingConnection(_fileOutputNode);
            _frameInputNode.Stop();

            // Add a handler which will transfer every audioData sample to audio frame
            _frameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;

            _audioCurrentPosition = 0;

            // Start process which will write audio file frame by frame
            // and will generated events QuantumStarted 
            _audioGraph.Start();
            // don't start fileOutputNode yet because it will record zeros

            // because we initialized frameInputNode in Stop mode we need to start it
            _frameInputNode.Start();

            // didn't find a better way to wait for writing to file
            while (!_finished)
                await Task.Delay(50);

            // when audioData samples ended and audioGraph already stopped
            await _fileOutputNode.FinalizeAsync();

            // clean status and progress 
            _ioStatus.Report("");
            _ioProgress.Report(0);

            return result;
        }


        private void FrameInputNode_QuantumStarted(
            AudioFrameInputNode sender,
            FrameInputNodeQuantumStartedEventArgs args)
        {
            if (_audioCurrentPosition == 0) _fileOutputNode.Start();

            var numSamplesNeeded = args.RequiredSamples;

            if (numSamplesNeeded == 0)
                return;

            var frame = ProcessOutputFrame(numSamplesNeeded);
            _frameInputNode.AddFrame(frame);

            if (_finished)
            {
                _fileOutputNode?.Stop();
                _audioGraph?.Stop();
            }

            // to not report too many times
            if (_audioGraph == null)
                return;
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

        private unsafe AudioFrame ProcessOutputFrame(int requiredSamples)
        {
            var bufferSize = (uint) requiredSamples * sizeof(float) *
                             _frameInputNode.EncodingProperties.ChannelCount;

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

                // Number of channels defines step between samples in buffer
                var channelCount = _frameInputNode.EncodingProperties.ChannelCount;

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
                        if (_audioToSaveIsStereo)
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
                        _finished = true;
                        return frame;
                    }
                }
            }

            return frame;
        }
    }
}