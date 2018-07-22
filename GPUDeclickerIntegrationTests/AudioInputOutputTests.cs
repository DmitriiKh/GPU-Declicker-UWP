using GPUDeclickerUWP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using GPUDeclickerUWP.Model.Data;
using GPUDeclickerUWP.Model.InputOutput;

namespace GPUDeclickerIntegrationTests
{
    [TestClass]
    public class AudioInputOutputTests
    {
        [TestMethod]
        public async Task AudioInputOutputSaveLoadTest()
        {
            var taskProgress = new Progress<double>();
            var taskStatus = new Progress<string>();

            var audioInputOutput =
                new AudioInputOutput();

            await audioInputOutput.Init(taskProgress);

            var audioLength = 44100;
            var inputAudio = new float[audioLength];

            for (var i = 0; i < inputAudio.Length; i++)
                inputAudio[i] = (float) Math.Sin(2 * Math.PI * i / 
                                                 ((float) audioLength / 5));

            var audioData =
                new AudioDataMono(inputAudio);

            for (var index = 0; index < audioData.LengthSamples(); index++)
                audioData.SetOutputSample(
                    index,
                    audioData.GetInputSample(index));

            audioInputOutput.SetAudioData(audioData);

            var testFolder = KnownFolders.MusicLibrary;
            var audioOutputFile = await testFolder.GetFileAsync("test.wav");

            if (audioOutputFile != null)
                await audioInputOutput.SaveAudioToFile(audioOutputFile, taskStatus);
        }
    }
}