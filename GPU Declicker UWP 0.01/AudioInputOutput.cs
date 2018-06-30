using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.Storage;

namespace GPU_Declicker_UWP_0._01
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
        private AudioData audioData;
        private int audioDataCurrentPosition;
        private AudioGraph audioGraph;
        private AudioFileInputNode fileInputNode;
        private AudioFileOutputNode fileOutputNode;
        private bool Finished;
        private AudioFrameInputNode frameInputNode;
        private AudioFrameOutputNode frameOutputNode;
        private IProgress<double> io_progress;

        public AudioData GetAudioData()
        {
            return audioData;
        }

        public void SetAudioData(AudioData value)
        {
            audioData = value;
        }

        /// <summary>
        ///     Creates an instance of AudioGraph and sets io_progress
        /// </summary>
        public async Task<CreateAudioGraphResult> Init(
            Progress<double> progress)
        {
            // set io_progress var to show progress of input-output
            io_progress = progress;

            // initialize settings for AudioGraph
            var settings =
                new AudioGraphSettings(
                    AudioRenderCategory.Media
                );

            // if audioGraph was previously created
            if (audioGraph != null)
            {
                audioGraph.Dispose();
                audioGraph = null;
            }

            var result =
                await AudioGraph.CreateAsync(settings);

            if (result.Status == AudioGraphCreationStatus.Success)
                audioGraph = result.Graph;

            return result;
        }

        /// <summary>
        ///     Creates instances of FileInputNode, FrameOutputNode, AudioData
        ///     starts AudioGraph, waits till loading of samples is finished
        /// </summary>
        /// <param name="file"> Input audio file</param>
        /// <param name="progress"></param>
        /// <param name="status"></param>
        public async Task<CreateAudioFileInputNodeResult>
            LoadAudioFromFile(
                StorageFile file,
                IProgress<double> progress,
                IProgress<string> status)
        {
            Finished = false;
            status.Report("Reading audio file");

            // Initialize FileInputNode
            var inputNodeCreation_result =
                await audioGraph.CreateFileInputNodeAsync(file);

            if (inputNodeCreation_result.Status != AudioFileNodeCreationStatus.Success)
                return inputNodeCreation_result;

            fileInputNode = inputNodeCreation_result.FileInputNode;


            // Read audio file encoding properties to pass them 
            //to FrameOutputNode creator

            var audioEncodingProperties =
                fileInputNode.EncodingProperties;

            // Initialize FrameOutputNode and connect it to fileInputNode
            frameOutputNode = audioGraph.CreateFrameOutputNode(
                audioEncodingProperties
            );
            frameOutputNode.Stop();
            fileInputNode.AddOutgoingConnection(frameOutputNode);

            // Add a handler for achiving the end of a file
            fileInputNode.FileCompleted += FileInput_FileCompleted;
            // Add a handler which will transfer every audio frame into audioData 
            audioGraph.QuantumStarted += FileInput_QuantumStarted;

            // Initialize audioData
            var numOfSamples = (int) Math.Ceiling(
                (decimal) 0.0000001
                * fileInputNode.Duration.Ticks
                * fileInputNode.EncodingProperties.SampleRate
            );
            if (audioEncodingProperties.ChannelCount == 1)
                SetAudioData(new AudioDataMono(new float[numOfSamples]));
            else
                SetAudioData(new AudioDataStereo(new float[numOfSamples],
                    new float[numOfSamples]));

            audioDataCurrentPosition = 0;

            // Start process which will read audio file frame by frame
            // and will generated events QuantumStarted when a frame is in memory
            audioGraph.Start();

            // didn't find a better way to wait for data
            while (!Finished)
                await Task.Delay(50);

            // crear status line
            status.Report("");

            return inputNodeCreation_result;
        }

        /// <summary>
        ///     Starts when reading of samples from input audio file finished
        /// </summary>
        private void FileInput_FileCompleted(AudioFileInputNode sender, object args)
        {
            audioGraph.Stop();
            frameOutputNode?.Stop();
            audioGraph.Dispose();
            audioGraph = null;
            Finished = true;
            io_progress?.Report(0);
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
                    * fileInputNode.Duration.Ticks
                    * fileInputNode.EncodingProperties.SampleRate;
                var dProgress =
                    100 *
                    (int) sender.CompletedQuantumCount
                    * sender.SamplesPerQuantum /
                    numOfSamples;
                io_progress?.Report(dProgress);
            }

            if (audioDataCurrentPosition == 0) frameOutputNode.Start();

            var frame = frameOutputNode.GetFrame();
            ProcessInputFrame(frame);

            if (Finished)
            {
                frameOutputNode?.Stop();
                audioGraph?.Stop();
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
                ((IMemoryBufferByteAccess) reference).GetBuffer(
                    out var dataInBytes,
                    out var capacityInBytes
                );
                // Discard first frame; it's full of zeros because of latency
                if (audioGraph.CompletedQuantumCount == 1) return;

                var dataInFloat = (float*) dataInBytes;
                var capacityInFloat = capacityInBytes / sizeof(float);
                // Number of channels defines step between samples in buffer
                var channelCount = fileInputNode.EncodingProperties.ChannelCount;
                // Transfer audio samples from buffer into audioData
                for (uint index = 0; index < capacityInFloat; index += channelCount)
                    if (audioDataCurrentPosition < GetAudioData().LengthSamples())
                    {
                        GetAudioData().SetCurrentChannelType(ChannelType.Left);
                        GetAudioData().SetInputSample(
                            audioDataCurrentPosition,
                            dataInFloat[index]
                        );
                        // if it's stereo
                        if (channelCount == 2)
                        {
                            GetAudioData().SetCurrentChannelType(ChannelType.Right);
                            GetAudioData().SetInputSample(
                                audioDataCurrentPosition,
                                dataInFloat[index + 1]
                            );
                        }

                        audioDataCurrentPosition++;
                    }
            }
        }

        public async Task<CreateAudioFileOutputNodeResult>
            SaveAudioToFile(
                StorageFile file,
                IProgress<double> progress,
                IProgress<string> status)
        {
            Finished = false;
            status.Report("Saving audio to file");

            var mediaEncodingProfile =
                CreateMediaEncodingProfile(file);

            if (!audioData.IsStereo)
                mediaEncodingProfile.Audio.ChannelCount = 1;

            // Initialize FileOutputNode
            var result =
                await audioGraph.CreateFileOutputNodeAsync(file, mediaEncodingProfile);

            if (result.Status != AudioFileNodeCreationStatus.Success)
                return result;

            fileOutputNode = result.FileOutputNode;
            fileOutputNode.Stop();

            // Initialize FrameInputNode and connect it to fileOutputNode
            frameInputNode = audioGraph.CreateFrameInputNode(
                // EncodingProprties are different than for input file
                fileOutputNode.EncodingProperties
                //audioEncodingProperties
            );

            frameInputNode.AddOutgoingConnection(fileOutputNode);
            frameInputNode.Stop();

            // Add a handler which will transfer every audioData sample to audio frame
            frameInputNode.QuantumStarted += FrameInputNode_QuantumStarted;

            audioDataCurrentPosition = 0;

            // Start process which will write audio file frame by frame
            // and will generated events QuantumStarted 
            audioGraph.Start();
            // don't start fileOutputNode yet because it will record zeros

            // because we initialised frameInputNode in Stop mode we need to start it
            frameInputNode.Start();

            // didn't find a better way to wait for writing to file
            while (!Finished)
                await Task.Delay(50);

            // when audioData samples ended and audioGraph already stoped
            await fileOutputNode.FinalizeAsync();

            // clean status and progress 
            status.Report("");
            io_progress.Report(0);

            return result;
        }


        private void FrameInputNode_QuantumStarted(
            AudioFrameInputNode sender,
            FrameInputNodeQuantumStartedEventArgs args)
        {
            if (audioDataCurrentPosition == 0) fileOutputNode.Start();

            // doesn't matter how many samples requested
            var frame = ProcessOutputFrame(audioGraph.SamplesPerQuantum);
            frameInputNode.AddFrame(frame);

            if (Finished)
            {
                fileOutputNode?.Stop();
                audioGraph?.Stop();
            }

            // to not report too many times
            if (audioGraph == null)
                return;
            if (audioGraph.CompletedQuantumCount % 100 == 0)
            {
                var dProgress =
                    (double) 100 *
                    audioDataCurrentPosition /
                    audioData.LengthSamples();
                io_progress?.Report(dProgress);
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
                    throw new ArgumentException("Can't create MediaEncodingProfile for this file extention");
            }
        }

        private unsafe AudioFrame ProcessOutputFrame(int requiredSamples)
        {
            var bufferSize = (uint) requiredSamples * sizeof(float) *
                             fileOutputNode.EncodingProperties.ChannelCount;

            var frame = new AudioFrame(bufferSize);

            using (var buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (var reference = buffer.CreateReference())
            {
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess) reference).GetBuffer(
                    out var dataInBytes,
                    out var capacityInBytes);

                // Cast to float since the data we are generating is float
                dataInFloat = (float*) dataInBytes;
                var capacityInFloat = capacityInBytes / sizeof(float);

                // Number of channels defines step between samples in buffer
                var channelCount = fileOutputNode.EncodingProperties.ChannelCount;

                for (uint index = 0; index < capacityInFloat; index += channelCount)
                {
                    if (audioDataCurrentPosition < audioData.LengthSamples())
                    {
                        GetAudioData().SetCurrentChannelType(ChannelType.Left);
                        dataInFloat[index] = audioData.GetOutputSample(
                            audioDataCurrentPosition);
                    }

                    // if it's stereo
                    if (channelCount == 2)
                    {
                        // if processed audio is sretero
                        if (audioData.IsStereo)
                        {
                            GetAudioData().SetCurrentChannelType(ChannelType.Right);
                            dataInFloat[index + 1] = audioData.GetOutputSample(
                                audioDataCurrentPosition);
                        }
                        else
                        {
                            // mute channel
                            dataInFloat[index + 1] = 0;
                        }
                    }

                    audioDataCurrentPosition++;
                    if (audioDataCurrentPosition >= audioData.LengthSamples())
                    {
                        // last frame may be not full
                        Finished = true;
                        return frame;
                    }
                }
            }

            return frame;
        }
    }
}