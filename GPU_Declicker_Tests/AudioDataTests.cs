using GPU_Declicker_UWP_0._01;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace GPU_Declicker_Tests
{
    [TestClass]
    public class AudioDataTests
    {
        AudioData _audioData;
        AudioClick _audioClick;

        [TestInitialize]
        public void AudioDataBeforeRunningTest()
        {
            var inputAudio = new float[4096];
            for (int index = 0; index < inputAudio.Length; index++)
                inputAudio[index] = 0.5f;
            for (int index = 1051; index < 1059; index++)
                inputAudio[index] = -0.5f;


            _audioData = new AudioDataMono(inputAudio);
            for (int index = 0; index < inputAudio.Length; index++)
                _audioData.SetOutputSample(
                    index, 
                    _audioData.GetInputSample(index));

            _audioClick = new AudioClick(
                1051, 
                10, 
                111, 
                new AudioDataMono(inputAudio), 
                ChannelType.Left);
        }

        [TestMethod]
        public void OnClickChanged_Shrinked_RestoreSampleAndRepair()
        {
            _audioData.SetOutputSample(_audioClick.Position - 1, 0);
            _audioData.SetOutputSample(_audioClick.Position + _audioClick.Length, 0);

            _audioData.OnClickChanged(
                _audioClick, 
                new ClickEventArgs { Shrinked = true });

            for (int index = 0; index < _audioData.LengthSamples(); index++)
                Assert.AreEqual(
                    0.5f, 
                    _audioData.GetOutputSample(index), 
                    "error at index " + index.ToString());
        }

        [TestMethod]
        public void OnClickChanged_NotShrinked_Repair()
        {
            _audioData.OnClickChanged(
                _audioClick,
                new ClickEventArgs { Shrinked = true });

            for (int index = 0; index < _audioData.LengthSamples(); index++)
                Assert.AreEqual(
                    0.5f,
                    _audioData.GetOutputSample(index),
                    "error at index " + index.ToString());
        }
    }
}